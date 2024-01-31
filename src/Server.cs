using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

while (true)
{
    var socket = server.AcceptSocket(); // wait for client
    Thread thread = new Thread(() => HandleClient(socket));
    thread.Start();
}

static void HandleClient(Socket socket)
{
    while (true)
    {
        byte[] bytes = new Byte[1024];
        var numByte = socket.Receive(bytes);

        var data = Encoding.ASCII.GetString(bytes,
                                   0, numByte);

        var buffer = Encoding.UTF8.GetBytes("+PONG\r\n");
        socket.Send(buffer);
    }
    
}



