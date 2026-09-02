namespace XR18BarControl.Audio;
public static class VolumeMapper
{
 public static double PercentToDb(double percent){percent=Math.Clamp(percent,0,100);if(percent<=0)return double.NegativeInfinity;return Math.Log10(percent/100)*40;}
 public static double DbToPercent(double db){if(double.IsNegativeInfinity(db)||db<=-90)return 0;return Math.Clamp(Math.Pow(10,db/40)*100,0,100);}
 public static float DbToOsc(double db){if(double.IsNegativeInfinity(db)||db<=-90)return 0;return(float)Math.Clamp(db switch{<-60=>(db+90)/480,<-30=>(db+70)/160,<-10=>(db+50)/80,_=>(db+30)/40},0,1);}
 public static double OscToDb(float value){value=Math.Clamp(value,0,1);if(value<=0)return double.NegativeInfinity;return value switch{<.0625f=>value*480-90,<.25f=>value*160-70,<.5f=>value*80-50,_=>value*40-30};}
}
