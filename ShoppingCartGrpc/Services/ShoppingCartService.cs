using AutoMapper;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ShoppingCartGrpc.Data;
using ShoppingCartGrpc.Models;
using ShoppingCartGrpc.Protos;

namespace ShoppingCartGrpc.Services;

public class ShoppingCartService : ShoppingCartGrpcProtoService.ShoppingCartGrpcProtoServiceBase
{
    private readonly ShoppingCartContext _shoppingCartContext;
    private readonly ILogger<ShoppingCartService> _logger;
    private readonly IMapper _mapper;

    public ShoppingCartService(ShoppingCartContext shoppingCartContext,
        ILogger<ShoppingCartService> logger,
        IMapper mapper)
    {
        _shoppingCartContext = shoppingCartContext ?? throw new ArgumentNullException(nameof(shoppingCartContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public override async Task<ShoppingCartModel> GetShoppingCart(GetShoppingCartRequest request, ServerCallContext context)
    {
        var shoppingCart = await _shoppingCartContext.ShoppingCart
            .FirstOrDefaultAsync(s => s.UserName == request.Username);
        if (shoppingCart is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"ShoppingCart with Username={request.Username} not found"));
        }

        var shoppingCartModel = _mapper.Map<ShoppingCartModel>(shoppingCart);
        return shoppingCartModel;
    }

    public override async Task<ShoppingCartModel> CreateShoppingCart(ShoppingCartModel request, ServerCallContext context)
    {
        var shoppingCart = _mapper.Map<ShoppingCart>(request);

        var exists = await _shoppingCartContext.ShoppingCart.AnyAsync(s => s.UserName == shoppingCart.UserName);
        if (exists)
        {
            _logger.LogError("Invalid username for ShoppingCart creation. Username: {userName}", shoppingCart.UserName);
            throw new RpcException(new Status(StatusCode.NotFound,
                $"ShoppingCart with UserName={request.Username} already exists"));
        }

        _shoppingCartContext.ShoppingCart.Add(shoppingCart);
        await _shoppingCartContext.SaveChangesAsync();
        
        _logger.LogInformation("ShoppingCart is successfully created, Username: {userName}", shoppingCart.UserName);
        
        var shoppingCartModel = _mapper.Map<ShoppingCartModel>(shoppingCart);
        return shoppingCartModel;
    }

    public override async Task<RemoveItemFromShoppingCartResponse> RemoveItemFromShoppingCart(RemoveItemFromShoppingCartRequest request, ServerCallContext context)
    {
        var shoppingCart = await _shoppingCartContext.ShoppingCart.SingleOrDefaultAsync(s => s.UserName == request.Username);
        if (shoppingCart is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"ShoppingCart with UserName={request.Username} does not exist."));
        }

        var removeCartItem = shoppingCart.Items.SingleOrDefault(i => i.ProductId == request.RemoveCartItem.ProductId);
        if (null == removeCartItem)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"CartItem with ProductId={request.RemoveCartItem.ProductId} is not found in the ShoppingCart."));
        }

        shoppingCart.Items.Remove(removeCartItem);

        var removeCount = await _shoppingCartContext.SaveChangesAsync();

        var response = new RemoveItemFromShoppingCartResponse
        {
            Success = removeCount > 0
        };

        return response;           
    }

    public override async Task<AddItemIntoShoppingCartResponse> AddItemIntoShoppingCart(IAsyncStreamReader<AddItemIntoShoppingCartRequest> requestStream, ServerCallContext context)
    {
        while (await requestStream.MoveNext())
        {
            var shoppingCart = await _shoppingCartContext.ShoppingCart.SingleOrDefaultAsync(s => s.UserName == requestStream.Current.Username);
            if (shoppingCart is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"ShoppingCart with UserName={requestStream.Current.Username} does not exist."));
            }

            var newlyAddedCartItem = _mapper.Map<ShoppingCartItem>(requestStream.Current.NewCartItem);
            var cartItem = shoppingCart.Items.SingleOrDefault(i => i.ProductId == newlyAddedCartItem.ProductId);
            if (cartItem is not null)
            {
                cartItem.Quantity++;
            }
            else
            {
                // grpc call discount services - check and calculate 
                float discount = 100;
                newlyAddedCartItem.Price -= discount;
                shoppingCart.Items.Add(newlyAddedCartItem);
            }
            
        }

        var insertCount = await _shoppingCartContext.SaveChangesAsync();
        var response = new AddItemIntoShoppingCartResponse()
        {
            Success = insertCount > 0,
            InsertCount = insertCount
        };
        return response;
    }
}