using Scalar.AspNetCore;

namespace TheSampleApi.Startup;

public static class CorsConfig
{
    private const string AllAllCorsPolicyName = "AllowAllOrigins";
    public static void AddCorsServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(AllAllCorsPolicyName,
                policy =>
                {
                    policy.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
        });
    }
    public static void UseCorsConfig(this WebApplication app)
    {
        app.UseCors(AllAllCorsPolicyName);
    }
}
