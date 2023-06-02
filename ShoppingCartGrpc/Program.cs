using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using ShoppingCartGrpc.Data;
using ShoppingCartGrpc.Extensions;
using ShoppingCartGrpc.Services;

// using ShoppingCartGrpc.Services;

var builder = WebApplication.CreateBuilder(args);

bool isMacOS = RuntimeInformation
    .IsOSPlatform(OSPlatform.OSX);

if (isMacOS)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Setup a HTTP/2 endpoint without TLS.
        options.ListenLocalhost(5013, o => o.Protocols =
            HttpProtocols.Http2);
    });
}


// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddDbContext<ShoppingCartContext>(options =>
{
    options.UseInMemoryDatabase("ShoppingCart");
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ShoppingCartService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.SeedDatabase();
app.Run();