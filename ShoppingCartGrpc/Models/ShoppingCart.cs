namespace ShoppingCartGrpc.Models;

public class ShoppingCart
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public List<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();

    public ShoppingCart()
    {
        
    }

    public ShoppingCart(string userName)
    {
        
    }

    public float TotalPrice
    {
        get
        {
            float totalPrice = 0;
            foreach (var item in Items)
            {
                totalPrice += item.Price * item.Quantity;
            }

            return totalPrice;
        }
    }
}