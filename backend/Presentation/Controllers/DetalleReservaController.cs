using Microsoft.AspNetCore.Mvc;
using HotelManagement.DTOs;
using HotelManagement.Services;
using HotelManagement.Models;
using HotelManagement.Datos.Config;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Aplicacion.Exceptions;

namespace HotelManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DetalleReservaController : ControllerBase
    {
        private readonly IDetalleReservaService _service;
        private readonly ILogger<DetalleReservaController> _logger;
        private readonly HotelDbContext _context;

        public DetalleReservaController(
            IDetalleReservaService service,
            ILogger<DetalleReservaController> logger,
            HotelDbContext context)
        {
            _service = service;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DetalleReservaDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DetalleReservaDto>>> GetAll(
            [FromQuery] string? reserva_id = null,
            [FromQuery] string? habitacion_id = null,
            [FromQuery] string? huesped_id = null,
            [FromQuery] DateTime? fecha_entrada = null,
            [FromQuery] DateTime? fecha_salida = null)
        {
            _logger.LogInformation("Obteniendo todos los detalles de reservas");
            var detalles = await _service.GetAllAsync();
            
            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(reserva_id))
                detalles = detalles.Where(d => d.Reserva_ID.Equals(reserva_id, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrWhiteSpace(habitacion_id))
                detalles = detalles.Where(d => d.Habitacion_ID.Equals(habitacion_id, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrWhiteSpace(huesped_id))
                detalles = detalles.Where(d => d.Huesped_ID.Equals(huesped_id, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (fecha_entrada.HasValue)
                detalles = detalles.Where(d => d.Fecha_Entrada >= fecha_entrada.Value).ToList();
            
            if (fecha_salida.HasValue)
                detalles = detalles.Where(d => d.Fecha_Salida <= fecha_salida.Value).ToList();
            
            return Ok(detalles);
        }

        [HttpGet("reserva/{reservaId}")]
        [ProducesResponseType(typeof(List<DetalleReservaDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DetalleReservaDto>>> GetByReservaId(string reservaId)
        {
            _logger.LogInformation("Obteniendo detalles de reserva: {ReservaId}", reservaId);
            var detalles = await _service.GetByReservaIdAsync(reservaId);
            return Ok(detalles);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DetalleReservaDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DetalleReservaDto>> Create([FromBody] DetalleReservaCreateDto dto)
        {
            _logger.LogInformation("Creando nuevo detalle de reserva para habitación: {HabitacionId}", dto.Habitacion_ID);
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var created = await _service.CreateAsync(dto);
                
                // Actualizar estado de la habitación a "Reservada"
                if (Guid.TryParse(dto.Habitacion_ID, out var habitacionGuid))
                {
                    var habitacionBytes = habitacionGuid.ToByteArray();
                    var habitacion = await _context.Habitaciones
                        .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(habitacionBytes));
                    
                    if (habitacion != null)
                    {
                        _logger.LogInformation("Actualizando habitación {NumeroHabitacion} a estado Reservada", habitacion.Numero_Habitacion);
                        habitacion.Estado_Habitacion = "Reservada";
                        habitacion.Fecha_Actualizacion = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogWarning("No se encontró la habitación con ID: {HabitacionId}", dto.Habitacion_ID);
                    }
                }
                
                await transaction.CommitAsync();
                _logger.LogInformation("Detalle de reserva creado exitosamente: {DetalleId}", created.ID);
                return Created($"/api/DetalleReserva/{created.ID}", created);
            }
            catch (Exception ex) when (ex is ValidationException or NotFoundException or BadRequestException or ConflictException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al crear detalle de reserva");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocurrió un error interno al crear el detalle de reserva.");
            }
        }

        /// <summary>
        /// Crea múltiples detalles de reserva (múltiples habitaciones con múltiples huéspedes)
        /// </summary>
        [HttpPost("multiple")]
        [ProducesResponseType(typeof(List<DetalleReservaDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<DetalleReservaDto>>> CreateMultiple([FromBody] DetalleReservaMultipleCreateDto dto)
        {
            _logger.LogInformation("Creando múltiples detalles para reserva: {ReservaId}", dto.Reserva_ID);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var detallesCreados = new List<DetalleReservaDto>();
                var habitacionesIds = new HashSet<string>();

                foreach (var habitacion in dto.Habitaciones)
                {
                    habitacionesIds.Add(habitacion.Habitacion_ID);
                    
                    foreach (var huespedId in habitacion.Huesped_IDs)
                    {
                        var detalleDto = new DetalleReservaCreateDto
                        {
                            Reserva_ID = dto.Reserva_ID,
                            Habitacion_ID = habitacion.Habitacion_ID,
                            Huesped_ID = huespedId,
                            Fecha_Entrada = habitacion.Fecha_Entrada,
                            Fecha_Salida = habitacion.Fecha_Salida
                        };

                        var creado = await _service.CreateAsync(detalleDto);
                        detallesCreados.Add(creado);
                    }
                }

                // Actualizar estado de todas las habitaciones a "Reservada"
                foreach (var habitacionId in habitacionesIds)
                {
                    if (Guid.TryParse(habitacionId, out var habitacionGuid))
                    {
                        var habitacionBytes = habitacionGuid.ToByteArray();
                        var habitacion = await _context.Habitaciones
                            .FirstOrDefaultAsync(h => h.ID != null && h.ID.SequenceEqual(habitacionBytes));
                        
                        if (habitacion != null)
                        {
                            _logger.LogInformation("Actualizando habitación {NumeroHabitacion} a estado Reservada", habitacion.Numero_Habitacion);
                            habitacion.Estado_Habitacion = "Reservada";
                            habitacion.Fecha_Actualizacion = DateTime.Now;
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Created($"/api/DetalleReserva/reserva/{dto.Reserva_ID}", detallesCreados);
            }
            catch (Exception ex) when (ex is ValidationException or NotFoundException or BadRequestException or ConflictException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al crear múltiples detalles de reserva");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocurrió un error interno al crear los detalles de reserva.");
            }
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(DetalleReservaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DetalleReservaDto>> PartialUpdate(string id, [FromBody] DetalleReservaUpdateDto dto)
        {
            _logger.LogInformation("Actualizando parcialmente detalle de reserva con ID: {Id}", id);
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }
    }
}