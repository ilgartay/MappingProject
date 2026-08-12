using MapProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using LocationEntity = MapProject.Entities.Location;

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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var locations = await _context.Locations
            .Select(l => new
            {
                l.Id,
                l.Name,
                Latitude = l.Coordinates.Y,
                Longitude = l.Coordinates.X
            })
            .ToListAsync();

        return Ok(locations);
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