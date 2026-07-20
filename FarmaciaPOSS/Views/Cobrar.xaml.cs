using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class Cobrar : Window
    {
        private readonly ObservableCollection<VentaItem> carrito;
        private readonly decimal subtotal;
        private readonly decimal total;

        public bool VentaCompletada { get; private set; } = false;

        public Cobrar(ObservableCollection<VentaItem> carritoActual)
        {
            InitializeComponent();

            carrito = carritoActual;
            dgResumenCobro.ItemsSource = carrito;

            subtotal = carrito.Sum(x => x.Subtotal);
            total = subtotal;

            txtSubtotalCobro.Text = subtotal.ToString("C");
            txtTotalCobro.Text = total.ToString("C");

            Loaded += (s, e) => txtMontoRecibido.Focus();
        }

        private void txtMontoRecibido_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtMontoRecibido.Text, out decimal pago) && pago >= 0)
            {
                decimal cambio = pago - total;
                txtCambioCobro.Text = cambio >= 0 ? cambio.ToString("C") : "$0.00";
            }
            else
            {
                txtCambioCobro.Text = "$0.00";
            }
        }

        private void BtnConfirmarCobro_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                MessageBox.Show("No hay productos en el carrito.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtMontoRecibido.Text, out decimal pago))
            {
                MessageBox.Show("Ingresa un monto válido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pago < total)
            {
                MessageBox.Show("El pago es insuficiente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal cambio = pago - total;
            string folio = $"VTA-{DateTime.Now:yyyyMMddHHmmss}";

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            SqlTransaction trans = conn.BeginTransaction();

            try
            {
                string sqlVenta =
                @"INSERT INTO Ventas
                (Folio, Fecha, Subtotal, Descuento, Total, MetodoPago, Estado, UsuarioId)
                VALUES
                (@Folio, GETDATE(), @Subtotal, 0, @Total, 'Efectivo', 'Completada', @UsuarioId);
                SELECT SCOPE_IDENTITY();";

                SqlCommand cmdVenta = new SqlCommand(sqlVenta, conn, trans);
                cmdVenta.Parameters.AddWithValue("@Folio", folio);
                cmdVenta.Parameters.AddWithValue("@Subtotal", subtotal);
                cmdVenta.Parameters.AddWithValue("@Total", total);
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
            }
            catch (Exception ex)
            {
                trans.Rollback();
                MessageBox.Show("No se pudo registrar la venta: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(
                $"✅ Venta realizada\n\nTotal:  {total:C}\nPago:   {pago:C}\nCambio: {cambio:C}",
                "Venta exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            PreguntarImprimirTicket(folio, pago, cambio);

            VentaCompletada = true;
            DialogResult = true;
        }

        private void PreguntarImprimirTicket(string folio, decimal pago, decimal cambio)
        {
            var resultado = MessageBox.Show(
                "¿Deseas imprimir el ticket de esta venta?",
                "Imprimir ticket",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado != MessageBoxResult.Yes)
                return;

            var config = ConfiguracionPosHelper.Cargar();

            if (string.IsNullOrWhiteSpace(config.ImpresoraTicket))
            {
                MessageBox.Show(
                    "No hay ninguna impresora de tickets configurada. Ve a Configuración para asignar una.",
                    "Impresora no configurada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                ImpresoraTicketHelper.ImprimirTicketVenta(
                    config.ImpresoraTicket,
                    folio,
                    Sesion.NombreUsuario,
                    carrito,
                    subtotal,
  
                    total,
                    pago,
                    cambio);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "La venta se registró correctamente, pero no se pudo imprimir el ticket: " + ex.Message,
                    "Error de impresión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void BtnCancelarCobro_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}