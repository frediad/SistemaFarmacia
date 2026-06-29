using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class ConfiguracionWindow : Window
    {
        public ConfiguracionWindow()
        {
            InitializeComponent();
        }

        // =====================================
        // TICKET
        // =====================================

        private void BtnTicket_Click(
            object sender,
            RoutedEventArgs e)
        {
            TicketConfigWindow ticket =
                new TicketConfigWindow();

            ticket.ShowDialog();
        }

        // =====================================
        // PERIFERICOS
        // =====================================

        private void BtnPerifericos_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Configuración periféricos");
        }

        // =====================================
        // USUARIOS
        // =====================================

        private void BtnUsuarios_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Usuarios y roles");
        }

        // =====================================
        // POS
        // =====================================

        private void BtnPOS_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Configuración POS");
        }

        // =====================================
        // FARMACIA
        // =====================================

        private void BtnFarmacia_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Configuración farmacia");
        }

        // =====================================
        // NEGOCIO
        // =====================================

        private void BtnNegocio_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Información negocio");
        }

        // =====================================
        // PROVEEDORES
        // =====================================

        private void BtnProveedor_Click(
            object sender,
            RoutedEventArgs e)
        {
            ProveedoresWindow proveedor =
                new ProveedoresWindow();

            proveedor.ShowDialog();
        }

        // =====================================
        // ACERCA
        // =====================================

        private void BtnAcerca_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "FarmaciaPOS v1.0");
        }
    }
}