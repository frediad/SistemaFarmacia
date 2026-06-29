namespace FarmaciaPOS.Models
{
    internal class Subcategoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int CategoriaId { get; set; }
    }
}