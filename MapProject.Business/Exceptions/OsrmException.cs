namespace MapProject.Business.Exceptions;

/// <summary>
/// OSRM'e ulaşılamadığında ya da beklenmedik bir cevap döndüğünde atılır.
///
/// GeoServerException'la aynı gerekçe: rota sunucusu Docker'da ayrı bir
/// süreç ve kapalı olması bizim kodumuzun hatası değil. Controller bunu
/// 503'e çeviriyor, kullanıcı da "rota servisi kapalı" mesajını görüp
/// tekrar denemeyi seçebiliyor.
/// </summary>
public class OsrmException : Exception
{
    public OsrmException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
