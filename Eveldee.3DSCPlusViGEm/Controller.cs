using MarcusD._3DSCPlusDummy;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
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

        private readonly ViGEmClient _viGEmClient;
        private IXbox360Controller _controller;
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
            _controller = _viGEmClient.CreateXbox360Controller();
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
                _controller.SetButtonState(Xbox360Button.A, i.B);
                _controller.SetButtonState(Xbox360Button.B, i.A);
                _controller.SetButtonState(Xbox360Button.X, i.Y);
                _controller.SetButtonState(Xbox360Button.Y, i.X);

                _controller.SetButtonState(Xbox360Button.Left, i.Left);
                _controller.SetButtonState(Xbox360Button.Up, i.Up);
                _controller.SetButtonState(Xbox360Button.Down, i.Down);
                _controller.SetButtonState(Xbox360Button.Right, i.Right);

                _controller.SetButtonState(Xbox360Button.LeftShoulder, i.L);
                _controller.SetButtonState(Xbox360Button.RightShoulder, i.R);

                _controller.SetSliderValue(Xbox360Slider.LeftTrigger, i.ZL ? byte.MaxValue : byte.MinValue);
                _controller.SetSliderValue(Xbox360Slider.RightTrigger, i.ZR ? byte.MaxValue : byte.MinValue);

                _controller.SetButtonState(Xbox360Button.Start, i.Start);
                _controller.SetButtonState(Xbox360Button.Back, i.Select);

                _controller.SetAxisValue(Xbox360Axis.LeftThumbX, (short)(i.LeftStickX * LeftStickMultiplier));
                _controller.SetAxisValue(Xbox360Axis.LeftThumbY, (short)(i.LeftStickY * LeftStickMultiplier));

                _controller.SetAxisValue(Xbox360Axis.RightThumbX, (short)(i.RightStickX * RightStickMultiplier));
                _controller.SetAxisValue(Xbox360Axis.RightThumbY, (short)(i.RightStickY * RightStickMultiplier));

                _controller.SetButtonState(Xbox360Button.Guide, i.IsTouch);
            });

            dummyState.Touch.With(t =>
            {
                // Maybe Stick Press on touch side
                // Will have to see with config
            });

            if (_dummy.Running)
            {
                _controller.SubmitReport();
            }
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
