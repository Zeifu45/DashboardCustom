using DashboardCustom.Server.Security;
using DashboardCustom.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace DashboardCustom.Server.Controllers
{
    [ApiController]
    [Route("api/audio")]
    [ApiKey]
    public class AudioController(AudioService audioService) : ControllerBase
    {
        [HttpGet("current")]
        public IActionResult GetCurrentAudioDevice()
        {
            var device = audioService.GetCurrentAudioDevice();
            return Ok(device);
        }
        [HttpPost("set")]
        public IActionResult SetDefaultDevice([FromQuery] string name)
        {
            var success = audioService.SetDefaultDevice(name);
            return success ? Ok(new { message = $"Sortie : {name}" }) : BadRequest(new { error = "Périphérique introuvable" });
        }
        [HttpPost("volume")]
        public IActionResult SetVolume([FromQuery] int volume)
        {
            if (volume is < 0 or > 100)
                return BadRequest(new { error = "Volume invalide" });
            audioService.SetVolume(volume);
            return Ok(new { volume });
        }
    }
}