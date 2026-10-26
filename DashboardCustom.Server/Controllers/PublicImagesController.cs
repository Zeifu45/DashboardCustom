using DashboardCustom.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace DashboardCustom.Server.Controllers;

[ApiController]
[Route("api/public/images")]
public class PublicImagesController(DataService dataService) : ControllerBase
{
    [HttpGet("{fileName}")]
    public IActionResult GetImage(string fileName)
    {
        var filePath = dataService.GetImagePath(fileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };
        return PhysicalFile(filePath, contentType);
    }
}