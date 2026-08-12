using MapProject.Data;
using LocationEntity = MapProject.Entities.Location;
using MapProject.Entities;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly AppDbContext _context;

    public LocationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(LocationCreateDto dto)
    {
        var location = new LocationEntity
        {
            Name = dto.Name,
            Coordinates = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 }
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        return Ok(new { location.Id, location.Name });
    }
}

public class LocationCreateDto
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}