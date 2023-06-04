using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using IdentityModel.Client;
using ProductGrpc.Protos;
using ShoppingCartGrpc.Protos;

namespace ShoppingCartWorkService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            HttpClient.DefaultProxy = new WebProxy(); // Note: ensure package matches to avoid errors
            var shoppingCartServerUrl = _configuration.GetValue<string>("WorkerService:ShoppingCartServerUrl");
            using var scChannel =
                GrpcChannel.ForAddress(shoppingCartServerUrl);

            var scClient = new ShoppingCartGrpcProtoService.ShoppingCartGrpcProtoServiceClient(scChannel);

            var token = await GetTokenFromIS4();
            
            var scModel = await GetOrCreateShoppingCartAsync(scClient, token);

            using var scClientStream = scClient.AddItemIntoShoppingCart();
            
            var productServerUrl = _configuration.GetValue<string>("WorkerService:ProductServerUrl");
            using var productChannel =
                GrpcChannel.ForAddress(productServerUrl);

            var productClient = new ProductProtoService.ProductProtoServiceClient(productChannel);
            
            _logger.LogInformation("GetAllProducts started...");
            using var clientData = productClient.GetAllProducts(new GetAllProductsRequest());
            await foreach (var responseData in clientData.ResponseStream.ReadAllAsync())
            {
                _logger.LogInformation("GetAllProducts Stream Response: {responseData}", responseData);
                //3 Add sc items into SC with client stream
                var addNewScItem = new AddItemIntoShoppingCartRequest
                {
                    Username = _configuration.GetValue<string>("WorkerService:Username"),
                    DiscountCode = "CODE_100",
                    NewCartItem = new ShoppingCartItemModel
                    {
                        ProductId = responseData.ProductId,
                        ProductName = responseData.Name,
                        Price = responseData.Price,
                        Color = "Black",
                        Quantity = 1
                    }
                };

                await scClientStream.RequestStream.WriteAsync(addNewScItem);
                _logger.LogInformation("ShoppingCart Client Stream Added New Item : {addNewScItem}", addNewScItem);
            }
            await scClientStream.RequestStream.CompleteAsync();

            var addItemIntoShoppingCartResponse = await scClientStream;                
            _logger.LogInformation("AddItemIntoShoppingCart Client Stream Response: {addItemIntoShoppingCartResponse}", addItemIntoShoppingCartResponse);

            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(_configuration.GetValue<int>("WorkerService:TaskInterval"), stoppingToken);
        }
    }

    private async Task<ShoppingCartModel> GetOrCreateShoppingCartAsync(
        ShoppingCartGrpcProtoService.ShoppingCartGrpcProtoServiceClient client, string token)
    {
        ShoppingCartModel shoppingCartModel = default;
        
        var headers = new Metadata();
        headers.Add("Authorization", $"Bearer {token}");

        try
        {
            _logger.LogInformation($"GetShoppingCartAsync started...");

            shoppingCartModel = await client.GetShoppingCartAsync(new GetShoppingCartRequest()
            {
                Username = _configuration.GetValue<string>("WorkerService:Username")
            }, headers);

            _logger.LogInformation($"GetShoppingCartAsync response: {shoppingCartModel}", shoppingCartModel);
        }
        catch (RpcException ex)
        {
            if (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogInformation($"CreateShoppingCartAsync started...");
                shoppingCartModel = await client.CreateShoppingCartAsync(new ShoppingCartModel()
                {
                    Username = _configuration.GetValue<string>("WorkerService:Username")
                }, headers);
                
                _logger.LogInformation($"CreateShoppingCartAsync response: {shoppingCartModel}", shoppingCartModel);

            } else 
                throw;
        }

        return shoppingCartModel;
    }

    public async Task<string> GetTokenFromIS4()
    {
        var client = new HttpClient();
        var identityServerUrl = _configuration.GetValue<string>("WorkerService:IdentityServerUrl");
        var discoveryDocumentResponse =
            await client.GetDiscoveryDocumentAsync(identityServerUrl);
        if (discoveryDocumentResponse.IsError)
        {
            _logger.LogInformation("Error discovering document {discoveryDocumentResponse.Error}", discoveryDocumentResponse.Error);
            return string.Empty;
        }

        var tokenResponse = await client.RequestClientCredentialsTokenAsync(
            new ClientCredentialsTokenRequest()
            {
                Address = discoveryDocumentResponse.TokenEndpoint,
                ClientId = "ShoppingCartClient",
                ClientSecret = "secret",
                Scope = "ShoppingCartAPI"
            });

        if (tokenResponse.IsError)
        {
            _logger.LogInformation("Error fetching token {tokenResponse.Error}", tokenResponse.Error);
            return string.Empty;
        }

        return tokenResponse.AccessToken;

    }
}