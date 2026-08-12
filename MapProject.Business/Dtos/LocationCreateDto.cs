using System.ComponentModel.DataAnnotations;

namespace MapProject.Business.Dtos;

public class LocationCreateDto
{
    [Required(ErrorMessage = "Konum adı zorunludur.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalıdır.")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalıdır.")]
    public double Longitude { get; set; }
}
