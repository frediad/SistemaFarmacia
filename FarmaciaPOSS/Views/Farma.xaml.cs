using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class Farma : Window
    {
        public Farma()
        {
            InitializeComponent();
        }

        private void btnCaducidades_Click(object sender, RoutedEventArgs e)
        {
            Caducidades ventana = new Caducidades();
            ventana.ShowDialog();

        }

        private void btnGraficas_Click(object sender, RoutedEventArgs e)
        {
            Graficas ventana = new Graficas();
            ventana.ShowDialog();
        }



        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}