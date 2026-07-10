using System.ComponentModel;

namespace FarmaciaPOS.Models
{
    public class SugerenciaCompraItem : INotifyPropertyChanged
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int StockMinimo { get; set; }
        public int StockMaximo{ get; set; }
        public int CantidadSugerida { get; set; }
        public decimal CostoUnitario { get; set; }

        public decimal CostoEstimado => CantidadSugerida * CostoUnitario;

        private bool _seleccionado = true;
        public bool Seleccionado
        {
            get => _seleccionado;
            set
            {
                _seleccionado = value;
                OnPropertyChanged(nameof(Seleccionado));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}