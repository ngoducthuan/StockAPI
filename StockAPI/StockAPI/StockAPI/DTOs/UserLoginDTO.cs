namespace StockAPI.DTOs
{
    public class UserLoginDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class User
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }

    public class UserInfo
    {
        public int user_id { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public DateTime? birth { get; set; }
        public string? organization { get; set; }
        public string? location { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int? remaining_balance { get; set; }
        public int? money_spent { get; set; }
    }
}
