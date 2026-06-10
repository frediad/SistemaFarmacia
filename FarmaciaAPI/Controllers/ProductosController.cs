using FarmaciaAPI.Data;
using FarmaciaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmaciaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductosController(
            AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // OBTENER TODOS
        // =========================================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>>
            GetProductos()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .ToListAsync();
        }

        // =========================================
        // OBTENER POR ID
        // =========================================

        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>>
            GetProducto(int id)
        {
            var producto =
                await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(
                    p => p.Id == id);

            if (producto == null)
            {
                return NotFound();
            }

            return producto;
        }

        // =========================================
        // CREAR PRODUCTO
        // =========================================

        [HttpPost]
        public async Task<ActionResult<Producto>>
            PostProducto(Producto producto)
        {
            producto.FechaCreacion =
                DateTime.Now;

            _context.Productos.Add(producto);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetProducto),
                new { id = producto.Id },
                producto);
        }

        // =========================================
        // ACTUALIZAR
        // =========================================

        [HttpPut("{id}")]
        public async Task<IActionResult>
            PutProducto(
                int id,
                Producto producto)
        {
            if (id != producto.Id)
            {
                return BadRequest();
            }

            _context.Entry(producto)
                .State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // =========================================
        // ELIMINAR
        // =========================================

        [HttpDelete("{id}")]
        public async Task<IActionResult>
            DeleteProducto(int id)
        {
            var producto =
                await _context.Productos
                .FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            producto.Activo = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}