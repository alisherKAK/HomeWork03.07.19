using System.Net.Sockets;

namespace MessangerServer
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TcpClient Client { get; set; }
    }
}
