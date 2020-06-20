using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;
using Eveldee._3DSCPlusViGEm;
using System.Windows.Input;

namespace MarcusD._3DSCPlusDummy
{
    public class Dummy
    {
        public delegate void ConnectedHandler(Dummy source);
        public event ConnectedHandler Connected;

        public delegate void DisconnectedHandler(Dummy source);
        public event DisconnectedHandler Disconnected;

        public delegate void StateHandler(Dummy source, DummyState dummyState);
        public event StateHandler StateChanged;

        public const int PollTimeout = 3 * 1000 * 1000;
        public const bool Debug = true;
        public const ushort Port = 6956;

        public bool Running { get; set; } = false;

        private bool dcexit = false;
        private int deadzone = 12;

        private IPAddress _remoteIP;

        public Dummy(IPAddress remoteIP)
        {
            _remoteIP = remoteIP;
        }

        public void HandleLoop()
        {
            IPEndPoint sockaddr_in = new IPEndPoint(_remoteIP, Port);
            EndPoint dummyaddr_in = new IPEndPoint(IPAddress.Any, Port);
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            byte[] packet;
            byte[] buf = new byte[0x1000];

            int timeout = 0;
            bool connected = false;
            while (Running)
            {
                if (!sock.Poll(timeout, SelectMode.SelectRead))
                {
                    if (connected)
                    {
                        Console.WriteLine("Socket timeout");
                        connected = false;
                    }

                    timeout = PollTimeout;

                    packet = new byte[]
                    {
                        0, // PacketID
                        0, // altkey (dummy)
                        1, // extdata (osu!C compatible flag)
                        0, // padding
                        0, // altkey1
                        0, // altkey2
                        0, // altkey3
                        0  // altkey4
                    };

                    Console.WriteLine("Sending ping packet");
                    sock.SendTo(packet, sockaddr_in);
                    continue;
                }

                if (sock.Poll(0, SelectMode.SelectError))
                {
                    object obj = sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
                    Console.WriteLine("Error with type " + obj.GetType().FullName + " received");
                    continue;
                }


                int recvret;
                try
                {
                    recvret = sock.ReceiveFrom(buf, ref dummyaddr_in);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    continue;
                }

                if (recvret <= 0) continue;


                if (!(dummyaddr_in is IPEndPoint))
                {
                    Console.WriteLine("Unsupported EndPoint type");
                    continue;
                }
                else
                {
                    IPEndPoint recv = (IPEndPoint)dummyaddr_in;
                    if (!sockaddr_in.Address.Equals(recv.Address))
                    {
                        Console.WriteLine("Ignoring message from invalid address " + recv.Address + ":" + sockaddr_in.Address);
                        continue;
                    }
                }

                switch (buf[0])
                {
                    case 0: //CONNECT
                        Console.WriteLine("Pong received");
                        connected = true;

                        packet = new byte[]
                        {
                            0x7D, // PacketID TouchCalEx
                            0, // altkey (dummy)
                            1, // extdata (osu!C compatible flag)
                            0 // padding
                        };

                        Console.WriteLine("Sending ping packet");
                        sock.SendTo(packet, sockaddr_in);

                        Connected?.Invoke(this);
                        continue;

                    case 1: // DISCONNECT
                        Console.WriteLine("Disconnected");
                        connected = false;
                        if (dcexit) Running = false;

                        continue;

                    case 2: // KEYS
                        int currkey = buf[4] | (buf[5] << 8) | (buf[6] << 16) | (buf[7] << 24);
                        short tx = (short)(buf[8] | (buf[9] << 8));
                        short ty = (short)(buf[10] | (buf[11] << 8));
                        short cx = (short)(buf[12] | (buf[13] << 8));
                        short cy = (short)(buf[14] | (buf[15] << 8));
                        short sx = (short)(buf[16] | (buf[17] << 8));
                        short sy = (short)(buf[18] | (buf[19] << 8));

                        if (Math.Abs(cx) < deadzone) cx = 0;
                        if (Math.Abs(cy) < deadzone) cy = 0;
                        if (Math.Abs(sx) < deadzone) sx = 0;
                        if (Math.Abs(sy) < deadzone) sy = 0;

                        if (Debug) Console.WriteLine($"[IN] K: {currkey:X8} T: {tx:+000;-000;0000}x{ty:+000;-000;0000} C: {cx:+000;-000;0000}x{cy:+000;-000;0000} S: {sx:+000;-000;0000}x{sy:+000;-000;0000}");

                        StateChanged?.Invoke(this, new DummyState()
                        {
                            Inputs = CreateInputFromKey(currkey, cx, cy, sx, sy),
                            Touch = new DummyState.TouchState()
                            {
                                IsTouch = IsPressed(InputMasks.Touch, currkey),
                                TouchX = tx,
                                TouchY = ty
                            }
                        });

                        break;

                    case 3: // SCREENSHOT (unused)
                        break;

                    case 0x7E: // KeyDownEx (osu!C)
                        currkey = buf[4] | (buf[5] << 8) | (buf[6] << 16) | (buf[7] << 24);
                        cx = KeyToAxis(currkey, InputMasks.LeftStickLeft, InputMasks.LeftStickRight);
                        cy = KeyToAxis(currkey, InputMasks.LeftStickDown, InputMasks.LeftStickUp);
                        sx = KeyToAxis(currkey, InputMasks.RightStickLeft, InputMasks.RightStickRight);
                        sy = KeyToAxis(currkey, InputMasks.RightStickDown, InputMasks.RightStickUp);

                        if (Debug) Console.WriteLine($"[KX] K: {currkey:X8}");

                        StateChanged?.Invoke(this, new DummyState()
                        {
                            Inputs = CreateInputFromKey(currkey, cx, cy, sx, sy)
                        });

                        break;

                    case 0x7F: // TouchEx (osu!C)
                        bool isTouch = ((buf[3] & 0x80) == 0) ? false : true;
                        short rtx = (short)(buf[4] | (buf[5] << 8));
                        short rty = (short)(buf[6] | (buf[7] << 8));

                        if (Debug) Console.WriteLine($"[TX] X: {rtx:X4} Y: {rty:X4} Touch: {isTouch}");

                        StateChanged?.Invoke(this, new DummyState()
                        {
                            Touch = new DummyState.TouchState()
                            {
                                IsTouch = isTouch,
                                TouchX = rtx,
                                TouchY = rty
                            }
                        });

                        break;

                    case 0x80: // JavaPing
                        sock.SendTo(new byte[] { 0x80, 0, 1, 0 }, sockaddr_in);
                        break;

                    default:
                        break;
                }

            }

            //keep the 3DS happy
            packet = new byte[]
            {
                1, // PacketID (DISCONNECT)
                0, // altkey (dummy)
                0, // padding1
                0, // padding2
                0, // altkey1
                0, // altkey2
                0, // altkey3
                0  // altkey4
            };

            Console.WriteLine("Sending disconnect packet");
            sock.SendTo(packet, sockaddr_in);

            sock.Shutdown(SocketShutdown.Both);
            sock.Close();

            Console.WriteLine("Task ended properly");
            Disconnected?.Invoke(this);
        }

        private bool IsPressed(int mask, int currkey)
        {
            return (mask & currkey) != 0;
        }

        private short KeyToAxis(int currkey, int low, int high)
        {
            return (short)(IsPressed(high, currkey) ? 156 : (IsPressed(low, currkey) ? -156 : 0));
        }

        private DummyState.InputState CreateInputFromKey(int key, short cx, short cy, short sx, short sy)
        {
            return new DummyState.InputState()
            {
                LeftStickX = cx,
                LeftStickY = cy,

                RightStickX = sx,
                RightStickY = sy,

                A = IsPressed(InputMasks.A, key),
                B = IsPressed(InputMasks.B, key),
                X = IsPressed(InputMasks.X, key),
                Y = IsPressed(InputMasks.Y, key),

                Left = IsPressed(InputMasks.Left, key),
                Up = IsPressed(InputMasks.Up, key),
                Right = IsPressed(InputMasks.Right, key),
                Down = IsPressed(InputMasks.Down, key),

                L = IsPressed(InputMasks.L, key),
                R = IsPressed(InputMasks.R, key),
                ZL = IsPressed(InputMasks.ZL, key),
                ZR = IsPressed(InputMasks.ZR, key),

                Start = IsPressed(InputMasks.Start, key),
                Select = IsPressed(InputMasks.Select, key),

                IsTouch = IsPressed(InputMasks.Touch, key)
            };
        }
    }
}