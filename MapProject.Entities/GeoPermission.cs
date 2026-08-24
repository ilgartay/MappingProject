using NetTopologySuite.Geometries;

namespace MapProject.Entities;

/// <summary>
/// Coğrafi yetki: bir kullanıcının veya rolün çizim yapabileceği alan.
/// UserId ya da RoleId'den yalnızca biri dolu olur.
///
/// Kullanıcının etkin alanı = kendi alanları + rollerinin alanları.
/// Hiç alan tanımlı değilse kısıt yok demektir; yoksa yetki tanımlamayı
/// unuttuğumuz herkes anında kilitlenirdi.
/// </summary>
public class GeoPermission : ITrackable
{
    public int Id { get; set; }

    /// <summary>Alanı tanımlayan açıklama, ör. "Ankara bölgesi".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ITrackable gereği; coğrafi yetkinin rengi anlamsız, kullanılmıyor.</summary>
    public string Color { get; set; } = "#009bff";

    /// <summary>
    /// EPSG:4326 alan. POLYGON ya da MULTIPOLYGON olabilir: elle çizilen
    /// alan tek parçadır, hazır bölgelerden birkaçı seçildiğinde ise
    /// birbirine değmeyen parçalar oluşur.
    /// </summary>
    public required Geometry Geometry { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public int? RoleId { get; set; }
    public Role? Role { get; set; }

    public int InsertedUserId { get; set; }
    public DateTime InsertedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
