using FarmaciaAPI.Data;
using FarmaciaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmaciaWeb.Controllers
{
    public class ProductosController : Controller
    {
        private readonly AppDbContext _context;

        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productos =
                await _context.Productos
                .Where(p => p.Activo)
                .ToListAsync();

            return View(productos);
        }

        [HttpPost]
        public async Task<IActionResult> Comprar(
            int productoId,
            int cantidad,
            string nombre,
            string telefono,
            string correo,
            string direccion)
        {
            var producto =
                await _context.Productos
                .FirstOrDefaultAsync(
                    p => p.Id == productoId);

            if (producto == null)
            {
                TempData["Error"] =
                    "Producto no encontrado";

                return RedirectToAction("Index");
            }

            if (producto.Stock < cantidad)
            {
                TempData["Error"] =
                    "Stock insuficiente";

                return RedirectToAction("Index");
            }

            var cliente =
                new Cliente
                {
                    Nombre = nombre,
                    Telefono = telefono,
                    Correo = correo,
                    Direccion = direccion
                };

            _context.Clientes.Add(cliente);

            await _context.SaveChangesAsync();

            producto.Stock -= cantidad;

            var pedido =
                new Pedido
                {
                    NumeroPedido =
                        "WEB-" +
                        DateTime.Now.ToString("yyyyMMddHHmmss"),

                    ClienteId =
                        cliente.Id,

                    FechaPedido =
                        DateTime.Now,

                    EstadoPedido =
                        "Pendiente",

                    Total =
                        producto.PrecioVenta * cantidad,

                    Observaciones =
                        "Pedido generado desde tienda web"
                };

            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();

            var detalle =
                new DetallePedido
                {
                    PedidoId =
                        pedido.Id,

                    ProductoId =
                        producto.Id,

                    Cantidad =
                        cantidad,

                    Precio =
                        producto.PrecioVenta,

                    Subtotal =
                        producto.PrecioVenta * cantidad
                };

            _context.DetallePedidos.Add(detalle);

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "Compra realizada correctamente";

            return RedirectToAction("Index");
        }
    }
}