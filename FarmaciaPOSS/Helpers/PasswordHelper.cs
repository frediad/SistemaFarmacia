using System.Security.Cryptography;
using System.Text;

namespace FarmaciaPOS.Helpers
{
    public static class PasswordHelper
    {
        public static string Hashear(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            var sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}