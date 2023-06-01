using ShoppingCartGrpc.Data;

namespace ShoppingCartGrpc.Extensions;

public static class DatabaseInitializerExtension
{
    public static IApplicationBuilder SeedDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var shoppingCartContext = services.GetRequiredService<ShoppingCartContext>();
        ShoppingCartContextSeed.SeedAsync(shoppingCartContext);

        return app;
    }
}