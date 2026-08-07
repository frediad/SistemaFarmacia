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
        bool passwordVisible = false;

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
            try
            {
                int? seleccionAnterior = cbRoles.SelectedValue as int?;

                List<Rol> lista = new();

                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query = "SELECT * FROM Roles ORDER BY Nombre";

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

                if (seleccionAnterior.HasValue)
                    cbRoles.SelectedValue = seleccionAnterior.Value;
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudieron cargar los roles: " + ex.Message, "Error", this);
            }
        }

        // =========================================
        // ✅ GESTIONAR ROLES (agregar/editar/eliminar)
        // =========================================

        private void BtnGestionarRoles_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new GestionRolesWindow
            {
                Owner = this
            };

            ventana.ShowDialog();

            if (ventana.HuboCambios)
            {
                CargarRoles();
            }
        }

        // =========================================
        // CARGAR USUARIOS
        // =========================================

        private void CargarUsuarios()
        {
            try
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
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudieron cargar los usuarios: " + ex.Message, "Error", this);
            }
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
                txtPasswordVisible.Text = "";

                txtPlaceholderPassword.Visibility = Visibility.Visible;

                cbRoles.SelectedValue = usuario.RolId;

                chkActivo.IsChecked = usuario.Activo;

                CargarPermisosUsuario(usuario.Id);
            }
        }

        // =========================================
        // CARGAR PERMISOS DEL USUARIO SELECCIONADO
        // =========================================

        private void CargarPermisosUsuario(int idUsuario)
        {
            InicializarModulos();

            try
            {
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
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudieron cargar los permisos: " + ex.Message, "Error", this);
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
            txtPasswordVisible.Text = "";

            txtPlaceholderPassword.Visibility = Visibility.Collapsed;

            cbRoles.SelectedIndex = -1;

            chkActivo.IsChecked = true;

            dgUsuarios.SelectedIndex = -1;

            InicializarModulos();
        }

        // =========================================
        // ✅ VALIDACIÓN COMPARTIDA
        // =========================================

        private bool ValidarFormulario(bool esUsuarioNuevo)
        {
            if (string.IsNullOrWhiteSpace(txtUsuarioLogin.Text))
            {
                MensajeHelper.Advertencia("Rellena todos los campos, por favor", "Aviso", this);
                return false;
            }

            if (cbRoles.SelectedValue == null)
            {
                MensajeHelper.Advertencia("Selecciona un rol", "Aviso", this);
                return false;
            }

            if (esUsuarioNuevo && string.IsNullOrWhiteSpace(ObtenerPasswordActual()))
            {
                MensajeHelper.Advertencia("La contraseña es obligatoria para un usuario nuevo", "Aviso", this);
                return false;
            }

            return true;
        }

        // ✅ Obtiene la contraseña sin importar si está visible u oculta en ese momento
        private string ObtenerPasswordActual()
        {
            return passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;
        }

        // =========================================
        // ✅ GUARDAR — SOLO PARA USUARIOS NUEVOS
        // =========================================

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (usuarioId != 0)
            {
                MensajeHelper.Advertencia(
                    "Ya tienes un usuario seleccionado. Usa \"Actualizar\" para modificarlo, o \"Nuevo\" para crear otro.",
                    "Aviso",
                    this);
                return;
            }

            if (!ValidarFormulario(esUsuarioNuevo: true))
                return;

            try
            {
                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                // ✅ Verifica que no exista ya un usuario con el mismo login o correo
                if (ExisteUsuarioDuplicado(conn, idExcluir: 0))
                    return;

                string query =
                @"INSERT INTO Usuarios
                (Nombre, Apellido, UsuarioLogin, Correo, PasswordHash, Telefono, RolId, Activo, FechaCreacion)
                VALUES
                (@Nombre, @Apellido, @UsuarioLogin, @Correo, @PasswordHash, @Telefono, @RolId, @Activo, GETDATE());
                SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text);
                cmd.Parameters.AddWithValue("@Apellido", txtApellido.Text);
                cmd.Parameters.AddWithValue("@UsuarioLogin", txtUsuarioLogin.Text.Trim());
                cmd.Parameters.AddWithValue("@Correo", txtCorreo.Text.Trim());
                cmd.Parameters.AddWithValue("@Telefono", txtTelefono.Text);
                cmd.Parameters.AddWithValue("@RolId", cbRoles.SelectedValue);
                cmd.Parameters.AddWithValue("@Activo", chkActivo.IsChecked ?? true);
                cmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.Hashear(ObtenerPasswordActual()));

                var resultado = cmd.ExecuteScalar();
                usuarioId = Convert.ToInt32(resultado);

                GuardarPermisos(usuarioId);

                MensajeHelper.Exito("Usuario creado correctamente", "Listo", this);

                CargarUsuarios();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "ERROR", this);
            }
        }

        // =========================================
        // ✅ ACTUALIZAR — SOLO PARA USUARIOS EXISTENTES
        // =========================================

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (usuarioId == 0)
            {
                MensajeHelper.Advertencia("Selecciona un usuario de la lista para actualizar", "Aviso", this);
                return;
            }

            if (!ValidarFormulario(esUsuarioNuevo: false))
                return;

            try
            {
                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                // ✅ Verifica duplicados, excluyendo al usuario que se está editando
                if (ExisteUsuarioDuplicado(conn, idExcluir: usuarioId))
                    return;

                string query =
                @"UPDATE Usuarios SET
                Nombre = @Nombre,
                Apellido = @Apellido,
                UsuarioLogin = @UsuarioLogin,
                Correo = @Correo,
                Telefono = @Telefono,
                RolId = @RolId,
                Activo = @Activo";

                string passwordActual = ObtenerPasswordActual();
                bool cambioPassword = !string.IsNullOrWhiteSpace(passwordActual);

                if (cambioPassword)
                {
                    query += ", PasswordHash = @PasswordHash";
                }

                query += " WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text);
                cmd.Parameters.AddWithValue("@Apellido", txtApellido.Text);
                cmd.Parameters.AddWithValue("@UsuarioLogin", txtUsuarioLogin.Text.Trim());
                cmd.Parameters.AddWithValue("@Correo", txtCorreo.Text.Trim());
                cmd.Parameters.AddWithValue("@Telefono", txtTelefono.Text);
                cmd.Parameters.AddWithValue("@RolId", cbRoles.SelectedValue);
                cmd.Parameters.AddWithValue("@Activo", chkActivo.IsChecked ?? true);
                cmd.Parameters.AddWithValue("@Id", usuarioId);

                if (cambioPassword)
                {
                    cmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.Hashear(passwordActual));
                }

                cmd.ExecuteNonQuery();

                GuardarPermisos(usuarioId);

                MensajeHelper.Exito("Usuario actualizado correctamente", "Listo", this);

                CargarUsuarios();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "ERROR", this);
            }
        }

        // =========================================
        // VERIFICAR DUPLICADOS (LOGIN O CORREO)
        // =========================================

        private bool ExisteUsuarioDuplicado(SqlConnection conn, int idExcluir)
        {
            string login = txtUsuarioLogin.Text.Trim();
            string correo = txtCorreo.Text.Trim();

            string queryLogin =
            @"SELECT COUNT(*) FROM Usuarios
            WHERE UsuarioLogin = @UsuarioLogin AND Id <> @IdExcluir";

            SqlCommand cmdLogin = new SqlCommand(queryLogin, conn);
            cmdLogin.Parameters.AddWithValue("@UsuarioLogin", login);
            cmdLogin.Parameters.AddWithValue("@IdExcluir", idExcluir);

            if (Convert.ToInt32(cmdLogin.ExecuteScalar()) > 0)
            {
                MensajeHelper.Advertencia(
                    $"Ya existe un usuario con el nombre de usuario \"{login}\". Elige otro.",
                    "Usuario duplicado",
                    this);
                return true;
            }

            if (!string.IsNullOrWhiteSpace(correo))
            {
                string queryCorreo =
                @"SELECT COUNT(*) FROM Usuarios
                WHERE Correo = @Correo AND Id <> @IdExcluir";

                SqlCommand cmdCorreo = new SqlCommand(queryCorreo, conn);
                cmdCorreo.Parameters.AddWithValue("@Correo", correo);
                cmdCorreo.Parameters.AddWithValue("@IdExcluir", idExcluir);

                if (Convert.ToInt32(cmdCorreo.ExecuteScalar()) > 0)
                {
                    MensajeHelper.Advertencia(
                        $"Ya existe un usuario con el correo \"{correo}\". Elige otro.",
                        "Correo duplicado",
                        this);
                    return true;
                }
            }

            return false;
        }

        // =========================================
        // GUARDAR PERMISOS
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
                    MensajeHelper.Advertencia("Selecciona un usuario", "Aviso", this);
                    return;
                }

                bool confirmar = MensajeHelper.Confirmar(
                    "¿Eliminar este usuario? Podrás reactivarlo más adelante si es necesario.",
                    "Confirmar",
                    this);

                if (!confirmar)
                    return;

                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query = "UPDATE Usuarios SET Activo = 0 WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", usuarioId);

                int filasAfectadas = cmd.ExecuteNonQuery();

                if (filasAfectadas > 0)
                {
                    MensajeHelper.Exito("Usuario eliminado correctamente", "Listo", this);
                }
                else
                {
                    MensajeHelper.Advertencia("No se encontró el usuario a eliminar", "Aviso", this);
                }

                Limpiar();
                CargarUsuarios();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "Error", this);
            }
        }

        // =========================================
        // ✅ MOSTRAR/OCULTAR CONTRASEÑA
        // =========================================

        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            passwordVisible = !passwordVisible;

            if (passwordVisible)
            {
                // Pasa el valor actual del PasswordBox al TextBox visible
                txtPasswordVisible.Text = txtPassword.Password;

                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;

                btnTogglePassword.Content = "🙈";

                if (!string.IsNullOrEmpty(txtPasswordVisible.Text))
                    txtPlaceholderPassword.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Pasa el valor actual del TextBox de vuelta al PasswordBox
                txtPassword.Password = txtPasswordVisible.Text;

                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;

                btnTogglePassword.Content = "👁";

                if (!string.IsNullOrEmpty(txtPassword.Password))
                    txtPlaceholderPassword.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPassword.Password))
            {
                txtPlaceholderPassword.Visibility = Visibility.Collapsed;
            }
            else if (usuarioId != 0)
            {
                txtPlaceholderPassword.Visibility = Visibility.Visible;
            }
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPasswordVisible.Text))
            {
                txtPlaceholderPassword.Visibility = Visibility.Collapsed;
            }
            else if (usuarioId != 0)
            {
                txtPlaceholderPassword.Visibility = Visibility.Visible;
            }
        }


        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}