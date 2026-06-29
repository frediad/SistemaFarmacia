using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class LoginWindow : Window
    {
        string connectionString =
            @"Server=.\SQLEXPRESS;
              Database=FarmaciaDB;
              Trusted_Connection=True;
              TrustServerCertificate=True;";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(
            object sender,
            RoutedEventArgs e)
        {
            string usuario =
                txtUsuario.Text;

            string password =
                txtPassword.Password;

            using SqlConnection conn =
                new SqlConnection(connectionString);

            conn.Open();

            string query =
            @"SELECT *
              FROM Usuarios
              WHERE UsuarioLogin = @Usuario
              AND PasswordHash = @Password";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(
                "@Usuario",
                usuario);

            cmd.Parameters.AddWithValue(
                "@Password",
                password);

            SqlDataReader reader =
                cmd.ExecuteReader();

            if (reader.Read())
            {
                Sesion.UsuarioId =
                Convert.ToInt32(reader["Id"]);

                Sesion.NombreUsuario =
                    reader["Nombre"].ToString();

                Sesion.RolId =
                    Convert.ToInt32(reader["RolId"]);

                MainWindow main =
                    new MainWindow();

                main.Show();

                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "Usuario o contraseña incorrectos");
            }
        }
    }
}