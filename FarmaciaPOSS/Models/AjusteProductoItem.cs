using System.ComponentModel;

namespace FarmaciaPOS.Models
{
    public class AjusteProductoItem : INotifyPropertyChanged
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int StockSistema { get; set; }

        private int _stockContado;
        public int StockContado
        {
            get => _stockContado;
            set
            {
                _stockContado = value;
                OnPropertyChanged(nameof(StockContado));
                OnPropertyChanged(nameof(Diferencia));
                OnPropertyChanged(nameof(TieneDiferencia));
            }
        }

        public int Diferencia => StockContado - StockSistema;
        public bool TieneDiferencia => Diferencia != 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}