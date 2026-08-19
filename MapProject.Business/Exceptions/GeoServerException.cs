namespace MapProject.Business.Exceptions;

/// <summary>
/// GeoServer'a ulaşılamadığında ya da beklenmedik bir cevap döndüğünde atılır.
///
/// Ayrı bir tip olması şunun için: veri artık dış bir servisten geliyor ve
/// o servis kapalıyken bu bizim kodumuzun hatası değil. Controller bunu
/// 503'e çevirip kullanıcıya "harita servisi şu an kapalı" diyebiliyor;
/// düz bir 500 "uygulama bozuldu" izlenimi verirdi.
/// </summary>
public class GeoServerException : Exception
{
    public GeoServerException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
