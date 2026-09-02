using XR18BarControl.Audio;
using XR18BarControl.Configuration;
using XR18BarControl.XR18;

var tests = new (string Name, Action Run)[]
{
    ("OSC query", () => Equal(new OscMessage("/xinfo"), OscCodec.Decode(OscCodec.Encode(new("/xinfo"))))),
    ("OSC integer", () => { var x=OscCodec.Decode(OscCodec.Encode(new("/lr/mix/on",1))); Equal("/lr/mix/on",x.Address);Equal(1,x.Value); }),
    ("OSC float", () => { var x=OscCodec.Decode(OscCodec.Encode(new("/lr/mix/fader",.75f))); Near(.75,(float)x.Value!,1e-6); }),
    ("OSC xinfo multi-string", () => { var d=OscCodec.Encode(new("/xinfo",new object[]{"V0.04","XR18-SIM","XR18","1.20"}));Equal(4,OscCodec.DecodeStringArguments(d).Count); }),
    ("Mapeo 0% a -infinito", () => True(double.IsNegativeInfinity(VolumeMapper.PercentToDb(0)))),
    ("100% = 0dB", () => Near(0,VolumeMapper.PercentToDb(100),1e-9)),
    ("Mapeo perceptual monótono", () => { var last=double.NegativeInfinity;for(var p=0;p<=100;p++){var next=VolumeMapper.PercentToDb(p);True(next>=last);last=next;} }),
    ("Ley de fader ida/vuelta", () => { True(double.IsNegativeInfinity(VolumeMapper.OscToDb(VolumeMapper.DbToOsc(-90))));foreach(var db in new[]{-70d,-60,-45,-30,-20,-10,0,10})Near(db,VolumeMapper.OscToDb(VolumeMapper.DbToOsc(db)),.001); }),
    ("Todas las salidas resuelven", () => { foreach(var id in XR18Commands.OutputChoices.Keys){var o=XR18Commands.ResolveOutput(id);True(o.FaderPaths.Count>0&&o.OnPaths.Count>0);} }),
    ("Whitelist solo volumen/mute", () => { Equal(14,XR18Commands.StatePaths.Count);True(XR18Commands.StatePaths.All(x=>x.EndsWith("/mix/fader")||x.EndsWith("/mix/on"))); }),
    ("Parejas enlazadas", () => { foreach(var id in new[]{"bus1_2","bus3_4","bus5_6"}){var o=XR18Commands.ResolveOutput(id);Equal(2,o.FaderPaths.Count);Equal(2,o.OnPaths.Count);} }),
    ("Configuración dinámica", () => { var c=new AppConfig{Zones=[new(){Name="INTERIOR",Output="mainLR"},new(){Name="TERRAZA",Output="bus1_2"},new(){Name="JARDIN",Output="bus3"}]};Equal(3,c.Zones.Count);Equal("bus3",c.Zones[2].Output); })
};

var failures = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS  {test.Name}"); }
    catch (Exception ex) { failures++; Console.WriteLine($"FAIL  {test.Name}: {ex.Message}"); }
}
Console.WriteLine($"\n{tests.Length-failures}/{tests.Length} tests correctos");
return failures == 0 ? 0 : 1;

static void True(bool value){if(!value)throw new Exception("condición falsa");}
static void Equal<T>(T expected,T actual){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"esperado={expected}, actual={actual}");}
static void Near(double expected,double actual,double tolerance){if(Math.Abs(expected-actual)>tolerance)throw new Exception($"esperado={expected}, actual={actual}");}
