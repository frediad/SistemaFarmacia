using FarmaciaPOS.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace FarmaciaPOS.Services
{
    public class ApiService
    {
        string baseUrl =
            "https://localhost:7056/api/";

        // =========================================
        // OBTENER PRODUCTOS
        // =========================================

        public async Task<List<Producto>> ObtenerProductos()
        {
            using HttpClient client = new HttpClient();

            var response =
                await client.GetAsync(baseUrl + "Productos");

            if (!response.IsSuccessStatusCode)
            {
                return new List<Producto>();
            }

            string json =
                await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<Producto>>(json)!;
        }

        // =========================================
        // OBTENER PRODUCTOS POR CADUCAR
        // =========================================

        public async Task<List<Producto>> ObtenerProductosPorCaducar()
        {
            var productos = await ObtenerProductos();

            return productos
                .Where(p => p.Caducidad.HasValue)
                .OrderBy(p => p.Caducidad)
                .ToList();
        }

        // =========================================
        // CREAR PRODUCTO
        // =========================================

        public async Task<bool> CrearProducto(Producto producto)
        {
            using HttpClient client = new HttpClient();

            string json =
                JsonConvert.SerializeObject(producto);

            StringContent content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await client.PostAsync(
                    baseUrl + "Productos",
                    content);

            return response.IsSuccessStatusCode;
        }
    }
}