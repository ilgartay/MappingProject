using MapProject.Business.Dtos;
using MapProject.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using LocationEntity = MapProject.Entities.Location;

namespace MapProject.Business.Services;

public class LocationService : ILocationService
{
    private readonly AppDbContext _context;

    public LocationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LocationDto>> GetAllAsync()
    {
        // Select veritabanı tarafında çalışır: tüm Point nesnelerini
        // belleğe çekmek yerine sadece X/Y değerlerini okuyoruz.
        return await _context.Locations
            .AsNoTracking()
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Latitude = l.Coordinates.Y,
                Longitude = l.Coordinates.X
            })
            .ToListAsync();
    }

    public async Task<LocationDto> CreateAsync(LocationCreateDto dto)
    {
        var location = new LocationEntity
        {
            Name = dto.Name,
            // DİKKAT: Point(x, y) yani önce boylam, sonra enlem.
            // Ters yazılırsa nokta Türkiye yerine okyanusta çıkar.
            // SRID 4326 verilmezse PostGIS 0 kabul eder ve mesafe hesapları bozulur.
            Coordinates = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 }
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        return new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
    }
}
