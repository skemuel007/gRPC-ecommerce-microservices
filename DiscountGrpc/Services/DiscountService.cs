using DiscountGrpc.Data;
using DiscountGrpc.Protos;
using Grpc.Core;

namespace DiscountGrpc.Services;

public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(ILogger<DiscountService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<DiscountModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var discount = DiscountContext.Discounts.FirstOrDefault(d => d.Code == request.DiscountCode);

        if (discount is null)
        {
            /*throw new RpcException(new Status(StatusCode.NotFound,
                $"Discount code {request.DiscountCode} does not exists!"));*/
            _logger.LogInformation("Discount code {request.DiscountCode} does not exists!", request.DiscountCode);
            
            return Task.FromResult(new DiscountModel()
            {
                DiscountId = 0,
                Code = request.DiscountCode,
                Amount = 0,
            });
        }
        
        _logger.LogInformation("Discount is operation with discount code {discountCode} has amount {discountAmount}",
            discount.Code, discount.Amount);
        
        return Task.FromResult(new DiscountModel()
        {
            DiscountId = discount.DiscountId,
            Code = discount.Code,
            Amount = discount.Amount
        });
    }
}