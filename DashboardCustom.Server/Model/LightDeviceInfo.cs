using YeelightAPI;

namespace DashboardCustom.Server.Model;

public class LightDeviceInfo
{
    public LightDeviceInfo(Device device)
    {
        NameSelected = string.IsNullOrEmpty(device.Name) ? "Lampe Xiaomi" : device.Name;
        try
        {
            Brightness = device.Properties["bright"] switch
            {
                int brightnessInt => brightnessInt,
                string brightnessStr when int.TryParse(brightnessStr, out var parsed) => parsed,
                _ => 0
            };
        }
        catch
        {
            Brightness = 0;
        }
        var powerStr = device.Properties.TryGetValue("power", out var property) ? property as string : null;
        PoweredOn = powerStr?.Equals("on", StringComparison.OrdinalIgnoreCase) == true;
        try
        {
            switch (device.Properties["rgb"])
            {
                case string rgbStr when int.TryParse(rgbStr, out var rgbParsed):
                    R = (rgbParsed >> 16) & 0xFF;
                    G = (rgbParsed >> 8) & 0xFF;
                    B = rgbParsed & 0xFF;
                    break;
                case int rgbInt:
                    R = (rgbInt >> 16) & 0xFF;
                    G = (rgbInt >> 8) & 0xFF;
                    B = rgbInt & 0xFF;
                    break;
            }
        }
        catch
        {
            R = G = B = 0;
        }
    }
    public bool PoweredOn { get; set; }
    public string NameSelected { get; set; }
    public int Brightness { get; set; }
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
}