using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Logs from your program will appear here!");

Dictionary<string, Tuple<string, DateTime>> dataStore = new Dictionary<string, Tuple<string, DateTime>>();

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

while (true)
{
    var socket = server.AcceptSocket(); // wait for client
    Thread thread = new Thread(() => HandleClient(socket));
    thread.Start();
}

void HandleClient(Socket socket)
{
    while (true)
    {
        byte[] bytes = new Byte[1024];
        var numByte = socket.Receive(bytes);

        var data = Encoding.ASCII.GetString(bytes,
                                   0, numByte)
            .Split("\r\n")
            .Select(x => x.Trim())
            .Where(item => !string.IsNullOrEmpty(item))
            .ToList();

        var parsedRequest = ParseRequest(data);

        HandleCommand(socket, parsedRequest.CommandName, parsedRequest.Args);

    }

}

void HandleCommand(Socket socket, string command, List<object> args)
{
    string response;
    switch (command)
    {
        case "PING":
            socket.Send(Encoding.UTF8.GetBytes("+PONG\r\n"));
            break;
        case "ECHO":
            {
                response = string.Join(" ",args);

                socket.Send(Encoding.UTF8.GetBytes($"+{response}\r\n"));
            }

            break;

        case "SET":
            if(args.Count >= 4 && args[2].ToString()?.ToUpper() == "PX")
            {
                var key = args[0].ToString() ?? "";
                var value = args[1].ToString() ?? "";
                
                response = dataStore.TryAdd(key, new Tuple<string, DateTime>(value, DateTime.Now.AddMilliseconds((double)args[3]))) ? "+OK" : "$-1";
            }
            else
            {
                var key = args[0].ToString() ?? "";
                var value = args[1].ToString() ?? "";

                response = dataStore.TryAdd(key, new Tuple<string, DateTime>(value, DateTime.MaxValue)) ? "+OK" : "$-1";
                
            }
            socket.Send(Encoding.UTF8.GetBytes($"{response}\r\n"));
            break;

        case "GET":
            if(args.Count == 1)
            {
                var key = args[0].ToString() ?? "";
                Tuple<string, DateTime> valuePair;
                if (dataStore.TryGetValue(key, out valuePair))
                {
                    if(valuePair.Item2 < valuePair.Item2)
                    {
                        socket.Send(Encoding.UTF8.GetBytes($"+{valuePair.Item1}\r\n"));
                    }
                    else
                    {
                        socket.Send(Encoding.UTF8.GetBytes($"$-1\r\n"));
                    }
                }
                else
                {
                    socket.Send(Encoding.UTF8.GetBytes($"$-1\r\n"));
                }
            }
            else
            {
                socket.Send(Encoding.UTF8.GetBytes($"$-1\r\n"));
            }
            break;

        case "INFO":
            socket.Send(Encoding.UTF8.GetBytes("+INFO\r\n"));
            break;
        default:
            socket.Send(Encoding.UTF8.GetBytes($"-ERR invalid command \r\n"));
            break;
    }
}


static (string CommandName, List<object> Args) ParseRequest(List<string> arrResp)
{
    var parsedResp = ParseResp(arrResp);
    var command = ((List<object>)parsedResp.Content)[0].ToString();
    ((List<object>)(parsedResp.Content)).RemoveAt(0);

    switch (command.ToUpper())
    {
        default:
            return (command.ToUpper(), (List<object>)parsedResp.Content);
    }

}

static (object Content, List<string> ArrResp) ParseResp(List<string> arrResp)
{
    while (arrResp.Count > 0)
    {
        var element = arrResp[0];
        arrResp.RemoveAt(0);

        switch (element[0])
        {
            case '*':
                // array
                var arrLen = int.Parse(element[1..]);
                var arr = new List<object>();
                for (int j = 0; j < arrLen; j++)
                {
                    var parsedContent = ParseResp(arrResp);
                    arr.Add(parsedContent.Content);
                    arrResp = parsedContent.ArrResp;
                }

                return (arr, arrResp);
            case '+':
                // string
                var st = element[1..];
                return (st, arrResp);
            case '$':
                // bulk string
                var strlen = int.Parse(element[1..]);
                var str = arrResp[0];
                arrResp.RemoveAt(0);
                return (str, arrResp);
            case ':':
                // integer
                var integer = int.Parse(element[1..]);
                return (integer, arrResp);
            default:
                break;
        }
    }



    return (null, arrResp);
}



