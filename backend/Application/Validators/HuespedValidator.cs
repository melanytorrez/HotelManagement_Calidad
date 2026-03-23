using HotelManagement.DTOs;
using HotelManagement.Aplicacion.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Globalization;

namespace HotelManagement.Aplicacion.Validators
{
    public interface IHuespedValidator
    {
        Task ValidateCreateAsync(HuespedCreateDto dto);
        Task ValidateUpdateAsync(string id, HuespedUpdateDto dto);
        Task ValidateDeleteAsync(string id);
    }

    public class HuespedValidator : IHuespedValidator
    {
        private readonly Datos.Config.HotelDbContext _context;

        private const string DocumentoIdentidadField = "documento_Identidad";
        public HuespedValidator(Datos.Config.HotelDbContext context)
        {
            _context = context;
        }

        public async Task ValidateCreateAsync(HuespedCreateDto dto)
        {
            var errors = new Dictionary<string, List<string>>();

            ValidateText("nombre", dto.Nombre, "Nombre", errors);
            ValidateText("apellido", dto.Apellido, "Apellido", errors);
            ValidateText("segundo_Apellido", dto.Segundo_Apellido ?? "", "Segundo Apellido", errors, isOptional: true);

            await ValidateDocumentAsync(dto.Documento_Identidad, errors);

            ValidatePhone(dto.Telefono, errors);

            ValidateBirthDate(dto.Fecha_Nacimiento, errors);

            if (errors.Count != 0)
                throw new ValidationException(errors);
        }

        public async Task ValidateUpdateAsync(string id, HuespedUpdateDto dto)
        {
            var errors = new Dictionary<string, List<string>>();

            if (!Guid.TryParse(id, out var guid))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = guid.ToByteArray();
            var huesped = await _context.Huespedes.FirstOrDefaultAsync(h => h.ID.SequenceEqual(guidBytes));

            if (huesped == null)
                throw new NotFoundException($"No se encontró el huésped con ID: {id}", "id");

            if (!string.IsNullOrEmpty(dto.Nombre))
                ValidateText("nombre", dto.Nombre, "Nombre", errors);

            if (!string.IsNullOrEmpty(dto.Apellido))
                ValidateText("apellido", dto.Apellido, "Apellido", errors);

            if (!string.IsNullOrEmpty(dto.Segundo_Apellido))
                ValidateText("segundo_Apellido", dto.Segundo_Apellido, "Segundo Apellido", errors, isOptional: true);

            if (!string.IsNullOrEmpty(dto.Documento_Identidad))
                await ValidateDocumentUpdateAsync(dto.Documento_Identidad, guidBytes, errors);

            ValidatePhone(dto.Telefono, errors);
            ValidateBirthDate(dto.Fecha_Nacimiento, errors);

            if (errors.Count != 0)
                throw new ValidationException(errors);
        }

        public async Task ValidateDeleteAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                throw new BadRequestException("El ID debe ser un UUID válido", "id");

            var guidBytes = guid.ToByteArray();
            var exists = await _context.Huespedes.AnyAsync(h => h.ID.SequenceEqual(guidBytes));

            if (!exists)
                throw new NotFoundException($"No se encontró el huésped con ID: {id}", "id");

            var hasDetalles = await _context.DetalleReservas
                .AnyAsync(d => d.Huesped_ID.SequenceEqual(guidBytes));

            if (hasDetalles)
                throw new ConflictException("No se puede eliminar el huésped porque tiene detalles de reserva asociados", "id");
        }

        private static void ValidateText(string fieldName, string value, string label, Dictionary<string, List<string>> errors, bool isOptional = false)
        {
            if (isOptional && string.IsNullOrWhiteSpace(value)) return;

            if (string.IsNullOrWhiteSpace(value))
            {
                errors[fieldName] = new List<string> { $"El {label} es obligatorio" };
                return;
            }

            if (value.Length < 2)
            {
                errors[fieldName] = new List<string> { $"El {label} debe tener al menos 2 caracteres" };
 
            }
    
            if (value.Length > 30)
            {                
                errors[fieldName] = new List<string> { $"El {label} no puede exceder 30 caracteres" };
            }

            if (!Regex.IsMatch(value, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
            {
                errors[fieldName] = new List<string> { $"El {label} debe contener solo letras" };
            }
        }

        private async Task ValidateDocumentAsync(string? documento, Dictionary<string, List<string>> errors)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                errors[DocumentoIdentidadField] = new List<string> { "El Documento de Identidad es obligatorio" };
                return;
            }

            if (documento.Length < 5 || documento.Length > 20)
            {
                errors[DocumentoIdentidadField] = new List<string> { "El Documento de Identidad debe tener entre 5 y 20 caracteres" };
            }

            if (!Regex.IsMatch(documento, @"^\d+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
            {
                errors[DocumentoIdentidadField] = new List<string> { "El Documento de Identidad debe contener solo números" };
                return;
            }

            var docExists = await _context.Huespedes.AnyAsync(h => h.Documento_Identidad == documento);
            if (docExists)
            {
                errors[DocumentoIdentidadField] = new List<string> { $"Ya existe un huésped con el Documento de Identidad: {documento}" };
            }
        }

        private async Task ValidateDocumentUpdateAsync(string documento, byte[] currentId, Dictionary<string, List<string>> errors)
        {
            if (documento.Length < 5 || documento.Length > 20)
                errors[DocumentoIdentidadField] = new List<string> { "El Documento de Identidad debe tener entre 5 y 20 caracteres" };

            if (!Regex.IsMatch(documento, @"^\d+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
            {
                errors[DocumentoIdentidadField] = new List<string> { "El Documento de Identidad debe contener solo números" };
                return;
            }

            var docExists = await _context.Huespedes
                .AnyAsync(h => h.Documento_Identidad == documento && !h.ID.SequenceEqual(currentId));

            if (docExists)
                errors[DocumentoIdentidadField] = new List<string> { $"Ya existe otro huésped con el Documento de Identidad: {documento}" };
        }

        private static void ValidatePhone(string? telefono, Dictionary<string, List<string>> errors)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return;

            if (telefono.Length < 7 || telefono.Length > 20)
            {
                errors["telefono"] = new List<string> { "El Teléfono debe tener entre 7 y 20 caracteres" };
            }
            else if (!Regex.IsMatch(telefono, @"^[0-9+\-\s()]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
            {
                errors["telefono"] = new List<string> { "El Teléfono debe contener solo números y caracteres válidos (+, -, espacios, paréntesis)" };
            }
        }

        private static void ValidateBirthDate(string? fechaNacimientoStr, Dictionary<string, List<string>> errors)
        {
            if (string.IsNullOrWhiteSpace(fechaNacimientoStr)) return;

            if (!DateTime.TryParse(fechaNacimientoStr,CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaNacimiento))
            {
                errors["fecha_Nacimiento"] = new List<string> { "La Fecha de Nacimiento tiene un formato inválido. Use formato: YYYY-MM-DD" };
                return;
            }

            var edad = DateTime.Now.Year - fechaNacimiento.Year;
            if (fechaNacimiento > DateTime.Now.AddYears(-edad)) edad--;

            if (fechaNacimiento > DateTime.Now)
            {
                errors["fecha_Nacimiento"] = new List<string> { "La Fecha de Nacimiento no puede ser una fecha futura" };
            }
            else if (edad > 150)
            {
                errors["fecha_Nacimiento"] = new List<string> { "La Fecha de Nacimiento no es válida" };
            }
        }
    }
}
