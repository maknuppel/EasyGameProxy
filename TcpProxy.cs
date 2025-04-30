using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace EasyGameProxy
{
    public class TcpProxy
    {
        private readonly IRouteConfigService _routeService;
        private Dictionary<string, RouteEntry> _routes = new();

        public TcpProxy(IRouteConfigService routeService)
        {
            _routeService = routeService;
            LoadRoutes();

            Console.WriteLine($"[INFO] Easy Game Proxy started at {DateTime.Now}");
            foreach (var x in _routes)
            {
                Console.WriteLine($"[INFO] Configuration found: {JsonSerializer.Serialize(x.Value)}");
            }
        }

        private void LoadRoutes()
        {
            _routes = _routeService.GetRoutes().ToDictionary(r => r.Hostname, r => r);
        }

        public Task ReloadRoutesAsync()
        {
            LoadRoutes();
            Console.WriteLine($"[INFO] Reloaded {_routes.Count} routes.");
            Console.WriteLine($"[INFO] Current Routes:");
            foreach (var x in _routes)
            {
                Console.WriteLine($"[INFO] {JsonSerializer.Serialize(x.Value)}");
            }
            return Task.CompletedTask;
        }

        public async Task StartAsync(int port, CancellationToken cancellationToken)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"[INFO] Easy Game proxy started listening on port {port}");

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            }
        }

        public async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var networkStream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                client.Close();
                return;
            }

            string hostname = ExtractDestinationHostname(buffer, bytesRead);
            Console.WriteLine($"[REQUEST] Hostname requested: {hostname}");
            if (!_routes.TryGetValue(hostname, out var route))
            {
                Console.WriteLine($"[WARN] No route for hostname: {hostname}");
                client.Close();
                return;
            }

            using var server = new TcpClient();
            try
            {
                await server.ConnectAsync(route.Host, route.Port, token);
            }
            catch
            {
                Console.WriteLine("[ERROR] Failed to connect to backend.");
                client.Close();
                return;
            }

            var serverStream = server.GetStream();
            await serverStream.WriteAsync(buffer, 0, bytesRead);

            var clientToServer = networkStream.CopyToAsync(serverStream, token);
            var serverToClient = serverStream.CopyToAsync(networkStream, token);

            await Task.WhenAny(clientToServer, serverToClient);
        }

        private string ExtractDestinationHostname(byte[] data, int length)
        {
            try
            {
                int index = 0;
                ReadVarInt(data, ref index); // length
                ReadVarInt(data, ref index); // packet ID
                ReadVarInt(data, ref index); // protocol version

                int hostLength = ReadVarInt(data, ref index);
                string host = Encoding.UTF8.GetString(data, index, hostLength);
                return host;
            }
            catch
            {
                return string.Empty;
            }
        }

        private int ReadVarInt(byte[] buffer, ref int index)
        {
            int value = 0;
            int position = 0;
            byte currentByte;

            do
            {
                currentByte = buffer[index++];
                value |= (currentByte & 0x7F) << position;
                position += 7;
                if (position > 35)
                    throw new Exception("VarInt too big");
            } while ((currentByte & 0x80) != 0);

            return value;
        }
    }
}
