using DashboardCustom.Server.Model;
using NAudio.CoreAudioApi;
using System.Diagnostics;

namespace DashboardCustom.Server.Service;

public class AudioService : IDisposable
{
    private MMDeviceEnumerator? _deviceEnumerator;
    private readonly Lock _lock = new();
    public AudioService()
    {
        InitializeAudioEngine();
    }
    private void InitializeAudioEngine()
    {
        lock (_lock)
        {
            try
            {
                _deviceEnumerator?.Dispose();
                _deviceEnumerator = new MMDeviceEnumerator();
            }
            catch
            {
                _deviceEnumerator = null;
            }
        }
    }
    private T ExecuteSafely<T>(Func<MMDeviceEnumerator, T> action, T fallbackValue)
    {
        lock (_lock)
        {
            try
            {
                if (_deviceEnumerator == null)
                {
                    InitializeAudioEngine();
                }
                return _deviceEnumerator == null ? fallbackValue : action(_deviceEnumerator);
            }
            catch
            {
                InitializeAudioEngine();
                try
                {
                    if (_deviceEnumerator != null)
                    {
                        return action(_deviceEnumerator);
                    }
                }
                catch
                {
                    // ignored
                }
                return fallbackValue;
            }
        }
    }
    private bool ExecuteSafely(Action<MMDeviceEnumerator> action)
    {
        lock (_lock)
        {
            try
            {
                if (_deviceEnumerator == null)
                {
                    InitializeAudioEngine();
                }
                if (_deviceEnumerator == null)
                {
                    return false;
                }
                action(_deviceEnumerator);
                return true;
            }
            catch
            {
                InitializeAudioEngine();
                try
                {
                    if (_deviceEnumerator != null)
                    {
                        action(_deviceEnumerator);
                        return true;
                    }
                }
                catch
                {
                    // ignored
                }
                return false;
            }
        }
    }
    public AudioDeviceInfo? GetCurrentAudioDevice()
    {
        return ExecuteSafely(enumerator =>
        {
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return new AudioDeviceInfo
            {
                NameSelected = device.FriendlyName,
                Volume = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100)
            };
        }, null);
    }
    public bool SetDefaultDevice(string name)
    {
        return ExecuteSafely(enumerator =>
        {
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            var targetDevice = devices.FirstOrDefault(d => d.ID == name || d.FriendlyName.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (targetDevice == null) throw new InvalidOperationException();
            var script = $"Set-AudioDevice -ID '{targetDevice.ID}'";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(3000);
            if (process?.ExitCode == 0) return;
            throw new InvalidOperationException();
        });
    }
    public void SetVolume(int volume)
    {
        ExecuteSafely(enumerator =>
        {
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var scalar = volume / 100.0f;
            device.AudioEndpointVolume.MasterVolumeLevelScalar = scalar;
        });
    }
    public void Dispose()
    {
        lock (_lock)
        {
            _deviceEnumerator?.Dispose();
            _deviceEnumerator = null;
        }
    }
}