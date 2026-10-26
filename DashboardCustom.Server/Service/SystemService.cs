using DashboardCustom.Server.Model;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DashboardCustom.Server.Service;

public class SystemService : IDisposable
{
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;
    private readonly List<PerformanceCounter> _gpuCounters = [];
    private readonly Lock _lock = new();
    public SystemService()
    {
        InitializeCounters();
    }

    private void InitializeCounters()
    {
        lock (_lock)
        {
            try
            {
                DisposeCounters();
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                _cpuCounter.NextValue();
                InitGpuCounters();
            }
            catch (Exception ex)
            {
                LoggerService.Log("Erreur Init SystemService", ex);
                DisposeCounters();
            }
        }
    }

    private void InitGpuCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            foreach (var instance in category.GetInstanceNames())
            {
                if (instance.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                    _gpuCounters.Add(new PerformanceCounter("GPU Engine", "Utilization Percentage", instance));
            }
        }
        catch
        {
            _gpuCounters.Clear();
        }
    }

    public SystemInfo GetSystemMetrics()
    {
        lock (_lock)
        {
            try
            {
                if (_cpuCounter == null || _ramCounter == null)
                {
                    InitializeCounters();
                }

                var memStatus = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
                GlobalMemoryStatusEx(ref memStatus);

                double totalRamMb = memStatus.ullTotalPhys / 1024.0 / 1024.0;
                double availableRamMb = _ramCounter?.NextValue() ?? 0;
                double cpuUsage = 0;

                try
                {
                    cpuUsage = _cpuCounter?.NextValue() ?? 0;
                }
                catch
                {
                    InitializeCounters();
                    cpuUsage = _cpuCounter?.NextValue() ?? 0;
                }

                double memoryUsage = totalRamMb > 0
                    ? Math.Round(((totalRamMb - availableRamMb) / totalRamMb) * 100, 1)
                    : 0;

                return new SystemInfo
                {
                    CpuUsage = Math.Round(cpuUsage, 1),
                    MemoryUsage = memoryUsage,
                    GraphicUsage = Math.Round(GetGpuUsage(), 1),
                    UptimeSeconds = GetTickCount64() / 1000.0
                };
            }
            catch (Exception ex)
            {
                LoggerService.Log("Erreur GetSystemMetrics", ex);
                return new SystemInfo();
            }
        }
    }

    private float GetGpuUsage()
    {
        float total = 0;
        try
        {
            foreach (var counter in _gpuCounters)
            {
                total += counter.NextValue();
            }
        }
        catch
        {
            _gpuCounters.Clear();
            InitGpuCounters();
        }
        return total;
    }

    public void RestartExplorer()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                process.Kill();
                process.WaitForExit(3000);
            }
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur lors de l'arrêt d'explorer", ex);
        }
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur lors du redémarrage d'explorer", ex);
        }
    }
    public bool StartApplicationByKey(string key)
    {
        try
        {
            var tempConfig = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                        .Build();
            var allowedApps = tempConfig.GetSection("AllowedApps").Get<Dictionary<string, string>>()
                              ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!allowedApps.TryGetValue(key, out var path) || !File.Exists(path)) return false;
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return true;
        }
        catch (Exception e)
        {
            LoggerService.Log($"Erreur lancement application: {key}", e);
            return false;
        }
    }

    private void DisposeCounters()
    {
        try
        {
            _cpuCounter?.Dispose();
            _cpuCounter = null;
            _ramCounter?.Dispose();
            _ramCounter = null;

            foreach (var counter in _gpuCounters)
            {
                counter.Dispose();
            }
            _gpuCounters.Clear();
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur DisposeCounters", ex);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            DisposeCounters();
        }
        GC.SuppressFinalize(this);
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [DllImport("kernel32.dll")]
    private static extern ulong GetTickCount64();
}