using System.Runtime.InteropServices;using System.Security.Cryptography;
namespace XR18BarControl.Security;
public static class AdminPinService
{
 const int Iterations=210000; public static string Hash(string pin){var salt=CreateSalt();var hash=Pbkdf2(pin,salt,Iterations,32);return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";}
 public static bool Verify(string pin,string encoded){try{var p=encoded.Split('$');var salt=Convert.FromBase64String(p[2]);var expected=Convert.FromBase64String(p[3]);var actual=Pbkdf2(pin,salt,int.Parse(p[1]),expected.Length);return CryptographicOperations.FixedTimeEquals(actual,expected);}catch{return false;}}
 static byte[] CreateSalt(){var salt=new byte[16];if(!RtlGenRandom(salt,salt.Length))throw new CryptographicException("No se pudo obtener aleatoriedad segura");return salt;}
 static byte[] Pbkdf2(string pin,byte[] salt,int iterations,int length){using var hmac=new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(pin));var result=new byte[length];var block=new byte[salt.Length+4];Buffer.BlockCopy(salt,0,block,0,salt.Length);var offset=0;for(var index=1;offset<length;index++){block[^4]=(byte)(index>>24);block[^3]=(byte)(index>>16);block[^2]=(byte)(index>>8);block[^1]=(byte)index;var u=hmac.ComputeHash(block);var t=(byte[])u.Clone();for(var round=1;round<iterations;round++){u=hmac.ComputeHash(u);for(var j=0;j<t.Length;j++)t[j]^=u[j];}var count=Math.Min(t.Length,length-offset);Buffer.BlockCopy(t,0,result,offset,count);offset+=count;}return result;}
 [DllImport("advapi32.dll",EntryPoint="SystemFunction036",SetLastError=true)]static extern bool RtlGenRandom(byte[] buffer,int length);
}
