namespace ReactwithDotnetCore.Model
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public DateTime DateOfJoin { get; set; } = DateTime.Now;
    }
}
