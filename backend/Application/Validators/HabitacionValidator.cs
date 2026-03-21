using HotelManagement.DTOs;
using HotelManagement.Aplicacion.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HotelManagement.Aplicacion.Validators
{
    public interface IHabitacionValidator
    {
        Task ValidateCreateAsync(HabitacionCreateDTO dto);
        Task ValidateUpdateAsync(string id, HabitacionCreateDTO dto);
        Task ValidatePartialUpdateAsync(string id, HabitacionUpdateDTO dto);
        Task ValidateDeleteAsync(string id);
    }

    public class HabitacionValidator : IHabitacionValidator
    {
        private readonly Datos.Config.HotelDbContext _context;

        public HabitacionValidator(Datos.Config.HotelDbContext context)
        {
            _context = context;
        }

        public async Task ValidateCreateAsync(HabitacionCreateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            // Validar Número de Habitación
            if (string.IsNullOrWhiteSpace(dto.Numero_Habitacion))
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación es obligatorio" };
            }
            else if (dto.Numero_Habitacion.Length > 10)
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación no puede exceder 10 caracteres" };
            }
            else if (!Regex.IsMatch(dto.Numero_Habitacion, @"^[0-9A-Za-z\-]+$", RegexOptions.None, timeout))
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación debe contener solo números, letras y guiones" };
            }
            else
            {
                // Verificar si el número ya existe
                var numeroExists = await _context.Habitaciones.AnyAsync(h => h.Numero_Habitacion == dto.Numero_Habitacion);
                if (numeroExists)
                {
                    errors["numero_Habitacion"] = new List<string> { $"Ya existe una habitación con el número de habitación: {dto.Numero_Habitacion}" };
                }
            }

            // Validar Piso
            if (dto.Piso < 0)
            {
                errors["piso"] = new List<string> { "El Piso no puede ser negativo" };
            }
            else if (dto.Piso > 100)
            {
                errors["piso"] = new List<string> { "El Piso no puede ser mayor a 100" };
            }

            // Validar Estado de Habitación
            var estadosValidos = new[] { "Libre", "Disponible", "Reservada", "Ocupada", "Fuera de Servicio", "Mantenimiento" };
            if (string.IsNullOrWhiteSpace(dto.Estado_Habitacion))
            {
                errors["estado_Habitacion"] = new List<string> { "El Estado de Habitación es obligatorio" };
            }
            else if (!estadosValidos.Contains(dto.Estado_Habitacion))
            {
                errors["estado_Habitacion"] = new List<string> { $"El Estado de Habitación debe ser uno de: {string.Join(", ", estadosValidos)}" };
            }

            // Validar Tipo de Habitación
            if (string.IsNullOrWhiteSpace(dto.Tipo_Habitacion_ID))
            {
                errors["tipo_Habitacion_ID"] = new List<string> { "El Tipo de Habitación es obligatorio" };
            }
            else if (!Guid.TryParse(dto.Tipo_Habitacion_ID, out var tipoGuid))
            {
                errors["tipo_Habitacion_ID"] = new List<string> { "El Tipo de Habitación ID debe ser un UUID válido" };
            }
            else
            {
                var tipoBytes = tipoGuid.ToByteArray();
                var tipoExists = await _context.TipoHabitaciones.AnyAsync(t => t.ID.SequenceEqual(tipoBytes));
                if (!tipoExists)
                {
                    errors["tipo_Habitacion_ID"] = new List<string> { "El Tipo de Habitación especificado no existe" };
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidateUpdateAsync(string id, HabitacionCreateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            // Validar ID
            if (!Guid.TryParse(id, out var guid))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = guid.ToByteArray();
            var habitacion = await _context.Habitaciones.FirstOrDefaultAsync(h => h.ID.SequenceEqual(guidBytes));

            if (habitacion == null)
                throw new NotFoundException($"No se encontró la habitación con ID: {id}", "id");

            // Validar Número de Habitación
            if (string.IsNullOrWhiteSpace(dto.Numero_Habitacion))
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación es obligatorio" };
            }
            else if (dto.Numero_Habitacion.Length > 10)
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación no puede exceder 10 caracteres" };
            }
            else if (!Regex.IsMatch(dto.Numero_Habitacion, @"^[0-9A-Za-z\-]+$", RegexOptions.None, timeout))
            {
                errors["numero_Habitacion"] = new List<string> { "El Número de Habitación debe contener solo números, letras y guiones" };
            }
            else
            {
                var numeroExists = await _context.Habitaciones
                    .AnyAsync(h => h.Numero_Habitacion == dto.Numero_Habitacion && !h.ID.SequenceEqual(guidBytes));
                if (numeroExists)
                {
                    errors["numero_Habitacion"] = new List<string> { $"Ya existe otra habitación con el número de habitación: {dto.Numero_Habitacion}" };
                }
            }

            // Validar Piso
            if (dto.Piso < 0 || dto.Piso > 100)
            {
                errors["piso"] = new List<string> { "El Piso debe estar entre 0 y 100" };
            }

            // Validar Estado de Habitación
            var estadosValidos = new[] { "Libre", "Disponible", "Reservada", "Ocupada", "Fuera de Servicio", "Mantenimiento" };
            if (!estadosValidos.Contains(dto.Estado_Habitacion))
            {
                errors["estado_Habitacion"] = new List<string> { $"El Estado de Habitación debe ser uno de: {string.Join(", ", estadosValidos)}" };
            }

            // Validar Tipo de Habitación
            if (!Guid.TryParse(dto.Tipo_Habitacion_ID, out var tipoGuid))
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

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidatePartialUpdateAsync(string id, HabitacionUpdateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            // Validar ID
            if (!Guid.TryParse(id, out var guid))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = guid.ToByteArray();
            var habitacion = await _context.Habitaciones.FirstOrDefaultAsync(h => h.ID.SequenceEqual(guidBytes));

            if (habitacion == null)
                throw new NotFoundException($"No se encontró la habitación con ID: {id}", "id");

            // Validar Número de Habitación (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Numero_Habitacion))
            {
                if (dto.Numero_Habitacion.Length > 10)
                {
                    errors["numero_Habitacion"] = new List<string> { "El Número de Habitación no puede exceder 10 caracteres" };
                }
                else if (!Regex.IsMatch(dto.Numero_Habitacion, @"^[0-9A-Za-z\-]+$", RegexOptions.None, timeout))
                {
                    errors["numero_Habitacion"] = new List<string> { "El Número de Habitación debe contener solo números, letras y guiones" };
                }
                else
                {
                    var numeroExists = await _context.Habitaciones
                        .AnyAsync(h => h.Numero_Habitacion == dto.Numero_Habitacion && !h.ID.SequenceEqual(guidBytes));
                    if (numeroExists)
                    {
                        errors["numero_Habitacion"] = new List<string> { $"Ya existe otra habitación con el número de habitación: {dto.Numero_Habitacion}" };
                    }
                }
            }

            // Validar Piso (si se proporciona)
            if (dto.Piso.HasValue)
            {
                if (dto.Piso.Value < 0 || dto.Piso.Value > 100)
                {
                    errors["piso"] = new List<string> { "El Piso debe estar entre 0 y 100" };
                }
            }

            // Validar Estado de Habitación (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Estado_Habitacion))
            {
                var estadosValidos = new[] { "Libre", "Disponible", "Reservada", "Ocupada", "Fuera de Servicio", "Mantenimiento" };
                if (!estadosValidos.Contains(dto.Estado_Habitacion))
                {
                    errors["estado_Habitacion"] = new List<string> { $"El Estado de Habitación debe ser uno de: {string.Join(", ", estadosValidos)}" };
                }
            }

            // Validar Tipo de Habitación (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Tipo_Habitacion_ID))
            {
                if (!Guid.TryParse(dto.Tipo_Habitacion_ID, out var tipoGuid))
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

            if (errors.Any())
                throw new ValidationException(errors);
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
