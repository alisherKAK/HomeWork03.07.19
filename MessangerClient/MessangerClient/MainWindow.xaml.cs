using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MessangerClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient client;
        string clientName;

        public MainWindow()
        {
            InitializeComponent();

            connectDisconnectButton.Tag = false;
            ipTextBox.Text = "10.1.4.87";
            portTextBox.Text = "12345";
            nameTextBox.Text = "AlishBEk";
        }

        private void ConnectDisconnectButtonClick(object sender, RoutedEventArgs e)
        {
            if(!(bool)connectDisconnectButton.Tag)
            {
                clientName = nameTextBox.Text;
                client = new TcpClient();
                client.BeginConnect(IPAddress.Parse(ipTextBox.Text.Trim()), int.Parse(portTextBox.Text.Trim()), ClientWork, client);

                connectDisconnectButton.Tag = true;
                connectDisconnectButton.Content = "Disconnect";
            }
            else
            {
                client.Client.Shutdown(SocketShutdown.Both);
                client.Close();

                connectDisconnectButton.Tag = false;
                connectDisconnectButton.Content = "Connect";
            }
        }

        private void ClientWork(object state)
        {
            TcpClient client = (state as IAsyncResult).AsyncState as TcpClient;

            if (!string.IsNullOrEmpty(clientName))
            {
                Dispatcher.Invoke(() =>
                {
                    client.Client.Send(Encoding.UTF8.GetBytes(nameTextBox.Text.Trim()));
                });

                ThreadPool.QueueUserWorkItem(ClientListen, client);

                MessageBox.Show("Connected");
            }
            else
            {
                MessageBox.Show("Напишите свое имя");

                client.Client.Shutdown(SocketShutdown.Both);
                client.Close();

                Dispatcher.Invoke(() =>
                {
                    connectDisconnectButton.Tag = false;
                    connectDisconnectButton.Content = "Connect";
                });
            }
        }

        private void ClientListen(object state)
        {
            TcpClient client = state as TcpClient;
            byte[] buf = new byte[4 * 1024];
            int recSize;

            try
            {
                while (client.Connected)
                {
                    recSize = client.Client.Receive(buf);
                    Dispatcher.Invoke(() =>
                    {
                        messageTextBox.Text += Encoding.UTF8.GetString(buf, 0, recSize) + "\n";
                    });
                }
            }
            catch (NullReferenceException) { }
            catch (SocketException) { }
        }

        private void SendButtonClick(object sender, RoutedEventArgs e)
        {
            if ((bool)connectDisconnectButton.Tag)
            {
                client.Client.Send(Encoding.UTF8.GetBytes(sendMessageTextBox.Text.Trim() + "#" + userIpTextBox.Text.Trim()));
                sendMessageTextBox.Text = string.Empty;
            }
        }

        private void UserIpTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void PortTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void IpTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal || e.Key == Key.OemPeriod)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }
    }
}
