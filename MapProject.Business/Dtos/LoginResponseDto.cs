namespace MapProject.Business.Dtos;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    /// <summary>Token'ın son geçerlilik anı (UTC). Frontend sayaç için kullanabilir.</summary>
    public DateTime ExpiresAt { get; set; }
}
