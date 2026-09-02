using System.IO;using System.Text.Json;using XR18BarControl.Security;using XR18BarControl.Services;
namespace XR18BarControl.Configuration;
public sealed class ConfigService
{
 private static readonly JsonSerializerOptions Options=new(){WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase}; public string ConfigPath {get;}=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"XR18BarControl","config.json");
 public async Task<AppConfig> LoadAsync(){try{if(File.Exists(ConfigPath)){var loaded=JsonSerializer.Deserialize<AppConfig>(await File.ReadAllTextAsync(ConfigPath),Options)??NewConfig();if(loaded.Zones.Count==0){loaded.Zones=[new ZoneConfig{Id="interior",Name="INTERIOR",Output=loaded.Indoor.Output},new ZoneConfig{Id="terraza",Name="TERRAZA",Output=loaded.Terrace.Output}];AppLog.Info("Configuración migrada al modelo de zonas dinámicas");await SaveAsync(loaded);}else if(loaded.Zones.Count==2&&loaded.Zones.All(x=>x.Name=="NUEVA ZONA")){loaded.Zones[0].Name="INTERIOR";loaded.Zones[1].Name="TERRAZA";AppLog.Info("Nombres de zonas antiguas reparados durante la migración");await SaveAsync(loaded);}return loaded;}}catch(Exception ex){AppLog.Error("No se pudo leer la configuración",ex);}var c=NewConfig();await SaveAsync(c);return c;}
 public async Task SaveAsync(AppConfig c){Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);var t=ConfigPath+".tmp";await File.WriteAllTextAsync(t,JsonSerializer.Serialize(c,Options));File.Move(t,ConfigPath,true);AppLog.Info("Configuración guardada");}
 private static AppConfig NewConfig(){var c=new AppConfig();c.Zones=[c.Indoor,c.Terrace];c.AdminPinHash=AdminPinService.Hash("1234");return c;}
}
