using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System;

namespace FarmaciaPOS.Views
{
    public partial class PedidosWindow : Window
    {
        string connectionString =
            @"Server=.\SQLEXPRESS;
              Database=FarmaciaDB;
              Trusted_Connection=True;
              TrustServerCertificate=True;";

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
                MessageBox.Show(
                    ex.Message,
                    "ERROR",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================
        // CARGAR PEDIDOS
        // =========================================

        private void CargarPedidos(
            string estado = "")
        {
            List<PedidoView> lista =
                new();

            using SqlConnection conn =
                new SqlConnection(connectionString);

            conn.Open();

            string query =
            @"SELECT
                p.Id,
                p.NumeroPedido,

                c.Nombre + ' ' + c.Apellido
                    AS ClienteNombre,

                p.FechaPedido,
                p.Total,
                p.EstadoPedido,
                p.HoraRecogida,
                p.Observaciones

            FROM Pedidos p

            INNER JOIN Clientes c
                ON p.ClienteId = c.Id";

            if (estado != "" &&
                estado != "Todos")
            {
                query +=
                    " WHERE p.EstadoPedido = @Estado";
            }

            query +=
                " ORDER BY p.FechaPedido DESC";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            if (estado != "" &&
                estado != "Todos")
            {
                cmd.Parameters.AddWithValue(
                    "@Estado",
                    estado);
            }

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new PedidoView
                {
                    Id =
                        (int)reader["Id"],

                    NumeroPedido =
                        reader["NumeroPedido"]
                        .ToString(),

                    ClienteNombre =
                        reader["ClienteNombre"]
                        .ToString(),

                    FechaPedido =
                        (DateTime)reader["FechaPedido"],

                    Total =
                        (decimal)reader["Total"],

                    EstadoPedido =
                        reader["EstadoPedido"]
                        .ToString(),

                    HoraRecogida =
                        reader["HoraRecogida"]
                        .ToString(),

                    Observaciones =
                        reader["Observaciones"]
                        .ToString()
                });
            }

            dgPedidos.ItemsSource =
                lista;
        }

        // =========================================
        // FILTRAR ESTADO
        // =========================================

        private void cbEstado_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (cbEstado.SelectedItem
                is ComboBoxItem item)
            {
                CargarPedidos(
                    item.Content.ToString());
            }
        }
    }
}