using System.Windows;
using XR18BarControl.Configuration;
using XR18BarControl.Services;
using XR18BarControl.ViewModels;
using XR18BarControl.XR18;
namespace XR18BarControl;
public partial class App : Application
{
    private CancellationTokenSource? _shutdown; private XR18OscClient? _client;
    protected override async void OnStartup(StartupEventArgs e) { base.OnStartup(e);AppLog.Initialize();DispatcherUnhandledException += (_,x)=>{AppLog.Error("Excepción no controlada en la interfaz",x.Exception);x.Handled=true;};AppDomain.CurrentDomain.UnhandledException+=(_,x)=>AppLog.Error("Excepción fatal no controlada",x.ExceptionObject as Exception??new Exception(x.ExceptionObject?.ToString()));TaskScheduler.UnobservedTaskException+=(_,x)=>{AppLog.Error("Excepción no observada en tarea asíncrona",x.Exception);x.SetObserved();};AppLog.Info($"Aplicación iniciada | argumentos={string.Join(' ',e.Args)}");var configs=new ConfigService();var config=await configs.LoadAsync();StartupService.Apply(config.StartWithWindows);_shutdown=new();_client=new(config.Xr18.Ip,config.Xr18.Port);var vm=new MainViewModel(_client,configs,config);var window=new MainWindow(vm);MainWindow=window;window.Show();if(config.AutoConnect)_=vm.ConnectAsync(_shutdown.Token);else AppLog.Info("Conexión automática desactivada");if(e.Args.Contains("--diagnose-admin",StringComparer.OrdinalIgnoreCase)){AppLog.Audit("Diagnóstico automático: se abrirá administración en un segundo");await Task.Delay(1000);vm.AdminCommand.Execute(null);}_=CheckForUpdatesAsync(window);}
    protected override void OnExit(ExitEventArgs e){_shutdown?.Cancel();_client?.Dispose();_shutdown?.Dispose();AppLog.Info("Aplicación cerrada");base.OnExit(e);}
    private static async Task CheckForUpdatesAsync(Window owner)
    {
        var svc=new UpdateService(); var update=await svc.CheckAsync(CancellationToken.None); if(update is null)return;
        var dialog=new UpdateWindow(update.Version,update.Notes){Owner=owner};
        if(dialog.ShowDialog()==true){AppLog.Audit($"Actualización aceptada por el usuario | version={update.Version}");if(await svc.DownloadAndInstallAsync(update.InstallerUrl,CancellationToken.None))Current.Shutdown();}
        else AppLog.Audit($"Actualización pospuesta por el usuario | version={update.Version}");
    }
}
