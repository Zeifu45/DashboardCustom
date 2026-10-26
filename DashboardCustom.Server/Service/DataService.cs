using DashboardCustom.Server.Model;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DashboardCustom.Server.Service;

public class DataService
{
    private readonly string _dataDir;
    private readonly string _imgDir;
    private readonly string _categoriesPath;
    private readonly string _linksPath;
    private readonly string _appsPath;
    private readonly string _defaultIconPath;
    private readonly HttpClient _httpClient;
    private readonly Lock _lock = new();
    public DataService(IConfiguration configuration)
    {
        _dataDir = configuration["AppDatas"] ?? Path.Combine(AppContext.BaseDirectory, "AppDatas");
        _imgDir = Path.Combine(_dataDir, "img");
        _categoriesPath = Path.Combine(_dataDir, "categories.json");
        _linksPath = Path.Combine(_dataDir, "links.json");
        _appsPath = Path.Combine(_dataDir, "apps.json");
        _defaultIconPath = Path.Combine(_imgDir, "default.png");
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        EnsureDirectoryStructure();
        EnsureDefaultIcon();
    }
    private void EnsureDirectoryStructure()
    {
        if (!Directory.Exists(_dataDir)) Directory.CreateDirectory(_dataDir);
        if (!Directory.Exists(_imgDir)) Directory.CreateDirectory(_imgDir);
    }
    private void EnsureDefaultIcon()
    {
        if (File.Exists(_defaultIconPath)) return;
        byte[] transparentPng = [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82
        ];
        File.WriteAllBytes(_defaultIconPath, transparentPng);
    }
    public DashboardData GetAllData()
    {
        lock (_lock)
        {
            return new DashboardData
            {
                Categories = LoadJson<Category>(_categoriesPath),
                Links = LoadJson<LinkItem>(_linksPath),
                Apps = LoadJson<AppItem>(_appsPath)
            };
        }
    }
    public async Task ProcessAndEnsureIconsAsync()
    {
        List<LinkItem> links;
        lock (_lock)
        {
            links = LoadJson<LinkItem>(_linksPath);
        }
        var updated = false;
        foreach (var link in links.Where(link => string.IsNullOrWhiteSpace(link.IconFileName) || !File.Exists(Path.Combine(_imgDir, link.IconFileName))))
        {
            var iconName = await DownloadOrAssignFaviconAsync(link.Id, link.Url);
            link.IconFileName = iconName;
            updated = true;
        }
        if (updated)
        {
            lock (_lock)
            {
                SaveJson(_linksPath, links);
            }
        }
    }
    private async Task<string> DownloadOrAssignFaviconAsync(string id, string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return "default.png";
        }
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return "default.png";
            var host = uri.Host;
            if (string.IsNullOrWhiteSpace(host)) return "default.png";
            var faviconUrl = $"https://www.google.com/s2/favicons?sz=64&domain={Uri.EscapeDataString(host)}";
            var bytes = await _httpClient.GetByteArrayAsync(faviconUrl).ConfigureAwait(false);
            if (bytes.Length > 0)
            {
                var fileName = $"{id}.png";
                var localPath = Path.Combine(_imgDir, fileName);
                await File.WriteAllBytesAsync(localPath, bytes).ConfigureAwait(false);
                return fileName;
            }
        }
        catch (Exception ex)
        {
            LoggerService.Log($"Erreur téléchargement favicon pour {url}", ex);
        }
        return "default.png";
    }
    public Category AddCategory(Category category)
    {
        lock (_lock)
        {
            var list = LoadJson<Category>(_categoriesPath);
            if (string.IsNullOrWhiteSpace(category.Id))
                category.Id = Guid.NewGuid().ToString("N")[..8];
            list.Add(category);
            SaveJson(_categoriesPath, list);
            return category;
        }
    }
    public bool UpdateCategory(Category category)
    {
        lock (_lock)
        {
            var list = LoadJson<Category>(_categoriesPath);
            var idx = list.FindIndex(c => c.Id == category.Id);
            if (idx == -1) return false;
            list[idx] = category;
            SaveJson(_categoriesPath, list);
            return true;
        }
    }
    public bool DeleteCategory(string id)
    {
        lock (_lock)
        {
            var list = LoadJson<Category>(_categoriesPath);
            var removed = list.RemoveAll(c => c.Id == id) > 0;
            if (removed) SaveJson(_categoriesPath, list);
            return removed;
        }
    }
    public async Task<LinkItem> AddLinkAsync(LinkItem link)
    {
        if (string.IsNullOrWhiteSpace(link.Id))
            link.Id = Guid.NewGuid().ToString("N")[..8];
        if (string.IsNullOrWhiteSpace(link.IconFileName))
        {
            link.IconFileName = await DownloadOrAssignFaviconAsync(link.Id, link.Url);
        }
        lock (_lock)
        {
            var list = LoadJson<LinkItem>(_linksPath);
            list.Add(link);
            SaveJson(_linksPath, list);
        }
        return link;
    }
    public async Task<bool> UpdateLinkAsync(LinkItem link)
    {
        lock (_lock)
        {
            var list = LoadJson<LinkItem>(_linksPath);
            var idx = list.FindIndex(l => l.Id == link.Id);
            if (idx == -1) return false;
            if (string.IsNullOrWhiteSpace(link.IconFileName))
                link.IconFileName = list[idx].IconFileName;
            list[idx] = link;
            SaveJson(_linksPath, list);
            return true;
        }
    }
    public bool DeleteLink(string id)
    {
        lock (_lock)
        {
            var list = LoadJson<LinkItem>(_linksPath);
            var item = list.FirstOrDefault(l => l.Id == id);
            if (item == null) return false;
            if (!string.IsNullOrWhiteSpace(item.IconFileName) && item.IconFileName != "default.png")
            {
                var imgPath = Path.Combine(_imgDir, item.IconFileName);
                if (File.Exists(imgPath)) File.Delete(imgPath);
            }
            list.Remove(item);
            SaveJson(_linksPath, list);
            return true;
        }
    }
    public AppItem AddApp(AppItem app)
    {
        lock (_lock)
        {
            var list = LoadJson<AppItem>(_appsPath);
            if (string.IsNullOrWhiteSpace(app.Id))
                app.Id = Guid.NewGuid().ToString("N")[..8];
            if (string.IsNullOrWhiteSpace(app.IconFileName))
                app.IconFileName = "default.png";
            list.Add(app);
            SaveJson(_appsPath, list);
            return app;
        }
    }
    public bool UpdateApp(AppItem app)
    {
        lock (_lock)
        {
            var list = LoadJson<AppItem>(_appsPath);
            var idx = list.FindIndex(a => a.Id == app.Id);
            if (idx == -1) return false;
            list[idx] = app;
            SaveJson(_appsPath, list);
            return true;
        }
    }
    public bool DeleteApp(string id)
    {
        lock (_lock)
        {
            var list = LoadJson<AppItem>(_appsPath);
            var item = list.FirstOrDefault(a => a.Id == id);
            if (item == null) return false;
            if (!string.IsNullOrWhiteSpace(item.IconFileName) && item.IconFileName != "default.png")
            {
                var imgPath = Path.Combine(_imgDir, item.IconFileName);
                if (File.Exists(imgPath)) File.Delete(imgPath);
            }
            list.Remove(item);
            SaveJson(_appsPath, list);
            return true;
        }
    }
    public async Task<string?> SaveCustomIconAsync(string entityId, bool isApp, IFormFile file)
    {
        if (file.Length == 0) return null;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".webp", ".ico", ".svg" };
        if (!allowedExts.Contains(ext)) return null;
        var fileName = $"{entityId}_custom{ext}";
        var fullPath = Path.Combine(_imgDir, fileName);
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream).ConfigureAwait(false);
        }
        lock (_lock)
        {
            if (isApp)
            {
                var apps = LoadJson<AppItem>(_appsPath);
                var app = apps.FirstOrDefault(a => a.Id == entityId);
                if (app == null) return fileName;
                app.IconFileName = fileName;
                SaveJson(_appsPath, apps);
            }
            else
            {
                var links = LoadJson<LinkItem>(_linksPath);
                var link = links.FirstOrDefault(l => l.Id == entityId);
                if (link == null) return fileName;
                link.IconFileName = fileName;
                SaveJson(_linksPath, links);
            }
        }
        return fileName;
    }
    public string GetImagePath(string fileName)
    {
        var file = Path.Combine(_imgDir, fileName);
        return File.Exists(file) ? file : _defaultIconPath;
    }
    private static List<T> LoadJson<T>(string path)
    {
        if (!File.Exists(path)) return [];
        try
        {
            var content = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<T>>(content) ?? [];
        }
        catch
        {
            return [];
        }
    }
    private static void SaveJson<T>(string path, List<T> data)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            var content = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, content);
        }
        catch (Exception ex)
        {
            LoggerService.Log($"Erreur sauvegarde JSON: {path}", ex);
        }
    }
}