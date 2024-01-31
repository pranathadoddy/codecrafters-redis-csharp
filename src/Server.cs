using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

var socket = server.AcceptSocket(); // wait for client

byte[] bytes = new Byte[1024];
string data;

while (true)
{
    int numByte = socket.Receive(bytes);

    data = Encoding.ASCII.GetString(bytes,
                               0, numByte);
    if(data == "ping")
    {
        var buffer = Encoding.UTF8.GetBytes("+PONG\r\n");
        socket.Send(buffer);
    }
}
