namespace EasyGameProxy
{
    public interface IRouteConfigService
    {
        List<RouteEntry> GetRoutes();
        Task AddRouteAsync(RouteEntry entry);
        Task DeleteRouteAsync(string hostname);
        Task SaveAsync();
    }
}
