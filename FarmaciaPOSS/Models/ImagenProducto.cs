namespace FarmaciaPOS.Models
{
    public class ImagenProducto
    {
        public int Id { get; set; }

        public int ProductoId { get; set; }

        public string RutaImagen { get; set; } = string.Empty;

        public int Orden { get; set; }


        public byte[]? ImagenData { get; set; }

    }
}