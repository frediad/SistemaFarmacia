using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class CrearAdmin : Window
    {
        public bool UsuarioCreado { get; private set; } = false;

        public CrearAdmin()
        {
            InitializeComponent();
        }

        private void BtnCrear_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreCompleto.Text.Trim();
            string apellido = txtApellido.Text.Trim();
            string correo = txtCorreo.Text.Trim();
            string telefono = txtTelefono.Text.Trim();
            string usuario = txtUsuario.Text.Trim();
            string password = txtPassword.Password;
            string passwordConfirmar = txtPasswordConfirmar.Password;

            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(telefono) ||
                string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(password))
            {
                MensajeHelper.Error("Todos los campos son obligatorios.");
                return;
            }

            if (password.Length < 6)
            {
                MensajeHelper.Error("La contraseña debe tener al menos 6 caracteres.");
                return;
            }

            if (password != passwordConfirmar)
            {
                MensajeHelper.Error("Las contraseñas no coinciden.");
                return;
            }

            try
            {
                // Verificación dentro del try: si falla la conexión, se muestra el error
                // en pantalla en vez de crashear la app.
                if (UsuarioSetupHelper.ExisteAdministrador())
                {
                    MensajeHelper.Error("Ya existe un usuario administrador. Cierra esta ventana e inicia sesión normalmente.");
                    return;
                }

                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string queryExiste = "SELECT COUNT(*) FROM Usuarios WHERE UsuarioLogin = @Usuario";
                SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
                cmdExiste.Parameters.AddWithValue("@Usuario", usuario);

                if ((int)cmdExiste.ExecuteScalar() > 0)
                {
                    MensajeHelper.Error("Ese nombre de usuario ya está en uso.");
                    return;
                }

                string queryRol = "SELECT Id FROM Roles WHERE Nombre = 'Administrador'";
                SqlCommand cmdRol = new SqlCommand(queryRol, conn);
                object resultadoRol = cmdRol.ExecuteScalar();

                if (resultadoRol == null)
                {
                    MensajeHelper.Error("No se encontró el rol 'Administrador' en la base de datos. Verifica la tabla Roles.");
                    return;
                }

                int rolId = Convert.ToInt32(resultadoRol);

                string hash = PasswordHelper.Hashear(password);

                string query =
                @"INSERT INTO Usuarios
                (Nombre, Apellido, Correo, Telefono, UsuarioLogin, PasswordHash, RolId, Activo, FechaCreacion)
                VALUES
                (@Nombre, @Apellido, @Correo, @Telefono, @UsuarioLogin, @PasswordHash, @RolId, @Activo, @FechaCreacion)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Apellido", apellido);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Telefono", telefono);
                cmd.Parameters.AddWithValue("@UsuarioLogin", usuario);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                cmd.Parameters.AddWithValue("@RolId", rolId);
                cmd.Parameters.AddWithValue("@Activo", true);
                cmd.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

                cmd.ExecuteNonQuery();

                UsuarioCreado = true;

                MensajeHelper.Exito(
                    "Usuario administrador creado correctamente. Ya puedes iniciar sesión.",
                    "Éxito");

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("Error al crear el usuario: " + ex.Message);
            }
        }


        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}