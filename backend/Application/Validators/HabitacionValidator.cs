using HotelManagement.DTOs;
using HotelManagement.Aplicacion.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HotelManagement.Aplicacion.Validators
{
    public interface IHabitacionValidator
    {
        Task ValidateCreateAsync(HabitacionCreateDto dto);
        Task ValidateUpdateAsync(string id, HabitacionCreateDto dto);
        Task ValidatePartialUpdateAsync(string id, HabitacionUpdateDto dto);
        Task ValidateDeleteAsync(string id);
    }

    public class HabitacionValidator : IHabitacionValidator
    {
        private readonly Datos.Config.HotelDbContext _context;

        public HabitacionValidator(Datos.Config.HotelDbContext context)
        {
            _context = context;
        }

        public async Task ValidateCreateAsync(HabitacionCreateDto dto)
        {
            var errors = new Dictionary<string, List<string>>();

            await ValidateNumeroHabitacionAsync(dto.Numero_Habitacion, errors);
            ValidatePiso(dto.Piso!.Value, errors);
            ValidateEstadoHabitacion(dto.Estado_Habitacion, errors);
            await ValidateTipoHabitacionAsync(dto.Tipo_Habitacion_ID, errors);

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidateUpdateAsync(string id, HabitacionCreateDto dto)
        {
            var errors = new Dictionary<string, List<string>>();

            if (!Guid.TryParse(id, out var guid))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = guid.ToByteArray();
            var habitacion = await _context.Habitaciones.FirstOrDefaultAsync(h => h.ID.SequenceEqual(guidBytes));

            if (habitacion == null)
                throw new NotFoundException($"No se encontró la habitación con ID: {id}", "id");

            await ValidateNumeroHabitacionAsync(dto.Numero_Habitacion, errors, guidBytes);
            ValidatePiso(dto.Piso!.Value, errors);
            ValidateEstadoHabitacion(dto.Estado_Habitacion, errors);
            await ValidateTipoHabitacionAsync(dto.Tipo_Habitacion_ID, errors);

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidatePartialUpdateAsync(string id, HabitacionUpdateDto dto)
        {
            var errors = new Dictionary<string, List<string>>();

            if (!Guid.TryParse(id, out var guid))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = guid.ToByteArray();
            var habitacion = await _context.Habitaciones.FirstOrDefaultAsync(h => h.ID.SequenceEqual(guidBytes));

            if (habitacion == null)
                throw new NotFoundException($"No se encontró la habitación con ID: {id}", "id");

            if (!string.IsNullOrEmpty(dto.Numero_Habitacion))
                await ValidateNumeroHabitacionAsync(dto.Numero_Habitacion, errors, guidBytes);

            if (dto.Piso.HasValue)
                ValidatePiso(dto.Piso.Value, errors);

            if (!string.IsNullOrEmpty(dto.Estado_Habitacion))
                ValidateEstadoHabitacion(dto.Estado_Habitacion, errors);

            if (!string.IsNullOrEmpty(dto.Tipo_Habitacion_ID))
                await ValidateTipoHabitacionAsync(dto.Tipo_Habitacion_ID, errors);

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidateNumeroHabitacionAsync(string? numero, Dictionary<string, List<string>> errors, byte[]? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(numero))
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación es obligatorio" };
                return;
            }

            if (numero.Length > 10)
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación no puede exceder 10 caracteres" };
            }
            else if (!Regex.IsMatch(numero, @"^[0-9A-Za-z\-]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación debe contener solo números, letras y guiones" };
            }
            else
            {
                var query = _context.Habitaciones.AsQueryable();
                if (currentId != null)
                {
                    query = query.Where(h => !h.ID.SequenceEqual(currentId));
                }

                var exists = await query.AnyAsync(h => h.Numero_Habitacion == numero);
                if (exists)
                {
                    errors["numero_Habitacion"] = new List<string> { $"Ya existe {(currentId == null ? "una" : "otra")} habitación con el número de habitación: {numero}" };
                }
            }
        }

        private static void ValidatePiso(int piso, Dictionary<string, List<string>> errors)
        {
            if (piso < 0 || piso > 100)
            {
                errors["piso"] = new List<string> { "El Piso debe estar entre 0 y 100" };
            }
        }

        private static void ValidateEstadoHabitacion(string? estado, Dictionary<string, List<string>> errors)
        {
            var estadosValidos = new[] { "Libre", "Disponible", "Reservada", "Ocupada", "Fuera de Servicio", "Mantenimiento" };
            if (string.IsNullOrWhiteSpace(estado))
            {
                errors["estado_Habitacion"] = new List<string> { "El Estado de Habitación es obligatorio" };
            }
            else if (!estadosValidos.Contains(estado))
            {
                errors["estado_Habitacion"] = new List<string> { $"El Estado de Habitación debe ser uno de: {string.Join(", ", estadosValidos)}" };
            }
        }

        private async Task ValidateTipoHabitacionAsync(string? tipoId, Dictionary<string, List<string>> errors)
        {
            if (string.IsNullOrWhiteSpace(tipoId))
            {
                errors["tipo_Habitacion_ID"] = new List<string> { "El Tipo de Habitación es obligatorio" };
            }
            else if (!Guid.TryParse(tipoId, out var tipoGuid))
            {
                errors["tipo_Habitacion_ID"] = new List<string> { "El Tipo de Habitación ID debe ser un UUID válido" };
            }
            else
            {
                var tipoBytes = tipoGuid.ToByteArray();
                var tipoExists = await _context.TipoHabitaciones.AnyAsync(t => t.ID.SequenceEqual(tipoBytes));
                if (!tipoExists)
                {
                    errors["tipo_Habitacion_ID"] = new List<string> { "El tipo de habitación especificado no existe" };
                }
            }
        }

        public async Task ValidateDeleteAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                throw new BadRequestException("El ID debe ser un UUID válido", "id");

            var guidBytes = guid.ToByteArray();
            var exists = await _context.Habitaciones.AnyAsync(h => h.ID.SequenceEqual(guidBytes));

            if (!exists)
                throw new NotFoundException($"No se encontró la habitación con ID: {id}", "id");

            var hasDetalles = await _context.DetalleReservas
                .AnyAsync(d => d.Habitacion_ID.SequenceEqual(guidBytes));

            if (hasDetalles)
                throw new ConflictException("No se puede eliminar la habitación porque tiene detalles de reserva asociados", "id");
        }
    }
}
