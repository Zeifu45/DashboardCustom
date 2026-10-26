using DashboardCustom.Server.Model;

namespace DashboardCustom.Server.Model;

public class DashboardData
{
    public List<Category> Categories { get; set; } = [];
    public List<LinkItem> Links { get; set; } = [];
    public List<AppItem> Apps { get; set; } = [];
    public string DefaultTab { get; set; } = "barre";
}