namespace FarmaciaPOS.Models
{
    // Datos para una tarjeta de estadística (mensual o anual):
    // periodo actual vs. periodo anterior, con la diferencia y el signo.
    public class EstadisticaVentas
    {
        public string EtiquetaActual { get; set; } = "";
        public decimal MontoActual { get; set; }

        public string EtiquetaAnterior { get; set; } = "";
        public decimal MontoAnterior { get; set; }

        public decimal Diferencia => MontoActual - MontoAnterior;

        public double PorcentajeCambio =>
            MontoAnterior == 0 ? 0 : (double)(Diferencia / MontoAnterior) * 100;

        public bool EsPositivo => Diferencia >= 0;
    }
}