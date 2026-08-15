using System;

namespace FarmaciaPOS.Helpers
{
    public static class ImagenHelper
    {
        public static byte[]? Base64ToBytes(string? base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return null;

            try
            {
                // Por si el Base64 viene acompañado de: data:image/png;base64,...
                if (base64String.Contains(","))
                {
                    base64String = base64String.Substring(base64String.IndexOf(",") + 1);
                }

                return Convert.FromBase64String(base64String);
            }
            catch
            {
                return null;
            }
        }
    }
}