namespace StockAPI.DTOs
{
    public class UserFinanceDTO
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public decimal remaining_balance { get; set; }
        public decimal money_spent { get; set; }
    }
}
