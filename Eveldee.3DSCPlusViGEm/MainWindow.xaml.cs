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

namespace Eveldee._3DSCPlusViGEm
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string SettingsPath = "ip.txt";

        private bool _isActivated = false;
        private readonly Controller _controller;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();

            _controller = new Controller();

            Closed += MainWindow_Closed;
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                string ip = File.ReadAllText(SettingsPath).Trim();

                if (IPAddress.TryParse(ip, out var iPAddress))
                {
                    Txt_IP.Text = ip;
                }
            }
        }
        private async Task SaveSettings()
        {
            using (var file = File.CreateText(SettingsPath))
            {
                await file.WriteLineAsync(Txt_IP.Text);
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
