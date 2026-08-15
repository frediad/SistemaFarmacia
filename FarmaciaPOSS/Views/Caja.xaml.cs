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

        // ✅ Valores calculados por el sistema, guardados para comparar contra lo "Contado"
        decimal calculadoEfectivo = 0;
        decimal calculadoTarjeta = 0;
        decimal calculadoTransferencia = 0;

        public CajaWindow()
        {
            InitializeComponent();

            CargarCajaAbierta();
            CargarMovimientos();
        }

        // =====================================
        // CARGAR CAJA ABIERTA
        // =====================================

        private void CargarCajaAbierta()
        {
            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            // ✅ Ahora filtra también por Usuario, para que cada quien vea
            // únicamente la caja que él mismo abrió  y no la más reciente
            // del sistema en general.
            string query =
            @"SELECT TOP 1 *
              FROM Caja
              WHERE Estado = 'ABIERTA'
              AND UsuarioId = @UsuarioId
              ORDER BY Id DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);

            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                cajaActualId = Convert.ToInt32(reader["Id"]);
                fechaAperturaActual = Convert.ToDateTime(reader["FechaApertura"]);
                montoInicialActual = Convert.ToDecimal(reader["MontoInicial"]);

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
            (UsuarioId, FechaApertura, MontoInicial, Estado)
            VALUES
            (@UsuarioId, @FechaApertura, @MontoInicial, 'ABIERTA')";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
            cmd.Parameters.AddWithValue("@FechaApertura", DateTime.Now);
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
            (CajaId, TipoMovimiento, Monto, Motivo, Fecha)
            VALUES
            (@CajaId, @TipoMovimiento, @Monto, @Motivo, @Fecha)";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CajaId", cajaActualId);
            cmd.Parameters.AddWithValue("@TipoMovimiento", tipo);
            cmd.Parameters.AddWithValue("@Monto", monto);
            cmd.Parameters.AddWithValue("@Motivo", txtMotivo.Text);
            cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);

            cmd.ExecuteNonQuery();

            MessageBox.Show("Movimiento registrado");

            txtMontoMovimiento.Clear();
            txtMotivo.Clear();

            CargarMovimientos();
            ActualizarResumenEnVivo();
        }

        // =====================================
        // CARGAR MOVIMIENTOS
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
            @"SELECT TipoMovimiento, Monto, Motivo, Fecha
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
        // RESUMEN EN VIVO
        // =====================================

        private void ActualizarResumenEnVivo()
        {
            if (cajaActualId == 0)
                return;

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

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

            decimal ventasEfectivo = 0, ventasTarjeta = 0, ventasTransferencia = 0;

            try
            {
                string queryVentas =
                @"SELECT
                    ISNULL(SUM(CASE WHEN MetodoPago = 'Efectivo' THEN Total ELSE 0 END), 0) AS Efectivo,
                    ISNULL(SUM(CASE WHEN MetodoPago = 'Tarjeta' THEN Total ELSE 0 END), 0) AS Tarjeta,
                    ISNULL(SUM(CASE WHEN MetodoPago = 'Transferencia' THEN Total ELSE 0 END), 0) AS Transferencia
                  FROM Ventas
                  WHERE Fecha >= @FechaApertura
                  AND Estado = 'Completada'
                  AND UsuarioId = @UsuarioId";

                SqlCommand cmdVentas = new SqlCommand(queryVentas, conn);
                cmdVentas.Parameters.AddWithValue("@FechaApertura", fechaAperturaActual);
                cmdVentas.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);

                using SqlDataReader readerVentas = cmdVentas.ExecuteReader();
                if (readerVentas.Read())
                {
                    ventasEfectivo = Convert.ToDecimal(readerVentas["Efectivo"]);
                    ventasTarjeta = Convert.ToDecimal(readerVentas["Tarjeta"]);
                    ventasTransferencia = Convert.ToDecimal(readerVentas["Transferencia"]);
                }
            }
            catch
            {
                ventasEfectivo = 0;
                ventasTarjeta = 0;
                ventasTransferencia = 0;
            }

            // El efectivo esperado en el cajón incluye monto inicial + entradas/salidas manuales.
            // Tarjeta y Transferencia no pasan físicamente por la caja, así que no suman aquí.
            decimal totalEsperado = montoInicialActual + ventasEfectivo + totalEntradas - totalSalidas;

            // Guardamos los "calculados" para usarlos en el corte
            calculadoEfectivo = totalEsperado;
            calculadoTarjeta = ventasTarjeta;
            calculadoTransferencia = ventasTransferencia;

            txtResumenInicial.Text = montoInicialActual.ToString("C");
            txtResumenVentasEfectivo.Text = ventasEfectivo.ToString("C");
            txtResumenVentasTarjeta.Text = ventasTarjeta.ToString("C");
            txtResumenVentasTransferencia.Text = ventasTransferencia.ToString("C");
            txtResumenEntradas.Text = totalEntradas.ToString("C");
            txtResumenSalidas.Text = totalSalidas.ToString("C");
            txtResumenEsperado.Text = totalEsperado.ToString("C");
        }

        // =====================================
        // ✅ ABRIR OVERLAY DE CORTE (ESTILO SICAR)
        // =====================================

        private void BtnAbrirCorte_Click(object sender, RoutedEventArgs e)
        {
            if (cajaActualId == 0)
            {
                MessageBox.Show("No hay caja abierta");
                return;
            }

            txtContadoEfectivo.Text = "0";
            txtContadoTarjeta.Text = "0";
            txtContadoTransferencia.Text = "0";

            txtCalculadoEfectivo.Text = calculadoEfectivo.ToString("C");
            txtCalculadoTarjeta.Text = calculadoTarjeta.ToString("C");
            txtCalculadoTransferencia.Text = calculadoTransferencia.ToString("C");

            ActualizarDiferenciasCorte();

            overlayCorte.Visibility = Visibility.Visible;
        }

        private void BtnCancelarCorte_Click(object sender, RoutedEventArgs e)
        {
            overlayCorte.Visibility = Visibility.Collapsed;
        }

        // =====================================
        // ✅ RECALCULAR DIFERENCIAS AL ESCRIBIR "CONTADO"
        // =====================================

        private void ContadoCorte_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtCorteEsperado == null || txtCorteContado == null || txtCorteDiferencia == null)
                return;

            ActualizarDiferenciasCorte();
        }

        private void ActualizarDiferenciasCorte()
        {
            decimal Leer(TextBox txt) => decimal.TryParse(txt.Text, out decimal v) ? v : 0;

            decimal contadoEfectivo = Leer(txtContadoEfectivo);
            decimal contadoTarjeta = Leer(txtContadoTarjeta);
            decimal contadoTransferencia = Leer(txtContadoTransferencia);

            decimal difEfectivo = contadoEfectivo - calculadoEfectivo;
            decimal difTarjeta = contadoTarjeta - calculadoTarjeta;
            decimal difTransferencia = contadoTransferencia - calculadoTransferencia;

            txtDiferenciaEfectivo.Text = difEfectivo.ToString("C");
            txtDiferenciaTarjeta.Text = difTarjeta.ToString("C");
            txtDiferenciaTransferencia.Text = difTransferencia.ToString("C");

            PintarDiferencia(txtDiferenciaEfectivo, difEfectivo);
            PintarDiferencia(txtDiferenciaTarjeta, difTarjeta);
            PintarDiferencia(txtDiferenciaTransferencia, difTransferencia);

            decimal totalContado = contadoEfectivo + contadoTarjeta + contadoTransferencia;
            decimal totalCalculado = calculadoEfectivo + calculadoTarjeta + calculadoTransferencia;
            decimal totalDiferencia = totalContado - totalCalculado;

            txtCorteContado.Text = totalContado.ToString("C");
            txtCorteEsperado.Text = totalCalculado.ToString("C");
            txtCorteDiferencia.Text = totalDiferencia.ToString("C");

            PintarDiferencia(txtCorteDiferencia, totalDiferencia);
        }

        private void PintarDiferencia(TextBlock control, decimal diferencia)
        {
            if (diferencia == 0)
                control.Foreground = System.Windows.Media.Brushes.Green;
            else if (diferencia > 0)
                control.Foreground = System.Windows.Media.Brushes.DarkOrange;
            else
                control.Foreground = System.Windows.Media.Brushes.Red;
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
                FechaCierre = @FechaCierre,
                Estado = 'CERRADA',
                MontoFinalEsperado = @Esperado,
                MontoFinalContado = @Contado,
                Diferencia = @Diferencia
              WHERE Id = @Id";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", cajaActualId);
            cmd.Parameters.AddWithValue("@FechaCierre", DateTime.Now);
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
        // CERRAR CAJA SIN CORTE
        // =====================================

        private void BtnCerrarCaja_Click(object sender, RoutedEventArgs e)
        {
            if (cajaActualId == 0)
            {
                MessageBox.Show("No hay caja abierta");
                return;
            }

            var confirmar = MessageBox.Show(
                "Vas a cerrar la caja SIN hacer el corte.\n" +
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
                FechaCierre = @FechaCierre,
                Estado = 'CERRADA'
              WHERE Id = @Id";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", cajaActualId);
            cmd.Parameters.AddWithValue("@FechaCierre", DateTime.Now);

            cmd.ExecuteNonQuery();

            MessageBox.Show("Caja cerrada correctamente");

            Close();
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}