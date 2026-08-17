using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FarmaciaPOS.Models;

namespace FarmaciaPOS.Views
{
    public partial class ClientesWindow : Window
    {
        private List<Cliente> clientes = new();

        // Cliente que se está dando de alta/editando en la pestaña 1
        private int clienteId = 0;

        // Cliente cuyo historial se está viendo en la pestaña 3 (independiente
        // del que se está editando, para no mezclar los dos flujos)
        private int clienteIdHistorial = 0;

        // ✅ Filtro de periodo activo para Historial de Compras y de Abonos
        private string filtroHistorialActivo = "Todo";

        public ClientesWindow()
        {
            InitializeComponent();

            CargarClientes();
        }

        // =========================================
        // CARGAR CLIENTES (fuente única para las 3 pestañas)
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

            RefrescarListasClientes();
        }

        // Refresca las dos listas dependientes (grid de "Clientes Registrados"
        // y combo de "Historial de Cliente") a partir de la lista maestra —
        // se llama cada vez que la lista maestra cambia (alta/edición/borrado).
        private void RefrescarListasClientes()
        {
            string textoBusquedaRegistrados = txtBuscarClientesRegistrados?.Text?.Trim().ToLower() ?? "";

            dgClientesRegistrados.ItemsSource = string.IsNullOrWhiteSpace(textoBusquedaRegistrados)
                ? clientes
                : clientes.Where(c =>
                    c.Nombre.ToLower().Contains(textoBusquedaRegistrados) ||
                    c.Telefono.ToLower().Contains(textoBusquedaRegistrados) ||
                    c.Correo.ToLower().Contains(textoBusquedaRegistrados))
                  .ToList();

            string textoBusquedaHistorial = txtBuscarClienteHistorial?.Text?.Trim().ToLower() ?? "";

            cbClienteHistorial.ItemsSource = string.IsNullOrWhiteSpace(textoBusquedaHistorial)
                ? clientes
                : clientes.Where(c => c.Nombre.ToLower().Contains(textoBusquedaHistorial)).ToList();
        }

        // =========================================
        // ✅ PESTAÑA 2: CLIENTES REGISTRADOS
        // =========================================

        private void txtBuscarClientesRegistrados_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscarClientesRegistrados.Text.Trim().ToLower();

            dgClientesRegistrados.ItemsSource = string.IsNullOrWhiteSpace(texto)
                ? clientes
                : clientes.Where(c =>
                    c.Nombre.ToLower().Contains(texto) ||
                    c.Telefono.ToLower().Contains(texto) ||
                    c.Correo.ToLower().Contains(texto))
                  .ToList();
        }

        // ✅ Botón "✏️ Editar" en cada fila del listado — carga ese cliente
        // en el formulario de la pestaña 1 y cambia a esa pestaña.
        private void BtnEditarClienteRegistrado_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not Cliente cliente)
                return;

            CargarClienteEnFormulario(cliente);
            tabPrincipalClientes.SelectedIndex = 0;
        }

        // ✅ Atajo: también puedes ver el historial directo desde el listado
        private void BtnVerHistorialClienteRegistrado_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not Cliente cliente)
                return;

            cbClienteHistorial.SelectedItem = null; // fuerza el SelectionChanged aunque sea el mismo
            cbClienteHistorial.SelectedValue = cliente.Id;
            tabPrincipalClientes.SelectedIndex = 2;
        }

        // =========================================
        // ✅ PESTAÑA 1: FORMULARIO (NUEVO / EDITAR)
        // =========================================

        private void CargarClienteEnFormulario(Cliente cliente)
        {
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
        }

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
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MensajeHelper.Error("El nombre del cliente es obligatorio");
                    return;
                }

                if (!decimal.TryParse(txtLimiteCredito.Text, out decimal limiteCredito) || limiteCredito < 0)
                {
                    MensajeHelper.Error("Ingresa un límite de crédito válido");
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
                    (@Nombre, @Telefono, @Correo, @Direccion, @RFC, @LimiteCredito, 0, @FechaRegistro, @Activo);
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
                    // ✅ Hora local del equipo, no GETDATE() del servidor — mismo
                    // fix que ya aplicamos en Ventas y Caja para evitar
                    // desfases de fecha si la conexión activa es Azure SQL.
                    cmd.Parameters.AddWithValue("@FechaRegistro", DateTime.Now);

                    var resultado = cmd.ExecuteScalar();
                    clienteId = Convert.ToInt32(resultado);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@Id", clienteId);
                    cmd.ExecuteNonQuery();
                }

                MensajeHelper.Exito("Cliente guardado correctamente");

                CargarClientes();

                var clienteActual = clientes.FirstOrDefault(c => c.Id == clienteId);
                if (clienteActual != null)
                    CargarClienteEnFormulario(clienteActual);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("Error al guardar el cliente: " + ex.Message, "Error");
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (clienteId == 0)
            {
                MensajeHelper.Info("Selecciona un cliente para eliminar (desde la pestaña 'Clientes Registrados', botón Editar)", "Aviso");
                return;
            }

            var cliente = clientes.FirstOrDefault(c => c.Id == clienteId);

            if (cliente != null && cliente.SaldoActual > 0)
            {
                MensajeHelper.Info(
                    $"No puedes eliminar a \"{cliente.Nombre}\" porque tiene un saldo pendiente de {cliente.SaldoActual:C}.\n" +
                    "Registra el abono correspondiente antes de eliminarlo, o márcalo como inactivo.",
                    "Aviso");
                return;
            }

            var confirmar = MensajeHelper.Confirmar(
                "¿Eliminar este cliente? Esta acción no se puede deshacer.",
                "Confirmar");

            if (confirmar != true)
                return;

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query = "DELETE FROM Clientes WHERE Id = @Id";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", clienteId);
            cmd.ExecuteNonQuery();

            MensajeHelper.Info("Cliente eliminado", "Aviso");

            Limpiar();
            CargarClientes();
        }

        // =========================================
        // ABONOS / PAGOS A CUENTA (desde la pestaña 1)
        // =========================================

        private void BtnRegistrarAbono_Click(object sender, RoutedEventArgs e)
        {
            if (clienteId == 0)
            {
                MensajeHelper.Info("Selecciona un cliente (pestaña 'Clientes Registrados', botón Editar) para registrar el abono", "Aviso");
                return;
            }

            if (!decimal.TryParse(txtMontoAbono.Text, out decimal monto) || monto <= 0)
            {
                MensajeHelper.Info("Ingresa un monto de abono válido", "Aviso");
                return;
            }

            var cliente = clientes.FirstOrDefault(c => c.Id == clienteId);

            if (cliente != null && monto > cliente.SaldoActual)
            {
                var confirmar = MensajeHelper.Confirmar(
                    $"El abono ({monto:C}) es mayor al saldo pendiente ({cliente.SaldoActual:C}). " +
                    "El saldo quedará en $0.00 y no se generará saldo a favor.\n\n¿Continuar?",
                    "Aviso");

                if (confirmar != true)
                    return;

                monto = cliente.SaldoActual; // no permitir saldo negativo
            }

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string queryAbono =
            @"INSERT INTO AbonosCliente (ClienteId, Monto, Motivo, UsuarioId, Fecha)
              VALUES (@ClienteId, @Monto, @Motivo, @UsuarioId, @Fecha)";

            SqlCommand cmdAbono = new SqlCommand(queryAbono, conn);
            cmdAbono.Parameters.AddWithValue("@ClienteId", clienteId);
            cmdAbono.Parameters.AddWithValue("@Monto", monto);
            cmdAbono.Parameters.AddWithValue("@Motivo",
                string.IsNullOrWhiteSpace(txtMotivoAbono.Text) ? (object)DBNull.Value : txtMotivoAbono.Text.Trim());
            cmdAbono.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
            // ✅ Hora local, no GETDATE() del servidor.
            cmdAbono.Parameters.AddWithValue("@Fecha", DateTime.Now);
            cmdAbono.ExecuteNonQuery();

            string queryActualizarSaldo =
                "UPDATE Clientes SET SaldoActual = SaldoActual - @Monto WHERE Id = @ClienteId";

            SqlCommand cmdSaldo = new SqlCommand(queryActualizarSaldo, conn);
            cmdSaldo.Parameters.AddWithValue("@Monto", monto);
            cmdSaldo.Parameters.AddWithValue("@ClienteId", clienteId);
            cmdSaldo.ExecuteNonQuery();

            MensajeHelper.Info($"Abono de {monto:C} registrado correctamente", "Aviso");

            txtMontoAbono.Clear();
            txtMotivoAbono.Clear();

            CargarClientes();

            var clienteActualizado = clientes.FirstOrDefault(c => c.Id == clienteId);
            if (clienteActualizado != null)
                CargarClienteEnFormulario(clienteActualizado);

            // ✅ Si el cliente al que se le acaba de registrar el abono es el
            // mismo que se está viendo en la pestaña de Historial, refresca
            // esa vista también para que el abono aparezca de inmediato.
            if (clienteIdHistorial == clienteId)
                CargarHistorialAbonos(clienteIdHistorial);
        }

        // =========================================
        // ✅ PESTAÑA 3: HISTORIAL DE CLIENTE
        // =========================================

        private void txtBuscarClienteHistorial_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscarClienteHistorial.Text.Trim().ToLower();

            cbClienteHistorial.ItemsSource = string.IsNullOrWhiteSpace(texto)
                ? clientes
                : clientes.Where(c => c.Nombre.ToLower().Contains(texto)).ToList();
        }

        private void cbClienteHistorial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbClienteHistorial.SelectedItem is not Cliente cliente)
            {
                clienteIdHistorial = 0;
                pnlInfoClienteHistorial.Visibility = Visibility.Collapsed;
                dgHistorialCompras.ItemsSource = null;
                dgHistorialAbonos.ItemsSource = null;
                txtTotalHistorialCompras.Text = "Selecciona un cliente para ver su historial";
                return;
            }

            clienteIdHistorial = cliente.Id;

            pnlInfoClienteHistorial.Visibility = Visibility.Visible;
            txtNombreClienteHistorial.Text = cliente.Nombre;
            txtInfoClienteHistorial.Text =
                $"Saldo actual: {cliente.SaldoActual:C}  |  Disponible: {cliente.CreditoDisponible:C}  |  Límite: {cliente.LimiteCredito:C}";
            txtInfoClienteHistorial.Foreground = cliente.SaldoActual > 0
                ? System.Windows.Media.Brushes.DarkOrange
                : System.Windows.Media.Brushes.Green;

            CargarHistorialCompras(clienteIdHistorial);
            CargarHistorialAbonos(clienteIdHistorial);
        }

        // =========================================
        // ✅ FILTRO DE PERIODO (aplica al cliente activo en Historial)
        // =========================================

        private void BtnFiltroHistorial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag)
                return;

            filtroHistorialActivo = tag;

            foreach (var b in new[] { btnFiltroHoy, btnFiltroSemana, btnFiltroMes, btnFiltroAnio, btnFiltroTodoHistorial })
                b.Style = (Style)FindResource("BtnFiltroHistorial");

            btn.Style = (Style)FindResource("BtnFiltroHistorialActivo");

            if (clienteIdHistorial != 0)
            {
                CargarHistorialCompras(clienteIdHistorial);
                CargarHistorialAbonos(clienteIdHistorial);
            }
        }

        // Traduce el filtro activo a un rango [Desde, Hasta) usando la hora
        // LOCAL del equipo — mismo criterio que ya usamos en VentasReporteHelper.
        private (DateTime desde, DateTime hasta) ObtenerRangoFiltroHistorial()
        {
            DateTime hoy = DateTime.Now.Date;
            DateTime mañana = hoy.AddDays(1);

            return filtroHistorialActivo switch
            {
                "Dia" => (hoy, mañana),
                "Semana" => (hoy.AddDays(-((int)hoy.DayOfWeek == 0 ? 6 : (int)hoy.DayOfWeek - 1)), mañana),
                "Mes" => (new DateTime(hoy.Year, hoy.Month, 1), mañana),
                "Anio" => (new DateTime(hoy.Year, 1, 1), mañana),
                _ => (new DateTime(2000, 1, 1), mañana) // "Todo"
            };
        }

        // ✅ Trae TODOS los abonos del cliente (sin filtrar) para poder
        // reconstruir el "saldo después de cada abono" en orden cronológico
        // correcto, y SOLO AL FINAL aplica el filtro de periodo para mostrar
        // — así el filtro nunca descuadra el cálculo de saldo acumulado.
        private void CargarHistorialAbonos(int idCliente)
        {
            var abonosTemp = new List<(decimal Monto, string Motivo, DateTime Fecha)>();

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

            while (reader.Read())
            {
                abonosTemp.Add((
                    Convert.ToDecimal(reader["Monto"]),
                    reader["Motivo"]?.ToString() ?? "",
                    Convert.ToDateTime(reader["Fecha"])
                ));
            }

            reader.Close();

            var cliente = clientes.FirstOrDefault(c => c.Id == idCliente);
            decimal saldoActual = cliente?.SaldoActual ?? 0;

            (DateTime desde, DateTime hasta) = ObtenerRangoFiltroHistorial();

            var lista = new List<AbonoClienteView>();

            foreach (var abono in abonosTemp) // ya vienen del más reciente al más antiguo
            {
                if (abono.Fecha >= desde && abono.Fecha < hasta)
                {
                    lista.Add(new AbonoClienteView
                    {
                        Fecha = abono.Fecha,
                        Monto = abono.Monto,
                        Motivo = abono.Motivo,
                        SaldoDespues = saldoActual
                    });
                }

                saldoActual += abono.Monto; // reconstruye el saldo antes de este abono
            }

            dgHistorialAbonos.ItemsSource = lista;
        }

        // ✅ Respeta el filtro de periodo activo (Hoy/Semana/Mes/Año/Todo).
        private void CargarHistorialCompras(int idCliente)
        {
            List<CompraClienteView> lista = new();

            (DateTime desde, DateTime hasta) = ObtenerRangoFiltroHistorial();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT Id, Fecha, Total, EsCredito
              FROM Ventas
              WHERE ClienteId = @ClienteId
              AND Estado = 'Completada'
              AND Fecha >= @Desde AND Fecha < @Hasta
              ORDER BY Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ClienteId", idCliente);
            cmd.Parameters.AddWithValue("@Desde", desde);
            cmd.Parameters.AddWithValue("@Hasta", hasta);

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

            decimal totalPeriodo = lista.Sum(c => c.Total);
            txtTotalHistorialCompras.Text = lista.Count == 0
                ? "Sin compras en este periodo"
                : $"{lista.Count} compra{(lista.Count == 1 ? "" : "s")}  •  Total: {totalPeriodo:C}";
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}