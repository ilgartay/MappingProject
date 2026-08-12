namespace MapProject.Business.Dtos;

/// <summary>
/// Dışarıya dönen konum modeli. Entity'yi doğrudan döndürmüyoruz:
/// NetTopologySuite'in Point tipi JSON'a çevrilirken istemcinin
/// ihtiyacı olmayan geometri detaylarını da taşır.
/// </summary>
public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
