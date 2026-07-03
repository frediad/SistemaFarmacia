using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using FarmaciaPOS.Helpers;

namespace FarmaciaPOS.Views
{
    public partial class Graficas : Window
    {
        public Graficas()
        {
            InitializeComponent();

            cmbPeriodo.SelectedIndex = 2; // Mes

            CargarResumen();
            CargarGraficaMes();
            
        }
    

    // Aquí van todos los demás métodos...



public ISeries[] Series { get; set; } = Array.Empty<ISeries>();

        public Axis[] XAxes { get; set; } = Array.Empty<Axis>();

        public Axis[] YAxes { get; set; } = Array.Empty<Axis>();

        private void CargarResumen()
           
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);

                cn.Open();

                // Total vendido
                string sqlTotal = "SELECT ISNULL(SUM(Total),0) FROM Ventas";

                SqlCommand cmdTotal = new SqlCommand(sqlTotal, cn);

                decimal total = Convert.ToDecimal(cmdTotal.ExecuteScalar());

                txtTotal.Text = total.ToString("C");

                // Número de ventas
                string sqlVentas = "SELECT COUNT(*) FROM Ventas";

                SqlCommand cmdVentas = new SqlCommand(sqlVentas, cn);

                int ventas = Convert.ToInt32(cmdVentas.ExecuteScalar());

                txtVentas.Text = ventas.ToString();

                // Promedio
                decimal promedio = ventas > 0 ? total / ventas : 0;

                txtPromedio.Text = promedio.ToString("C");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el resumen:\n\n" + ex.Message);
            }
        }
        private void CargarGraficaDia()
        {
            try
            {

                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                string sql = @"
        SELECT 
            DAY(Fecha) AS Dia,
            SUM(Total) AS Total
        FROM Ventas
        WHERE Fecha IS NOT NULL
          AND MONTH(Fecha) = MONTH(GETDATE())
          AND YEAR(Fecha) = YEAR(GETDATE())
        GROUP BY DAY(Fecha)
        ORDER BY Dia";

                SqlCommand cmd = new SqlCommand(sql, cn);
                SqlDataReader dr = cmd.ExecuteReader();

                List<double> valores = new();
                List<string> etiquetas = new();

                while (dr.Read())
                {
                    if (dr["Total"] != DBNull.Value)
                    {
                        etiquetas.Add(dr["Dia"].ToString());
                        valores.Add(Convert.ToDouble(dr["Total"]));
                    }
                }

                MessageBox.Show("Registros encontrados: " + valores.Count);

                GraficaVentas.Series = new ISeries[]
                {
            new ColumnSeries<double>
            {
                Values = valores
            }
                };

                GraficaVentas.XAxes = new Axis[]
                {
            new Axis
            {
                Labels = etiquetas
            }
                };

                GraficaVentas.YAxes = new Axis[]
                {
            new Axis()
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica Día:\n" + ex.Message);
            }
        }

        private void CargarGraficaSemana()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                string sql = @"
        SELECT 
            CAST(Fecha AS DATE) AS Dia,
            SUM(Total) AS Total
        FROM Ventas
        WHERE Fecha >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
        GROUP BY CAST(Fecha AS DATE)
        ORDER BY Dia";

                SqlCommand cmd = new SqlCommand(sql, cn);
                SqlDataReader dr = cmd.ExecuteReader();

                double[] valores = new double[7];
                string[] dias = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };

                while (dr.Read())
                {
                    DateTime fecha = Convert.ToDateTime(dr["Dia"]);
                    double total = Convert.ToDouble(dr["Total"]);

                    int index = (int)fecha.DayOfWeek;
                    if (index == 0) index = 6; else index--;

                    valores[index] = total;
                }

                GraficaVentas.Series = new ISeries[]
                {
            new ColumnSeries<double>
            {
                Values = valores
            }
                };

                GraficaVentas.XAxes = new Axis[]
                {
            new Axis
            {
                Labels = dias
            }
                };

                GraficaVentas.YAxes = new Axis[]
                {
            new Axis()
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica Semana:\n" + ex.Message);
            }
        }


        private void CargarGraficaAnio()
        {
            using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
            cn.Open();

            string sql = @"
    SELECT 
        MONTH(Fecha) AS Mes,
        SUM(Total) AS Total
    FROM Ventas
    WHERE YEAR(Fecha) = YEAR(GETDATE())
    GROUP BY MONTH(Fecha)
    ORDER BY Mes";

            SqlCommand cmd = new SqlCommand(sql, cn);
            SqlDataReader dr = cmd.ExecuteReader();

            double[] valores = new double[12];
            string[] meses = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

            while (dr.Read())
            {
                int mes = Convert.ToInt32(dr["Mes"]) - 1;
                valores[mes] = Convert.ToDouble(dr["Total"]);
            }

            GraficaVentas.Series = new ISeries[]
            {
        new ColumnSeries<double>
        {
            Values = valores
        }
            };

            GraficaVentas.XAxes = new Axis[]
            {
        new Axis
        {
            Labels = meses
        }
            };
        }

        private void CargarGraficaMes()
            

        {
            try
            {


                List<double> valores = new();
                List<string> etiquetas = new();

                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);

                cn.Open();

                string sql = @"
        SELECT
            MONTH(Fecha) AS Mes,
            SUM(Total) AS Total
        FROM Ventas
        GROUP BY MONTH(Fecha)
        ORDER BY Mes";

                SqlCommand cmd = new SqlCommand(sql, cn);

                SqlDataReader dr = cmd.ExecuteReader();

                string[] meses =
                {
            "Ene","Feb","Mar","Abr",
            "May","Jun","Jul","Ago",
            "Sep","Oct","Nov","Dic"
        };

                while (dr.Read())
                {
                    int mes = Convert.ToInt32(dr["Mes"]);

                    valores.Add(Convert.ToDouble(dr["Total"]));

                    etiquetas.Add(meses[mes - 1]);
                }

                Series = new ISeries[]
                {
            new ColumnSeries<double>
            {
                Values = valores
            }
                };

                XAxes = new Axis[]
                {
            new Axis
            {
                Labels = etiquetas
            }
                };

                YAxes = new Axis[]
                {
            new Axis()
                };

                GraficaVentas.Series = Series;
                GraficaVentas.XAxes = XAxes;
                GraficaVentas.YAxes = YAxes;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPeriodo.SelectedItem == null)
                return;

            string opcion = ((ComboBoxItem)cmbPeriodo.SelectedItem).Content.ToString();

            switch (opcion)
            {
                case "Día":
                    CargarGraficaDia();
                    break;

                case "Semana":
                    CargarGraficaSemana();
                    break;

                case "Mes":
                    CargarGraficaMes();
                    break;

                case "Año":
                    CargarGraficaAnio();
                    break;
            }
        }
        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}



