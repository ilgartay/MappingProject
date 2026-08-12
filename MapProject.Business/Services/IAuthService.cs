using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

public interface IAuthService
{
    /// <summary>
    /// Kullanıcı adı/şifre doğruysa JWT üretir, yanlışsa null döner.
    /// Controller "null geldiyse 401" kararını verir.
    /// </summary>
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
}
