using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

public interface ILocationService
{
    Task<IReadOnlyList<LocationDto>> GetAllAsync();
    Task<LocationDto> CreateAsync(LocationCreateDto dto);
}
