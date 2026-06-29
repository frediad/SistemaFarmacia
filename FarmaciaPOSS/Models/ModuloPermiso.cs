using System.ComponentModel;

namespace FarmaciaPOS.Models
{
    public class ModuloPermiso : INotifyPropertyChanged
    {
        public string NombreModulo { get; set; } = string.Empty;

        private bool _tieneAcceso;
        public bool TieneAcceso
        {
            get => _tieneAcceso;
            set
            {
                _tieneAcceso = value;
                OnPropertyChanged(nameof(TieneAcceso));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string nombre)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
        }
    }
}