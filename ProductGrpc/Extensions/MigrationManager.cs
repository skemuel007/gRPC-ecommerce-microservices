using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductGrpc.Data;

namespace ProductGrpc.Extensions
{
    public static class MigrationManager
    {
        public static WebApplication MigrateDatabase<TContext>(this WebApplication app, Logger<TContext> logger) where TContext : ProductsContext
        {
            using(var scope = app.Services.CreateScope())
            {
                using(var context = scope.ServiceProvider.GetRequiredService<TContext>())
                {
                    try
                    {
                        /*logger.LogInformation("Database migration started");
                        context.Database.Migrate();
                        logger.LogInformation("Database migration completed");*/

                        logger.LogInformation("Database seeding started");
                        ProductsContextSeed.SeedAsync(context);
                        logger.LogInformation("Database seeding completed");

                    } catch(Exception ex)
                    {
                        logger.LogError($"{ex.Message}", ex);
                        throw;
                    }
                }
            }

            return app;
        }
    }
}
