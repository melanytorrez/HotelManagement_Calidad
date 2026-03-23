using HotelManagement.Datos.Config;
using HotelManagement.DTOs;
using HotelManagement.Aplicacion.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HotelManagement.Presentacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HuespedController : ControllerBase
    {
        private readonly HotelDbContext _context;
        private readonly IHuespedValidator _validator;
        private readonly ILogger<HuespedController> _logger;

        public HuespedController(
            HotelDbContext context, 
            IHuespedValidator validator,
            ILogger<HuespedController> logger)
        {
            _context = context;
            _validator = validator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? nombre = null,
            [FromQuery] string? apellido = null,
            [FromQuery] string? documento_identidad = null,
            [FromQuery] string? telefono = null,
            [FromQuery] bool? activo = null)
        {
            var huespedes = await _context.Huespedes
                .Select(h => new HuespedDto
                {
                    ID = GuidToString(h.ID),
                    Nombre = h.Nombre,
                    Apellido = h.Apellido,
                    Segundo_Apellido = h.Segundo_Apellido,
                    Documento_Identidad = h.Documento_Identidad,
                    Telefono = h.Telefono,
                    Fecha_Nacimiento = h.Fecha_Nacimiento,
                    Activo = h.Activo
                })
                .ToListAsync();

            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(nombre))
                huespedes = huespedes.Where(h => h.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrWhiteSpace(apellido))
                huespedes = huespedes.Where(h => h.Apellido.Contains(apellido, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrWhiteSpace(documento_identidad))
                huespedes = huespedes.Where(h => h.Documento_Identidad.Contains(documento_identidad, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrWhiteSpace(telefono))
                huespedes = huespedes.Where(h => h.Telefono != null && h.Telefono.Contains(telefono, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (activo.HasValue)
                huespedes = huespedes.Where(h => h.Activo == activo.Value).ToList();

            return Ok(huespedes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HuespedDto>> GetById(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return BadRequest("ID inválido.");

            var bytes = guid.ToByteArray();
            var huesped = await _context.Huespedes
                .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(bytes));

            if (huesped == null)
                return NotFound();

            return Ok(new HuespedDto
            {
                ID = GuidToString(huesped.ID),
                Nombre = huesped.Nombre,
                Apellido = huesped.Apellido,
                Segundo_Apellido = huesped.Segundo_Apellido,
                Documento_Identidad = huesped.Documento_Identidad,
                Telefono = huesped.Telefono,
                Fecha_Nacimiento = huesped.Fecha_Nacimiento,
                Activo = huesped.Activo
            });
        }

        [HttpPost]
        public async Task<ActionResult<HuespedDto>> Create([FromBody] HuespedCreateDto dto)
        {
            _logger.LogInformation("POST /api/Huesped - Creando nuevo huésped");
            _logger.LogInformation("Datos recibidos: Nombre={Nombre}, Apellido={Apellido}, Documento={Documento}", 
                dto.Nombre, dto.Apellido, dto.Documento_Identidad);

            await _validator.ValidateCreateAsync(dto);

            DateTime? fechaNacimiento = null;
            if (!string.IsNullOrWhiteSpace(dto.Fecha_Nacimiento))
            {
                if (!DateTime.TryParse(dto.Fecha_Nacimiento, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    return BadRequest("Fecha_Nacimiento tiene un formato inválido.");
                fechaNacimiento = parsedDate;
            }

            var huesped = new HotelManagement.Models.Huesped
            {
                ID = Guid.NewGuid().ToByteArray(),
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Segundo_Apellido = dto.Segundo_Apellido,
                Documento_Identidad = dto.Documento_Identidad,
                Telefono = dto.Telefono,
                Fecha_Nacimiento = fechaNacimiento,
                Activo = true,
                Fecha_Creacion = DateTime.Now,
                Usuario_Creacion_ID = null
            };

            _context.Huespedes.Add(huesped);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Huésped creado exitosamente con ID: {ID}", GuidToString(huesped.ID));

            var result = new HuespedDto
            {
                ID = GuidToString(huesped.ID),
                Nombre = huesped.Nombre,
                Apellido = huesped.Apellido,
                Segundo_Apellido = huesped.Segundo_Apellido,
                Documento_Identidad = huesped.Documento_Identidad,
                Telefono = huesped.Telefono,
                Fecha_Nacimiento = huesped.Fecha_Nacimiento,
                Activo = huesped.Activo
            };

            return CreatedAtAction(nameof(GetById), new { id = result.ID }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<HuespedDto>> Update(string id, [FromBody] HuespedUpdateDto dto)
        {
            _logger.LogInformation("🟡 PUT /api/Huesped/{ID} - Actualizando huésped", id);

            await _validator.ValidateUpdateAsync(id, dto);

            if (!Guid.TryParse(id, out var guid))
                return BadRequest("ID inválido.");

            var bytes = guid.ToByteArray();
            var huesped = await _context.Huespedes
                .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(bytes));

            if (huesped == null)
                return NotFound();

            if (dto.Nombre != null) huesped.Nombre = dto.Nombre;
            if (dto.Apellido != null) huesped.Apellido = dto.Apellido;
            huesped.Segundo_Apellido = dto.Segundo_Apellido;
            if (dto.Documento_Identidad != null) huesped.Documento_Identidad = dto.Documento_Identidad;
            huesped.Telefono = dto.Telefono;

            if (!string.IsNullOrWhiteSpace(dto.Fecha_Nacimiento))
            {
                if (!DateTime.TryParse(dto.Fecha_Nacimiento, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    return BadRequest("Fecha_Nacimiento tiene un formato inválido.");
                huesped.Fecha_Nacimiento = parsedDate;
            }
            else
            {
                huesped.Fecha_Nacimiento = null;
            }
            
            if (dto.Activo.HasValue) huesped.Activo = dto.Activo.Value;
            huesped.Fecha_Actualizacion = DateTime.Now;
            huesped.Usuario_Actualizacion_ID = null;

            await _context.SaveChangesAsync();

            return Ok(new HuespedDto
            {
                ID = GuidToString(huesped.ID),
                Nombre = huesped.Nombre,
                Apellido = huesped.Apellido,
                Segundo_Apellido = huesped.Segundo_Apellido,
                Documento_Identidad = huesped.Documento_Identidad,
                Telefono = huesped.Telefono,
                Fecha_Nacimiento = huesped.Fecha_Nacimiento,
                Activo = huesped.Activo
            });
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<HuespedDto>> PartialUpdate(string id, [FromBody] HuespedUpdateDto dto)
        {
            _logger.LogInformation("PATCH /api/Huesped/{ID} - Actualización parcial", id);

            await _validator.ValidateUpdateAsync(id, dto);

            if (!Guid.TryParse(id, out var guid))
                return BadRequest("ID inválido.");

            var bytes = guid.ToByteArray();
            var huesped = await _context.Huespedes
                .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(bytes));

            if (huesped == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Nombre))
                huesped.Nombre = dto.Nombre;
            
            if (!string.IsNullOrWhiteSpace(dto.Apellido))
                huesped.Apellido = dto.Apellido;
            
            if (dto.Segundo_Apellido != null)
                huesped.Segundo_Apellido = dto.Segundo_Apellido;
            
            if (!string.IsNullOrWhiteSpace(dto.Documento_Identidad))
                huesped.Documento_Identidad = dto.Documento_Identidad;
            
            if (dto.Telefono != null)
                huesped.Telefono = dto.Telefono;

            if (!string.IsNullOrWhiteSpace(dto.Fecha_Nacimiento))
            {
                if (!DateTime.TryParse(dto.Fecha_Nacimiento, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    return BadRequest("Fecha_Nacimiento tiene un formato inválido.");
                huesped.Fecha_Nacimiento = parsedDate;
            }
            
            if (dto.Activo.HasValue)
                huesped.Activo = dto.Activo.Value;
            
            huesped.Fecha_Actualizacion = DateTime.Now;
            huesped.Usuario_Actualizacion_ID = null;

            await _context.SaveChangesAsync();

            return Ok(new HuespedDto
            {
                ID = GuidToString(huesped.ID),
                Nombre = huesped.Nombre,
                Apellido = huesped.Apellido,
                Segundo_Apellido = huesped.Segundo_Apellido,
                Documento_Identidad = huesped.Documento_Identidad,
                Telefono = huesped.Telefono,
                Fecha_Nacimiento = huesped.Fecha_Nacimiento,
                Activo = huesped.Activo
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation("DELETE /api/Huesped/{ID} - Eliminando huésped", id);

            await _validator.ValidateDeleteAsync(id);

            if (!Guid.TryParse(id, out var guid))
                return BadRequest("ID inválido.");

            var bytes = guid.ToByteArray();
            var huesped = await _context.Huespedes
                .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(bytes));

            if (huesped == null)
                return NotFound();

            _context.Huespedes.Remove(huesped);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string GuidToString(byte[] bytes) => new Guid(bytes).ToString();
    }
}
