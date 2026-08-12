namespace MapProject.Business.Settings;

/// <summary>
/// appsettings.json içindeki "Jwt" bölümünün karşılığı.
/// Program.cs'te Configure&lt;JwtSettings&gt;() ile bağlanıyor.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Token'ı imzalayan anahtar. HMAC-SHA256 için en az 32 karakter olmalı.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Token'ı kim üretti (bizim API).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Token kimin için üretildi (bizim React uygulaması).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Token kaç dakika geçerli. Görev gereği 5-10 dakika.</summary>
    public int ExpiryMinutes { get; set; } = 10;
}
