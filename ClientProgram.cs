using System.Net.Sockets;
using System.Text.Json;

namespace client2;

public class ClientInfo
{
    public string remoteendpoint { get; set; }
    public string username { get; set; }
    public bool isOnline { get; set; }
}

public class Program
{
    static void Main()
    {
        var client = new TcpClient("127.0.0.1", 27001);
        var bw = new BinaryWriter(client.GetStream());
        var br = new BinaryReader(client.GetStream());

        Console.Write("Username: ");
        string name = Console.ReadLine();
        var info = new ClientInfo { username = name, isOnline = true };
        bw.Write(JsonSerializer.Serialize(info));

        Task.Run(() => {
            while (true)
            {
                try
                {
                    string data = br.ReadString();
                    if (data.StartsWith("LIST|"))
                    {
                        var list = JsonSerializer.Deserialize<List<ClientInfo>>(data.Substring(5));
                        Console.Clear();
                        Console.WriteLine("=== ALL USERS ===");
                        foreach (var u in list)
                            Console.WriteLine($"{u.username} | {u.remoteendpoint} | {(u.isOnline ? "Online" : "Offline")}");
                        Console.Write("\nInput: ");
                    }
                    else if (data.StartsWith("MSG|")) Console.WriteLine($"\n{data.Substring(4)}\nInput: ");
                }
                catch { break; }
            }
        });

        while (true)
        {
            string txt = Console.ReadLine();
            if (!string.IsNullOrEmpty(txt)) bw.Write(txt);
        }
    }
}
