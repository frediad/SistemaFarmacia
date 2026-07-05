using System;
using System.Collections.Generic;
using System.Linq;

namespace FarmaciaPOS.Models
{
    public class VentaEnEspera
    {
        public int Id { get; set; }
        public string Referencia { get; set; }
        public DateTime Hora { get; set; } = DateTime.Now;
        public List<VentaItem> Items { get; set; } = new();

        public decimal Total => Items.Sum(x => x.Subtotal);
    }
}