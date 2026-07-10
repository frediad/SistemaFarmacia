using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class CajaWindow : Window
    {
        int cajaActualId = 0;
        DateTime fechaAperturaActual;
        decimal montoInicialActual = 0;

        public CajaWindow()
        {
            InitializeComponent();

            CargarCajaAbierta();
            CargarMovimientos();
            CargarHistorialCortes();
        }

        // =====================================
        // CARGAR CAJA ABIERTA
        // =====================================

        private void CargarCajaAbierta()
        {
            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT TOP 1 *
              FROM Caja
              WHERE Estado = 'ABIERTA'
              ORDER BY Id DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                cajaActualId = Convert.ToInt32(reader["Id"]);
                fechaAperturaActual = Convert.ToDateTime(reader["FechaApertura"]);
                montoInicialActual = Convert.ToDecimal(reader["MontoInicial"]);

                // ✅ Muestra el panel de movimientos, oculta el de apertura
                pnlApertura.Visibility = Visibility.Collapsed;
                pnlMovimientos.Visibility = Visibility.Visible;

                ActualizarResumenEnVivo();
            }
            else
            {
                cajaActualId = 0;

                pnlApertura.Visibility = Visibility.Visible;
                pnlMovimientos.Visibility = Visibility.Collapsed;

                txtMontoInicial.Text = "";
                txtMontoInicial.IsEnabled = true;
                btnAbrirCaja.IsEnabled = true;
            }
        }

        // =====================================
        // ABRIR CAJA
        // =====================================

        private void BtnAbrirCaja_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtMontoInicial.Text, out decimal monto) || monto < 0)
            {
                MessageBox.Show("Ingresa un monto inicial válido");
                return;
            }

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"INSERT INTO Caja
            (
                UsuarioId,
                FechaApertura,
                MontoInicial,
                Estado
            )
            VALUES
            (
                @UsuarioId,
                GETDATE(),
                @MontoInicial,
                'ABIERTA'
            )";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
            cmd.Parameters.AddWithValue("@MontoInicial", monto);

            cmd.ExecuteNonQuery();

            MessageBox.Show("Caja abierta correctamente");

            CargarCajaAbierta();
            CargarMovimientos();
        }

        // =====================================
        // REGISTRAR MOVIMIENTO
        // =====================================

        private void BtnMovimiento_Click(object sender, RoutedEventArgs e)
        {
            if (cajaActualId == 0)
            {
                MessageBox.Show("No hay caja abierta");
                return;
            }

            if (!decimal.TryParse(txtMontoMovimiento.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingresa un monto válido mayor a cero");
                return;
            }

            ComboBoxItem item = cbTipoMovimiento.SelectedItem as ComboBoxItem;

            if (item == null)
            {
                MessageBox.Show("Selecciona tipo");
                return;
            }

            string tipo = item.Content.ToString();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"INSERT INTO MovimientosCaja
            (
                CajaId,
                TipoMovimiento,
                Monto,
                Motivo,
                Fecha
            )
            VALUES
            (
                @CajaId,
                @TipoMovimiento,
                @Monto,
                @Motivo,
                GETDATE()
            )";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CajaId", cajaActualId);
            cmd.Parameters.AddWithValue("@TipoMovimiento", tipo);
            cmd.Parameters.AddWithValue("@Monto", monto);
            cmd.Parameters.AddWithValue("@Motivo", txtMotivo.Text);

            cmd.ExecuteNonQuery();

            MessageBox.Show("Movimiento registrado");

            txtMontoMovimiento.Clear();
            txtMotivo.Clear();

            CargarMovimientos();
            ActualizarResumenEnVivo();
        }

        // =====================================
        // CARGAR MOVIMIENTOS (solo de la caja actual)
        // =====================================

        private void CargarMovimientos()
        {
            if (cajaActualId == 0)
            {
                dgMovimientos.ItemsSource = null;
                return;
            }

            List<dynamic> lista = new();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                TipoMovimiento,
                Monto,
                Motivo,
                Fecha
              FROM MovimientosCaja
              WHERE CajaId = @CajaId
              ORDER BY Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CajaId", cajaActualId);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new
                {
                    Tipo = reader["TipoMovimiento"],
                    Monto = Convert.ToDecimal(reader["Monto"]).ToString("C"),
                    Motivo = reader["Motivo"],
                    Fecha = Convert.ToDateTime(reader["Fecha"]).ToString("dd/MM/yyyy HH:mm")
                });
            }

            dgMovimientos.ItemsSource = lista;
        }

        // =====================================
        // ✅ RESUMEN EN VIVO (estilo Sicar)
        // =====================================

        private void ActualizarResumenEnVivo()
        {
            if (cajaActualId == 0)
                return;

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            // Entradas y salidas manuales
            string queryMovs =
            @"SELECT
                ISNULL(SUM(CASE WHEN TipoMovimiento = 'ENTRADA' THEN Monto ELSE 0 END), 0) AS TotalEntradas,
                ISNULL(SUM(CASE WHEN TipoMovimiento = 'SALIDA' THEN Monto ELSE 0 END), 0) AS TotalSalidas
              FROM MovimientosCaja
              WHERE CajaId = @CajaId";

            SqlCommand cmdMovs = new SqlCommand(queryMovs, conn);
            cmdMovs.Parameters.AddWithValue("@CajaId", cajaActualId);

            decimal totalEntradas = 0, totalSalidas = 0;

            using (SqlDataReader reader = cmdMovs.ExecuteReader())
            {
                if (reader.Read())
                {
                    totalEntradas = Convert.ToDecimal(reader["TotalEntradas"]);
                    totalSalidas = Convert.ToDecimal(reader["TotalSalidas"]);
                }
            }

            // ✅ Ventas en efectivo desde que se abrió la caja
            // NOTA: asume que tu tabla Ventas tiene una columna "MetodoPago" con valor 'Efectivo'.
            // Si tu columna se llama distinto, ajusta el nombre aquí.
            decimal ventasEfectivo = 0;

            try
            {
                string queryVentas =
                @"SELECT ISNULL(SUM(Total), 0) AS TotalEfectivo
                  FROM Ventas
                  WHERE Fecha >= @FechaApertura
                  AND Estado = 'Completada'
                  AND MetodoPago = 'Efectivo'";

                SqlCommand cmdVentas = new SqlCommand(queryVentas, conn);
                cmdVentas.Parameters.AddWithValue("@FechaApertura", fechaAperturaActual);

                var resultado = cmdVentas.ExecuteScalar();
                ventasEfectivo = resultado != null ? Convert.ToDecimal(resultado) : 0;
            }
            catch
            {
                // Si la columna MetodoPago no existe todavía, se omite este dato sin romper la ventana
                ventasEfectivo = 0;
            }

            decimal totalEsperado = montoInicialActual + ventasEfectivo + totalEntradas - totalSalidas;

            txtResumenInicial.Text = montoInicialActual.ToString("C");
            txtResumenVentasEfectivo.Text = ventasEfectivo.ToString("C");
            txtResumenEntradas.Text = totalEntradas.ToString("C");
            txtResumenSalidas.Text = totalSalidas.ToString("C");
            txtResumenEsperado.Text = totalEsperado.ToString("C");
        }

        // =====================================
        // ✅ ABRIR OVERLAY DE CORTE (ARQUEO)
        // =====================================

        private void BtnAbrirCorte_Click(object sender, RoutedEventArgs e)
        {
            if (cajaActualId == 0)
            {
                MessageBox.Show("No hay caja abierta");
                return;
            }

            // Limpia el conteo anterior
            txtCant1000.Text = "0";
            txtCant500.Text = "0";
            txtCant200.Text = "0";
            txtCant100.Text = "0";
            txtCant50.Text = "0";
            txtCant20.Text = "0";
            txtCant10.Text = "0";
            txtCant5.Text = "0";
            txtCant2.Text = "0";
            txtCant1.Text = "0";
            txtCant050.Text = "0";

            // Extrae el total esperado ya calculado en el resumen en vivo
            string textoEsperado = txtResumenEsperado.Text.Replace("$", "").Replace(",", "");
            decimal.TryParse(textoEsperado, out decimal esperado);

            txtCorteEsperado.Text = esperado.ToString("C");
            txtCorteContado.Text = "$0.00";
            txtCorteDiferencia.Text = "$0.00";

            overlayCorte.Visibility = Visibility.Visible;
        }

        private void BtnCancelarCorte_Click(object sender, RoutedEventArgs e)
        {
            overlayCorte.Visibility = Visibility.Collapsed;
        }

        // =====================================
        // ✅ CALCULAR TOTAL CONTADO AL CAMBIAR CUALQUIER DENOMINACIÓN
        // =====================================

        private void Denominacion_TextChanged(object sender, TextChangedEventArgs e)
        {
            // ✅ Evita el crash: durante InitializeComponent, estos controles
            // pueden no existir todavía si el TextBox de denominación se construye antes.
            if (txtCorteEsperado == null || txtCorteContado == null || txtCorteDiferencia == null)
                return;

            int Cant(TextBox txt) => int.TryParse(txt.Text, out int v) ? v : 0;

            decimal total =
                Cant(txtCant1000) * 1000m +
                Cant(txtCant500) * 500m +
                Cant(txtCant200) * 200m +
                Cant(txtCant100) * 100m +
                Cant(txtCant50) * 50m +
                Cant(txtCant20) * 20m +
                Cant(txtCant10) * 10m +
                Cant(txtCant5) * 5m +
                Cant(txtCant2) * 2m +
                Cant(txtCant1) * 1m +
                Cant(txtCant050) * 0.5m;

            txtCorteContado.Text = total.ToString("C");

            string textoEsperado = txtCorteEsperado.Text.Replace("$", "").Replace(",", "");
            decimal.TryParse(textoEsperado, out decimal esperado);

            decimal diferencia = total - esperado;
            txtCorteDiferencia.Text = diferencia.ToString("C");

            if (diferencia == 0)
                txtCorteDiferencia.Foreground = System.Windows.Media.Brushes.Green;
            else if (diferencia > 0)
                txtCorteDiferencia.Foreground = System.Windows.Media.Brushes.DarkOrange;
            else
                txtCorteDiferencia.Foreground = System.Windows.Media.Brushes.Red;
        }

        // =====================================
        // ✅ CONFIRMAR CORTE Y CERRAR CAJA
        // =====================================

        private void BtnConfirmarCorte_Click(object sender, RoutedEventArgs e)
        {
            string textoEsperado = txtCorteEsperado.Text.Replace("$", "").Replace(",", "");
            string textoContado = txtCorteContado.Text.Replace("$", "").Replace(",", "");
            string textoDiferencia = txtCorteDiferencia.Text.Replace("$", "").Replace(",", "");

            decimal.TryParse(textoEsperado, out decimal esperado);
            decimal.TryParse(textoContado, out decimal contado);
            decimal.TryParse(textoDiferencia, out decimal diferencia);

            string estadoTexto =
                diferencia == 0 ? "Cuadre exacto" :
                diferencia > 0 ? $"Sobrante de {diferencia:C}" :
                                 $"Faltante de {Math.Abs(diferencia):C}";

            var confirmar = MessageBox.Show(
                $"Esperado: {esperado:C}\n" +
                $"Contado: {contado:C}\n" +
                $"Resultado: {estadoTexto}\n\n" +
                "¿Confirmas el corte y deseas cerrar la caja?",
                "Confirmar corte de caja",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes)
                return;

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"UPDATE Caja
              SET
                FechaCierre = GETDATE(),
                Estado = 'CERRADA',
                MontoFinalEsperado = @Esperado,
                MontoFinalContado = @Contado,
                Diferencia = @Diferencia
              WHERE Id = @Id";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", cajaActualId);
            cmd.Parameters.AddWithValue("@Esperado", esperado);
            cmd.Parameters.AddWithValue("@Contado", contado);
            cmd.Parameters.AddWithValue("@Diferencia", diferencia);

            cmd.ExecuteNonQuery();

            overlayCorte.Visibility = Visibility.Collapsed;

            MessageBox.Show(
                $"Caja cerrada correctamente.\n{estadoTexto}",
                "Corte completado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }

        // =====================================
        // CERRAR CAJA SIN CORTE (rápido, sin arqueo)
        // =====================================

        private void BtnCerrarCaja_Click(object sender, RoutedEventArgs e)
        {
            if (cajaActualId == 0)
            {
                MessageBox.Show("No hay caja abierta");
                return;
            }

            var confirmar = MessageBox.Show(
                "Vas a cerrar la caja SIN hacer el arqueo de efectivo.\n" +
                "Se recomienda usar \"Realizar Corte de Caja\" para llevar un control exacto.\n\n" +
                "¿Deseas continuar de todas formas?",
                "Confirmar cierre rápido",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmar != MessageBoxResult.Yes)
                return;

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"UPDATE Caja
              SET
                FechaCierre = GETDATE(),
                Estado = 'CERRADA'
              WHERE Id = @Id";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", cajaActualId);

            cmd.ExecuteNonQuery();

            MessageBox.Show("Caja cerrada correctamente");

            Close();
        }

        // =====================================
        // ✅ HISTORIAL DE CORTES
        // =====================================

        private void CargarHistorialCortes()
        {
            List<HistorialCorteView> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT TOP 100 *
              FROM Caja
              WHERE Estado = 'CERRADA'
              ORDER BY FechaCierre DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                decimal? esperado = reader["MontoFinalEsperado"] != DBNull.Value
                    ? Convert.ToDecimal(reader["MontoFinalEsperado"]) : null;

                decimal? contado = reader["MontoFinalContado"] != DBNull.Value
                    ? Convert.ToDecimal(reader["MontoFinalContado"]) : null;

                decimal? diferencia = reader["Diferencia"] != DBNull.Value
                    ? Convert.ToDecimal(reader["Diferencia"]) : null;

                string estado =
                    diferencia == null ? "Sin arqueo" :
                    diferencia == 0 ? "✅ Cuadre exacto" :
                    diferencia > 0 ? $"🟡 Sobrante" :
                                     $"🔴 Faltante";

                lista.Add(new HistorialCorteView
                {
                    FechaApertura = Convert.ToDateTime(reader["FechaApertura"]),
                    FechaCierre = reader["FechaCierre"] != DBNull.Value
                        ? Convert.ToDateTime(reader["FechaCierre"]) : (DateTime?)null,
                    MontoInicial = Convert.ToDecimal(reader["MontoInicial"]),
                    MontoFinalEsperado = esperado ?? 0,
                    MontoFinalContado = contado ?? 0,
                    Diferencia = diferencia ?? 0,
                    Estado = estado
                });
            }

            dgHistorialCortes.ItemsSource = lista;
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // =====================================
    // ✅ CLASE AUXILIAR PARA EL HISTORIAL
    // =====================================

    public class HistorialCorteView
    {
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal MontoFinalEsperado { get; set; }
        public decimal MontoFinalContado { get; set; }
        public decimal Diferencia { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}