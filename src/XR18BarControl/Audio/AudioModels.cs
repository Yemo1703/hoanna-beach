namespace XR18BarControl.Audio;
public enum AudioZoneId{Indoor,Terrace} public sealed record AudioZone(AudioZoneId Id,string DisplayName,OutputMapping Output);public sealed record AudioSource(string Id,string DisplayName);public sealed record OutputMapping(string Id,IReadOnlyList<string> FaderPaths,IReadOnlyList<string> OnPaths);
