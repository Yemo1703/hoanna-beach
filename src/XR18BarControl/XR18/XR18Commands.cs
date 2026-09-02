using XR18BarControl.Audio;
namespace XR18BarControl.XR18;
public static class XR18Commands
{
 public const string MainFader="/lr/mix/fader",MainOn="/lr/mix/on",Bus1Fader="/bus/1/mix/fader",Bus1On="/bus/1/mix/on",Bus2Fader="/bus/2/mix/fader",Bus2On="/bus/2/mix/on";
 public static readonly IReadOnlyDictionary<string,string> OutputChoices=new Dictionary<string,string>{{"mainLR","Main LR"},{"bus1","Aux 1"},{"bus2","Aux 2"},{"bus3","Aux 3"},{"bus4","Aux 4"},{"bus5","Aux 5"},{"bus6","Aux 6"},{"bus1_2","Aux 1/2 enlazados"},{"bus3_4","Aux 3/4 enlazados"},{"bus5_6","Aux 5/6 enlazados"}};
 public static readonly HashSet<string> StatePaths=BuildStatePaths();
 public static OutputMapping ResolveOutput(string id)=>id switch{"mainLR"=>new(id,[MainFader],[MainOn]),"bus1_2"=>ForBuses(id,1,2),"bus3_4"=>ForBuses(id,3,4),"bus5_6"=>ForBuses(id,5,6),_ when id.StartsWith("bus")&&int.TryParse(id[3..],out var bus)&&bus is>=1 and<=6=>ForBuses(id,bus),_=>throw new InvalidOperationException($"Salida no permitida: {id}")};
 static OutputMapping ForBuses(string id,params int[] buses)=>new(id,buses.Select(x=>$"/bus/{x}/mix/fader").ToArray(),buses.Select(x=>$"/bus/{x}/mix/on").ToArray());
 static HashSet<string> BuildStatePaths(){var paths=new HashSet<string>{MainFader,MainOn};for(var i=1;i<=6;i++){paths.Add($"/bus/{i}/mix/fader");paths.Add($"/bus/{i}/mix/on");}return paths;}
}
