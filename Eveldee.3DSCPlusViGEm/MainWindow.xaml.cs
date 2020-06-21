using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YamlDotNet.Serialization;

namespace Eveldee._3DSCPlusViGEm
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string SettingsPath = "Settings.yaml";
        public const string KeyMapPath = "KeyMap.yaml";
        public const string LogPath = "Logs.txt";

        public static MainWindow Instance { get; private set; }

        public Settings Settings { get; private set; }
        public Dictionary<N3DSInputs, N3DSInputs> KeyMap { get; private set; }

        private bool _isActivated = false;
        private readonly Controller _controller;

        private readonly Serializer _serializer;
        private readonly Deserializer _deserializer;
        private FileSystemWatcher _keyMapWatcher;
        private object _keyMapLock = new object();

        public MainWindow()
        {
            Instance = this;

            InitializeComponent();

            _serializer = new Serializer();
            _deserializer = new Deserializer();

            try
            {
                LoadSettings();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Couldn't load settings: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                File.AppendAllText(LogPath, $"{e.Message} \n");
                File.AppendAllText(LogPath, e.StackTrace);

                Environment.Exit(1);
            }

            Picker_TargetType.ItemsSource = Enum.GetValues(typeof(TargetType));
            Picker_TargetType.SelectedItem = Settings.TargetType;

            _controller = new Controller();

            Closed += MainWindow_Closed;
        }

        private void LoadSettings()
        {
            // Load Settings
            if (File.Exists(SettingsPath))
            {
                string text = File.ReadAllText(SettingsPath);
                Settings = _deserializer.Deserialize<Settings>(text);

                if (IPAddress.TryParse(Settings.IP, out var _))
                {
                    Txt_IP.Text = Settings.IP;
                }
            }
            else
            {
                Settings = new Settings();
            }

            // Load KeyMap
            if (File.Exists(KeyMapPath))
            {
                string text = File.ReadAllText(KeyMapPath);

                KeyMap = _deserializer.Deserialize<Dictionary<N3DSInputs, N3DSInputs>>(text);
            }
            else
            {
                KeyMap = new Dictionary<N3DSInputs, N3DSInputs>()
                {
                    { N3DSInputs.LeftStickLeft, N3DSInputs.LeftStickLeft },
                    { N3DSInputs.LeftStickUp, N3DSInputs.LeftStickUp },
                    { N3DSInputs.LeftStickRight, N3DSInputs.LeftStickRight },
                    { N3DSInputs.LeftStickDown, N3DSInputs.LeftStickDown },
                    { N3DSInputs.LeftStick, N3DSInputs.None },

                    { N3DSInputs.RightStickLeft, N3DSInputs.RightStickLeft },
                    { N3DSInputs.RightStickUp, N3DSInputs.RightStickUp },
                    { N3DSInputs.RightStickRight, N3DSInputs.RightStickRight },
                    { N3DSInputs.RightStickDown, N3DSInputs.RightStickDown },
                    { N3DSInputs.RightStick, N3DSInputs.None },

                    { N3DSInputs.A, N3DSInputs.A },
                    { N3DSInputs.B, N3DSInputs.B },
                    { N3DSInputs.X, N3DSInputs.X },
                    { N3DSInputs.Y, N3DSInputs.Y },

                    { N3DSInputs.Left, N3DSInputs.Left },
                    { N3DSInputs.Up, N3DSInputs.Up },
                    { N3DSInputs.Right, N3DSInputs.Right },
                    { N3DSInputs.Down, N3DSInputs.Down },

                    { N3DSInputs.L, N3DSInputs.L },
                    { N3DSInputs.R, N3DSInputs.R },
                    { N3DSInputs.ZL, N3DSInputs.ZL },
                    { N3DSInputs.ZR, N3DSInputs.ZR },

                    { N3DSInputs.Start, N3DSInputs.Start },
                    { N3DSInputs.Select, N3DSInputs.Select },

                    { N3DSInputs.Touch, N3DSInputs.Touch }
                };

                File.WriteAllText(KeyMapPath, _serializer.Serialize(KeyMap));
            }

            _keyMapWatcher = new FileSystemWatcher(Environment.CurrentDirectory, KeyMapPath)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                IncludeSubdirectories = false
            };
            _keyMapWatcher.Changed += OnKeyMapChange;
            _keyMapWatcher.EnableRaisingEvents = true;
        }

        private async Task SaveSettings()
        {
            using (var file = File.CreateText(SettingsPath))
            {
                string text = _serializer.Serialize(Settings);

                await file.WriteAsync(text);
            }
        }

        private void OnKeyMapChange(object sender, FileSystemEventArgs e)
        {
            // Note that softwares like Notepad++ fires this even 2 times, nothing much can be done about it
            lock (_keyMapLock)
            {
                try
                {
                    string text = File.ReadAllText(KeyMapPath);

                    KeyMap = _deserializer.Deserialize<Dictionary<N3DSInputs, N3DSInputs>>(text);
                }
                catch (Exception)
                {
                    // Just wait next event, happens when it's fired 2 times
                    return;
                }
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _controller.Dispose();
            _keyMapWatcher.Dispose();
        }

        private async void Btn_Toggle_Click(object sender, RoutedEventArgs e)
        {
            if (_isActivated)
            {
                _isActivated = false;

                await Disconnect();
            }
            else
            {
                _isActivated = true;

                await Connect();
            }
        }

        private async Task Connect()
        {
            if (!IPAddress.TryParse(Txt_IP.Text, out var iPAddress))
            {
                MessageBox.Show("Invalid IP address", "Error", MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            Settings.TargetType = (TargetType)Picker_TargetType.SelectedItem;
            Settings.IP = Txt_IP.Text;
            await SaveSettings();

            Btn_Toggle.IsEnabled = false;
            Btn_Toggle.Content = "Connecting...";

            await _controller.Start(iPAddress);

            Btn_Toggle.Content = "Disconnect";
            Btn_Toggle.IsEnabled = true;
        }

        private async Task Disconnect()
        {
            Btn_Toggle.IsEnabled = false;
            Btn_Toggle.Content = "Diconnecting...";

            await Task.Run(() => _controller.Stop());

            Btn_Toggle.Content = "Connect";
            Btn_Toggle.IsEnabled = true;
        }
    }
}
