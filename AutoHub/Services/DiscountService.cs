namespace AutoHub.API.Services
{
    public class DiscountService
    {
        public decimal ApplyDiscount(decimal amount)
        {
            if (amount > 5000)
            {
                return amount * 0.90m;
            }

            return amount;
        }
    }
}
