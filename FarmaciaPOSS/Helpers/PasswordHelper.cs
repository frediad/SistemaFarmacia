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

            return Convert.ToBase64String(bytes);
        }
    }
}