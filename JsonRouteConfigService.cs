using System.Text.Json;

namespace EasyGameProxy
{
    public class JsonRouteConfigService : IRouteConfigService
    {
        private readonly string _filePath = Path.Combine(AppContext.BaseDirectory, "routes.json");
        private readonly List<RouteEntry> _routes = new();

        public JsonRouteConfigService()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var root = JsonSerializer.Deserialize<RoutesConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (root?.Routes != null)
                    _routes = root.Routes;
            }
        }

        public List<RouteEntry> GetRoutes() => _routes.ToList();

        public Task AddRouteAsync(RouteEntry entry)
        {
            _routes.Add(entry);
            return SaveAsync();
        }

        public Task DeleteRouteAsync(string hostname)
        {
            _routes.RemoveAll(r => r.Hostname == hostname);
            return SaveAsync();
        }

        public Task SaveAsync()
        {
            var json = JsonSerializer.Serialize(new RoutesConfig { Routes = _routes }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
            return Task.CompletedTask;
        }

        private class RoutesConfig
        {
            public List<RouteEntry> Routes { get; set; } = new();
        }
    }

}
