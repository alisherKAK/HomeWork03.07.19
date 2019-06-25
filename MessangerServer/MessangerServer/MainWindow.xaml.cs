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

namespace MessangerServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Thread serverThread;
        TcpListener server;

        List<User> users = new List<User>();
        int id = 1;

        public MainWindow()
        {
            InitializeComponent();

            startStopButton.Tag = false;

            ipsComboBox.Items.Add("0.0.0.0");
            ipsComboBox.Items.Add("127.0.0.1");

            foreach(IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                ipsComboBox.Items.Add(ip.ToString());
            }

            ipsComboBox.SelectedIndex = 0;
        }

        delegate void DelegateAppendTextBox(TextBox tb, string str);

        private void AppendTextBox(TextBox tb, string str)
        {
            tb.Text += str + "\n";
        }

        private void PortTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if( (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) )
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void StartStopButtonClick(object sender, RoutedEventArgs e)
        {
            if(!(bool)startStopButton.Tag)
            {
                startStopButton.Content = "Stop";
                startStopButton.Tag = true;

                server = new TcpListener(IPAddress.Parse(ipsComboBox.SelectedItem.ToString()), int.Parse(portTextBox.Text));
                server.Start(100);

                serverThread = new Thread(ServerThreadRoutine);
                serverThread.IsBackground = true;
                serverThread.Start(server);

                logTextBox.Text += "Server have started his work\n";
            }
            else
            {
                startStopButton.Content = "Start";
                startStopButton.Tag = false;

                server.Stop();
                users.Clear();
            }
        }

        private void ServerThreadRoutine(object state)
        {
            TcpListener server = state as TcpListener;
            byte[] buf = new byte[4 * 1024];
            int recSize;

            try
            {
                while (true)
                {
                    User user = new User() { Id = id};
                    user.Client = server.AcceptTcpClient();
                    ++id;


                    recSize = user.Client.Client.Receive(buf);
                    user.Client.Client.Send(Encoding.UTF8.GetBytes("Hello " + Encoding.UTF8.GetString(buf, 0, recSize) + "\n"));
                    user.Name = Encoding.UTF8.GetString(buf, 0, recSize);
                    users.Add(user);

                    Dispatcher.Invoke(new DelegateAppendTextBox(AppendTextBox), new object[] {logTextBox, $"Client {user.Client.Client.LocalEndPoint} added\n" });

                    ThreadPool.QueueUserWorkItem(ClientThreadRoutine, user);
                }
            }
            catch(InvalidOperationException)
            {

            }
            catch(SocketException)
            {

            }
        }

        private void ClientThreadRoutine(object state)
        {
            User user = state as User;
            byte[] buf = new byte[4 * 1024];
            int recSize;
            int userId;
            string fullString;
            string sendString = string.Empty;

            while(user.Client.Connected)
            {
                try
                {
                    recSize = user.Client.Client.Receive(buf);

                    fullString = Encoding.UTF8.GetString(buf, 0, recSize);

                    if (int.TryParse(fullString.Split('#')[fullString.Split('#').Length - 1], out userId) && userId != 0)
                    {
                        for(int i = 0; i < fullString.Split('#').Length - 1; i++)
                        {
                            sendString += fullString.Split('#')[i];
                        }

                        users.Where(sendUser => sendUser.Id == userId).FirstOrDefault().Client.Client.Send(Encoding.UTF8.GetBytes(sendString));

                        Dispatcher.Invoke(new DelegateAppendTextBox(AppendTextBox), new object[] { logTextBox, $"Client {user.Client.Client.LocalEndPoint} send to { users.Where(sendUser => sendUser.Id == userId).FirstOrDefault().Client.Client.LocalEndPoint} {sendString}\n" });

                        sendString = string.Empty;
                    }
                    else
                    {
                        for (int i = 0; i < fullString.Split('#').Length - 1; i++)
                        {
                            sendString += fullString.Split('#')[i];
                        }

                        foreach (User sendUser in users)
                        {
                            sendUser.Client.Client.Send(Encoding.UTF8.GetBytes(sendString + "\n"));
                        }

                        sendString = string.Empty;
                    }
                }
                catch(SocketException)
                {

                }
            }

            user.Client.Client.Shutdown(SocketShutdown.Both);
            user.Client.Close();
            users.Remove(user);
        }
    }
}
