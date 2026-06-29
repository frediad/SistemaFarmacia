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

            try
            {
                CargarVentas();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error");
            }
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
                Folio,
                Fecha,
                Total,
                MetodoPago,
                Estado
              FROM Ventas
              ORDER BY Fecha DESC";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new
                {
                    Id = reader["Id"],

                    Folio =
                        reader["Folio"]
                        ?.ToString(),

                    Fecha =
                        Convert.ToDateTime(
                            reader["Fecha"])
                        .ToString("dd/MM/yyyy HH:mm"),

                    MetodoPago =
                        reader["MetodoPago"]
                        ?.ToString(),

                    Estado =
                        reader["Estado"]
                        ?.ToString(),

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
            try
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
                    Folio,
                    Fecha,
                    Total,
                    MetodoPago,
                    Estado
                  FROM Ventas
                  WHERE Fecha
                  BETWEEN @Inicio
                  AND @Fin
                  ORDER BY Fecha DESC";

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
                        Id = reader["Id"],

                        Folio =
                            reader["Folio"]
                            ?.ToString(),

                        Fecha =
                            Convert.ToDateTime(
                                reader["Fecha"])
                            .ToString("dd/MM/yyyy HH:mm"),

                        MetodoPago =
                            reader["MetodoPago"]
                            ?.ToString(),

                        Estado =
                            reader["Estado"]
                            ?.ToString(),

                        Total =
                            Convert.ToDecimal(
                                reader["Total"])
                            .ToString("C")
                    });
                }

                dgVentas.ItemsSource =
                    lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error");
            }
        }
    }
}