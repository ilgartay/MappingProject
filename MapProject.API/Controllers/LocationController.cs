using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Geçerli JWT olmadan bu controller'ın hiçbir action'ına erişilemez.
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _locationService.GetAllAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(LocationCreateDto dto)
    {
        var created = await _locationService.CreateAsync(dto);

        // 201 + yeni kaydın adresi: REST'te doğru olan bu, düz 200 değil.
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }
}
