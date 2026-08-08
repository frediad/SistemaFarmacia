using FarmaciaPOS.Helpers;
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

        private void BtnTicket_Click(object sender, RoutedEventArgs e)
        {
            TicketConfigWindow ticket = new TicketConfigWindow();
            ticket.ShowDialog();
        }

        // =====================================
        // PERIFÉRICOS
        // =====================================

        private void BtnPerifericos_Click(object sender, RoutedEventArgs e)
        {
            Perifericos Ventana = new Perifericos();
            Ventana.ShowDialog();
        }

        // =====================================
        // USUARIOS
        // =====================================

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Usuarios y Roles"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            UsuariosWindow usuarios = new UsuariosWindow();
            usuarios.ShowDialog();
        }

        // =====================================
        // POS
        // =====================================

        private void BtnPOS_Click(object sender, RoutedEventArgs e)
        {
            PosConfig Ventana  = new PosConfig();
            Ventana.ShowDialog();
            
        }

        // =====================================
        // FARMACIA
        // =====================================

        private void BtnFarmacia_Click(object sender, RoutedEventArgs e)
        {
            Graficas ventana = new Graficas();

            ventana.ShowDialog();
        }

        // =====================================
        // NEGOCIO
        // =====================================

        private void BtnNegocio_Click(object sender, RoutedEventArgs e)
        {
            new InformacionNegocio().ShowDialog();
        }

        // =====================================
        // PROVEEDORES
        // =====================================

        private void BtnProveedor_Click(object sender, RoutedEventArgs e)
        {
            ProveedoresWindow proveedor = new ProveedoresWindow();
            proveedor.ShowDialog();
        }

        // =====================================
        // ACERCA
        // =====================================

        private void BtnAcerca_Click(object sender, RoutedEventArgs e)
        {
            new AcercaDelSistema().ShowDialog();
        }

        // =====================================
        // CERRAR
        // =====================================

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}