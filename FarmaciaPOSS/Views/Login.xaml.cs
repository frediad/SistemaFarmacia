using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
    public partial class LoginWindow : Window
    {
       

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

            string passwordHash = HashPassword(password);

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT U.*, R.Nombre AS Rol
            FROM Usuarios U
            INNER JOIN Roles R
            ON U.RolId = R.Id
            WHERE U.UsuarioLogin = @Usuario
            AND U.PasswordHash = @Password
            AND U.Activo = 1";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(
                "@Usuario",
                usuario);

            cmd.Parameters.AddWithValue(
                "@Password",
                passwordHash);

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

                Sesion.Rol =
                    reader["Rol"].ToString();

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

        
        private void Login_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}