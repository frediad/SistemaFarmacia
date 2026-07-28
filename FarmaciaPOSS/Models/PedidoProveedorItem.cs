using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FarmaciaPOS.Models
{
    public class PedidoProveedorItem : INotifyPropertyChanged
    {
        private int cantidad;
        private decimal costoUnitario;

        public string Nombre { get; set; } = string.Empty;

        public int Cantidad
        {
            get => cantidad;
            set
            {
                cantidad = value;
                OnPropertyChanged(nameof(Cantidad));
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal CostoUnitario
        {
            get => costoUnitario;
            set
            {
                costoUnitario = value;
                OnPropertyChanged(nameof(CostoUnitario));
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal Subtotal => Cantidad * CostoUnitario;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}