using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using XR18BarControl.Configuration;
using XR18BarControl.Security;
using XR18BarControl.Services;
using XR18BarControl.XR18;

namespace XR18BarControl;

public partial class AdminWindow : Window
{
    private readonly ConfigService _service; private readonly XR18OscClient _client;
    public AppConfig Config { get; private set; }
    public ObservableCollection<ZoneEditorRow> ZoneRows { get; } = [];
    public IReadOnlyList<OutputOption> OutputOptions { get; } = XR18Commands.OutputChoices.Select(x => new OutputOption(x.Key, x.Value)).ToArray();
    public ZoneEditorRow? SelectedZone { get; set; }

    public AdminWindow(ConfigService service, AppConfig config, XR18OscClient client)
    { InitializeComponent(); _service = service; _client = client; Config = config; DataContext = this; }

    private void AdminWindow_Loaded(object sender, RoutedEventArgs e) { PinBox.Focus(); Keyboard.Focus(PinBox); AppLog.Audit("Ventana de administración cargada; foco asignado al campo PIN"); }
    private void Pin_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Unlock_Click(sender, e); }
    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        if (!AdminPinService.Verify(PinBox.Password, Config.AdminPinHash)) { AppLog.Audit("Acceso de administración rechazado: PIN incorrecto"); PinError.Text = "PIN incorrecto"; PinBox.Clear(); return; }
        AppLog.Audit("Acceso de administración autorizado"); PinPanel.Visibility = Visibility.Collapsed; SettingsPanel.Visibility = Visibility.Visible;
        IpBox.Text = Config.Xr18.Ip; PortBox.Text = Config.Xr18.Port.ToString(); AutoConnectBox.IsChecked = Config.AutoConnect; FullscreenBox.IsChecked = Config.StartFullscreen; WindowsBox.IsChecked = Config.StartWithWindows;
        ZoneRows.Clear(); foreach (var zone in Config.Zones) ZoneRows.Add(new(zone.Id, zone.Name, zone.Output));
    }
    private void AddZone_Click(object sender, RoutedEventArgs e)
    {
        var used = ZoneRows.Select(x => x.Output).ToHashSet(); var output = OutputOptions.FirstOrDefault(x => !used.Contains(x.Id))?.Id ?? "bus1";
        var row = new ZoneEditorRow(Guid.NewGuid().ToString("N"), $"ZONA {ZoneRows.Count + 1}", output); ZoneRows.Add(row); ZonesGrid.SelectedItem = row; ZonesGrid.ScrollIntoView(row); AppLog.Audit("Clic en AÑADIR ZONA");
    }
    private void RemoveZone_Click(object sender, RoutedEventArgs e)
    {
        if (ZonesGrid.SelectedItem is not ZoneEditorRow row) { StatusText.Text = "Selecciona una zona para eliminar."; return; }
        if (ZoneRows.Count <= 1) { StatusText.Text = "Debe existir al menos una zona."; return; }
        ZoneRows.Remove(row); AppLog.Audit($"Zona eliminada del editor: {row.Name}");
    }
    private bool Validate(out IPAddress ip, out int port)
    {
        ip = IPAddress.None; port = 0; ZonesGrid.CommitEdit(); ZonesGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
        if (!IPAddress.TryParse(IpBox.Text, out ip!) || !int.TryParse(PortBox.Text, out port) || port is < 1 or > 65535) { StatusText.Text = "Revisa la IP y el puerto."; return false; }
        if (ZoneRows.Count == 0 || ZoneRows.Any(x => string.IsNullOrWhiteSpace(x.Name) || !XR18Commands.OutputChoices.ContainsKey(x.Output))) { StatusText.Text = "Revisa nombres y salidas."; return false; }
        if (ZoneRows.GroupBy(x => x.Output).Any(x => x.Count() > 1)) { StatusText.Text = "Una salida no puede estar asignada a dos zonas."; return false; }
        return true;
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        AppLog.Audit("Clic en GUARDAR ajustes"); if (!Validate(out var ip, out var port)) return;
        Config.Xr18.Ip = ip.ToString(); Config.Xr18.Port = port; Config.Zones = ZoneRows.Select(x => new ZoneConfig { Id = x.Id, Name = x.Name.Trim().ToUpperInvariant(), Output = x.Output }).ToList();
        Config.AutoConnect = AutoConnectBox.IsChecked == true; Config.StartFullscreen = FullscreenBox.IsChecked == true; Config.StartWithWindows = WindowsBox.IsChecked == true;
        var pinChanged = NewPinBox.Password.Length > 0; if (pinChanged) { if (NewPinBox.Password.Length < 4) { StatusText.Text = "El PIN debe tener al menos 4 caracteres."; return; } Config.AdminPinHash = AdminPinService.Hash(NewPinBox.Password); }
        AppLog.Audit($"Configuración aceptada | ip={Config.Xr18.Ip} puerto={port} zonas={string.Join(';', Config.Zones.Select(x => $"{x.Name}:{x.Output}"))} pinCambiado={pinChanged}");
        await _service.SaveAsync(Config); StartupService.Apply(Config.StartWithWindows); _client.Reconfigure(Config.Xr18.Ip, Config.Xr18.Port); DialogResult = true;
    }
    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        AppLog.Audit("Clic en PROBAR CONEXIÓN"); if (!Validate(out var ip, out var port)) return; StatusText.Text = "Probando conexión…";
        try { using var udp = new UdpClient(0); udp.Connect(new IPEndPoint(ip, port)); await udp.SendAsync(OscCodec.Encode(new("/xinfo"))); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)); var response = await udp.ReceiveAsync(timeout.Token); var ok = OscCodec.Decode(response.Buffer).Address is "/xinfo" or "/info"; StatusText.Text = ok ? "Conexión correcta con XR18." : "Respuesta inesperada."; AppLog.Audit($"Prueba de conexión resultado={ok}"); }
        catch (Exception ex) { AppLog.Error("Prueba de conexión fallida", ex); StatusText.Text = "No se recibió respuesta."; }
    }
    private async void Discover_Click(object sender, RoutedEventArgs e)
    {
        AppLog.Audit("Clic en BUSCAR XR18"); DiscoverButton.IsEnabled = false; StatusText.Text = "Buscando mesas X AIR…";
        try { var mixers = await XR18DiscoveryService.DiscoverAsync(); if (mixers.Count == 0) { StatusText.Text = "No se encontró ninguna mesa."; return; } var selected = mixers[0]; IpBox.Text = selected.Address.ToString(); PortBox.Text = "10024"; StatusText.Text = $"Encontrada: {selected.Description}."; AppLog.Audit($"Autodetección: {mixers.Count} mesa(s), seleccionada={selected.Description}"); }
        catch (Exception ex) { AppLog.Error("Error durante la autodetección", ex); StatusText.Text = "No se pudo completar la búsqueda."; }
        finally { DiscoverButton.IsEnabled = true; }
    }
}

public sealed class ZoneEditorRow(string id, string name, string output)
{ public string Id { get; set; } = id; public string Name { get; set; } = name; public string Output { get; set; } = output; }
public sealed record OutputOption(string Id, string Label);
