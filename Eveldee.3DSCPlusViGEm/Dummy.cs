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
        public const int MaxLeftAxisValue = 156;
        public const int MaxRightAxisValue = 146;

        public bool Running { get; set; } = false;
        public Dictionary<N3DSInputs, N3DSInputs> KeyMap => MainWindow.Instance.KeyMap;
        public StickSettings.AxisSettings LeftStickSettings => MainWindow.Instance.StickSettings.Left;
        public StickSettings.AxisSettings RightStickSettings => MainWindow.Instance.StickSettings.Right;
        public IEnumerable<TouchMap> TouchMaps => MainWindow.Instance.TouchMaps;

        private bool dcexit = false;
        private DummyState.TouchState _lastTouchExState;

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

                        if (Math.Abs(cx) < LeftStickSettings.DeadzoneX) cx = 0;
                        if (Math.Abs(cy) < LeftStickSettings.DeadzoneY) cy = 0;
                        if (Math.Abs(sx) < RightStickSettings.DeadzoneX) sx = 0;
                        if (Math.Abs(sy) < RightStickSettings.DeadzoneY) sy = 0;

                        cx *= (short)(LeftStickSettings.InvertX ? -1 : 1);
                        cy *= (short)(LeftStickSettings.InvertY ? -1 : 1);
                        sx *= (short)(RightStickSettings.InvertX ? -1 : 1);
                        sy *= (short)(RightStickSettings.InvertY ? -1 : 1);

                        if (Debug) Console.WriteLine($"[IN] K: {currkey:X8} T: {tx:+000;-000;0000}x{ty:+000;-000;0000} C: {cx:+000;-000;0000}x{cy:+000;-000;0000} S: {sx:+000;-000;0000}x{sy:+000;-000;0000}");

                        N3DSInputs inputs = (N3DSInputs)currkey;
                        //if (Debug) Console.WriteLine($"[Inputs] {inputs}");

                        var inputState = CreateStateFromInputs(inputs, cx, cy, sx, sy);

                        if (inputs.HasFlag(N3DSInputs.Touch))
                        {
                            ApplyTouchMapToState(inputState, tx, ty, isEx: false);
                        }

                        StateChanged?.Invoke(this, new DummyState()
                        {
                            Inputs = inputState,
                            Touch = new DummyState.TouchState()
                            {
                                IsTouch = inputs.HasFlag(N3DSInputs.Touch),
                                TouchX = tx,
                                TouchY = ty
                            }
                        });

                        break;

                    case 3: // SCREENSHOT (unused)
                        break;

                    case 0x7E: // KeyDownEx (osu!C)
                        currkey = buf[4] | (buf[5] << 8) | (buf[6] << 16) | (buf[7] << 24);
                        inputs = (N3DSInputs)currkey;
                        cx = KeyToAxis(inputs, N3DSInputs.LeftStickLeft, N3DSInputs.LeftStickRight, MaxLeftAxisValue);
                        cy = KeyToAxis(inputs, N3DSInputs.LeftStickDown, N3DSInputs.LeftStickUp, MaxLeftAxisValue);
                        sx = KeyToAxis(inputs, N3DSInputs.RightStickLeft, N3DSInputs.RightStickRight, MaxRightAxisValue);
                        sy = KeyToAxis(inputs, N3DSInputs.RightStickDown, N3DSInputs.RightStickUp, MaxRightAxisValue);

                        cx *= (short)(LeftStickSettings.InvertX ? -1 : 1);
                        cy *= (short)(LeftStickSettings.InvertY ? -1 : 1);
                        sx *= (short)(RightStickSettings.InvertX ? -1 : 1);
                        sy *= (short)(RightStickSettings.InvertY ? -1 : 1);

                        if (Debug) Console.WriteLine($"[KX] K: {currkey:X8} Inputs: {inputs}");

                        inputState = CreateStateFromInputs(inputs, cx, cy, sx, sy);

                        if (_lastTouchExState != null && _lastTouchExState.IsTouch)
                        {
                            ApplyTouchMapToState(inputState, _lastTouchExState.TouchX, _lastTouchExState.TouchY, isEx: true);
                        }

                        StateChanged?.Invoke(this, new DummyState()
                        {
                            Inputs = inputState
                        });

                        break;

                    case 0x7F: // TouchEx (osu!C)
                        bool isTouch = ((buf[3] & 0x80) == 0) ? false : true;
                        short rtx = (short)(buf[4] | (buf[5] << 8));
                        short rty = (short)(buf[6] | (buf[7] << 8));

                        //if (Debug) Console.WriteLine($"[TX] X: {rtx:X4} Y: {rty:X4} Touch: {isTouch}");
                        if (Debug) Console.WriteLine($"[TX] X: {rtx} Y: {rty} Touch: {isTouch}");

                        _lastTouchExState = new DummyState.TouchState()
                        {
                            IsTouch = isTouch,
                            TouchX = rtx,
                            TouchY = rty
                        };

                        StateChanged?.Invoke(this, new DummyState()
                        {
                            Touch = _lastTouchExState
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

        private bool IsPressed(N3DSInputs input, N3DSInputs inputs)
        {
            return inputs.HasFlag(KeyMap[input]);
        }

        private short KeyToAxis(N3DSInputs inputs, N3DSInputs low, N3DSInputs high, int value)
        {
            return (short)(IsPressed(high, inputs) ? value : (IsPressed(low, inputs) ? -value : 0));
        }

        private DummyState.InputState CreateStateFromInputs(N3DSInputs inputs, short cx, short cy, short sx, short sy)
        {
            return new DummyState.InputState()
            {
                LeftStickX = cx,
                LeftStickY = cy,
                LeftStick = IsPressed(N3DSInputs.LeftStick, inputs),

                RightStickX = sx,
                RightStickY = sy,
                RightStick = IsPressed(N3DSInputs.RightStick, inputs),

                A = IsPressed(N3DSInputs.A, inputs),
                B = IsPressed(N3DSInputs.B, inputs),
                X = IsPressed(N3DSInputs.X, inputs),
                Y = IsPressed(N3DSInputs.Y, inputs),

                Left = IsPressed(N3DSInputs.Left, inputs),
                Up = IsPressed(N3DSInputs.Up, inputs),
                Right = IsPressed(N3DSInputs.Right, inputs),
                Down = IsPressed(N3DSInputs.Down, inputs),

                L = IsPressed(N3DSInputs.L, inputs),
                R = IsPressed(N3DSInputs.R, inputs),
                ZL = IsPressed(N3DSInputs.ZL, inputs),
                ZR = IsPressed(N3DSInputs.ZR, inputs),

                Start = IsPressed(N3DSInputs.Start, inputs),
                Select = IsPressed(N3DSInputs.Select, inputs),

                IsTouch = IsPressed(N3DSInputs.Touch, inputs)
            };
        }

        private void ApplyTouchMapToState(DummyState.InputState inputState, int touchX, int touchY, bool isEx)
        {
            double xMax = isEx ? 4096 : 320;
            double yMax = isEx ? 4096 : 240;
            double x = touchX / xMax;
            double y = touchY / yMax;

            foreach (var touchMap in TouchMaps.Where(touchMap => touchMap.HasValidArea()))
            {
                // Check if in area
                if (x >= touchMap.X1 && x < touchMap.X2 && y >= touchMap.Y1 && y < touchMap.Y2)
                {
                    var inputs = touchMap.Inputs;

                    inputState.LeftStick = inputs.HasFlag(N3DSInputs.LeftStick) || inputState.LeftStick;
                    inputState.RightStick = inputs.HasFlag(N3DSInputs.RightStick) || inputState.RightStick;

                    inputState.A = inputs.HasFlag(N3DSInputs.A) || inputState.A;
                    inputState.B = inputs.HasFlag(N3DSInputs.B) || inputState.B;
                    inputState.X = inputs.HasFlag(N3DSInputs.X) || inputState.X;
                    inputState.Y = inputs.HasFlag(N3DSInputs.Y) || inputState.Y;

                    inputState.Left = inputs.HasFlag(N3DSInputs.Left) || inputState.Left;
                    inputState.Up = inputs.HasFlag(N3DSInputs.Up) || inputState.Up;
                    inputState.Right = inputs.HasFlag(N3DSInputs.Right) || inputState.Right;
                    inputState.Down = inputs.HasFlag(N3DSInputs.Down) || inputState.Down;

                    inputState.L = inputs.HasFlag(N3DSInputs.L) || inputState.L;
                    inputState.R = inputs.HasFlag(N3DSInputs.R) || inputState.R;
                    inputState.ZL = inputs.HasFlag(N3DSInputs.ZL) || inputState.ZL;
                    inputState.ZR = inputs.HasFlag(N3DSInputs.ZR) || inputState.ZR;

                    inputState.Start = inputs.HasFlag(N3DSInputs.Start) || inputState.Start;
                    inputState.Select = inputs.HasFlag(N3DSInputs.Select) || inputState.Select;

                    inputState.IsTouch = inputs.HasFlag(N3DSInputs.Touch) || inputState.IsTouch;
                }
            }
        }
    }
}