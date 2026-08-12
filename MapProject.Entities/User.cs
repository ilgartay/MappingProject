namespace MapProject.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    // Şifrenin kendisi asla saklanmaz, sadece BCrypt hash'i tutulur.
    public string PasswordHash { get; set; } = string.Empty;
}
