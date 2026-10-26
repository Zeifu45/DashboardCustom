using DashboardCustom.Server.Security;
using DashboardCustom.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace DashboardCustom.Server.Controllers;

[ApiController]
[Route("api/light")]
[ApiKey]
public class LightController(LightService lightService) : ControllerBase
{
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent([FromQuery] string? ip, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(ip))
        {
            await lightService.ConnectAsync(ip, ct);
        }
        var info = await lightService.GetStateAsync(ct);
        if (info == null)
        {
            return Ok(new { poweredOn = false, brightness = 0, r = 0, g = 0, b = 0, nameSelected = "" });
        }
        return Ok(new
        {
            poweredOn = info.PoweredOn,
            brightness = info.Brightness,
            r = info.R,
            g = info.G,
            b = info.B,
            nameSelected = info.NameSelected
        });
    }
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(CancellationToken ct)
    {
        await lightService.ToggleAsync(ct);
        return Ok();
    }
    [HttpPost("brightness")]
    public async Task<IActionResult> SetBrightness([FromQuery] int value, CancellationToken ct)
    {
        await lightService.SetBrightnessAsync(value, ct);
        return Ok();
    }
    [HttpPost("color")]
    public async Task<IActionResult> SetColor([FromQuery] int r, [FromQuery] int g, [FromQuery] int b, CancellationToken ct)
    {
        await lightService.SetColorAsync(r, g, b, ct);
        return Ok();
    }
}