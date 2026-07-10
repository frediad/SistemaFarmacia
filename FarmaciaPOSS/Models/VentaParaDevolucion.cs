using System;
using System.Collections.Generic;

namespace FarmaciaPOS.Models
{
    public class VentaParaDevolucion
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Folio { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public List<DetalleVentaDevolucion> Detalles { get; set; } = new();
    }

    public class DetalleVentaDevolucion
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int CantidadYaDevuelta { get; set; }
        public int CantidadADevolver { get; set; }
        public int CantidadDisponibleDevolver =>
            CantidadVendida - CantidadYaDevuelta;
    }
}