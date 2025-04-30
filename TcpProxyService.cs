using System.Net.Sockets;
using System.Net;

namespace EasyGameProxy
{
    public class TcpProxyService : BackgroundService
    {
        private readonly IRouteConfigService _routeService;
        private FileSystemWatcher? _watcher;
        private TcpProxy? _proxy;

        public TcpProxyService(IRouteConfigService routeService)
        {
            _routeService = routeService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var routes = _routeService.GetRoutes();
            var ports = routes.Select(r => r.ListenPort).Distinct();
            _proxy = new TcpProxy(_routeService);
            StartWatcher();

            foreach (var port in ports)
            {
                _ = Task.Run(() => _proxy.StartAsync(port, stoppingToken));
            }
        }


        private void StartWatcher()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "routes.json");
            var dir = Path.GetDirectoryName(configPath)!;

            _watcher = new FileSystemWatcher(dir, "routes.json")
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            _watcher.Changed += async (_, __) =>
            {
                Console.WriteLine("[INFO] Config file changed. Reloading...");
                await _proxy!.ReloadRoutesAsync();
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            _watcher?.Dispose();
        }
    }
}
