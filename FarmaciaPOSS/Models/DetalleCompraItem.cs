using System.ComponentModel;

namespace FarmaciaPOS.Models
{
    public class DetalleCompraItem : INotifyPropertyChanged
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public decimal CostoActual { get; set; }

        private int _cantidad = 1;
        public int Cantidad
        {
            get => _cantidad;
            set
            {
                _cantidad = value;
                OnPropertyChanged(nameof(Cantidad));
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        private decimal _costoUnitario;
        public decimal CostoUnitario
        {
            get => _costoUnitario;
            set
            {
                _costoUnitario = value;
                OnPropertyChanged(nameof(CostoUnitario));
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal Subtotal => Cantidad * CostoUnitario;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}