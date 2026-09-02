using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using XR18BarControl.Services;

namespace XR18BarControl.XR18;

public sealed record DiscoveredMixer(IPAddress Address, string Name, string Model, string Firmware)
{
    public string Description => $"{Model} {Name} — {Address}";
}

public static class XR18DiscoveryService
{
    public static async Task<IReadOnlyList<DiscoveredMixer>> DiscoverAsync(int port = 10024, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromSeconds(3);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout.Value);
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0)) { EnableBroadcast = true };
        var request = OscCodec.Encode(new OscMessage("/xinfo"));
        foreach (var target in GetBroadcastAddresses().Distinct())
        {
            try { await udp.SendAsync(request, new IPEndPoint(target, port), deadline.Token); }
            catch (Exception ex) when (ex is SocketException or InvalidOperationException) { AppLog.Error($"No se pudo consultar broadcast {target}", ex); }
        }

        var found = new Dictionary<IPAddress, DiscoveredMixer>();
        while (!deadline.IsCancellationRequested)
        {
            try
            {
                var response = await udp.ReceiveAsync(deadline.Token);
                var message = OscCodec.Decode(response.Buffer);
                if (message.Address is not ("/xinfo" or "/info")) continue;
                var fields = OscCodec.DecodeStringArguments(response.Buffer);
                var name = fields.Count > 1 ? fields[1] : "X AIR";
                var model = fields.Count > 2 ? fields[2] : "XR18/X AIR";
                var firmware = fields.Count > 3 ? fields[3] : "";
                found[response.RemoteEndPoint.Address] = new(response.RemoteEndPoint.Address, name, model, firmware);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { AppLog.Error("Respuesta de autodetección OSC inválida", ex); }
        }
        AppLog.Info($"Autodetección finalizada: {found.Count} mesa(s)");
        return found.Values.OrderBy(x => x.Address.ToString()).ToArray();
    }

    private static IEnumerable<IPAddress> GetBroadcastAddresses()
    {
        yield return IPAddress.Broadcast;
        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (network.OperationalStatus != OperationalStatus.Up || network.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            foreach (var unicast in network.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork || unicast.IPv4Mask is null) continue;
                var address = unicast.Address.GetAddressBytes(); var mask = unicast.IPv4Mask.GetAddressBytes(); var broadcast = new byte[4];
                for (var i = 0; i < 4; i++) broadcast[i] = (byte)(address[i] | ~mask[i]);
                yield return new IPAddress(broadcast);
            }
        }
    }
}
