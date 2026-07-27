using FarmaciaPOS.Helpers;
using System;
using System.Reflection;
using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class AcercaDelSistema : Window
    {
        public AcercaDelSistema()
        {
            InitializeComponent();
            CargarInformacion();
        }

        private void CargarInformacion()
        {
            try
            {
                // Versión del ensamblado (se toma de las propiedades del proyecto,
                // AssemblyInfo.cs o el .csproj bajo <Version>).
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    txtVersion.Text = $"Versión {version.Major}.{version.Minor}.{version.Build}";
                }

                // Fecha de compilación aproximada, usando la fecha de modificación del .exe/.dll.
                string ruta = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(ruta))
                {
                    DateTime fechaCompilacion = System.IO.File.GetLastWriteTime(ruta);
                    txtFecha.Text = fechaCompilacion.ToString("dd/MM/yyyy");
                }

                // Modo de conexión actual (Local / Azure), usando el DatabaseHelper existente.
                txtModoBD.Text = DatabaseHelper.ObtenerModoActual();
            }
            catch
            {
                // Si algo falla al leer metadatos, dejamos los valores por defecto del XAML.
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
