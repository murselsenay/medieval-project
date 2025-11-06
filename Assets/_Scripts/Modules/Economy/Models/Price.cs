using Modules.Economy.Enums;

namespace Modules.Economy.Models
{
    public class Price
    {
        public ECurrencyType Currency { get; set; }
        public int Amount { get; set; }
        public Price() { }
        public Price(ECurrencyType currency, int amount)
        {
            Currency = currency;
            Amount = amount;
        }
    }
}
