using DashboardCustom.Server.Model;
using DashboardCustom.Server.Security;
using DashboardCustom.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace DashboardCustom.Server.Controllers;

[ApiController]
[Route("api/data")]
[ApiKey]
public class DataController(DataService dataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllData()
    {
        await dataService.ProcessAndEnsureIconsAsync();
        var data = dataService.GetAllData();
        return Ok(data);
    }
    [HttpPost("category")]
    public IActionResult AddCategory([FromBody] Category category)
    {
        var created = dataService.AddCategory(category);
        return Ok(created);
    }
    [HttpPut("category")]
    public IActionResult UpdateCategory([FromBody] Category category)
    {
        return dataService.UpdateCategory(category) ? Ok() : NotFound();
    }
    [HttpDelete("category/{id}")]
    public IActionResult DeleteCategory(string id)
    {
        return dataService.DeleteCategory(id) ? Ok() : NotFound();
    }
    [HttpPost("link")]
    public async Task<IActionResult> AddLink([FromBody] LinkItem link)
    {
        var created = await dataService.AddLinkAsync(link);
        return Ok(created);
    }
    [HttpPut("link")]
    public async Task<IActionResult> UpdateLink([FromBody] LinkItem link)
    {
        return await dataService.UpdateLinkAsync(link) ? Ok() : NotFound();
    }
    [HttpDelete("link/{id}")]
    public IActionResult DeleteLink(string id)
    {
        return dataService.DeleteLink(id) ? Ok() : NotFound();
    }
    [HttpPost("app")]
    public IActionResult AddApp([FromBody] AppItem app)
    {
        var created = dataService.AddApp(app);
        return Ok(created);
    }
    [HttpPut("app")]
    public IActionResult UpdateApp([FromBody] AppItem app)
    {
        return dataService.UpdateApp(app) ? Ok() : NotFound();
    }

    [HttpDelete("app/{id}")]
    public IActionResult DeleteApp(string id)
    {
        return dataService.DeleteApp(id) ? Ok() : NotFound();
    }
    [HttpPost("upload-icon/{entityId}")]
    public async Task<IActionResult> UploadIcon(string entityId, [FromQuery] bool isApp, IFormFile file)
    {
        var iconName = await dataService.SaveCustomIconAsync(entityId, isApp, file);
        return iconName != null
            ? Ok(new { iconFileName = iconName })
            : BadRequest(new { error = "Format de fichier invalide ou échec du téléchargement" });
    }
}