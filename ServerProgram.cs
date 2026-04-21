using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.IO;

namespace server2;

public class ClientInfo
{
    public string remoteendpoint { get; set; }
    public string username { get; set; }
    public bool isOnline { get; set; }
}

public class Program
{
    static Dictionary<string, TcpClient> activeSockets = new();
    static string jsonPath = "users.json";

    static void Main()
    {
        var listener = new TcpListener(IPAddress.Any, 27001);
        listener.Start();
        Console.WriteLine("Server started...");

        while (true)
        {
            var client = listener.AcceptTcpClient();
            Task.Run(() => HandleClient(client));
        }
    }

    static void HandleClient(TcpClient tcp)
    {
        var br = new BinaryReader(tcp.GetStream());
        try
        {
        
            string jsonInput = br.ReadString();
            var newUser = JsonSerializer.Deserialize<ClientInfo>(jsonInput);
            newUser.remoteendpoint = tcp.Client.RemoteEndPoint.ToString();
            newUser.isOnline = true;

     
            SaveAndSyncUser(newUser, tcp);

            while (true)
            {
                var msg = br.ReadString();
                if (msg.Contains('|'))
                {
                    var p = msg.Split('|');
                    SendPrivate(p[0], $"[{newUser.username}]: {p[1]}");
                }
            }
        }
        catch { }
    }

    static void SaveAndSyncUser(ClientInfo user, TcpClient socket)
    {
        lock (activeSockets)
        {
            activeSockets[user.username] = socket;

            List<ClientInfo> allUsers = new();
            if (File.Exists(jsonPath))
            {
                string existingJson = File.ReadAllText(jsonPath);
                allUsers = JsonSerializer.Deserialize<List<ClientInfo>>(existingJson) ?? new();
            }

      
            var found = allUsers.FirstOrDefault(u => u.username == user.username);
            if (found == null) allUsers.Add(user);
            else found.isOnline = true;

           
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(allUsers));

            string broadcastData = "LIST|" + JsonSerializer.Serialize(allUsers);
            foreach (var s in activeSockets.Values)
            {
                try { new BinaryWriter(s.GetStream()).Write(broadcastData); } catch { }
            }
        }
    }

    static void SendPrivate(string name, string msg)
    {
        if (activeSockets.ContainsKey(name))
        {
            try { new BinaryWriter(activeSockets[name].GetStream()).Write("MSG|" + msg); } catch { }
        }
    }
}
