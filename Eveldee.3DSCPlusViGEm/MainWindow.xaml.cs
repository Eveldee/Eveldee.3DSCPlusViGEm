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

        public static Settings Settings { get; set; }

        private bool _isActivated = false;
        private readonly Controller _controller;

        private Settings _settings;
        private readonly Serializer _serializer;
        private readonly Deserializer _deserializer;

        public MainWindow()
        {
            InitializeComponent();

            _serializer = new Serializer();
            _deserializer = new Deserializer();

            LoadSettings();

            Picker_TargetType.ItemsSource = Enum.GetValues(typeof(TargetType));
            Picker_TargetType.SelectedItem = _settings.TargetType;

            _controller = new Controller();

            Closed += MainWindow_Closed;
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                string text = File.ReadAllText(SettingsPath);
                _settings = _deserializer.Deserialize<Settings>(text);

                if (IPAddress.TryParse(_settings.IP, out var _))
                {
                    Txt_IP.Text = _settings.IP;
                }
            }
            else
            {
                _settings = new Settings();
            }

            Settings = _settings;
        }

        private async Task SaveSettings()
        {
            using (var file = File.CreateText(SettingsPath))
            {
                string text = _serializer.Serialize(_settings);

                await file.WriteAsync(text);
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _controller.Dispose();
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

            _settings.TargetType = (TargetType)Picker_TargetType.SelectedItem;
            _settings.IP = Txt_IP.Text;
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
