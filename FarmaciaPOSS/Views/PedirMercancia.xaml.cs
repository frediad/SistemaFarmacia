using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class PedirMercanciaWindow : Window
    {
        private List<Proveedor> proveedores = new();
        private List<Producto> productos = new();
        private ObservableCollection<PedidoProveedorItem> itemsPedido = new();

        public PedirMercanciaWindow(Proveedor proveedorPreseleccionado = null, List<PedidoProveedorItem> itemsPrecargados = null)
        {
            InitializeComponent();

            dgItemsPedido.ItemsSource = itemsPedido;

            CargarProveedores();
            CargarProductos();

            if (proveedorPreseleccionado != null)
            {
                cbProveedor.SelectedValue = proveedorPreseleccionado.Id;
            }

            if (itemsPrecargados != null)
            {
                foreach (var item in itemsPrecargados)
                    itemsPedido.Add(item);

                ActualizarTotal();
            }
        }

        private void CargarProveedores()
        {
            proveedores.Clear();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query = "SELECT * FROM Proveedores ORDER BY Nombre";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                proveedores.Add(new Proveedor
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString(),
                    Telefono = reader["Telefono"].ToString(),
                    Correo = reader["Correo"].ToString(),
                    Direccion = reader["Direccion"].ToString(),
                    Contacto = reader["Contacto"].ToString()
                });
            }

            cbProveedor.ItemsSource = proveedores;
        }

        private void CargarProductos()
        {
            productos.Clear();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query = "SELECT * FROM Productos WHERE Activo = 1 ORDER BY Nombre";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(new Producto
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString(),
                    CodigoBarras = reader["CodigoBarras"].ToString(),
                    Stock = Convert.ToInt32(reader["Stock"]),
                    PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                    ImagenURL = reader["ImagenURL"].ToString(),
                });
            }
        }

        private void cbProveedor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reservado por si en el futuro quieres mostrar datos del proveedor seleccionado
        }

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new BuscarProductoWindow(productos)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado != true || ventana.ProductoSeleccionado == null)
                return;

            var producto = ventana.ProductoSeleccionado;

            var existente = itemsPedido.FirstOrDefault(x => x.Nombre == producto.Nombre);

            if (existente != null)
            {
                existente.Cantidad += 1;
            }
            else
            {
                itemsPedido.Add(new PedidoProveedorItem
                {
                    Nombre = producto.Nombre,
                    Cantidad = 1,
                    CostoUnitario = producto.PrecioCompra
                });
            }

            ActualizarTotal();
        }

        private void BtnQuitarItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PedidoProveedorItem item)
            {
                itemsPedido.Remove(item);
                ActualizarTotal();
            }
        }

        private void ActualizarTotal()
        {
            decimal total = itemsPedido.Sum(x => x.Subtotal);
            txtTotalPedido.Text = total.ToString("C");
        }

        private void BtnEnviarCorreo_Click(object sender, RoutedEventArgs e)
        {
            if (cbProveedor.SelectedItem is not Proveedor proveedor)
            {
                MessageBox.Show("Selecciona un proveedor");
                return;
            }

            if (itemsPedido.Count == 0)
            {
                MessageBox.Show("Agrega al menos un producto al pedido");
                return;
            }

            if (string.IsNullOrWhiteSpace(proveedor.Correo))
            {
                MessageBox.Show(
                    $"El proveedor \"{proveedor.Nombre}\" no tiene un correo electrónico registrado.\n" +
                    "Agrégalo desde el módulo de Proveedores antes de continuar.",
                    "Falta correo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string metodoPago = (cbMetodoPago.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Transferencia";

            string asunto = $"Pedido de mercancía — FarmaClick Yatzil ({DateTime.Now:dd/MM/yyyy})";

            var cuerpo = new StringBuilder();
            cuerpo.AppendLine($"Estimado(a) {(string.IsNullOrWhiteSpace(proveedor.Contacto) ? proveedor.Nombre : proveedor.Contacto)},");
            cuerpo.AppendLine();
            cuerpo.AppendLine("Por medio del presente solicitamos el siguiente pedido de mercancía:");
            cuerpo.AppendLine();
            cuerpo.AppendLine("--------------------------------------------------");

            foreach (var item in itemsPedido)
            {
                cuerpo.AppendLine($"- {item.Nombre}");
                cuerpo.AppendLine($"   Cantidad: {item.Cantidad}   Costo unitario estimado: {item.CostoUnitario:C}   Subtotal: {item.Subtotal:C}");
            }

            cuerpo.AppendLine("--------------------------------------------------");
            cuerpo.AppendLine($"TOTAL ESTIMADO: {itemsPedido.Sum(x => x.Subtotal):C}");
            cuerpo.AppendLine();
            cuerpo.AppendLine($"El pago de este pedido se realizará por: {metodoPago}.");
            cuerpo.AppendLine("Favor de confirmar disponibilidad, precios y tiempo de entrega.");
            cuerpo.AppendLine();
            cuerpo.AppendLine("Quedamos atentos, saludos.");
            cuerpo.AppendLine();
            cuerpo.AppendLine("FarmaClick Yatzil");

            try
            {
                CorreoHelper.AbrirCorreoPedido(proveedor.Correo, asunto, cuerpo.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
        }
    }
}