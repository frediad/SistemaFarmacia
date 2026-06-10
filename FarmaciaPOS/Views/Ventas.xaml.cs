using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class VentasWindow : Window
    {
        string connectionString =
            @"Server=.\SQLEXPRESS;
              Database=FarmaciaDB;
              Trusted_Connection=True;
              TrustServerCertificate=True;";

        List<Producto> productos =
            new();

        List<VentaItem> carrito =
            new();

        public VentasWindow()
        {
            InitializeComponent();

            CargarProductos();
        }

        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

        private void CargarProductos()
        {
            productos.Clear();

            using SqlConnection conn =
                new SqlConnection(connectionString);

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
            string texto =
                txtBuscar.Text.ToLower();

            dgProductos.ItemsSource =
                productos.Where(p =>
                    p.Nombre.ToLower()
                    .Contains(texto)

                    ||

                    p.CodigoBarras.ToLower()
                    .Contains(texto))
                .ToList();
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
                    new SqlConnection(connectionString);

                conn.Open();

                foreach (var item in carrito)
                {
                    string query =
                    @"UPDATE Productos
                      SET Stock = Stock - @Cantidad
                      WHERE Id = @Id";

                    SqlCommand cmd =
                        new SqlCommand(
                            query,
                            conn);

                    cmd.Parameters.AddWithValue(
                        "@Cantidad",
                        item.Cantidad);

                    cmd.Parameters.AddWithValue(
                        "@Id",
                        item.ProductoId);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show(
                    "Venta realizada");

                // =====================================
                // IMPRIMIR TICKET
                // =====================================

                decimal total =
                    carrito.Sum(
                        x => x.Subtotal);

                TicketPrinter ticket =
                    new TicketPrinter(
                        carrito,
                        total);

                ticket.Imprimir();

                carrito.Clear();

                ActualizarCarrito();

                CargarProductos();
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
    }
}