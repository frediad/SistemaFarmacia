using FarmaciaAPI.Models;
using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class ClientesWindow : Window
    {
        private List<Cliente> clientes = new();
        private int clienteId = 0;

        public ClientesWindow()
        {
            InitializeComponent();

            CargarClientes();
        }

        // =========================================
        // CARGAR CLIENTES
        // =========================================

        private void CargarClientes()
        {
            clientes.Clear();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query = "SELECT * FROM Clientes ORDER BY Nombre";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                clientes.Add(new Cliente
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString(),
                    Telefono = reader["Telefono"]?.ToString() ?? "",
                    Correo = reader["Correo"]?.ToString() ?? "",
                    Direccion = reader["Direccion"]?.ToString() ?? "",
                    RFC = reader["RFC"]?.ToString() ?? "",
                    LimiteCredito = Convert.ToDecimal(reader["LimiteCredito"]),
                    SaldoActual = Convert.ToDecimal(reader["SaldoActual"]),
                    FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"]),
                    Activo = Convert.ToBoolean(reader["Activo"])
                });
            }

            dgClientes.ItemsSource = null;
            dgClientes.ItemsSource = clientes;
        }

        // =========================================
        // BUSCAR
        // =========================================

        private void txtBuscarCliente_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscarCliente.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(texto))
            {
                dgClientes.ItemsSource = clientes;
                return;
            }

            dgClientes.ItemsSource = clientes
                .Where(c =>
                    c.Nombre.ToLower().Contains(texto) ||
                    c.Telefono.ToLower().Contains(texto) ||
                    c.Correo.ToLower().Contains(texto))
                .ToList();
        }

        // =========================================
        // SELECCIONAR CLIENTE DE LA LISTA
        // =========================================

        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClientes.SelectedItem is not Cliente cliente)
                return;

            clienteId = cliente.Id;

            txtTituloForm.Text = "Editar Cliente";
            txtNombre.Text = cliente.Nombre;
            txtTelefono.Text = cliente.Telefono;
            txtCorreo.Text = cliente.Correo;
            txtDireccion.Text = cliente.Direccion;
            txtRFC.Text = cliente.RFC;
            txtLimiteCredito.Text = cliente.LimiteCredito.ToString();
            chkActivo.IsChecked = cliente.Activo;

            txtSaldoInfo.Text = $"Saldo actual: {cliente.SaldoActual:C}  |  Disponible: {cliente.CreditoDisponible:C}";
            txtSaldoInfo.Foreground = cliente.SaldoActual > 0
                ? System.Windows.Media.Brushes.DarkOrange
                : System.Windows.Media.Brushes.Green;

            CargarHistorialCompras(cliente.Id);
            CargarHistorialAbonos(cliente.Id);
        }

        // =========================================
        // NUEVO / LIMPIAR
        // =========================================

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            Limpiar();
        }

        private void Limpiar()
        {
            clienteId = 0;

            txtTituloForm.Text = "Nuevo Cliente";
            txtNombre.Clear();
            txtTelefono.Clear();
            txtCorreo.Clear();
            txtDireccion.Clear();
            txtRFC.Clear();
            txtLimiteCredito.Text = "0";
            chkActivo.IsChecked = true;

            txtSaldoInfo.Text = "Saldo actual: $0.00";
            txtSaldoInfo.Foreground = System.Windows.Media.Brushes.Gray;

            txtMontoAbono.Clear();
            txtMotivoAbono.Clear();

            dgHistorialCompras.ItemsSource = null;
            dgHistorialAbonos.ItemsSource = null;

            dgClientes.SelectedItem = null;
        }

        // =========================================
        // GUARDAR (INSERTAR O ACTUALIZAR)
        // =========================================

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre del cliente es obligatorio");
                    return;
                }

                if (!decimal.TryParse(txtLimiteCredito.Text, out decimal limiteCredito) || limiteCredito < 0)
                {
                    MessageBox.Show("Ingresa un límite de crédito válido");
                    return;
                }

                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query;

                if (clienteId == 0)
                {
                    query =
                    @"INSERT INTO Clientes
                    (Nombre, Telefono, Correo, Direccion, RFC, LimiteCredito, SaldoActual, FechaRegistro, Activo)
                    VALUES
                    (@Nombre, @Telefono, @Correo, @Direccion, @RFC, @LimiteCredito, 0, GETDATE(), @Activo);
                    SELECT SCOPE_IDENTITY();";
                }
                else
                {
                    query =
                    @"UPDATE Clientes SET
                        Nombre = @Nombre,
                        Telefono = @Telefono,
                        Correo = @Correo,
                        Direccion = @Direccion,
                        RFC = @RFC,
                        LimiteCredito = @LimiteCredito,
                        Activo = @Activo
                      WHERE Id = @Id";
                }

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text.Trim());
                cmd.Parameters.AddWithValue("@Telefono", txtTelefono.Text.Trim());
                cmd.Parameters.AddWithValue("@Correo", txtCorreo.Text.Trim());
                cmd.Parameters.AddWithValue("@Direccion", txtDireccion.Text.Trim());
                cmd.Parameters.AddWithValue("@RFC", txtRFC.Text.Trim());
                cmd.Parameters.AddWithValue("@LimiteCredito", limiteCredito);
                cmd.Parameters.AddWithValue("@Activo", chkActivo.IsChecked ?? true);

                if (clienteId == 0)
                {
                    var resultado = cmd.ExecuteScalar();
                    clienteId = Convert.ToInt32(resultado);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@Id", clienteId);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Cliente guardado correctamente");

                CargarClientes();

                // Vuelve a seleccionar el cliente recién guardado
                var clienteActual = clientes.FirstOrDefault(c => c.Id == clienteId);
                if (clienteActual != null)
                    dgClientes.SelectedItem = clienteActual;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================
        // ELIMINAR
        // =========================================

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (clienteId == 0)
            {
                MessageBox.Show("Selecciona un cliente de la lista");
                return;
            }

            var cliente = clientes.FirstOrDefault(c => c.Id == clienteId);

            if (cliente != null && cliente.SaldoActual > 0)
            {
                MessageBox.Show(
                    $"No puedes eliminar a \"{cliente.Nombre}\" porque tiene un saldo pendiente de {cliente.SaldoActual:C}.\n" +
                    "Registra el abono correspondiente antes de eliminarlo, o márcalo como inactivo.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var confirmar = MessageBox.Show(
                "¿Eliminar este cliente? Esta acción no se puede deshacer.",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes)
                return;

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query = "DELETE FROM Clientes WHERE Id = @Id";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", clienteId);
            cmd.ExecuteNonQuery();

            MessageBox.Show("Cliente eliminado");

            Limpiar();
            CargarClientes();
        }

        // =========================================
        // ABONOS / PAGOS A CUENTA
        // =========================================

        private void BtnRegistrarAbono_Click(object sender, RoutedEventArgs e)
        {
            if (clienteId == 0)
            {
                MessageBox.Show("Selecciona un cliente para registrar el abono");
                return;
            }

            if (!decimal.TryParse(txtMontoAbono.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingresa un monto de abono válido");
                return;
            }

            var cliente = clientes.FirstOrDefault(c => c.Id == clienteId);

            if (cliente != null && monto > cliente.SaldoActual)
            {
                var confirmar = MessageBox.Show(
                    $"El abono ({monto:C}) es mayor al saldo pendiente ({cliente.SaldoActual:C}). " +
                    "El saldo quedará en $0.00 y no se generará saldo a favor.\n\n¿Continuar?",
                    "Aviso",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmar != MessageBoxResult.Yes)
                    return;

                monto = cliente.SaldoActual; // no permitir saldo negativo
            }

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string queryAbono =
            @"INSERT INTO AbonosCliente (ClienteId, Monto, Motivo, UsuarioId, Fecha)
              VALUES (@ClienteId, @Monto, @Motivo, @UsuarioId, GETDATE())";

            SqlCommand cmdAbono = new SqlCommand(queryAbono, conn);
            cmdAbono.Parameters.AddWithValue("@ClienteId", clienteId);
            cmdAbono.Parameters.AddWithValue("@Monto", monto);
            cmdAbono.Parameters.AddWithValue("@Motivo",
                string.IsNullOrWhiteSpace(txtMotivoAbono.Text) ? (object)DBNull.Value : txtMotivoAbono.Text.Trim());
            cmdAbono.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
            cmdAbono.ExecuteNonQuery();

            string queryActualizarSaldo =
                "UPDATE Clientes SET SaldoActual = SaldoActual - @Monto WHERE Id = @ClienteId";

            SqlCommand cmdSaldo = new SqlCommand(queryActualizarSaldo, conn);
            cmdSaldo.Parameters.AddWithValue("@Monto", monto);
            cmdSaldo.Parameters.AddWithValue("@ClienteId", clienteId);
            cmdSaldo.ExecuteNonQuery();

            MessageBox.Show($"Abono de {monto:C} registrado correctamente");

            txtMontoAbono.Clear();
            txtMotivoAbono.Clear();

            CargarClientes();

            var clienteActualizado = clientes.FirstOrDefault(c => c.Id == clienteId);
            if (clienteActualizado != null)
            {
                dgClientes.SelectedItem = clienteActualizado;
            }
        }

        private void CargarHistorialAbonos(int idCliente)
        {
            List<AbonoClienteView> lista = new();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT Monto, Motivo, Fecha
              FROM AbonosCliente
              WHERE ClienteId = @ClienteId
              ORDER BY Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ClienteId", idCliente);

            SqlDataReader reader = cmd.ExecuteReader();

            var abonosTemp = new List<(decimal Monto, string Motivo, DateTime Fecha)>();

            while (reader.Read())
            {
                abonosTemp.Add((
                    Convert.ToDecimal(reader["Monto"]),
                    reader["Motivo"]?.ToString() ?? "",
                    Convert.ToDateTime(reader["Fecha"])
                ));
            }

            reader.Close();

            // Recalcula el saldo "después de" cada abono, retrocediendo desde el saldo actual
            var cliente = clientes.FirstOrDefault(c => c.Id == idCliente);
            decimal saldoActual = cliente?.SaldoActual ?? 0;

            foreach (var abono in abonosTemp) // ya vienen del más reciente al más antiguo
            {
                lista.Add(new AbonoClienteView
                {
                    Fecha = abono.Fecha,
                    Monto = abono.Monto,
                    Motivo = abono.Motivo,
                    SaldoDespues = saldoActual
                });

                saldoActual += abono.Monto; // reconstruye el saldo antes de este abono
            }

            dgHistorialAbonos.ItemsSource = lista;
        }

        // =========================================
        // HISTORIAL DE COMPRAS DEL CLIENTE
        // =========================================

        private void CargarHistorialCompras(int idCliente)
        {
            List<CompraClienteView> lista = new();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT Id, Fecha, Total, EsCredito
              FROM Ventas
              WHERE ClienteId = @ClienteId
              AND Estado = 'Completada'
              ORDER BY Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ClienteId", idCliente);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new CompraClienteView
                {
                    VentaId = Convert.ToInt32(reader["Id"]),
                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                    Total = Convert.ToDecimal(reader["Total"]),
                    TipoPago = Convert.ToBoolean(reader["EsCredito"]) ? "Crédito" : "Contado"
                });
            }

            dgHistorialCompras.ItemsSource = lista;
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}