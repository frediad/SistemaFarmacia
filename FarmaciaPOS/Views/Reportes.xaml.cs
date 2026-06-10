using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class ReportesWindow : Window
    {
        string connectionString =
            @"Server=.\SQLEXPRESS;
              Database=FarmaciaDB;
              Trusted_Connection=True;
              TrustServerCertificate=True;";

        public ReportesWindow()
        {
            InitializeComponent();

            CargarVentas();
        }

        // =====================================
        // CARGAR VENTAS
        // =====================================

        private void CargarVentas()
        {
            List<dynamic> lista =
                new();

            using SqlConnection conn =
                new SqlConnection(connectionString);

            conn.Open();

            string query =
            @"SELECT
                Id,
                FechaVenta,
                Total
              FROM Ventas
              ORDER BY FechaVenta DESC";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new
                {
                    Id =
                        reader["Id"],

                    Fecha =
                        Convert.ToDateTime(
                            reader["FechaVenta"])
                        .ToString("dd/MM/yyyy HH:mm"),

                    Usuario =
                        "Administrador",

                    Total =
                        Convert.ToDecimal(
                            reader["Total"])
                        .ToString("C")
                });
            }

            dgVentas.ItemsSource =
                lista;
        }

        // =====================================
        // BUSCAR POR FECHAS
        // =====================================

        private void BtnBuscar_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (dpInicio.SelectedDate == null ||
                dpFin.SelectedDate == null)
            {
                MessageBox.Show(
                    "Selecciona fechas");

                return;
            }

            List<dynamic> lista =
                new();

            using SqlConnection conn =
                new SqlConnection(connectionString);

            conn.Open();

            string query =
            @"SELECT
                Id,
                FechaVenta,
                Total
              FROM Ventas
              WHERE FechaVenta
              BETWEEN @Inicio
              AND @Fin
              ORDER BY FechaVenta DESC";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(
                "@Inicio",
                dpInicio.SelectedDate.Value);

            cmd.Parameters.AddWithValue(
                "@Fin",
                dpFin.SelectedDate.Value
                .AddDays(1));

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new
                {
                    Id =
                        reader["Id"],

                    Fecha =
                        Convert.ToDateTime(
                            reader["FechaVenta"])
                        .ToString("dd/MM/yyyy HH:mm"),

                    Usuario =
                        "Administrador",

                    Total =
                        Convert.ToDecimal(
                            reader["Total"])
                        .ToString("C")
                });
            }

            dgVentas.ItemsSource =
                lista;
        }
    }
}