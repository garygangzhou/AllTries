using TheSampleApi.Data;

namespace TheSampleApi.Startup;

public static class DependenciesConfig
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApiServices();
        builder.Services.AddTransient<CourseData>();
        builder.Services.AddCorsServices();
        builder.Services.AddAllHealthCheckServices();
    }
}
