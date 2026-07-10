using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FarmaciaPOS.Models
{
    public class VentaItem : INotifyPropertyChanged
    {
        public int ProductoId { get; set; }

        public int Stock { get; set; }

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set
            {
                _nombre = value;
                OnPropertyChanged();
            }
        }

        private decimal _precio;
        public decimal Precio
        {
            get => _precio;
            set
            {
                _precio = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        private int _cantidad;
        public int Cantidad
        {
            get => _cantidad;
            set
            {
                _cantidad = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        private decimal _descuento;
        public decimal Descuento
        {
            get => _descuento;
            set
            {
                _descuento = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        // ⭐ SUBTOTAL
        public decimal Subtotal
        {
            get
            {
                return Precio * Cantidad;
            }
        }

        // =========================================
        // NOTIFICACIÓN DE CAMBIOS
        // =========================================

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? nombrePropiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}