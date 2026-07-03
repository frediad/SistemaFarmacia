namespace FarmaciaPOS.Models
{
    public class ReporteGananciaItem
    {
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Ingreso { get; set; }
        public decimal Costo { get; set; }
        public decimal Ganancia => Ingreso - Costo;
        public double Margen => Ingreso == 0 ? 0 :
            (double)((Ganancia / Ingreso) * 100);
    }
}