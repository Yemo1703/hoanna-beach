using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using XR18BarControl.Audio;
using XR18BarControl.Configuration;
using XR18BarControl.Services;
using XR18BarControl.XR18;

namespace XR18BarControl.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly XR18OscClient _client;
    private readonly ConfigService _configs;
    private readonly Dictionary<string, bool> _onStates = [];
    private AppConfig _config;
    private ConnectionState _connection;

    public ObservableCollection<ZoneViewModel> Zones { get; } = [];
    public ICommand AllZonesCommand { get; }
    public ICommand AdminCommand { get; }
    public bool ControlsEnabled => _connection == ConnectionState.Connected;
    public string ConnectionText => _connection switch { ConnectionState.Connected => "XR18 CONECTADA", ConnectionState.Connecting => "CONECTANDO…", _ => "XR18 DESCONECTADA" };
    public Brush ConnectionBrush => new SolidColorBrush(_connection switch { ConnectionState.Connected => Colors.LimeGreen, ConnectionState.Connecting => Colors.Gold, _ => Colors.OrangeRed });

    public MainViewModel(XR18OscClient client, ConfigService configs, AppConfig config)
    {
        _client = client; _configs = configs; _config = config;
        LoadZones();
        _client.ConnectionChanged += OnConnection;
        _client.StateReceived += OnState;
        AllZonesCommand = new AsyncCommand(() => SetAllZonesAsync());
        AdminCommand = new RelayCommand(OpenAdmin);
    }

    public Task ConnectAsync(CancellationToken ct) => _client.RunAsync(ct);

    private void LoadZones()
    {
        Zones.Clear();
        foreach (var zone in _config.Zones) Zones.Add(new ZoneViewModel(this, zone, XR18Commands.ResolveOutput(zone.Output)));
        Raise(nameof(Zones));
    }

    private void OpenAdmin()
    {
        AppLog.Audit("Apertura de la pantalla de administración");
        var window = new AdminWindow(_configs, _config, _client) { Owner = Application.Current.MainWindow };
        if (window.ShowDialog() == true)
        {
            _config = window.Config; LoadZones(); AppLog.Audit("Pantalla de administración cerrada después de guardar");
            if (ControlsEnabled) _ = _client.QueryAllAsync(CancellationToken.None);
        }
        else AppLog.Audit("Pantalla de administración cerrada sin guardar");
    }

    internal async Task SelectOnlyAsync(ZoneViewModel selected)
    {
        AppLog.Audit($"Clic en zona exclusiva {selected.Name}");
        if (!ControlsEnabled) return;
        try
        {
            foreach (var zone in Zones) await _client.SetZoneEnabledAsync(zone.Output, ReferenceEquals(zone, selected), CancellationToken.None);
            AppLog.Audit($"Zona exclusiva aplicada: {selected.Name}");
        }
        catch (Exception ex) { AppLog.Error($"Fallo al seleccionar zona {selected.Name}", ex); }
    }

    private async Task SetAllZonesAsync()
    {
        AppLog.Audit("Clic en TODOS");
        if (!ControlsEnabled) return;
        try { foreach (var zone in Zones) await _client.SetZoneEnabledAsync(zone.Output, true, CancellationToken.None); AppLog.Audit("Todas las zonas activadas"); }
        catch (Exception ex) { AppLog.Error("Fallo al activar todas las zonas", ex); }
    }

    internal async Task SetVolumeAsync(ZoneViewModel zone, double value)
    {
        AppLog.Audit($"Cambio de volumen solicitado | zona={zone.Name} porcentaje={value:F1}");
        if (!ControlsEnabled) return;
        try { await _client.SetZoneVolumeAsync(zone.Output, value, CancellationToken.None); AppLog.Audit($"Volumen enviado | zona={zone.Name} porcentaje={value:F1}"); }
        catch (Exception ex) { AppLog.Error($"Fallo al enviar volumen de {zone.Name}", ex); }
    }

    private void OnConnection(ConnectionState state) => Application.Current.Dispatcher.Invoke(() =>
    {
        _connection = state; Raise(nameof(ConnectionText)); Raise(nameof(ConnectionBrush)); Raise(nameof(ControlsEnabled));
    });

    private void OnState(OscMessage message) => Application.Current.Dispatcher.Invoke(() =>
    {
        if (message.Address.EndsWith("/on") && message.Value is int on) _onStates[message.Address] = on != 0;
        foreach (var zone in Zones)
        {
            if (zone.Output.FaderPaths.Contains(message.Address) && message.Value is float f)
                zone.SetFromMixer(VolumeMapper.DbToPercent(VolumeMapper.OscToDb(f)));
            if (zone.Output.OnPaths.Contains(message.Address))
                zone.IsOn = zone.Output.OnPaths.All(path => _onStates.TryGetValue(path, out var enabled) && enabled);
        }
    });
}

public sealed class ZoneViewModel : ObservableObject
{
    private readonly MainViewModel _owner;
    private double _volume;
    private bool _isOn;
    private bool _syncing;
    public string Id { get; }
    public string Name { get; }
    public OutputMapping Output { get; }
    public ICommand SelectCommand { get; }
    public double Volume { get => _volume; set { if (Set(ref _volume, value) && !_syncing) _ = _owner.SetVolumeAsync(this, value); } }
    public bool IsOn { get => _isOn; set => Set(ref _isOn, value); }

    public ZoneViewModel(MainViewModel owner, ZoneConfig config, OutputMapping output)
    { _owner = owner; Id = config.Id; Name = config.Name.ToUpperInvariant(); Output = output; SelectCommand = new AsyncCommand(() => owner.SelectOnlyAsync(this)); }
    public void SetFromMixer(double value) { _syncing = true; try { Volume = value; } finally { _syncing = false; } }
}

public sealed class RelayCommand(Action action) : ICommand
{ public event EventHandler? CanExecuteChanged { add { } remove { } } public bool CanExecute(object? parameter) => true; public void Execute(object? parameter) => action(); }

public sealed class AsyncCommand(Func<Task> action) : ICommand
{
    private bool _busy; public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_busy;
    public async void Execute(object? parameter) { if (_busy) return; _busy = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty); try { await action(); } finally { _busy = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
}
