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
    public partial class PedidosWindow : Window
    {
        List<PedidoView> listaTodosLosPedidos = new();
        int pedidoSeleccionadoId = 0;

        public PedidosWindow()
        {
            InitializeComponent();

            try
            {
                cbEstado.SelectedIndex = 0;
                CargarPedidos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // =========================================
        // CARGAR PEDIDOS
        // =========================================

        private void CargarPedidos(string estado = "")
        {
            listaTodosLosPedidos.Clear();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                p.Id,
                p.NumeroPedido,
                c.Nombre AS ClienteNombre,
                p.FechaPedido,
                p.Total,
                p.EstadoPedido,
                p.HoraRecogida,
                p.Observaciones
              FROM Pedidos p
              INNER JOIN Clientes c ON p.ClienteId = c.Id";

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                query += " WHERE p.EstadoPedido = @Estado";

            query += " ORDER BY p.FechaPedido DESC";

            SqlCommand cmd = new SqlCommand(query, conn);

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                cmd.Parameters.AddWithValue("@Estado", estado);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                listaTodosLosPedidos.Add(new PedidoView
                {
                    Id = (int)reader["Id"],
                    NumeroPedido = reader["NumeroPedido"].ToString() ?? "",
                    ClienteNombre = reader["ClienteNombre"].ToString() ?? "",
                    FechaPedido = (DateTime)reader["FechaPedido"],
                    Total = (decimal)reader["Total"],
                    EstadoPedido = reader["EstadoPedido"].ToString() ?? "",
                    HoraRecogida = reader["HoraRecogida"].ToString() ?? "",
                    Observaciones = reader["Observaciones"].ToString() ?? ""
                });
            }

            dgPedidos.ItemsSource = listaTodosLosPedidos;
        }

        // =========================================
        // FILTRO ESTADO
        // =========================================

        private void cbEstado_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (cbEstado.SelectedItem is ComboBoxItem item)
            {
                CargarPedidos(item.Content.ToString());
                txtBuscarCliente.Text = "";
            }
        }

        // =========================================
        // ✅ BUSCADOR POR CLIENTE
        // =========================================

        private void TxtBuscarCliente_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscarCliente.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(texto))
            {
                dgPedidos.ItemsSource = listaTodosLosPedidos;
                return;
            }

            dgPedidos.ItemsSource = listaTodosLosPedidos
                .Where(p => p.ClienteNombre.ToLower().Contains(texto))
                .ToList();
        }

        // =========================================
        // ✅ SELECCIONAR PEDIDO — CARGAR DETALLE
        // =========================================

        private void DgPedidos_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dgPedidos.SelectedItem is PedidoView pedido)
            {
                pedidoSeleccionadoId = pedido.Id;

                txtDetalleCliente.Text = pedido.ClienteNombre;
                txtDetalleFecha.Text = pedido.FechaPedido.ToString("dd/MM/yyyy");
                txtDetalleHora.Text = string.IsNullOrEmpty(pedido.HoraRecogida)
                    ? "Sin hora de recogida"
                    : pedido.HoraRecogida;
                txtDetalleObservaciones.Text = string.IsNullOrEmpty(pedido.Observaciones)
                    ? "Sin observaciones"
                    : pedido.Observaciones;
                txtDetalleTotalPedido.Text = pedido.Total.ToString("C");

                CargarDetallePedido(pedido.Id);
            }
        }

        // =========================================
        // ✅ CARGAR PRODUCTOS DEL PEDIDO
        // =========================================

        private void CargarDetallePedido(int idPedido)
        {
            List<DetallePedidoView> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                pr.Nombre AS NombreProducto,
                dp.Cantidad,
                dp.Precio,
                dp.Subtotal
              FROM DetallePedidos dp
              INNER JOIN Productos pr ON dp.ProductoId = pr.Id
              WHERE dp.PedidoId = @PedidoId";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PedidoId", idPedido);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new DetallePedidoView
                {
                    NombreProducto = reader["NombreProducto"].ToString() ?? "",
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    Precio = Convert.ToDecimal(reader["Precio"]),
                    Subtotal = Convert.ToDecimal(reader["Subtotal"])
                });
            }

            dgDetallePedido.ItemsSource = lista;
        }

        // =========================================
        // ✅ CAMBIAR ESTADO DEL PEDIDO
        // =========================================

        private void BtnCambiarEstado_Click(
            object sender, RoutedEventArgs e)
        {
            if (pedidoSeleccionadoId == 0)
            {
                MessageBox.Show("Selecciona un pedido primero",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nuevoEstado = (sender as Button)?.Tag?.ToString() ?? "";

            if (string.IsNullOrEmpty(nuevoEstado))
                return;

            try
            {
                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query =
                    "UPDATE Pedidos SET EstadoPedido = @Estado WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
                cmd.Parameters.AddWithValue("@Id", pedidoSeleccionadoId);

                cmd.ExecuteNonQuery();

                MessageBox.Show(
                    $"Estado cambiado a: {nuevoEstado}",
                    "Actualizado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Recargar preservando el filtro actual
                string estadoFiltro = (cbEstado.SelectedItem as ComboBoxItem)?
                    .Content.ToString() ?? "";

                CargarPedidos(estadoFiltro);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}