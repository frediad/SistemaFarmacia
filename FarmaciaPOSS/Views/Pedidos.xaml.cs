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

        public PedidosWindow()
        {
            InitializeComponent();

            dgPedidos.ItemsSource = listaTodosLosPedidos;

            try
            {
                cbEstado.SelectedIndex = 0;
                CargarPedidos();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "ERROR", this);
            }
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
                c.Correo AS ClienteCorreo,
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

                ActualizarBotonesEstado(pedido);
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
        // ✅ CAMBIAR ESTADO + NOTIFICAR POR CORREO
        // =========================================

        private void BtnCambiarEstado_Click(
            object sender, RoutedEventArgs e)
        {
            if (pedidoSeleccionadoId == 0 || pedidoSeleccionado == null)
            {
                MensajeHelper.Advertencia("Selecciona un pedido primero", "Aviso", this);
                return;
            }

            // ✅ Bloquea cualquier cambio si el pedido ya está en un estado final
            if (pedidoSeleccionado.EstadoPedido == "Entregado" ||
                pedidoSeleccionado.EstadoPedido == "Cancelado")
            {
                MensajeHelper.Advertencia(
                    $"Este pedido ya está \"{pedidoSeleccionado.EstadoPedido}\" y no se puede modificar.",
                    "Pedido finalizado",
                    this);
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
                    ActualizarBotonesEstado(pedidoActualizado); // ✅  refresca botones tras cambiar de "Preparando" a "Entregado", etc.
                }
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "ERROR", this);
            }
        }

        // =========================================
        // ✅ HABILITAR/DESHABILITAR BOTONES SEGÚN ESTADO
        // =========================================

        private void ActualizarBotonesEstado(PedidoView pedido)
        {
            bool esEstadoFinal = pedido.EstadoPedido == "Entregado" || pedido.EstadoPedido == "Cancelado";

            btnEstadoPendiente.IsEnabled = !esEstadoFinal;
            btnEstadoPreparando.IsEnabled = !esEstadoFinal;
            btnEstadoListoRecoger.IsEnabled = !esEstadoFinal;
            btnEstadoEntregado.IsEnabled = !esEstadoFinal;
            btnEstadoCancelado.IsEnabled = !esEstadoFinal;

            // Atenúa visualmente los botones deshabilitados
            double opacidad = esEstadoFinal ? 0.4 : 1.0;

            btnEstadoPendiente.Opacity = opacidad;
            btnEstadoPreparando.Opacity = opacidad;
            btnEstadoListoRecoger.Opacity = opacidad;
            btnEstadoEntregado.Opacity = opacidad;
            btnEstadoCancelado.Opacity = opacidad;

            txtPedidoFinalizado.Visibility = esEstadoFinal ? Visibility.Visible : Visibility.Collapsed;
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

                case "Entregado":
                    emoji = "✅";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} está listo para recoger — FarmaClick Yatzil";
                    cuerpo =
                        $"Estimado(a) {pedido.ClienteNombre},\n\n" +
                        $"{LINEA}\n" +
                        $"  📋  ESTADO: LISTO PARA RECOGER\n" +
                        $"{LINEA}\n\n" +
                        $"¡Tu pedido #{pedido.NumeroPedido} está listo!\n" +
                        $"Ya puedes pasar a recogerlo.\n\n" +
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
                        $"FarmaClick Yatzil";
                    break;

                case "Listo para recoger":
                    emoji = "📦";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} está listo para recoger — FarmaClick Yatzil";
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
                        $"FarmaClick Yatzil";
                    break;

                case "Cancelado":
                    emoji = "❌";
                    asunto =
                        $"Tu pedido #{pedido.NumeroPedido} ha sido cancelado — FarmaClick Yatzil";
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
                        $"FarmaClick Yatzil";
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