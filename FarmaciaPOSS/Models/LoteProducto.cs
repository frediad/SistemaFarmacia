using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmaciaPOS.Models
{
    internal class LoteProducto
    {
        public int Id { get; internal set; }
        public int ProductoId { get; internal set; }
        public string NumeroLote { get; internal set; }
        public int Cantidad { get; internal set; }
        public DateTime FechaCaducidad { get; internal set; }
    }
}
