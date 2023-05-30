using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using ProductGrpc.Protos;

namespace ProductWorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly ProductFactory _factory;

    public Worker(ILogger<Worker> logger, IConfiguration config, 
        ProductFactory factory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            
            var interval = _config.GetValue<int>("WorkService:TaskInterval");
            var serverUrl = _config.GetValue<string>("WorkService:ServerUrl");
            
            try
            {
                using var channel = GrpcChannel.ForAddress($"{serverUrl}");
                var client = new ProductProtoService.ProductProtoServiceClient(channel);
                
                _logger.LogInformation("AddProductAsync started..");                    
                /*var addProductResponse = await client.AddProductAsync(new AddProductRequest()
                {
                    Product = new ProductModel
                    {
                        Name = "Test01",
                        Description = $"Test02_Description",
                        Price = new Random().Next(1000),
                        Status = ProductStatus.Instock,
                        CreatedTime = Timestamp.FromDateTime(DateTime.UtcNow)
                    }
                });*/
                var addProductResponse = await client.AddProductAsync(await _factory.Generate());
                _logger.LogInformation("AddProduct Response: {product}", addProductResponse.ToString());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);
                throw exception;
            }

            /*_logger.LogInformation("GetProductAsync Started...");
            using var channel = GrpcChannel.ForAddress($"{serverUrl}");
            var client = new ProductProtoService.ProductProtoServiceClient(channel);
            
            var response = await client.GetProductAsync(
                new GetProductRequest()
                {
                    ProductId = 1
                });
            
            _logger.LogInformation($"GetProduct Response {response}");*/

            await Task.Delay(interval, stoppingToken);
        }
    }
}