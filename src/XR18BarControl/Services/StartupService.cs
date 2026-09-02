using Microsoft.Win32;
namespace XR18BarControl.Services;
public static class StartupService{public static void Apply(bool enabled){try{using var k=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");if(enabled)k.SetValue("XR18BarControl",$"\"{Environment.ProcessPath}\"");else k.DeleteValue("XR18BarControl",false);}catch(Exception ex){AppLog.Error("No se pudo configurar el inicio con Windows",ex);}}}
