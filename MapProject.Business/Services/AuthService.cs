using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MapProject.Business.Dtos;
using MapProject.Business.Settings;
using MapProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MapProject.Business.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        // Kullanıcı yok VEYA şifre yanlış -> aynı sonuç (null).
        // Ayrı ayrı mesaj vermek "bu kullanıcı adı var" bilgisini sızdırırdı.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        return new LoginResponseDto
        {
            Token = GenerateToken(user.Id, user.Username, expiresAt),
            Username = user.Username,
            ExpiresAt = expiresAt
        };
    }

    private string GenerateToken(int userId, string username, DateTime expiresAt)
    {
        // Claim = token'ın içine gömülen kullanıcı bilgileri.
        // Bunlar şifreli değil, sadece imzalı: herkes okuyabilir ama kimse değiştiremez.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            // Jti: token'a özel rastgele kimlik (aynı saniyede iki token üretilse bile farklı olur).
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
