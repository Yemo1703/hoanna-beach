using System.Windows;using System.Windows.Input;using System.Windows.Threading;using XR18BarControl.Services;using XR18BarControl.ViewModels;
namespace XR18BarControl;
public partial class MainWindow:Window
{
 readonly DispatcherTimer _hold=new(){Interval=TimeSpan.FromSeconds(2)};readonly MainViewModel _vm;
 public MainWindow(MainViewModel vm){InitializeComponent();DataContext=_vm=vm;_hold.Tick+=(_,_)=>{_hold.Stop();_vm.AdminCommand.Execute(null);};}
 void Admin_MouseDown(object s,MouseButtonEventArgs e){AppLog.Audit("Pulsación prolongada de AJUSTES iniciada");_hold.Stop();_hold.Start();}void Admin_MouseUp(object s,MouseButtonEventArgs e){AppLog.Audit("Pulsación de AJUSTES terminada");_hold.Stop();}void Admin_MouseLeave(object s,MouseEventArgs e)=>_hold.Stop();
}
