using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class MensajeWindow : Window
    {
        public bool Resultado { get; private set; } = false;

        public MensajeWindow()
        {
            InitializeComponent();
        }

        public void Configurar(string mensaje, string titulo, TipoMensaje tipo, bool esConfirmacion)
        {
            txtMensaje.Text = mensaje;
            txtTitulo.Text = titulo;

            switch (tipo)
            {
                case TipoMensaje.Exito:
                    txtIcono.Text = "✅";
                    borderIcono.Background = System.Windows.Media.Brushes.Transparent;
                    borderIcono.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#DCFCE7");
                    break;

                case TipoMensaje.Advertencia:
                    txtIcono.Text = "⚠️";
                    borderIcono.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FEF3C7");
                    break;

                case TipoMensaje.Error:
                    txtIcono.Text = "❌";
                    borderIcono.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FEE2E2");
                    break;

                case TipoMensaje.Pregunta:
                    txtIcono.Text = "❓";
                    borderIcono.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#EFF6FF");
                    break;

                default:
                    txtIcono.Text = "ℹ️";
                    borderIcono.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#EFF6FF");
                    break;
            }

            if (esConfirmacion)
            {
                btnNo.Visibility = Visibility.Visible;
                btnSiOk.Content = "Sí";
            }
            else
            {
                btnNo.Visibility = Visibility.Collapsed;
                btnSiOk.Content = "Aceptar";
            }
        }

        private void BtnSiOk_Click(object sender, RoutedEventArgs e)
        {
            Resultado = true;
            DialogResult = true;
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Resultado = false;
            DialogResult = false;
        }
    }

    public enum TipoMensaje
    {
        Informacion,
        Exito,
        Advertencia,
        Error,
        Pregunta
    }
}