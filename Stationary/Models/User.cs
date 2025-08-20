namespace Stationary.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }  // Hash in real app
        public string Role { get; set; }
    }
}
