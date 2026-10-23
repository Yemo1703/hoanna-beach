using System.Diagnostics;using System.IO;using System.Net.Http;using System.Reflection;using System.Text.Json;
namespace XR18BarControl.Services;
public sealed class UpdateService
{
 private const string ManifestUrl="https://raw.githubusercontent.com/Yemo1703/hoanna-beach/main/version.json";
 private static readonly JsonSerializerOptions Options=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase};

 public async Task<UpdateInfo?> CheckAsync(CancellationToken ct)
 {
  try
  {
   using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(10)};
   var json=await http.GetStringAsync(ManifestUrl,ct);
   var manifest=JsonSerializer.Deserialize<UpdateManifest>(json,Options);
   if(manifest is null||string.IsNullOrWhiteSpace(manifest.InstallerUrl)||!Version.TryParse(manifest.Version,out var remote))return null;
   var current=Assembly.GetExecutingAssembly().GetName().Version??new Version(0,0,0,0);
   if(!IsNewer(remote,current))return null;
   AppLog.Info($"Actualización disponible | actual={current} remota={remote}");
   return new(remote,manifest.InstallerUrl,manifest.Notes);
  }
  catch(Exception ex){AppLog.Error("No se pudo comprobar si hay actualizaciones",ex);return null;}
 }

 public async Task<bool> DownloadAndInstallAsync(string installerUrl,IProgress<UpdateProgress>? progress,CancellationToken ct)
 {
  try
  {
   progress?.Report(new(null,"Conectando con el servidor…"));
   using var http=new HttpClient{Timeout=TimeSpan.FromMinutes(5)};
   using var response=await http.GetAsync(installerUrl,HttpCompletionOption.ResponseHeadersRead,ct);
   response.EnsureSuccessStatusCode();
   var total=response.Content.Headers.ContentLength;
   var path=Path.Combine(Path.GetTempPath(),"HoannaBeachSetup.exe");
   await using(var source=await response.Content.ReadAsStreamAsync(ct))
   await using(var target=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None,81920,true))
   {
    var buffer=new byte[81920];long received=0;int read;
    while((read=await source.ReadAsync(buffer,ct))>0){await target.WriteAsync(buffer.AsMemory(0,read),ct);received+=read;double? percent=total>0?received*100d/total:null;progress?.Report(new(percent,percent is null?$"Descargados {received/1048576d:F1} MB":$"Descargando… {percent:F0} %"));}
   }
   var bytes=new FileInfo(path).Length;
   AppLog.Audit($"Instalador de actualización descargado | ruta={path} bytes={bytes}");
   progress?.Report(new(100,"Descarga completada. Preparando instalación…"));
   Process.Start(new ProcessStartInfo(path,"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS"){UseShellExecute=true});
   AppLog.Audit("Instalador de actualización lanzado");
   return true;
  }
  catch(Exception ex){AppLog.Error("No se pudo descargar o iniciar la actualización",ex);return false;}
 }

 public static bool IsNewer(Version remote,Version current)=>Normalize(remote)>Normalize(current);
 private static Version Normalize(Version v)=>new(v.Major,Math.Max(v.Minor,0),Math.Max(v.Build,0),Math.Max(v.Revision,0));
}

public sealed class UpdateManifest{public string Version{get;set;}="";public string InstallerUrl{get;set;}="";public string? Notes{get;set;}}
public sealed record UpdateInfo(Version Version,string InstallerUrl,string? Notes);
public sealed record UpdateProgress(double? Percent,string Message);
