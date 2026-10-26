namespace DashboardCustom.Server.Model
{
    public class Category
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Icon { get; set; } = string.Empty;
    }
}
