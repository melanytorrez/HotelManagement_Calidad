using HotelManagement.Datos.Config;
using HotelManagement.DTOs;
using HotelManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Presentacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HabitacionController : ControllerBase
    {
        private readonly HotelDbContext _context;

        public HabitacionController(HotelDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? numero_habitacion = null,
            [FromQuery] short? piso = null,
            [FromQuery] string? estado_habitacion = null,
            [FromQuery] string? tipo_habitacion_id = null)
        {
            var habitaciones = await _context.Habitaciones
                .Include(h => h.TipoHabitacion)
                .Select(h => new HabitacionDto
                {
                    ID = GuidToString(h.ID),
                    Numero_Habitacion = h.Numero_Habitacion,
                    Piso = h.Piso,
                    Estado_Habitacion = h.Estado_Habitacion,
                    Tipo_Habitacion_ID = h.TipoHabitacion != null ? GuidToString(h.Tipo_Habitacion_ID) : null,
                    Tipo_Nombre = h.TipoHabitacion != null ? h.TipoHabitacion.Nombre : null,
                    Capacidad_Maxima = h.TipoHabitacion != null ? h.TipoHabitacion.Capacidad_Maxima : (byte?)null,
                    Tarifa_Base = h.TipoHabitacion != null ? h.TipoHabitacion.Precio_Base : (decimal?)null
                })
                .ToListAsync();

            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(numero_habitacion))
                habitaciones = habitaciones.Where(h => h.Numero_Habitacion.Contains(numero_habitacion, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (piso.HasValue)
                habitaciones = habitaciones.Where(h => h.Piso == piso.Value).ToList();
            
            if (!string.IsNullOrWhiteSpace(estado_habitacion))
                habitaciones = habitaciones.Where(h => h.Estado_Habitacion.Equals(estado_habitacion, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrWhiteSpace(tipo_habitacion_id))
                habitaciones = habitaciones.Where(h => h.Tipo_Habitacion_ID != null && h.Tipo_Habitacion_ID.Equals(tipo_habitacion_id, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(habitaciones);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return BadRequest("ID inválido.");

            var bytes = guid.ToByteArray();
            var habitacion = await _context.Habitaciones
                .Include(h => h.TipoHabitacion)
                .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(bytes));

            if (habitacion == null)
                return NotFound();

            var result = new HabitacionDto
            {
                ID = GuidToString(habitacion.ID),
                Numero_Habitacion = habitacion.Numero_Habitacion,
                Piso = habitacion.Piso,
                Estado_Habitacion = habitacion.Estado_Habitacion,
                Tipo_Habitacion_ID = habitacion.TipoHabitacion != null ? GuidToString(habitacion.Tipo_Habitacion_ID) : null,
                Tipo_Nombre = habitacion.TipoHabitacion?.Nombre,
                Capacidad_Maxima = habitacion.TipoHabitacion?.Capacidad_Maxima,
                Tarifa_Base = habitacion.TipoHabitacion?.Precio_Base
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HabitacionCreateDto dto)
        {
            if (!Guid.TryParse(dto.Tipo_Habitacion_ID, out var tipoGuid))
                return BadRequest("Tipo_Habitacion_ID inválido.");

            var tipoBytes = tipoGuid.ToByteArray();
            var tipoExists = await _context.TipoHabitaciones
                .AnyAsync(t => t.ID != null && t.ID.SequenceEqual(tipoBytes));

            if (!tipoExists)
                return BadRequest("El tipo de habitación especificado no existe.");

            var habitacion = new Habitacion
            {
                ID = Guid.NewGuid().ToByteArray(),
                Tipo_Habitacion_ID = tipoBytes,
                Numero_Habitacion = dto.Numero_Habitacion,
                Piso = dto.Piso!.Value,
                Estado_Habitacion = dto.Estado_Habitacion,
                Fecha_Creacion = DateTime.Now,
                Usuario_Creacion_ID = null
            };

            _context.Habitaciones.Add(habitacion);
            await _context.SaveChangesAsync();

            await _context.Entry(habitacion).Reference(h => h.TipoHabitacion).LoadAsync();

            var result = new HabitacionDto
            {
                ID = GuidToString(habitacion.ID),
                Numero_Habitacion = habitacion.Numero_Habitacion,
                Piso = habitacion.Piso,
                Estado_Habitacion = habitacion.Estado_Habitacion,
                Tipo_Habitacion_ID = GuidToString(habitacion.Tipo_Habitacion_ID),
                Tipo_Nombre = habitacion.TipoHabitacion?.Nombre,
                Capacidad_Maxima = habitacion.TipoHabitacion?.Capacidad_Maxima,
                Tarifa_Base = habitacion.TipoHabitacion?.Precio_Base
            };

            return CreatedAtAction(nameof(GetById), new { id = result.ID }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] HabitacionCreateDto dto)
        {
            if (!Guid.TryParse(id, out var guid))
                return BadRequest("ID inválido.");

            var bytes = guid.ToByteArray();
            var habitacion = await _context.Habitaciones
                .Include(h => h.TipoHabitacion)
                .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(bytes));

            if (habitacion == null)
                return NotFound();

            if (!Guid.TryParse(dto.Tipo_Habitacion_ID, out var tipoGuid))
                return BadRequest("Tipo_Habitacion_ID inválido.");

            var tipoBytes = tipoGuid.ToByteArray();
            var tipoExists = await _context.TipoHabitaciones
                .AnyAsync(t => t.ID != null && t.ID.SequenceEqual(tipoBytes));

            if (!tipoExists)
                return BadRequest("El tipo de habitación especificado no existe.");

            habitacion.Tipo_Habitacion_ID = tipoBytes;
            habitacion.Numero_Habitacion = dto.Numero_Habitacion;
            habitacion.Piso = dto.Piso!.Value;
            habitacion.Estado_Habitacion = dto.Estado_Habitacion;
            habitacion.Fecha_Actualizacion = DateTime.Now;
            habitacion.Usuario_Actualizacion_ID = null;

            await _context.SaveChangesAsync();
            await _context.Entry(habitacion).Reference(h => h.TipoHabitacion).LoadAsync();

            var result = new HabitacionDto
            {
                ID = GuidToString(habitacion.ID),
                Numero_Habitacion = habitacion.Numero_Habitacion,
                Piso = habitacion.Piso,
                Estado_Habitacion = habitacion.Estado_Habitacion,
                Tipo_Habitacion_ID = GuidToString(habitacion.Tipo_Habitacion_ID),
                Tipo_Nombre = habitacion.TipoHabitacion?.Nombre,
                Capacidad_Maxima = habitacion.TipoHabitacion?.Capacidad_Maxima,
                Tarifa_Base = habitacion.TipoHabitacion?.Precio_Base
            };

            return Ok(result);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PartialUpdate(string id, [FromBody] HabitacionUpdateDto dto)
        {
            if (!Guid.TryParse(id, out var guid))
                return BadRequest("ID inválido.");

            var bytes = guid.ToByteArray();
            var habitacion = await _context.Habitaciones
                .Include(h => h.TipoHabitacion)
                .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(bytes));

            if (habitacion == null)
                return NotFound();

            if (!string.IsNullOrEmpty(dto.Tipo_Habitacion_ID))
            {
                if (!Guid.TryParse(dto.Tipo_Habitacion_ID, out var tipoGuid))
                    return BadRequest("Tipo_Habitacion_ID inválido.");

                var tipoBytes = tipoGuid.ToByteArray();
                var tipoExists = await _context.TipoHabitaciones
                    .AnyAsync(t => t.ID != null && t.ID.SequenceEqual(tipoBytes));

                if (!tipoExists)
                    return BadRequest("El tipo de habitación especificado no existe.");

                habitacion.Tipo_Habitacion_ID = tipoBytes;
            }

            if (!string.IsNullOrEmpty(dto.Numero_Habitacion))
                habitacion.Numero_Habitacion = dto.Numero_Habitacion;

            if (dto.Piso.HasValue)
                habitacion.Piso = dto.Piso.Value;

            if (!string.IsNullOrEmpty(dto.Estado_Habitacion))
                habitacion.Estado_Habitacion = dto.Estado_Habitacion;

            habitacion.Fecha_Actualizacion = DateTime.Now;
            habitacion.Usuario_Actualizacion_ID = null;

            await _context.SaveChangesAsync();
            await _context.Entry(habitacion).Reference(h => h.TipoHabitacion).LoadAsync();

            var result = new HabitacionDto
            {
                ID = GuidToString(habitacion.ID),
                Numero_Habitacion = habitacion.Numero_Habitacion,
                Piso = habitacion.Piso,
                Estado_Habitacion = habitacion.Estado_Habitacion,
                Tipo_Habitacion_ID = GuidToString(habitacion.Tipo_Habitacion_ID),
                Tipo_Nombre = habitacion.TipoHabitacion?.Nombre,
                Capacidad_Maxima = habitacion.TipoHabitacion?.Capacidad_Maxima,
                Tarifa_Base = habitacion.TipoHabitacion?.Precio_Base
            };

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return BadRequest("ID inválido.");

            var bytes = guid.ToByteArray();
            var habitacion = await _context.Habitaciones
                .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(bytes));

            if (habitacion == null)
                return NotFound();

            _context.Habitaciones.Remove(habitacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string GuidToString(byte[] bytes) => new Guid(bytes).ToString();
    }
}
