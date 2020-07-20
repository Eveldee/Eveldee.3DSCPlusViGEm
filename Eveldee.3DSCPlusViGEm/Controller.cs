using Eveldee._3DSCPlusViGEm.Utils;
using MarcusD._3DSCPlusDummy;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm
{
    public class Controller : IDisposable
    {
        public const short LeftStickMultiplier = 210;
        public const short RightStickMultiplier = 224;

        public StickSettings StickSettings => MainWindow.Instance.StickSettings;

        private readonly ViGEmClient _viGEmClient;
        private IVirtualGamepad _controller;
        private Dummy _dummy;
        private CancellationTokenSource _connectToken;
        private CancellationTokenSource _disconnectToken;

        public Controller()
        {
            _viGEmClient = new ViGEmClient();
        }

        public void Dispose()
        {
            if (_dummy?.Running == true)
            {
                _controller.Disconnect();
                _dummy.Running = false;
            }

            _viGEmClient.Dispose();
        }

        public async Task Start(IPAddress iPAddress)
        {
            _controller = MainWindow.Instance.Settings.TargetType == TargetType.Xbox360 ? (IVirtualGamepad)_viGEmClient.CreateXbox360Controller() : _viGEmClient.CreateDualShock4Controller();
            _controller.AutoSubmitReport = false;
            _controller.Connect();

            _dummy = new Dummy(iPAddress)
            {
                Running = true
            };

            _connectToken = new CancellationTokenSource();

            Dummy.ConnectedHandler handler = (d) => _connectToken.Cancel();
            _dummy.Connected += handler;
            _dummy.StateChanged += OnStateChanged;

            _ = Task.Run(() => _dummy.HandleLoop());

            await Task.Delay(-1, _connectToken.Token).ContinueWith((t) => { });

            _dummy.Connected -= handler;
        }

        private void OnStateChanged(Dummy source, DummyState dummyState)
        {
            dummyState.Inputs.With(i =>
            {
                if (_controller is IXbox360Controller xbox360)
                {
                    xbox360.SetButtonState(Xbox360Button.A, i.B);
                    xbox360.SetButtonState(Xbox360Button.B, i.A);
                    xbox360.SetButtonState(Xbox360Button.X, i.Y);
                    xbox360.SetButtonState(Xbox360Button.Y, i.X);

                    xbox360.SetButtonState(Xbox360Button.Left, i.Left);
                    xbox360.SetButtonState(Xbox360Button.Up, i.Up);
                    xbox360.SetButtonState(Xbox360Button.Down, i.Down);
                    xbox360.SetButtonState(Xbox360Button.Right, i.Right);

                    xbox360.SetButtonState(Xbox360Button.LeftShoulder, i.L);
                    xbox360.SetButtonState(Xbox360Button.RightShoulder, i.R);

                    xbox360.SetSliderValue(Xbox360Slider.LeftTrigger, i.ZL ? byte.MaxValue : byte.MinValue);
                    xbox360.SetSliderValue(Xbox360Slider.RightTrigger, i.ZR ? byte.MaxValue : byte.MinValue);

                    xbox360.SetButtonState(Xbox360Button.Start, i.Start);
                    xbox360.SetButtonState(Xbox360Button.Back, i.Select);

                    xbox360.SetAxisValue(Xbox360Axis.LeftThumbX, AmplifyStick(i.LeftStickX, LeftStickMultiplier * StickSettings.Left.SensibiltyX));
                    xbox360.SetAxisValue(Xbox360Axis.LeftThumbY, AmplifyStick(i.LeftStickY, LeftStickMultiplier * StickSettings.Left.SensibiltyY));
                    xbox360.SetButtonState(Xbox360Button.LeftThumb, i.LeftStick);

                    xbox360.SetAxisValue(Xbox360Axis.RightThumbX, AmplifyStick(i.RightStickX, RightStickMultiplier * StickSettings.Right.SensibiltyX));
                    xbox360.SetAxisValue(Xbox360Axis.RightThumbY, AmplifyStick(i.RightStickY, RightStickMultiplier * StickSettings.Right.SensibiltyY));
                    xbox360.SetButtonState(Xbox360Button.RightThumb, i.RightStick);

                    xbox360.SetButtonState(Xbox360Button.Guide, i.IsTouch);
                }
                else if (_controller is IDualShock4Controller ds4)
                {
                    ds4.SetButtonState(DualShock4Button.Cross, i.B);
                    ds4.SetButtonState(DualShock4Button.Circle, i.A);
                    ds4.SetButtonState(DualShock4Button.Square, i.Y);
                    ds4.SetButtonState(DualShock4Button.Triangle, i.X);

                    if (i.Up)
                    {
                        if (i.Left)
                        {
                            ds4.SetDPadDirection(DualShock4DPadDirection.Northwest);
                        }
                        else if (i.Right)
                        {
                            ds4.SetDPadDirection(DualShock4DPadDirection.Northeast);
                        }
                        else
                        {
                            ds4.SetDPadDirection(DualShock4DPadDirection.North);
                        }
                    }
                    else if (i.Down)
                    {
                        if (i.Left)
                        {
                            ds4.SetDPadDirection(DualShock4DPadDirection.Southwest);
                        }
                        else if (i.Right)
                        {
                            ds4.SetDPadDirection(DualShock4DPadDirection.Southeast);
                        }
                        else
                        {
                            ds4.SetDPadDirection(DualShock4DPadDirection.South);
                        }
                    }
                    else if (i.Left)
                    {
                        ds4.SetDPadDirection(DualShock4DPadDirection.West);
                    }
                    else if (i.Right)
                    {
                        ds4.SetDPadDirection(DualShock4DPadDirection.East);
                    }
                    else
                    {
                        ds4.SetDPadDirection(DualShock4DPadDirection.None);
                    }

                    ds4.SetButtonState(DualShock4Button.ShoulderLeft, i.L);
                    ds4.SetButtonState(DualShock4Button.ShoulderRight, i.R);

                    ds4.SetSliderValue(DualShock4Slider.LeftTrigger, i.ZL ? byte.MaxValue : byte.MinValue);
                    ds4.SetSliderValue(DualShock4Slider.RightTrigger, i.ZR ? byte.MaxValue : byte.MinValue);
                    ds4.SetButtonState(DualShock4Button.TriggerLeft, i.ZL);
                    ds4.SetButtonState(DualShock4Button.TriggerRight, i.ZR);

                    ds4.SetButtonState(DualShock4Button.Options, i.Start);
                    ds4.SetButtonState(DualShock4Button.Share, i.Select);

                    ds4.SetAxisValue(DualShock4Axis.LeftThumbX.Id, AmplifyStick(i.LeftStickX, LeftStickMultiplier * StickSettings.Left.SensibiltyX));
                    ds4.SetAxisValue(DualShock4Axis.LeftThumbY.Id, AmplifyStick(i.LeftStickY, LeftStickMultiplier * StickSettings.Left.SensibiltyY, true));
                    ds4.SetButtonState(DualShock4Button.ThumbLeft, i.LeftStick);

                    ds4.SetAxisValue(DualShock4Axis.RightThumbX.Id, AmplifyStick(i.RightStickX, RightStickMultiplier * StickSettings.Right.SensibiltyX));
                    ds4.SetAxisValue(DualShock4Axis.RightThumbY.Id, AmplifyStick(i.RightStickY, RightStickMultiplier * StickSettings.Right.SensibiltyY, true));
                    ds4.SetButtonState(DualShock4Button.ThumbRight, i.RightStick);

                    ds4.SetButtonState(DualShock4SpecialButton.Ps, i.IsTouch);
                }
            });

            if (_dummy.Running)
            {
                _controller.SubmitReport();
            }
        }

        private short AmplifyStick(int stickValue, double multiplier, bool reverse = false)
        {
            double value = Math.Abs(stickValue * multiplier);

            if (value > short.MaxValue)
            {
                value = short.MaxValue;
            }

            value *= Math.Sign(stickValue);

            return (short)(reverse ? -value : value);
        }

        public async Task Stop()
        {
            _dummy.StateChanged -= OnStateChanged;

            _ = Task.Run(() => _controller.Disconnect());

            _disconnectToken = new CancellationTokenSource();

            Dummy.DisconnectedHandler handler = (d) => _disconnectToken.Cancel();
            _dummy.Disconnected += handler;

            _dummy.Running = false;

            await Task.Delay(-1, _disconnectToken.Token).ContinueWith((t) => { });

            _dummy.Disconnected -= handler;
        }
    }
}
