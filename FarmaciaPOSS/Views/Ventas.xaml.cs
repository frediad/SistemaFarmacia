using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
    public partial class VentasWindow : Window
    {



        List<Producto> productos =
            new();

        List<VentaItem> carrito =
            new();

        public VentasWindow()
        {
            InitializeComponent();

            CargarProductos();

            KeyDown += VentasWindow_KeyDown;
        }

        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

        private void CargarProductos()
        {
            productos.Clear();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT *
              FROM Productos
              WHERE Activo = 1";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(new Producto
                {
                    Id =
                        Convert.ToInt32(
                            reader["Id"]),

                    CodigoBarras =
                        reader["CodigoBarras"]
                        .ToString(),

                    Nombre =
                        reader["Nombre"]
                        .ToString(),

                    PrecioVenta =
                        Convert.ToDecimal(
                            reader["PrecioVenta"]),

                    Stock =
                        Convert.ToInt32(
                            reader["Stock"])
                });
            }

            dgProductos.ItemsSource =
                productos;
        }

        // =========================================
        // BUSCAR
        // =========================================

        private void txtBuscar_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar producto...")
                return;

            string texto = txtBuscar.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(texto))
            {
                dgProductos.ItemsSource = productos;
                return;
            }

            dgProductos.ItemsSource = productos
                .Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.CodigoBarras.ToLower().Contains(texto))
                .ToList();
        }

        // =========================================
        // PLACEHOLDER
        // =========================================

        private void TxtBuscar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar producto...")
            {
                txtBuscar.Text = "";
                txtBuscar.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TxtBuscar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                txtBuscar.Text = "Buscar producto...";
                txtBuscar.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }



        // =========================================
        // AGREGAR CARRITO
        // =========================================

        private void dgProductos_MouseDoubleClick(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgProductos.SelectedItem
                is Producto producto)
            {
                var existente =
                    carrito.FirstOrDefault(
                        x =>
                        x.ProductoId
                        == producto.Id);

                if (existente != null)
                {
                    existente.Cantidad++;
                }
                else
                {
                    carrito.Add(
                        new VentaItem
                        {
                            ProductoId =
                                producto.Id,

                            Nombre =
                                producto.Nombre,

                            Precio =
                                producto.PrecioVenta,

                            Cantidad = 1
                        });
                }

                ActualizarCarrito();
            }
        }

        // =========================================
        // ACTUALIZAR CARRITO
        // =========================================

        private void ActualizarCarrito()
        {
            dgCarrito.ItemsSource = null;

            dgCarrito.ItemsSource =
                carrito;

            decimal total =
                carrito.Sum(
                    x => x.Subtotal);

            txtTotal.Text =
                total.ToString("C");
        }

        // =========================================
        // COBRAR
        // =========================================

        private void BtnCobrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                MessageBox.Show(
                    "No hay productos");

                return;
            }

            try
            {
                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                SqlTransaction transaction =
                    conn.BeginTransaction();

                try
                {
                    // =========================================
                    // VALIDAR INVENTARIO
                    // =========================================

                    foreach (var item in carrito)
                    {
                        string queryStock =
                        @"SELECT Stock
                          FROM Productos
                          WHERE Id = @Id";

                        SqlCommand cmdStock =
                            new SqlCommand(
                                queryStock,
                                conn,
                                transaction);

                        cmdStock.Parameters.AddWithValue(
                            "@Id",
                            item.ProductoId);

                        int stockActual =
                            Convert.ToInt32(
                                cmdStock.ExecuteScalar());

                        if (stockActual < item.Cantidad)
                        {
                            MessageBox.Show(
                                $"No hay suficiente stock para:\n\n{item.Nombre}\n\nStock disponible: {stockActual}");

                            transaction.Rollback();

                            return;
                        }
                    }

                    // =========================================
                    // TOTALES
                    // =========================================

                    decimal subtotal =
                        carrito.Sum(
                            x => x.Subtotal);

                    decimal iva =
                        subtotal * 0.16m;

                    decimal total =
                        subtotal + iva;

                    string folio =
                        $"VTA-{DateTime.Now:yyyyMMddHHmmss}";

                    // =========================================
                    // INSERTAR VENTA
                    // =========================================

                    string insertarVenta =
                    @"INSERT INTO Ventas
                    (
                    Folio,
                    Fecha,
                    Subtotal,
                    IVA,
                    Descuento,
                    Total,
                    MetodoPago,
                    Estado,
                    UsuarioId
                    )
                    VALUES
                    (
                    @Folio,
                    GETDATE(),
                    @Subtotal,
                    @IVA,
                    0,
                    @Total,
                    'Efectivo',
                    'Completada',
                    @UsuarioId
                    );

                    SELECT SCOPE_IDENTITY();";

                    SqlCommand cmdVenta =
                        new SqlCommand(
                            insertarVenta,
                            conn,
                            transaction);

                    cmdVenta.Parameters.AddWithValue(
                        "@Folio",
                        folio);

                    cmdVenta.Parameters.AddWithValue(
                        "@Subtotal",
                        subtotal);

                    cmdVenta.Parameters.AddWithValue(
                        "@IVA",
                        iva);

                    cmdVenta.Parameters.AddWithValue(
                        "@Total",
                        total);

                    cmdVenta.Parameters.AddWithValue(
                        "@UsuarioId",
                        Sesion.UsuarioId);

                    int ventaId =
                        Convert.ToInt32(
                            cmdVenta.ExecuteScalar());

                    // =========================================
                    // GUARDAR DETALLE DE VENTA
                    // =========================================

                    foreach (var item in carrito)
                    {
                        string detalle =
                        @"INSERT INTO DetalleVentas
                        (
                         VentaId,
                         ProductoId,
                         Cantidad,
                         PrecioUnitario,
                         Subtotal
                        )
                        VALUES
                        (
                        @VentaId,
                        @ProductoId,
                        @Cantidad,
                        @Precio,
                        @Subtotal
                        )";

                        SqlCommand cmdDetalle =
                            new SqlCommand(
                                detalle,
                                conn,
                                transaction);

                        cmdDetalle.Parameters.AddWithValue(
                            "@VentaId",
                            ventaId);

                        cmdDetalle.Parameters.AddWithValue(
                            "@ProductoId",
                            item.ProductoId);

                        cmdDetalle.Parameters.AddWithValue(
                            "@Cantidad",
                            item.Cantidad);

                        cmdDetalle.Parameters.AddWithValue(
                            "@Precio",
                            item.Precio);

                        cmdDetalle.Parameters.AddWithValue(
                            "@Subtotal",
                            item.Subtotal);

                        cmdDetalle.ExecuteNonQuery();
                    }

                    // =========================================
                    // DESCONTAR INVENTARIO
                    // =========================================

                    foreach (var item in carrito)
                    {
                        string actualizarStock =
                        @"UPDATE Productos
                          SET Stock = Stock - @Cantidad
                          WHERE Id = @Id";

                        SqlCommand cmdStock =
                            new SqlCommand(
                                actualizarStock,
                                conn,
                                transaction);

                        cmdStock.Parameters.AddWithValue(
                            "@Cantidad",
                            item.Cantidad);

                        cmdStock.Parameters.AddWithValue(
                            "@Id",
                            item.ProductoId);

                        cmdStock.ExecuteNonQuery();
                    }

                    // =========================================
                    // CONFIRMAR TRANSACCIÓN
                    // =========================================

                    transaction.Commit();

                    // =========================================
                    // IMPRIMIR TICKET
                    // =========================================

                    TicketPrinter ticket =
                        new TicketPrinter(
                            carrito,
                            total);

                    ticket.Imprimir();

                    MessageBox.Show(
                        $"Venta registrada correctamente\n\nFolio: {folio}");

                    carrito.Clear();

                    ActualizarCarrito();

                    CargarProductos();
                }
                catch
                {
                    transaction.Rollback();

                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message);
            }
        }

        private void BtnGenerarTicket_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                MessageBox.Show(
                    "No hay productos en el carrito");

                return;
            }

            try
            {
                decimal total =
                    carrito.Sum(
                        x => x.Subtotal);

                TicketPrinter ticket =
                    new TicketPrinter(
                        carrito,
                        total);

                ticket.Imprimir();

                MessageBox.Show(
                    "Ticket generado correctamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message);
            }
        }

        private void txtBuscar_KeyDown(
            object sender,
        System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key ==
                System.Windows.Input.Key.Enter)
            {
                MessageBox.Show(
                    "Código escaneado");
            }
        }

        // =========================================
        // CANCELAR
        // =========================================

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            carrito.Clear();

            ActualizarCarrito();
        }

        // =========================================
        // AGREGAR CANTIDAD
        // =========================================

        private void BtnAgregarCantidad_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (dgCarrito.SelectedItem is not VentaItem item)
            {
                MessageBox.Show(
                    "Selecciona un producto del carrito");

                return;
            }

            string input =
                Microsoft.VisualBasic.Interaction
                .InputBox(
                    "Nueva cantidad:",
                    "Modificar cantidad",
                    item.Cantidad.ToString());

            if (!int.TryParse(input, out int cantidad)
                || cantidad <= 0)
            {
                MessageBox.Show(
                    "Cantidad inválida");

                return;
            }

            item.Cantidad = cantidad;

            ActualizarCarrito();
        }

        // =========================================
        // TECLAS DE ACCESO RÁPIDO
        // =========================================

        private void VentasWindow_KeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            
            switch (e.Key)
            {
                case Key.F1:
                    BtnGenerarTicket_Click(sender, new RoutedEventArgs());
                    break;

                case Key.F2:
                    BtnCobrar_Click(sender, new RoutedEventArgs());
                    break;

                case Key.F3:
                    BtnCancelar_Click(sender, new RoutedEventArgs());
                    break;

                case Key.F4:
                    BtnAgregarCantidad_Click(sender, new RoutedEventArgs());
                    break;
            }
        }

        private void VentasWindow_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            
            {
                switch (e.Key)
                {
                    case Key.F1:
                        BtnGenerarTicket_Click(this, new RoutedEventArgs());
                        break;

                    case Key.F2:
                        BtnCobrar_Click(this, new RoutedEventArgs());
                        break;

                    case Key.F3:
                        BtnCancelar_Click(this, new RoutedEventArgs());
                        break;

                    case Key.F4:
                        BtnAgregarCantidad_Click(this, new RoutedEventArgs());
                        break;
                }
            }
            
            
        }
    }
}