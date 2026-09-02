using System.IO;
namespace XR18BarControl.Services;
public static class AppLog
{
 static readonly object Gate=new();static string?_path;
 public static string LogPath=>_path??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"XR18BarControl","logs","app.log");
 public static void Initialize(){var d=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"XR18BarControl","logs");Directory.CreateDirectory(d);_path=Path.Combine(d,"app.log");if(File.Exists(_path)&&new FileInfo(_path).Length>2000000)File.Move(_path,Path.Combine(d,"app.previous.log"),true);Write("SESSION",$"Nueva sesión | SO={Environment.OSVersion} | Runtime={Environment.Version} | Proceso={Environment.ProcessPath}",null);}
 public static void Info(string m)=>Write("INFO",m,null);public static void Audit(string m)=>Write("AUDIT",m,null);public static void Error(string m,Exception e)=>Write("ERROR",m,e);
 static void Write(string l,string m,Exception?e){try{lock(Gate){RotateIfNeeded();File.AppendAllText(_path!,$"{DateTimeOffset.Now:O} [{Environment.CurrentManagedThreadId}] {l} {m}{(e is null?"":$"{Environment.NewLine}{e}")}{Environment.NewLine}");}}catch{}}
 static void RotateIfNeeded(){if(_path is null||!File.Exists(_path)||new FileInfo(_path).Length<=2000000)return;var d=Path.GetDirectoryName(_path)!;File.Move(_path,Path.Combine(d,"app.previous.log"),true);}
}
