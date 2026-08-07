using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FarmaciaPOS.Models
{
    public class DetalleRecepcionItem : INotifyPropertyChanged
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = "";
        public int CantidadPedida { get; set; }
        public int StockActual { get; set; }
        public decimal CostoActual { get; set; }

        private int cantidadRecibida;
        public int CantidadRecibida
        {
            get => cantidadRecibida;
            set
            {
                cantidadRecibida = value;
                OnPropertyChanged(nameof(CantidadRecibida));
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        private decimal costoUnitarioReal;
        public decimal CostoUnitarioReal
        {
            get => costoUnitarioReal;
            set
            {
                costoUnitarioReal = value;
                OnPropertyChanged(nameof(CostoUnitarioReal));
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal Subtotal => CantidadRecibida * CostoUnitarioReal;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}