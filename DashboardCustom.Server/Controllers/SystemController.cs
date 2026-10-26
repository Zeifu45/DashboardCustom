using DashboardCustom.Server.Security;
using DashboardCustom.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace DashboardCustom.Server.Controllers;

[ApiController]
[Route("api/system")]
[ApiKey]
public class SystemController(SystemService systemService) : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetSystemMetrics()
    {
        var metrics = systemService.GetSystemMetrics();
        return Ok(metrics);
    }
    [HttpPost("restart-explorer")]
    public IActionResult RestartExplorer()
    {
        systemService.RestartExplorer();
        return Ok(new { message = "Explorer redémarré" });
    }
    [HttpPost("launch")]
    public IActionResult LaunchApplication([FromQuery] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { error = "La clé de l'application est requise." });
        }
        var success = systemService.StartApplicationByKey(key);
        return success ? Ok(new { message = $"Application '{key}' lancée avec succès." }) : BadRequest(new { error = $"Application introuvable ou non autorisée pour la clé : {key}" });
    }
}