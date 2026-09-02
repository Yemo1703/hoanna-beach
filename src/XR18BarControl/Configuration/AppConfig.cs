namespace XR18BarControl.Configuration;
public sealed class AppConfig { public Xr18Config Xr18 {get;set;}=new();public List<ZoneConfig> Zones {get;set;}=[];public ZoneConfig Indoor {get;set;}=new(){Id="interior",Name="INTERIOR",Output="mainLR"};public ZoneConfig Terrace {get;set;}=new(){Id="terraza",Name="TERRAZA",Output="bus1_2"};public string AdminPinHash {get;set;}="";public bool AutoConnect {get;set;}=true;public bool StartFullscreen {get;set;}=false;public bool StartWithWindows {get;set;} }
public sealed class Xr18Config {public string Ip {get;set;}="192.168.1.50";public int Port {get;set;}=10024;}
public sealed class ZoneConfig {public string Id {get;set;}=Guid.NewGuid().ToString("N");public string Name {get;set;}="NUEVA ZONA";public string Output {get;set;}="bus1";}
