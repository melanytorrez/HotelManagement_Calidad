using HotelManagement.DTOs;
using HotelManagement.Aplicacion.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Aplicacion.Validators
{
    public interface IDetalleReservaValidator
    {
        Task ValidateCreateAsync(DetalleReservaCreateDTO dto);
        Task ValidateUpdateAsync(string id, DetalleReservaUpdateDTO dto);
        Task ValidateDeleteAsync(string id);
    }

    public class DetalleReservaValidator : IDetalleReservaValidator
    {
        private readonly Datos.Config.HotelDbContext _context;

        private const string HabitacionIdField = "habitacion_ID";
        private const string HuespedIdField = "huesped_ID";
        public DetalleReservaValidator(Datos.Config.HotelDbContext context)
        {
            _context = context;
        }

        public async Task ValidateCreateAsync(DetalleReservaCreateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            if (!IsValidUuid(dto.Reserva_ID))
                errors["reserva_ID"] = new List<string> { "Reserva_ID debe ser un UUID válido" };
            if (!IsValidUuid(dto.Habitacion_ID))
                errors["HabitacionIdField"] = new List<string> { "Habitacion_ID debe ser un UUID válido" };
            if (!IsValidUuid(dto.Huesped_ID))
                errors["HuespedIdField"] = new List<string> { "Huesped_ID debe ser un UUID válido" };

            if (dto.Fecha_Entrada < DateTime.Today.AddDays(-1)) // Permitir reservas de hoy
                errors["fecha_Entrada"] = new List<string> { "Fecha_Entrada no puede ser muy anterior a hoy" };
            
            if (dto.Fecha_Salida <= dto.Fecha_Entrada)
                errors["fecha_Salida"] = new List<string> { "Fecha_Salida debe ser posterior a Fecha_Entrada" };

            if (IsValidUuid(dto.Reserva_ID))
            {
                var reservaExists = await _context.Reservas
                    .AnyAsync(r => r.ID == ConvertToGuid(dto.Reserva_ID));
                if (!reservaExists)
                    errors["reserva_ID"] = new List<string> { $"No existe una reserva con ID: {dto.Reserva_ID}" };
            }

            if (IsValidUuid(dto.Habitacion_ID))
            {
                var habitacionExists = await _context.Habitaciones
                    .AnyAsync(h => h.ID == ConvertToGuid(dto.Habitacion_ID));
                if (!habitacionExists)
                    errors["HabitacionIdField"] = new List<string> { $"No existe una habitación con ID: {dto.Habitacion_ID}" };
            }

            if (IsValidUuid(dto.Huesped_ID))
            {
                var huespedExists = await _context.Huespedes
                    .AnyAsync(h => h.ID == ConvertToGuid(dto.Huesped_ID));
                if (!huespedExists)
                    errors["HuespedIdField"] = new List<string> { $"No existe un huésped con ID: {dto.Huesped_ID}" };
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidateUpdateAsync(string id, DetalleReservaUpdateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            if (!IsValidUuid(id))
            {
                errors["id"] = new List<string> { "ID debe ser un UUID válido" };
            }
                

            ValidateDates(dto, errors);

            if (!string.IsNullOrEmpty(dto.Habitacion_ID))
            {
               await ValidateHabitacionExistenceAsync(dto.Habitacion_ID, errors);
            }

            if (!string.IsNullOrEmpty(dto.Huesped_ID))
            {
                await ValidateHuespedExistenceAsync(dto.Huesped_ID, errors);
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidateDeleteAsync(string id)
        {
            if (!IsValidUuid(id))
                throw new BadRequestException("ID debe ser un UUID válido", "id");

            var exists = await _context.DetalleReservas
                .AnyAsync(d => d.ID == ConvertToGuid(id));
            
            if (!exists)
                throw new NotFoundException($"No se encontró el detalle de reserva con ID: {id}", "id");
        }

        private bool IsValidUuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        private byte[] ConvertToGuid(string uuid)
        {
            Guid guid = Guid.Parse(uuid);
            byte[] bytes = guid.ToByteArray();

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 0, 4);
                Array.Reverse(bytes, 4, 2);
                Array.Reverse(bytes, 6, 2);
            }
        return bytes;
        }
        
        private async Task ValidateHabitacionExistenceAsync(string habitacionId, Dictionary<string, List<string>> errors)   
        {
            if (!IsValidUuid(habitacionId))
            {
                errors["HabitacionIdField"] = new List<string> { "Habitacion_ID debe ser un UUID válido" };
                return; 
            }

            var habitacionExists = await _context.Habitaciones
                .AnyAsync(h => h.ID == ConvertToGuid(habitacionId));

            if (!habitacionExists)
            {
                errors["HabitacionIdField"] = new List<string> { $"No existe una habitación con ID: {habitacionId}" };
            }
        }
        private static void ValidateDates(DetalleReservaUpdateDTO dto, Dictionary<string, List<string>> errors)
        {
            var entrada = dto.Fecha_Entrada;
            var salida = dto.Fecha_Salida;

            if (entrada.HasValue && entrada.Value < DateTime.Today)
            {
                errors["fecha_Entrada"] = new List<string> { "Fecha_Entrada no puede ser anterior a hoy" };
            }

            if (entrada.HasValue && salida.HasValue && salida.Value <= entrada.Value)
            {
                errors["fecha_Salida"] = new List<string> { "Fecha_Salida debe ser posterior a Fecha_Entrada" };
            }
        }

        private async Task ValidateHuespedExistenceAsync(string huespedId, Dictionary<string, List<string>> errors)
        {
            if (!IsValidUuid(huespedId))
            {
                errors["HuespedIdField"] = new List<string> { "Huesped_ID debe ser un UUID válido" };
                return; 
            }
            var huespedExists = await _context.Huespedes
                .AnyAsync(h => h.ID == ConvertToGuid(huespedId));

            if (!huespedExists)
            {
                errors["HuespedIdField"] = new List<string> { $"No existe un huésped con ID: {huespedId}" };
            }
        }
    }
}
