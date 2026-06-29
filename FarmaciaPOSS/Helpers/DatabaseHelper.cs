namespace FarmaciaPOS.Helpers
{
    public static class DatabaseHelper
    {
        public static string ConnectionString =
            @"Server=tcp:farmaciaserver2026.database.windows.net,1433;
              Initial Catalog=farmaciaDB;Persist Security Info=False; 
              User ID=Freddi;Password=Server4126;MultipleActiveResultSets=False;
              Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    }
}