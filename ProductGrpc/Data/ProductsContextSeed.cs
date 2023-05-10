using Microsoft.EntityFrameworkCore;
using ProductGrpc.Models;

namespace ProductGrpc.Data
{
    public class ProductsContextSeed
    {
        public static  void SeedAsync(ProductsContext context)
        {
            var isAvailableProducts = context.Product.Any();
            if (!isAvailableProducts)
            {
                var products = new List<Product> { 
                    new Product { 
                        ProductId = 1, 
                        Name = "Mi10T", 
                        Description = "New Xiaomi Phone Mi10T", 
                        Price = 699, 
                        Status = ProductStatus.INSTOCK, 
                        CreatedTime = DateTime.UtcNow 
                    }, 
                    new Product { 
                        ProductId = 2, 
                        Name = "P40", 
                        Description = "New Huawei Phone P40", 
                        Price = 899, 
                        Status = ProductGrpc.Models.ProductStatus.INSTOCK, 
                        CreatedTime = DateTime.UtcNow 
                    }, 
                    new Product { 
                        ProductId = 3, 
                        Name = "A50", 
                        Description = "New Samsung Phone A50", 
                        Price = 399, 
                        Status = ProductGrpc.Models.ProductStatus.INSTOCK, 
                        CreatedTime = DateTime.UtcNow 
                    } 
                }; 

                context.Product.AddRange(products); 
                context.SaveChanges();
            }
        }
    }
}
