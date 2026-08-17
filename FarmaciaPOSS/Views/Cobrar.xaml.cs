using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
    // ✅ Item simple para el combo de clientes — "Público en general" es la
    // opción por defecto (Id = null), seguida de los clientes reales de la BD.
    // Incluye datos de crédito para mostrarlos al seleccionar un cliente.
    public class ClienteVentaOption
    {
        public int? Id { get; set; }
        public string Nombre { get; set; } = "";
        public decimal LimiteCredito { get; set; }
        public decimal SaldoActual { get; set; }
        public decimal CreditoDisponible => LimiteCredito - SaldoActual;
        public bool TieneCredito => LimiteCredito > 0;
    }

    public partial class Cobrar : Window
    {
        private readonly ObservableCollection<VentaItem> carrito;
        private readonly decimal total;

        public bool VentaCompletada { get; private set; } = false;

        public Cobrar(ObservableCollection<VentaItem> carritoVenta)
        {
            InitializeComponent();

            carrito = carritoVenta;

            dgResumenVenta.ItemsSource = carrito;

            total = carrito.Sum(x => x.Subtotal);

            txtTotalCobrar.Text = total.ToString("C");

            CargarClientes();

            Loaded += (s, e) => txtMontoRecibido.Focus();
        }

        // =========================================
        // ✅ CLIENTE (venta al público o a uno específico)
        // =========================================

        private void CargarClientes()
        {
            var opciones = new List<ClienteVentaOption>
            {
                new ClienteVentaOption { Id = null, Nombre = "🧑‍🤝‍🧑 Público en general" }
            };

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query = "SELECT Id, Nombre, LimiteCredito, SaldoActual FROM Clientes WHERE Activo = 1 ORDER BY Nombre";
                SqlCommand cmd = new SqlCommand(query, conn);

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    opciones.Add(new ClienteVentaOption
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nombre = reader["Nombre"].ToString() ?? "",
                        LimiteCredito = Convert.ToDecimal(reader["LimiteCredito"]),
                        SaldoActual = Convert.ToDecimal(reader["SaldoActual"])
                    });
                }
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo cargar la lista de clientes: " + ex.Message, "Error");
            }

            cbCliente.ItemsSource = opciones;
            cbCliente.SelectedIndex = 0; // "Público en general" por defecto
        }

        // ✅ Muestra el estado de crédito del cliente seleccionado (siempre que
        // no sea "Público en general"), indicando claramente si cuenta con
        // crédito suficiente, crédito insuficiente, o si no tiene crédito habilitado.
        private void cbCliente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pnlCreditoCliente == null)
                return;

            var cliente = cbCliente.SelectedItem as ClienteVentaOption;

            // Público en general: no aplica crédito, se oculta el panel.
            if (cliente == null || cliente.Id == null)
            {
                pnlCreditoCliente.Visibility = Visibility.Collapsed;
                ActualizarDisponibilidadCredito(null);
                return;
            }

            pnlCreditoCliente.Visibility = Visibility.Visible;

            // Caso 1: el cliente no tiene ningún límite de crédito habilitado.
            if (!cliente.TieneCredito)
            {
                pnlDetalleCredito.Visibility = Visibility.Collapsed;

                txtEstadoCredito.Text = "🚫 Este cliente no cuenta con crédito habilitado.";
                txtEstadoCredito.Foreground = System.Windows.Media.Brushes.Gray;
                ActualizarDisponibilidadCredito(cliente);
                return;
            }

            // Caso 2: sí tiene crédito habilitado — mostrar el desglose.
            pnlDetalleCredito.Visibility = Visibility.Visible;

            txtLimiteCredito.Text = cliente.LimiteCredito.ToString("C");
            txtSaldoActual.Text = cliente.SaldoActual.ToString("C");
            txtCreditoDisponible.Text = cliente.CreditoDisponible.ToString("C");

            bool sinCreditoDisponible = cliente.CreditoDisponible <= 0;
            bool alcanzaParaEstaVenta = cliente.CreditoDisponible >= total;

            if (sinCreditoDisponible)
            {
                txtEstadoCredito.Text = "🔴 Sin crédito disponible — ya alcanzó su límite.";
                txtEstadoCredito.Foreground = System.Windows.Media.Brushes.Red;
                txtCreditoDisponible.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (!alcanzaParaEstaVenta)
            {
                txtEstadoCredito.Text = $"🟠 Crédito insuficiente para esta venta ({total:C}).";
                txtEstadoCredito.Foreground = System.Windows.Media.Brushes.DarkOrange;
                txtCreditoDisponible.Foreground = System.Windows.Media.Brushes.DarkOrange;
            }
            else
            {
                txtEstadoCredito.Text = "🟢 Cuenta con crédito suficiente para esta venta.";
                txtEstadoCredito.Foreground = System.Windows.Media.Brushes.Green;
                txtCreditoDisponible.Foreground = System.Windows.Media.Brushes.Green;
            }

            ActualizarDisponibilidadCredito(cliente);
        }

        // ✅ Habilita/deshabilita la opción "Crédito" del método de pago según
        // si el cliente actualmente seleccionado tiene crédito suficiente.
        // Si "Crédito" estaba elegido y deja de aplicar, regresa a "Efectivo".
        private void ActualizarDisponibilidadCredito(ClienteVentaOption? cliente)
        {
            bool puedeUsarCredito = cliente?.Id != null
                && cliente.TieneCredito
                && cliente.CreditoDisponible >= total;

            itemCredito.IsEnabled = puedeUsarCredito;

            if (!puedeUsarCredito && itemCredito.IsSelected)
            {
                cbMetodoPago.SelectedIndex = 0; // Efectivo
            }
        }

        // =========================================
        // MÉTODO DE PAGO
        // =========================================

        private void cbMetodoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtMontoRecibido == null || txtEtiquetaMonto == null || txtCambioCobrar == null)
                return;

            string metodo = (cbMetodoPago.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Efectivo";

            if (metodo == "Efectivo")
            {
                txtEtiquetaMonto.Text = "MONTO RECIBIDO";
                txtMontoRecibido.IsEnabled = true;
                txtMontoRecibido.Text = "";
                txtCambioCobrar.Text = "$0.00";
                txtMontoRecibido.Focus();
            }
            else if (metodo == "Crédito")
            {
                txtEtiquetaMonto.Text = "SE CARGARÁ AL CRÉDITO";
                txtMontoRecibido.IsEnabled = false;
                txtMontoRecibido.Text = total.ToString("0.00");
                txtCambioCobrar.Text = "$0.00";
            }
            else
            {
                txtEtiquetaMonto.Text = "MONTO A COBRAR";
                txtMontoRecibido.IsEnabled = false;
                txtMontoRecibido.Text = total.ToString("0.00");
                txtCambioCobrar.Text = "$0.00";
            }
        }

        private void txtMontoRecibido_TextChanged(object sender, TextChangedEventArgs e)
        {
            string metodo = (cbMetodoPago.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Efectivo";

            if (metodo != "Efectivo")
            {
                txtCambioCobrar.Text = "$0.00";
                return;
            }

            if (decimal.TryParse(txtMontoRecibido.Text, out decimal pago) && pago >= 0)
            {
                decimal cambio = pago - total;
                txtCambioCobrar.Text = cambio >= 0 ? cambio.ToString("C") : "$0.00";
            }
            else
            {
                txtCambioCobrar.Text = "$0.00";
            }
        }

        private void txtMontoRecibido_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnConfirmarVenta_Click(sender, new RoutedEventArgs());
        }

        // =========================================
        // CONFIRMAR VENTA
        // =========================================

        private void BtnConfirmarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                MensajeHelper.Info("No hay productos en el carrito.", "Aviso");
                return;
            }

            string metodoPago = (cbMetodoPago.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Efectivo";

            // ✅ Cliente seleccionado (null = público en general)
            var clienteSeleccionado = cbCliente.SelectedItem as ClienteVentaOption;
            int? clienteId = clienteSeleccionado?.Id;
            string nombreCliente = clienteSeleccionado?.Nombre ?? "Público en general";

            decimal pago;
            decimal cambio;

            if (metodoPago == "Efectivo")
            {
                if (!decimal.TryParse(txtMontoRecibido.Text, out pago))
                {
                    MensajeHelper.Info("Ingresa un monto válido.", "Aviso");
                    return;
                }

                if (pago < total)
                {
                    MensajeHelper.Info("El pago es insuficiente.", "Aviso");
                    return;
                }

                cambio = pago - total;
            }
            else if (metodoPago == "Crédito")
            {
                // ✅ Validación de crédito — nunca confiamos solo en que el
                // combo esté deshabilitado en la UI; se revalida aquí por si
                // el cliente cambió de opinión a mitad de la venta o los
                // datos en memoria quedaron desactualizados.
                if (clienteSeleccionado?.Id == null)
                {
                    MensajeHelper.Info("Selecciona un cliente específico para vender a crédito.", "Aviso");
                    return;
                }

                if (!clienteSeleccionado.TieneCredito)
                {
                    MensajeHelper.Info("Este cliente no cuenta con crédito habilitado.", "Aviso");
                    return;
                }

                if (clienteSeleccionado.CreditoDisponible < total)
                {
                    MensajeHelper.Info(
                        $"El crédito disponible ({clienteSeleccionado.CreditoDisponible:C}) no alcanza para esta venta ({total:C}).",
                        "Crédito insuficiente");
                    return;
                }

                pago = total;
                cambio = 0;
            }
            else
            {
                pago = total;
                cambio = 0;
            }

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            SqlTransaction trans = conn.BeginTransaction();

            try
            {
                string folio = $"VTA-{DateTime.Now:yyyyMMddHHmmss}";

                bool esVentaACredito = metodoPago == "Crédito";

                // ✅ Se guarda la hora LOCAL del equipo (@Fecha) en vez de
                // GETDATE(), que se ejecuta en el servidor y puede venir en
                // UTC si la conexión activa es Azure SQL — esa era la causa
                // del desfase de fechas en el historial.
                string sqlVenta =
                @"INSERT INTO Ventas
                (Folio, ClienteId, Fecha, Subtotal, Descuento, Total, MetodoPago, Estado, EsCredito, IVA, UsuarioId)
                VALUES
                (@Folio, @ClienteId, @Fecha, @Subtotal, 0, @Total, @MetodoPago, 'Completada', @EsCredito, 0, @UsuarioId);

                SELECT SCOPE_IDENTITY();";

                SqlCommand cmdVenta = new SqlCommand(sqlVenta, conn, trans);
                cmdVenta.Parameters.AddWithValue("@Folio", folio);
                cmdVenta.Parameters.AddWithValue("@ClienteId", (object?)clienteId ?? DBNull.Value);
                cmdVenta.Parameters.AddWithValue("@Fecha", DateTime.Now);
                cmdVenta.Parameters.AddWithValue("@Subtotal", total);
                cmdVenta.Parameters.AddWithValue("@Total", total);
                cmdVenta.Parameters.AddWithValue("@MetodoPago", metodoPago);
                cmdVenta.Parameters.AddWithValue("@EsCredito", esVentaACredito);
                cmdVenta.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);

                int ventaId = Convert.ToInt32(cmdVenta.ExecuteScalar());

                // ✅ Si la venta es a crédito, se descuenta del crédito
                // disponible del cliente (aumenta su saldo usado), dentro de
                // la misma transacción — si algo falla más abajo, esto
                // también se revierte y el crédito no queda descontado a medias.
                if (esVentaACredito && clienteId != null)
                {
                    SqlCommand cmdCredito = new SqlCommand(
                    @"UPDATE Clientes SET SaldoActual = SaldoActual + @Total WHERE Id = @ClienteId",
                    conn, trans);

                    cmdCredito.Parameters.AddWithValue("@Total", total);
                    cmdCredito.Parameters.AddWithValue("@ClienteId", clienteId);
                    cmdCredito.ExecuteNonQuery();
                }

                foreach (var item in carrito)
                {
                    SqlCommand cmdDetalle = new SqlCommand(
                    @"INSERT INTO DetalleVentas
                    (VentaId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
                    VALUES
                    (@VentaId, @ProductoId, @Cantidad, @Precio, @Subtotal)",
                    conn, trans);

                    cmdDetalle.Parameters.AddWithValue("@VentaId", ventaId);
                    cmdDetalle.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                    cmdDetalle.Parameters.AddWithValue("@Precio", item.Precio);
                    cmdDetalle.Parameters.AddWithValue("@Subtotal", item.Subtotal);
                    cmdDetalle.ExecuteNonQuery();

                    SqlCommand cmdStock = new SqlCommand(
                    @"UPDATE Productos SET Stock = Stock - @Cantidad WHERE Id = @ProductoId",
                    conn, trans);

                    cmdStock.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                    cmdStock.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdStock.ExecuteNonQuery();
                }

                trans.Commit();

                VentaCompletada = true;

                // ✅ Mensaje de confirmación mejorado: incluye cliente,
                // cantidad de artículos, hora exacta y desglose de pago,
                // en vez del texto plano anterior.
                int totalArticulos = carrito.Sum(x => x.Cantidad);

                string mensajeExito =
                    $"Venta registrada correctamente\n\n" +
                    $"Folio:        {folio}\n" +
                    $"Cliente:      {nombreCliente}\n" +
                    $"Artículos:    {totalArticulos}\n" +
                    $"Método:       {metodoPago}\n" +
                    $"Total:        {total:C}\n" +
                    (metodoPago == "Efectivo"
                        ? $"Recibido:     {pago:C}\n" +
                          $"Cambio:       {cambio:C}\n"
                        : "") +
                    (esVentaACredito
                        ? $"Crédito usado:      {total:C}\n" +
                          $"Crédito restante:   {(clienteSeleccionado!.CreditoDisponible - total):C}\n"
                        : "") +
                    $"Hora:         {DateTime.Now:dd/MM/yyyy HH:mm}";

                MensajeHelper.Exito(mensajeExito, "✅ Venta exitosa");

                PreguntarEImprimirTicket(folio, nombreCliente, pago, cambio);

                DialogResult = true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                MensajeHelper.Error("Error al registrar la venta: " + ex.Message, "Error");
            }
        }

        // =========================================
        // IMPRIMIR TICKET
        // =========================================

        private void PreguntarEImprimirTicket(string folio, string nombreCliente, decimal pago, decimal cambio)
        {
            var resultado = MensajeHelper.Confirmar(
                "¿Deseas imprimir el ticket de esta venta?",
                "Imprimir ticket");

            if (resultado != true)
                return;

            var config = ConfiguracionPosHelper.Cargar();

            if (string.IsNullOrWhiteSpace(config.ImpresoraTicket))
            {
                MensajeHelper.Info(
                    "No hay una impresora configurada. Ve a Configuración para asignar una.",
                    "Impresora no configurada");
                return;
            }

            try
            {
                ImpresoraTicketHelper.ImprimirTicketVenta(
                    config.ImpresoraTicket,
                    folio,
                    Sesion.NombreUsuario,
                    carrito,
                    total,
                    total,
                    pago,
                    cambio,
                    config.AnchoTicketMM);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo imprimir el ticket: " + ex.Message, "Error de impresión");
            }
        }

        private void BtnCancelarCobro_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}