using System.Net;
using System.Runtime.InteropServices;
using DiscountGrpc.Protos;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
        options.ListenLocalhost(5002, o =>
        {
            o.Protocols =
                HttpProtocols.Http2;
            // o.UseHttps();
        });
        
        /*var (httpPort, grpcPort) = GetDefinedPorts(builder.Configuration);

        options.Listen(IPAddress.Any, httpPort, listenOptions =>
        {
            // listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.Protocols = HttpProtocols.Http2;
            listenOptions.UseHttps();
        });*/

        /*options.Listen(IPAddress.Any, grpcPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });*/
        
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
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>(
    options => options.Address = new Uri(builder.Configuration["GrpcConfigs:DiscountUrl"]));

builder.Services.AddScoped<DiscountService>();
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:5005";
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateAudience = false,
        };
        options.RequireHttpsMetadata = false; // disables https for the purpose of testing
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapGrpcService<ShoppingCartService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.SeedDatabase();
app.Run();

static (int httpPort, int grpcPort) GetDefinedPorts(IConfiguration configuration)
{
    var grpcPort = configuration.GetValue("GRPC_PORT", 7002);
    var port = configuration.GetValue("PORT", 5002);

    return (port, grpcPort);
}