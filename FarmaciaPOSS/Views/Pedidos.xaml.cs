using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class PedidosWindow : Window
    {
        ObservableCollection<PedidoView> listaTodosLosPedidos = new();
        int pedidoSeleccionadoId = 0;
        PedidoView? pedidoSeleccionado = null;

        // ✅ Filtro de periodo activo (Dia/Semana/Mes/Anio/Todo)
        private string filtroPeriodoActivo = "Todo";

        // ✅ Para detectar pedidos nuevos: el Id más alto que ya conocíamos
        // la última vez que se revisó (al abrir la ventana o al actualizar).
        private int ultimoIdPedidoConocido = 0;

        // ✅ Evita que sincronizar visualmente el combo de estado (al elegir
        // un pedido distinto en la tabla) dispare por accidente un cambio de
        // estado real en la base de datos.
        private bool sincronizandoComboEstado = false;

        public PedidosWindow()
        {
            InitializeComponent();

            dgPedidos.ItemsSource = listaTodosLosPedidos;

            try
            {
                cbEstado.SelectedIndex = 0;
                CargarPedidos();

                // Al abrir la ventana, el Id más alto actual se considera ya
                // "conocido" — no se marca como pedido nuevo lo que ya existía.
                ultimoIdPedidoConocido = ObtenerMaxIdPedidoActual();
                ActualizarBadgeNuevosPedidos(0);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "ERROR", this);
            }
        }

        // =========================================
        // CARGAR PEDIDOS
        // =========================================

        // ✅ Ahora respeta también el filtro de periodo activo, además del
        // filtro de estado que ya tenías.
        private void CargarPedidos(string estado = "")
        {
            listaTodosLosPedidos.Clear();

            (DateTime desde, DateTime hasta) = ObtenerRangoFiltroPeriodo();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                p.Id,
                p.NumeroPedido,
                c.Nombre AS ClienteNombre,
                c.Correo AS ClienteCorreo,
                p.FechaPedido,
                p.Total,
                p.EstadoPedido,
                p.HoraRecogida,
                p.Observaciones
              FROM Pedidos p
              INNER JOIN Clientes c ON p.ClienteId = c.Id
              WHERE p.FechaPedido >= @Desde AND p.FechaPedido < @Hasta";

            if (!string.IsNullOrEmpty(estado) && estado != "Todos los pedidos")
                query += " AND p.EstadoPedido = @Estado";

            query += " ORDER BY p.FechaPedido DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde);
            cmd.Parameters.AddWithValue("@Hasta", hasta);

            if (!string.IsNullOrEmpty(estado) && estado != "Todos los pedidos")
                cmd.Parameters.AddWithValue("@Estado", estado);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                listaTodosLosPedidos.Add(new PedidoView
                {
                    Id = (int)reader["Id"],
                    NumeroPedido = reader["NumeroPedido"].ToString() ?? "",
                    ClienteNombre = reader["ClienteNombre"].ToString() ?? "",
                    ClienteCorreo = reader["ClienteCorreo"].ToString() ?? "",
                    FechaPedido = (DateTime)reader["FechaPedido"],
                    Total = (decimal)reader["Total"],
                    EstadoPedido = reader["EstadoPedido"].ToString() ?? "",
                    HoraRecogida = reader["HoraRecogida"].ToString() ?? "",
                    Observaciones = reader["Observaciones"].ToString() ?? ""
                });
            }
        }

        // =========================================
        // ✅ FILTRO DE PERIODO (Hoy / Semana / Mes / Año / Todo)
        // =========================================

        private void BtnFiltroPeriodoPedidos_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag)
                return;

            filtroPeriodoActivo = tag;

            foreach (var b in new[] { btnFiltroHoyPedidos, btnFiltroSemanaPedidos, btnFiltroMesPedidos, btnFiltroAnioPedidos, btnFiltroTodoPedidos })
                b.Style = (Style)FindResource("BtnFiltroPeriodoPedidos");

            btn.Style = (Style)FindResource("BtnFiltroPeriodoPedidosActivo");

            string estadoFiltro = (cbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            CargarPedidos(estadoFiltro);
        }

        // Rango [Desde, Hasta) con hora LOCAL del equipo — mismo criterio que
        // ya usamos en Ventas y Clientes para los filtros de periodo.
        private (DateTime desde, DateTime hasta) ObtenerRangoFiltroPeriodo()
        {
            DateTime hoy = DateTime.Now.Date;
            DateTime mañana = hoy.AddDays(1);

            return filtroPeriodoActivo switch
            {
                "Dia" => (hoy, mañana),
                "Semana" => (hoy.AddDays(-((int)hoy.DayOfWeek == 0 ? 6 : (int)hoy.DayOfWeek - 1)), mañana),
                "Mes" => (new DateTime(hoy.Year, hoy.Month, 1), mañana),
                "Anio" => (new DateTime(hoy.Year, 1, 1), mañana),
                _ => (new DateTime(2000, 1, 1), mañana) // "Todo"
            };
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
        // BUSCADOR POR CLIENTE
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
        // ✅ ACTUALIZAR + DETECTAR PEDIDOS NUEVOS
        // =========================================

        private int ObtenerMaxIdPedidoActual()
        {
            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(Id), 0) FROM Pedidos", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private int ContarPedidosNuevos()
        {
            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Pedidos WHERE Id > @UltimoId", conn);
            cmd.Parameters.AddWithValue("@UltimoId", ultimoIdPedidoConocido);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private void BtnActualizarPedidos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int nuevos = ContarPedidosNuevos();

                string estadoFiltro = (cbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                CargarPedidos(estadoFiltro);

                ultimoIdPedidoConocido = ObtenerMaxIdPedidoActual();

                ActualizarBadgeNuevosPedidos(nuevos);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo actualizar: " + ex.Message, "Error", this);
            }
        }

        private void ActualizarBadgeNuevosPedidos(int nuevos)
        {
            if (nuevos > 0)
            {
                txtBadgeNuevosPedidos.Text = nuevos == 1
                    ? "🔔 1 pedido nuevo"
                    : $"🔔 {nuevos} pedidos nuevos";
                txtBadgeNuevosPedidos.Visibility = Visibility.Visible;
            }
            else
            {
                txtBadgeNuevosPedidos.Visibility = Visibility.Collapsed;
            }
        }

        // =========================================
        // SELECCIONAR PEDIDO
        // =========================================

        private void DgPedidos_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dgPedidos.SelectedItem is PedidoView pedido)
            {
                pedidoSeleccionadoId = pedido.Id;
                pedidoSeleccionado = pedido;

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

                SincronizarComboEstado(pedido);
            }
        }

        // =========================================
        // CARGAR PRODUCTOS DEL PEDIDO
        // =========================================

        private void CargarDetallePedido(int idPedido)
        {
            try
            {
                List<DetallePedidoView> lista = new();

                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query =
                @"SELECT
                pr.Nombre AS NombreProducto,
                pr.CodigoBarras,
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
                        Nombre = reader["NombreProducto"].ToString() ?? "",
                        Cantidad = Convert.ToInt32(reader["Cantidad"]),
                        Precio = Convert.ToDecimal(reader["Precio"]),
                        Subtotal = Convert.ToDecimal(reader["Subtotal"])
                    });
                }

                dgDetallePedido.ItemsSource = lista;

                if (lista.Count == 0)
                {
                    Debug.WriteLine($"El pedido #{idPedido} no tiene productos en DetallePedidos.");
                }
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(
                    "No se pudieron cargar los productos del pedido: " + ex.Message,
                    "Error",
                    this);
            }
        }

        // =========================================
        // ✅ CAMBIAR ESTADO  + NOTIFICAR POR CORREO
        // =========================================

        // Pone el combo en el estado actual del pedido seleccionado SIN
        // disparar el cambio real en la base de datos, y actualiza el
        // bloqueo/aviso si el pedido ya está finalizado.
        private void SincronizarComboEstado(PedidoView pedido)
        {
            sincronizandoComboEstado = true;

            foreach (ComboBoxItem item in cbCambiarEstado.Items)
            {
                if (item.Tag?.ToString() == pedido.EstadoPedido)
                {
                    cbCambiarEstado.SelectedItem = item;
                    break;
                }
            }

            bool esEstadoFinal = pedido.EstadoPedido == "Entregado" || pedido.EstadoPedido == "Cancelado";

            cbCambiarEstado.IsEnabled = !esEstadoFinal;
            txtPedidoFinalizado.Visibility = esEstadoFinal ? Visibility.Visible : Visibility.Collapsed;

            sincronizandoComboEstado = false;
        }

        private void cbCambiarEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (sincronizandoComboEstado)
                return;

            if (pedidoSeleccionadoId == 0 || pedidoSeleccionado == null)
                return;

            if (cbCambiarEstado.SelectedItem is not ComboBoxItem itemSeleccionado)
                return;

            string nuevoEstado = itemSeleccionado.Tag?.ToString() ?? "";

            if (string.IsNullOrEmpty(nuevoEstado) || nuevoEstado == pedidoSeleccionado.EstadoPedido)
                return;

            // ✅ Bloquea cualquier cambio si el pedido ya está en un estado final
            if (pedidoSeleccionado.EstadoPedido == "Entregado" ||
                pedidoSeleccionado.EstadoPedido == "Cancelado")
            {
                MensajeHelper.Advertencia(
                    $"Este pedido ya está \"{pedidoSeleccionado.EstadoPedido}\" y no se puede modificar.",
                    "Pedido finalizado",
                    this);

                SincronizarComboEstado(pedidoSeleccionado); // regresa el combo a su valor real
                return;
            }

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

                pedidoSeleccionado.EstadoPedido = nuevoEstado;

                MensajeHelper.Exito($"Estado cambiado a: {nuevoEstado}", "Actualizado", this);

                if (!string.IsNullOrWhiteSpace(pedidoSeleccionado.ClienteCorreo))
                {
                    bool notificar = MensajeHelper.Confirmar(
                        $"¿Deseas notificar al cliente por correo?\n\n" +
                        $"📧 {pedidoSeleccionado.ClienteCorreo}\n" +
                        $"Estado nuevo: {nuevoEstado}",
                        "Notificar cliente",
                        this);

                    if (notificar)
                        EnviarCorreoEstado(pedidoSeleccionado, nuevoEstado);
                }
                else
                {
                    MensajeHelper.Advertencia(
                        "El cliente no tiene correo registrado — no se puede enviar notificación.",
                        "Sin correo",
                        this);
                }

                string estadoFiltro = (cbEstado.SelectedItem as ComboBoxItem)?
                    .Content.ToString() ?? "";

                int idPedidoActual = pedidoSeleccionadoId;

                CargarPedidos(estadoFiltro);

                var pedidoActualizado = listaTodosLosPedidos
                    .FirstOrDefault(p => p.Id == idPedidoActual);

                if (pedidoActualizado != null)
                {
                    dgPedidos.SelectedItem = pedidoActualizado;
                    SincronizarComboEstado(pedidoActualizado); // ✅ refresca combo tras el cambio
                }
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "ERROR", this);
            }
        }

        // =========================================
        // ✅ ENVIAR CORREO AL CLIENTE SEGÚN ESTADO
        // =========================================

        private void EnviarCorreoEstado(PedidoView pedido, string estado)
        {
            string asunto = "";
            string cuerpo = "";
            string emoji = "";

            const string LINEA = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

            switch (estado)
            {
                case "Pendiente":
                    emoji = "⏳";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} ha sido recibido — FarmaClick Yatzil";
                    cuerpo =
                        $"Estimado(a) {pedido.ClienteNombre},\n\n" +
                        $"{LINEA}\n" +
                        $"  📋  ESTADO: PENDIENTE\n" +
                        $"{LINEA}\n\n" +
                        $"Hemos recibido tu pedido #{pedido.NumeroPedido},\n" +
                        $"realizado el {pedido.FechaPedido:dd/MM/yyyy}.\n\n" +
                        $"Tu pedido está en nuestra lista de espera y pronto\n" +
                        $"comenzaremos a prepararlo.\n\n" +
                        $"    💰  Total: {pedido.Total:C}\n\n" +
                        $"Te notificaremos cuando tu pedido esté en preparación.\n\n" +
                        $"Gracias por tu preferencia.\n\n" +
                        $"{LINEA}\n" +
                        $"Atentamente,\n" +
                        $"FarmaClick Yatzil";
                    break;

                case "Confirmado":
                    emoji = "👌";
                    asunto = $"Tu pedido #{pedido.NumeroPedido} ha sido confirmado — FarmaClick Yatzil";
                    cuerpo = $"Estimado(a) {pedido.ClienteNombre},\n\n" +
                             $"{LINEA}\n" +
                             $"  📋  ESTADO: CONFIRMADO\n" +
                             $"{LINEA}\n\n" +
                             $"Tu pedido #{pedido.NumeroPedido} ha sido confirmado.\n" +
                             $"¡Gracias por tu preferencia!\n\n" +
                             $"{LINEA}\n" +
                             $"Atentamente,\n" +
                             $"FarmaClick Yatzil";
                    break;

                case "Preparando":
                    emoji = "⚙️";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} está siendo preparado — FarmaClick Yatzil";
                    cuerpo =
                        $"Estimado(a) {pedido.ClienteNombre},\n\n" +
                        $"{LINEA}\n" +
                        $"  📋  ESTADO: EN PREPARACIÓN\n" +
                        $"{LINEA}\n\n" +
                        $"¡Buenas noticias! Tu pedido #{pedido.NumeroPedido}\n" +
                        $"ya está siendo preparado por nuestro equipo.\n\n" +
                        $"    💰  Total: {pedido.Total:C}\n\n" +
                        $"Te avisaremos en cuanto esté listo para que\n" +
                        $"puedas pasar a recogerlo.\n\n" +
                        $"Gracias por tu paciencia.\n\n" +
                        $"{LINEA}\n" +
                        $"Atentamente,\n" +
                        $"FarmaClick Yatzil";
                    break;

                case "En camino":
                    emoji = "🚚";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} está en camino — Farmacia Yatzil";
                    cuerpo =
                        $"Estimado(a) {pedido.ClienteNombre},\n\n" +
                        $"{LINEA}\n" +
                        $"  📋  ESTADO: EN CAMINO\n" +
                        $"{LINEA}\n\n" +
                        $"¡Tu pedido #{pedido.NumeroPedido} ya está en camino!\n" +
                        $"Nuestro equipo de entrega lo llevará a la dirección proporcionada.\n\n" +
                        $"    💰  Total: {pedido.Total:C}\n\n" +
                        $"Te pedimos que estés atento(a) a la llegada de tu pedido.\n\n" +
                        $"Gracias por elegirnos.\n\n" +
                        $"{LINEA}\n" +
                        $"Atentamente,\n" +
                        $"Farmacia Yatzil";
                    break;

                case "Entregado":
                    emoji = "✅";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} ha sido entregado — Farmacia Yatzil";
                    cuerpo =
                        $"Estimado(a) {pedido.ClienteNombre},\n\n" +
                        $"{LINEA}\n" +
                        $"  📋  ESTADO: ENTREGADO\n" +
                        $"{LINEA}\n\n" +
                        $"¡Tu pedido #{pedido.NumeroPedido} ha sido entregado!\n\n" +
                        $"    💰  Total: {pedido.Total:C}\n\n" +
                        $"Gracias por tu compra, esperamos verte pronto de nuevo.\n\n" +
                        $"{LINEA}\n" +
                        $"Atentamente,\n" +
                        $"Farmacia Yatzil";
                    break;

                case "Listo para recoger":
                    emoji = "📦";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} está listo para recoger — Farmacia Yatzil";
                    cuerpo =
                        $"Estimado(a) {pedido.ClienteNombre},\n\n" +
                        $"{LINEA}\n" +
                        $"  📋  ESTADO: LISTO PARA RECOGER\n" +
                        $"{LINEA}\n\n" +
                        $"¡Tu pedido #{pedido.NumeroPedido} ya está listo!\n" +
                        $"Puedes pasar a recogerlo en nuestra farmacia.\n\n" +
                        $"    💰  Total a pagar: {pedido.Total:C}\n" +
                        (string.IsNullOrEmpty(pedido.HoraRecogida)
                            ? ""
                            : $"    🕐  Hora acordada: {pedido.HoraRecogida}\n") +
                        (string.IsNullOrEmpty(pedido.Observaciones)
                            ? ""
                            : $"    📝  Observaciones: {pedido.Observaciones}\n") +
                        $"\nTe esperamos en FarmaClick Yatzil.\n\n" +
                        $"{LINEA}\n" +
                        $"Atentamente,\n" +
                        $"Farmacia Yatzil";
                    break;

                case "Cancelado":
                    emoji = "❌";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} ha sido cancelado — Farmacia Yatzil";
                    cuerpo =
                        $"Estimado(a) {pedido.ClienteNombre},\n\n" +
                        $"{LINEA}\n" +
                        $"  📋  ESTADO: CANCELADO\n" +
                        $"{LINEA}\n\n" +
                        $"Te informamos que tu pedido #{pedido.NumeroPedido}\n" +
                        $"ha sido cancelado.\n\n" +
                        $"Si tienes alguna duda o deseas hacer un nuevo pedido,\n" +
                        $"no dudes en contactarnos.\n\n" +
                        $"Disculpa los inconvenientes.\n\n" +
                        $"{LINEA}\n" +
                        $"Atentamente,\n" +
                        $"Farmacia Yatzil";
                    break;

                default:
                    return;
            }

            try
            {
                string urlGmail =
                    "https://mail.google.com/mail/?view=cm" +
                    "&fs=1" +
                    $"&to={Uri.EscapeDataString(pedido.ClienteCorreo)}" +
                    $"&su={Uri.EscapeDataString($"{emoji} {asunto}")}" +
                    $"&body={Uri.EscapeDataString(cuerpo)}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = urlGmail,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(
                    $"Error al abrir Gmail:\n{ex.Message}",
                    "Error",
                    this);
            }
        }

        // =========================================
        // CERRAR
        // =========================================

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}