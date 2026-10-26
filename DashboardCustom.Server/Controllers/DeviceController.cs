using DashboardCustom.Server.Security;
using DashboardCustom.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace DashboardCustom.Server.Controllers;

[ApiController]
[Route("api/device")]
[ApiKey]
public class DeviceController(DeviceService deviceService) : ControllerBase
{
    [HttpGet("keyboard-battery")]
    public async Task<IActionResult> GetKeyboardBattery()
    {
        var level = await deviceService.GetKeyboardBatteryAsync();
        return Ok(new { level });
    }
    [HttpGet("mouse-battery")]
    public async Task<IActionResult> GetMouseBattery()
    {
        var level = await deviceService.GetMouseBatteryAsync();
        return Ok(new { level });
    }
    [HttpGet("xboxcontroller-battery")]
    public async Task<IActionResult> GetXboxControllerBattery()
    {
        var level = await deviceService.GetXboxControllerBatteryAsync();
        return Ok(new { level });
    }
}