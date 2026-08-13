using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

public interface IFeatureService
{
    Task<FeatureCollectionDto> GetAllAsync();

    Task<FeatureDto> CreatePointAsync(FeatureCreateDto dto);
    Task<FeatureDto> CreateLineAsync(FeatureCreateDto dto);
    Task<FeatureDto> CreatePolygonAsync(FeatureCreateDto dto);

    /// <summary>Kayıt bulunup silindiyse true, zaten yoksa false döner.</summary>
    Task<bool> DeletePointAsync(int id);
    Task<bool> DeleteLineAsync(int id);
    Task<bool> DeletePolygonAsync(int id);
}

/// <summary>
/// WKT metni bozuk ya da beklenen geometri tipinde değilse fırlatılır.
/// Controller bunu 400'e çeviriyor.
/// </summary>
public class InvalidGeometryException : Exception
{
    public InvalidGeometryException(string message) : base(message)
    {
    }
}
