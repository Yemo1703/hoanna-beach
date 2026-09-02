using System.ComponentModel;using System.Runtime.CompilerServices;
namespace XR18BarControl.ViewModels;
public abstract class ObservableObject:INotifyPropertyChanged{public event PropertyChangedEventHandler?PropertyChanged;protected bool Set<T>(ref T f,T v,[CallerMemberName]string?n=null){if(EqualityComparer<T>.Default.Equals(f,v))return false;f=v;PropertyChanged?.Invoke(this,new(n));return true;}protected void Raise([CallerMemberName]string?n=null)=>PropertyChanged?.Invoke(this,new(n));}
