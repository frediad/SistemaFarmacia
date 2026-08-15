using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
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

            Loaded += (s, e) => txtMontoRecibido.Focus();
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

                string sqlVenta =
                @"INSERT INTO Ventas
                (Folio, Fecha, Subtotal, Descuento, Total, MetodoPago, Estado, UsuarioId)
                VALUES
                (@Folio, GETDATE(), @Subtotal, 0, @Total, @MetodoPago, 'Completada', @UsuarioId);

                SELECT SCOPE_IDENTITY();";

                SqlCommand cmdVenta = new SqlCommand(sqlVenta, conn, trans);
                cmdVenta.Parameters.AddWithValue("@Folio", folio);
                cmdVenta.Parameters.AddWithValue("@Subtotal", total);
                cmdVenta.Parameters.AddWithValue("@Total", total);
                cmdVenta.Parameters.AddWithValue("@MetodoPago", metodoPago);
                cmdVenta.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);

                int ventaId = Convert.ToInt32(cmdVenta.ExecuteScalar());

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

                MensajeHelper.Info(
                    $"✅ Venta registrada correctamente.\n\nFolio: {folio}\nMétodo: {metodoPago}\nCambio: {cambio:C}",
                    "Venta exitosa"  );

                PreguntarEImprimirTicket(folio, pago, cambio);

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

        private void PreguntarEImprimirTicket(string folio, decimal pago, decimal cambio)
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
                    cambio);
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