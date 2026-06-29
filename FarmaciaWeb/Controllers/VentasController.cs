using FarmaciaAPI.Data;
using FarmaciaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmaciaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("comprar")]
        public async Task<IActionResult> Comprar(
            [FromBody] CompraRequest request)
        {
            try
            {
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(
                        p => p.Id == request.ProductoId);

                if (producto == null)
                {
                    return BadRequest(
                        "Producto no encontrado");
                }

                if (producto.Stock < request.Cantidad)
                {
                    return BadRequest(
                        "Stock insuficiente");
                }

                producto.Stock -= request.Cantidad;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Compra realizada",
                    stock = producto.Stock
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}