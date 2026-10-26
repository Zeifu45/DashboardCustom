using DashboardCustom.Server.Model;
using YeelightAPI;
using YeelightAPI.Models;

namespace DashboardCustom.Server.Service;

public class LightService : IDisposable
{
    private const int DiscoverTimeoutSeconds = 6;
    private const PROPERTIES StateProps = PROPERTIES.power | PROPERTIES.bright | PROPERTIES.rgb | PROPERTIES.color_mode;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Device? _lamp;
    private string? _cachedHost;
    public bool IsConnected() => _lamp is { IsConnected: true };
    public async Task<bool> ConnectAsync(string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                _cachedHost = ipAddress.Trim();
            }
            return await ConnectCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
    public async Task<LightDeviceInfo?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!await EnsureConnectedCoreAsync(cancellationToken).ConfigureAwait(false)) return null;
            var success = await ExecuteWithReconnectAndReturnAsync(
                async () =>
                {
                    await RefreshPropertiesCoreAsync(cancellationToken).ConfigureAwait(false);
                    return SafeBuildInfo();
                },
                cancellationToken).ConfigureAwait(false);

            return success;
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur lecture état lampe", ex);
            SafeDisposeLamp();
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }
    public async Task ToggleAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!await EnsureConnectedCoreAsync(cancellationToken).ConfigureAwait(false)) return;
            await ExecuteWithReconnectAsync(async () => await _lamp!.Toggle().ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur Toggle Lampe", ex);
        }
        finally
        {
            _gate.Release();
        }
    }
    public async Task SetBrightnessAsync(int value, CancellationToken cancellationToken = default)
    {
        value = Math.Clamp(value, 1, 100);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!await EnsureConnectedCoreAsync(cancellationToken).ConfigureAwait(false)) return;
            await ExecuteWithReconnectAsync(async () => await _lamp!.SetBrightness(value).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur Brightness Lampe", ex);
        }
        finally
        {
            _gate.Release();
        }
    }
    public async Task SetColorAsync(int r, int g, int b, CancellationToken cancellationToken = default)
    {
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!await EnsureConnectedCoreAsync(cancellationToken).ConfigureAwait(false)) return;
            await ExecuteWithReconnectAsync(async () => await _lamp!.SetRGBColor(r, g, b).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LoggerService.Log("Erreur Color Lampe", ex);
        }
        finally
        {
            _gate.Release();
        }
    }
    public void Dispose()
    {
        _gate.Wait();
        try
        {
            SafeDisposeLamp();
        }
        finally
        {
            _gate.Release();
        }
        _gate.Dispose();
    }
    private async Task ExecuteWithReconnectAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LoggerService.Log("Commande lampe échouée, nouvelle tentative", ex);
            SafeDisposeLamp();
            if (!await ConnectCoreAsync(cancellationToken).ConfigureAwait(false)) return;
            await action().ConfigureAwait(false);
        }
    }
    private async Task<T?> ExecuteWithReconnectAndReturnAsync<T>(Func<Task<T?>> action, CancellationToken cancellationToken) where T : class
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LoggerService.Log("Lecture état lampe échouée, nouvelle tentative", ex);
            SafeDisposeLamp();
            if (!await ConnectCoreAsync(cancellationToken).ConfigureAwait(false)) return null;
            return await action().ConfigureAwait(false);
        }
    }
    private async Task<bool> EnsureConnectedCoreAsync(CancellationToken cancellationToken)
    {
        if (_lamp is { IsConnected: true }) return true;
        return await ConnectCoreAsync(cancellationToken).ConfigureAwait(false);
    }
    private async Task<bool> ConnectCoreAsync(CancellationToken cancellationToken)
    {
        SafeDisposeLamp();
        if (!string.IsNullOrEmpty(_cachedHost))
        {
            var d = new Device(_cachedHost);
            try
            {
                if (await d.Connect().ConfigureAwait(false))
                {
                    _lamp = d;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log($"Connexion lampe {_cachedHost} échouée", ex);
            }
            d.Dispose();
        }
        try
        {
            using var discoverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            discoverCts.CancelAfter(TimeSpan.FromSeconds(DiscoverTimeoutSeconds));
            var devices = await DeviceLocator.DiscoverAsync(discoverCts.Token).ConfigureAwait(false);
            var device = devices.FirstOrDefault();
            if (device == null)
            {
                LoggerService.Log("Aucune lampe Yeelight découverte sur le réseau");
                return false;
            }
            if (await device.Connect().ConfigureAwait(false))
            {
                _lamp = device;
                _cachedHost = device.Hostname;
                return true;
            }
            device.Dispose();
        }
        catch (OperationCanceledException)
        {
            LoggerService.Log("Découverte lampe annulée ou timeout");
        }
        catch (Exception ex)
        {
            LoggerService.Log("Échec découverte / connexion lampe", ex);
        }
        return false;
    }
    private async Task RefreshPropertiesCoreAsync(CancellationToken _)
    {
        if (_lamp is not { IsConnected: true }) return;
        var fresh = await _lamp.GetProps(StateProps).ConfigureAwait(false);
        if (fresh == null) return;
        foreach (var kv in fresh)
        {
            _lamp[kv.Key] = kv.Value;
        }
    }
    private LightDeviceInfo? SafeBuildInfo()
    {
        return _lamp == null ? null : new LightDeviceInfo(_lamp);
    }
    private void SafeDisposeLamp()
    {
        try
        {
            _lamp?.Dispose();
        }
        catch (Exception ex)
        {
            LoggerService.Log("Dispose lampe", ex);
        }
        _lamp = null;
    }
}