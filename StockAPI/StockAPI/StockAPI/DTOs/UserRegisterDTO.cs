namespace StockAPI.DTOs
{
    public class UserRegisterDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? Birth { get; set; }
        public string? Organization { get; set; }
        public string? Location { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
