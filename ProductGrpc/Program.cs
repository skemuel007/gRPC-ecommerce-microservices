using Microsoft.EntityFrameworkCore;
using ProductGrpc.Data;
using ProductGrpc.Extensions;
using ProductGrpc.Services;

namespace ProductGrpc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

            // Add services to the container.
            builder.Services.AddGrpc();
            builder.Services.AddDbContext<ProductsContext>(options => options.UseInMemoryDatabase(databaseName: "Products"));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<ProductService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            ILogger<ProductsContext> logger = app.Services.GetRequiredService<ILogger<ProductsContext>>();
            app.MigrateDatabase<ProductsContext>((Logger<ProductsContext>)logger).Run();
        }

    }
}