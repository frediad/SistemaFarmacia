using FarmaciaAPI.Models;
using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class UsuariosWindow : Window
    {
        int usuarioId = 0;

        // ✅ Lista fija de módulos del sistema
        readonly List<string> modulosDisponibles = new()
        {
            "Ventas",
            "Pedidos",
            "Productos",
            "Inventario",
            "Reportes",
            "Configuración",
            "Caja",
            "Usuarios y Roles",
            "Proveedores",
            "FarmaciaConfi",
            "Devoluciones",
            "Clientes",


        };

        ObservableCollection<ModuloPermiso> listaModulos = new();
      

        public UsuariosWindow()
        {
            InitializeComponent();

            CargarRoles();
            CargarUsuarios();
            InicializarModulos();
        }

        // =========================================
        // INICIALIZAR CHECKLIST VACÍO
        // =========================================

        private void InicializarModulos()
        {
            listaModulos.Clear();

            foreach (var modulo in modulosDisponibles)
            {
                listaModulos.Add(new ModuloPermiso
                {
                    NombreModulo = modulo,
                    TieneAcceso = false
                });
            }

            icModulos.ItemsSource = listaModulos;
        }

        // =========================================
        // CARGAR ROLES
        // =========================================

        private void CargarRoles()
        {
            List<Rol> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query = "SELECT * FROM Roles";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Rol
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString() ?? "",
                    Descripcion = reader["Descripcion"].ToString() ?? ""
                });
            }

            cbRoles.ItemsSource = lista;
        }

        // =========================================
        // CARGAR USUARIOS
        // =========================================

        private void CargarUsuarios()
        {
            List<Usuario> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT u.*, r.Nombre AS NombreRol
              FROM Usuarios u
              INNER JOIN Roles r ON u.RolId = r.Id
              WHERE u.Activo = 1
              ORDER BY u.Nombre";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Usuario
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString() ?? "",
                    Apellido = reader["Apellido"].ToString() ?? "",
                    UsuarioLogin = reader["UsuarioLogin"].ToString() ?? "",
                    Correo = reader["Correo"].ToString() ?? "",
                    Telefono = reader["Telefono"].ToString() ?? "",
                    RolId = Convert.ToInt32(reader["RolId"]),
                    Activo = Convert.ToBoolean(reader["Activo"]),
                    Rol = new Rol
                    {
                        Nombre = reader["NombreRol"].ToString() ?? ""
                    }
                });
            }

            dgUsuarios.ItemsSource = lista;
        }

        // =========================================
        // SELECCIONAR USUARIO
        // =========================================

        private void DgUsuarios_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is Usuario usuario)
            {
                usuarioId = usuario.Id;

                txtNombre.Text = usuario.Nombre;
                txtApellido.Text = usuario.Apellido;
                txtUsuarioLogin.Text = usuario.UsuarioLogin;
                txtCorreo.Text = usuario.Correo;
                txtTelefono.Text = usuario.Telefono;
                txtPassword.Password = "";

                // ✅ Mostrar el placeholder indicando que ya hay contraseña
                txtPlaceholderPassword.Visibility = Visibility.Visible;

                cbRoles.SelectedValue = usuario.RolId;

                chkActivo.IsChecked = usuario.Activo;

                CargarPermisosUsuario(usuario.Id);
            }
        }

        // =========================================
        // ✅ CARGAR PERMISOS DEL USUARIO SELECCIONADO
        // =========================================

        private void CargarPermisosUsuario(int idUsuario)
        {
            InicializarModulos();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT NombreModulo, TieneAcceso
              FROM PermisosUsuario
              WHERE UsuarioId = @UsuarioId";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UsuarioId", idUsuario);

            SqlDataReader reader = cmd.ExecuteReader();

            var permisosGuardados = new Dictionary<string, bool>();

            while (reader.Read())
            {
                permisosGuardados[reader["NombreModulo"].ToString() ?? ""] =
                    Convert.ToBoolean(reader["TieneAcceso"]);
            }

            foreach (var modulo in listaModulos)
            {
                if (permisosGuardados.ContainsKey(modulo.NombreModulo))
                {
                    modulo.TieneAcceso = permisosGuardados[modulo.NombreModulo];
                }
            }
        }

        // =========================================
        // NUEVO
        // =========================================

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            Limpiar();
        }

        private void Limpiar()
        {
            usuarioId = 0;

            txtNombre.Clear();
            txtApellido.Clear();
            txtUsuarioLogin.Clear();
            txtCorreo.Clear();
            txtTelefono.Clear();
            txtPassword.Password = "";

            txtPlaceholderPassword.Visibility = Visibility.Collapsed;

            cbRoles.SelectedIndex = -1;

            chkActivo.IsChecked = true;

            InicializarModulos();
        }

        // =========================================
        // GUARDAR USUARIO + PERMISOS
        // =========================================

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUsuarioLogin.Text))
                {
                    MessageBox.Show("Rellena todos los campos, por favor");
                    return;
                }

                if (cbRoles.SelectedValue == null)
                {
                    MessageBox.Show("Selecciona un rol");
                    return;
                }

                if (usuarioId == 0 && string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    MessageBox.Show("La contraseña es obligatoria para un usuario nuevo");
                    return;
                }

                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query;

                if (usuarioId == 0)
                {
                    query =
                    @"INSERT INTO Usuarios
                    (Nombre, Apellido, UsuarioLogin, Correo, PasswordHash, Telefono, RolId, Activo, FechaCreacion)
                    VALUES
                    (@Nombre, @Apellido, @UsuarioLogin, @Correo, @PasswordHash, @Telefono, @RolId, @Activo, GETDATE());
                    SELECT SCOPE_IDENTITY();";
                }
                else
                {
                    query =
                    @"UPDATE Usuarios SET
                        Nombre = @Nombre,
                        Apellido = @Apellido,
                        UsuarioLogin = @UsuarioLogin,
                        Correo = @Correo,
                        Telefono = @Telefono,
                        RolId = @RolId,
                        Activo = @Activo";

                    if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                    {
                        query += ", PasswordHash = @PasswordHash";
                    }

                    query += " WHERE Id = @Id";
                }

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text);
                cmd.Parameters.AddWithValue("@Apellido", txtApellido.Text);
                cmd.Parameters.AddWithValue("@UsuarioLogin", txtUsuarioLogin.Text);
                cmd.Parameters.AddWithValue("@Correo", txtCorreo.Text);
                cmd.Parameters.AddWithValue("@Telefono", txtTelefono.Text);
                cmd.Parameters.AddWithValue("@RolId", cbRoles.SelectedValue);
                cmd.Parameters.AddWithValue("@Activo", chkActivo.IsChecked ?? true);

                if (usuarioId == 0 || !string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    string hash = HashPassword(txtPassword.Password);
                    cmd.Parameters.AddWithValue("@PasswordHash", hash);
                }

                if (usuarioId != 0)
                {
                    cmd.Parameters.AddWithValue("@Id", usuarioId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var resultado = cmd.ExecuteScalar();
                    usuarioId = Convert.ToInt32(resultado);
                }

                // ✅ Guardar permisos
                GuardarPermisos(usuarioId);

                MessageBox.Show("Usuario guardado correctamente");

                CargarUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================
        // ✅ GUARDAR PERMISOS (borra e inserta de nuevo)
        // =========================================

        private void GuardarPermisos(int idUsuario)
        {
            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string deleteQuery = "DELETE FROM PermisosUsuario WHERE UsuarioId = @UsuarioId";
            SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn);
            deleteCmd.Parameters.AddWithValue("@UsuarioId", idUsuario);
            deleteCmd.ExecuteNonQuery();

            foreach (var modulo in listaModulos)
            {
                string insertQuery =
                @"INSERT INTO PermisosUsuario (UsuarioId, NombreModulo, TieneAcceso)
                  VALUES (@UsuarioId, @NombreModulo, @TieneAcceso)";

                SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@UsuarioId", idUsuario);
                insertCmd.Parameters.AddWithValue("@NombreModulo", modulo.NombreModulo);
                insertCmd.Parameters.AddWithValue("@TieneAcceso", modulo.TieneAcceso);

                insertCmd.ExecuteNonQuery();
            }
        }

        // =========================================
        // ELIMINAR
        // =========================================

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (usuarioId == 0)
                {
                    MessageBox.Show("Selecciona un usuario");
                    return;
                }

                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query = "UPDATE Usuarios SET Activo = 0 WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", usuarioId);

                int filasAfectadas = cmd.ExecuteNonQuery(); 

                if (filasAfectadas > 0)
                {
                    MessageBox.Show("Usuario eliminado correctamente");
                }
                else
                {
                    MessageBox.Show("No se encontró el usuario a eliminar");
                }

                Limpiar();
                CargarUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Si el usuario empieza a escribir, ocultamos el placeholder
            if (!string.IsNullOrEmpty(txtPassword.Password))
            {
                txtPlaceholderPassword.Visibility = Visibility.Collapsed;
            }
            else if (usuarioId != 0)
            {
                // Si lo borra todo y es un usuario existente, mostramos el placeholder de nuevo
                txtPlaceholderPassword.Visibility = Visibility.Visible;
            }
        }

        // =========================================
        // HASH DE CONTRASEÑA (SHA256)
        // =========================================

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}