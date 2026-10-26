using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace DashboardCustom.Server.Service;
public class DeviceService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<int> GetMouseBatteryAsync()
    {
        return await Task.Run(() => ExecuteSafely(GetRazerBatteryFromSynapse4, -1)).ConfigureAwait(false);
    }
    public async Task<int> GetKeyboardBatteryAsync()
    {
        return await Task.Run(() => ExecuteSafely(() => GetBatteryLogitechFromJson("g915"), -1)).ConfigureAwait(false);
    }
    public async Task<int> GetXboxControllerBatteryAsync()
    {
        return await ExecuteSafelyAsync(GetXboxControllerBatteryCoreAsync, -1).ConfigureAwait(false);
    }
    private T ExecuteSafely<T>(Func<T> action, T fallbackValue)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur critique dans DeviceService", ex);
            return fallbackValue;
        }
    }
    private async Task<T> ExecuteSafelyAsync<T>(Func<Task<T>> action, T fallbackValue)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur critique asynchrone dans DeviceService", ex);
            return fallbackValue;
        }
    }
    private int GetBatteryLogitechFromJson(string deviceName)
    {
        try
        {
            SQLitePCL.Batteries.Init();
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"LGHUB\settings.db");
            if (!File.Exists(dbPath)) return -1;
            using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT file FROM data LIMIT 1";
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return -1;
            var jsonContent = reader.GetString(0);
            using var doc = JsonDocument.Parse(jsonContent);
            foreach (var property in doc.RootElement.EnumerateObject()
                         .Where(p => p.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase) && p.Name.Contains("battery", StringComparison.OrdinalIgnoreCase)))
            {
                if (property.Value.TryGetProperty("percentage", out var percentageProp))
                {
                    return (int)percentageProp.GetDouble();
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur G-Hub batterie clavier", ex);
        }
        return -1;
    }

    private int GetRazerBatteryFromSynapse4()
    {
        try
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Razer\RazerAppEngine\User Data\Logs");
            if (!Directory.Exists(logDir)) return -1;
            var logFile = new DirectoryInfo(logDir).GetFiles("background-manager*.log")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();
            if (logFile == null) return -1;
            string content;
            using (var fs = new FileStream(logFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                content = reader.ReadToEnd();
            }
            if (string.IsNullOrEmpty(content)) return -1;
            var match = Regex.Match(content, @"level.*?:(\d+)", RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var level)) return level;
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur extraction batterie Razer", ex);
        }
        return -1;
    }

    private async Task<int> GetXboxControllerBatteryCoreAsync()
    {
        try
        {
            const string batteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
            const string script =
                "Get-PnpDevice | Where-Object { $_.FriendlyName -match 'Xbox Wireless Controller' } | " +
                $"Get-PnpDeviceProperty -KeyName '{batteryKey}' -ErrorAction SilentlyContinue | " +
                "Where-Object { $_.Data -ne $null } | Select-Object -ExpandProperty Data";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return -1;
            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(output))
            {
                foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(line.Trim(), out var level)) return level;
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur batterie Xbox", ex);
        }
        return -1;
    }
}