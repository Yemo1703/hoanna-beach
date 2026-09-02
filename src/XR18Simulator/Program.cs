using System.Net;
using System.Net.Sockets;
using XR18BarControl.XR18;

const int port = 10024;
var states = new Dictionary<string, object>
{
    [XR18Commands.MainFader] = .75f,
    [XR18Commands.MainOn] = 1,
    [XR18Commands.Bus1Fader] = .70f,
    [XR18Commands.Bus1On] = 1,
    [XR18Commands.Bus2Fader] = .70f,
    [XR18Commands.Bus2On] = 1
};
for (var bus = 3; bus <= 6; bus++) { states[$"/bus/{bus}/mix/fader"] = .65f; states[$"/bus/{bus}/mix/on"] = 0; }
var clients = new Dictionary<string, (IPEndPoint Endpoint, DateTime Expires)>();
using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
udp.EnableBroadcast = true;
Console.WriteLine($"XR18 Simulator escuchando en UDP 0.0.0.0:{port}");
Console.WriteLine("No controla ningún dispositivo real. Ctrl+C para salir.");
Console.WriteLine("Comandos: status | main on/off | terrace on/off | main 0-100 | terrace 0-100");

var networkTask = ReceiveLoopAsync(shutdown.Token);
var consoleTask = ConsoleLoopAsync(shutdown.Token);
await Task.WhenAny(networkTask, consoleTask);
shutdown.Cancel();
try { await Task.WhenAll(networkTask, consoleTask); } catch (OperationCanceledException) { }

async Task ReceiveLoopAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        UdpReceiveResult packet;
        try { packet = await udp.ReceiveAsync(ct); } catch (OperationCanceledException) { break; }
        try
        {
            var message = OscCodec.Decode(packet.Buffer);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} RX {packet.RemoteEndPoint} {message.Address} {Format(message.Value)}");
            switch (message.Address)
            {
                case "/xinfo": case "/info":
                    await ReplyAsync(packet.RemoteEndPoint, new("/xinfo", new object[] { "V0.04", "XR18-SIM", "XR18", "1.20-sim" }), ct);
                    break;
                case "/status":
                    await ReplyAsync(packet.RemoteEndPoint, new("/status", new object[] { "active", "192.168.0.1", "XR18-SIM", "simulator" }), ct);
                    break;
                case "/xremote":
                    clients[packet.RemoteEndPoint.ToString()] = (packet.RemoteEndPoint, DateTime.UtcNow.AddSeconds(10));
                    break;
                default:
                    if (!states.ContainsKey(message.Address)) { Console.WriteLine("  IGNORADO: ruta fuera de whitelist"); break; }
                    if (message.Value is null) await ReplyAsync(packet.RemoteEndPoint, new(message.Address, states[message.Address]), ct);
                    else
                    {
                        states[message.Address] = ValidateValue(message.Address, message.Value);
                        await BroadcastStateAsync(new(message.Address, states[message.Address]), ct);
                    }
                    break;
            }
        }
        catch (Exception ex) { Console.WriteLine($"  ERROR {ex.GetType().Name}: {ex.Message}"); }
    }
}

async Task ConsoleLoopAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        var line = await Task.Run(Console.ReadLine, ct);
        if (line is null) { await Task.Delay(250, ct); continue; }
        var parts = line.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) continue;
        if (parts[0] is "quit" or "exit") { shutdown.Cancel(); return; }
        if (parts[0] == "status") { PrintStatus(); continue; }
        if (parts.Length != 2 || parts[0] is not ("main" or "terrace")) { Console.WriteLine("Comando no reconocido"); continue; }
        var paths = parts[0] == "main" ? new[] { XR18Commands.MainFader } : new[] { XR18Commands.Bus1Fader, XR18Commands.Bus2Fader };
        var onPaths = parts[0] == "main" ? new[] { XR18Commands.MainOn } : new[] { XR18Commands.Bus1On, XR18Commands.Bus2On };
        if (parts[1] is "on" or "off") foreach (var path in onPaths) { states[path] = parts[1] == "on" ? 1 : 0; await BroadcastStateAsync(new(path, states[path]), ct); }
        else if (double.TryParse(parts[1], out var percent) && percent is >= 0 and <= 100) foreach (var path in paths) { states[path] = (float)(percent / 100); await BroadcastStateAsync(new(path, states[path]), ct); }
        else Console.WriteLine("Usa on, off o un porcentaje entre 0 y 100");
    }
}

object ValidateValue(string path, object value) => path.EndsWith("/on") && value is int i ? Math.Clamp(i, 0, 1) : path.EndsWith("/fader") && value is float f ? Math.Clamp(f, 0, 1) : states[path];
async Task ReplyAsync(IPEndPoint target, OscMessage message, CancellationToken ct) { var data = OscCodec.Encode(message); await udp.SendAsync(data, target, ct); Console.WriteLine($"{DateTime.Now:HH:mm:ss} TX {target} {message.Address} {Format(message.Value)}"); }
async Task BroadcastStateAsync(OscMessage message, CancellationToken ct)
{
    var now = DateTime.UtcNow;
    foreach (var stale in clients.Where(x => x.Value.Expires <= now).Select(x => x.Key).ToArray()) clients.Remove(stale);
    foreach (var client in clients.Values) await ReplyAsync(client.Endpoint, message, ct);
}
void PrintStatus() { Console.WriteLine(string.Join(Environment.NewLine, states.Select(x => $"{x.Key} = {Format(x.Value)}"))); Console.WriteLine($"Clientes /xremote activos: {clients.Count}"); }
string Format(object? value) => value switch { null => "(query)", object[] values => string.Join(", ", values), float f => f.ToString("F4"), _ => value.ToString() ?? "" };
