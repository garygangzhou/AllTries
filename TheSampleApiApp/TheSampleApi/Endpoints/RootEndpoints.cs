using Microsoft.AspNetCore.StaticAssets;

namespace TheSampleApi.Endpoints;

public static class RootEndpoints
{
    public static void AddRootEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => "Welcome to The Sample API!");
    }
}
