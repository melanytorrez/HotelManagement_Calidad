using HotelManagement.DTOs;
using HotelManagement.Aplicacion.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HotelManagement.Aplicacion.Validators
{
    public interface IHuespedValidator
    {
        Task ValidateCreateAsync(HuespedCreateDTO dto);
        Task ValidateUpdateAsync(string id, HuespedUpdateDTO dto);
        Task ValidateDeleteAsync(string id);
    }

    public class HuespedValidator : IHuespedValidator
    {
        private readonly Datos.Config.HotelDbContext _context;

        public HuespedValidator(Datos.Config.HotelDbContext context)
        {
            _context = context;
        }

        public async Task ValidateCreateAsync(HuespedCreateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            ValidateText("nombre", dto.Nombre, "Nombre", errors);
            ValidateText("apellido", dto.Apellido, "Apellido", errors);
            ValidateText("segundo_Apellido", dto.Segundo_Apellido ?? "", "Segundo Apellido", errors, isOptional: true);

            await ValidateDocumentAsync(dto.Documento_Identidad, errors);
           
            ValidateTelefono(dto.Telefono, errors);

            // Validar Fecha de Nacimiento (opcional)
            if (!string.IsNullOrWhiteSpace(dto.Fecha_Nacimiento))
            {
                if (!DateTime.TryParse(dto.Fecha_Nacimiento, out var fechaNacimiento))
                {
                    errors["fecha_Nacimiento"] = new List<string> { "La Fecha de Nacimiento tiene un formato inválido. Use formato: YYYY-MM-DD" };
                }
                else
                {
                    var edad = DateTime.Now.Year - fechaNacimiento.Year;
                    if (fechaNacimiento > DateTime.Now.AddYears(-edad)) edad--;

                    if (edad < 0 || fechaNacimiento > DateTime.Now)
                    {
                        errors["fecha_Nacimiento"] = new List<string> { "La Fecha de Nacimiento no puede ser una fecha futura" };
                    }
                    else if (edad > 150)
                    {
                        errors["fecha_Nacimiento"] = new List<string> { "La Fecha de Nacimiento no es válida" };
                    }
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidateUpdateAsync(string id, HuespedUpdateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            // Validar ID
            if (!Guid.TryParse(id, out var guid))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = guid.ToByteArray();
            var huesped = await _context.Huespedes.FirstOrDefaultAsync(h => h.ID.SequenceEqual(guidBytes));

            if (huesped == null)
                throw new NotFoundException($"No se encontró el huésped con ID: {id}", "id");

            // Validar Nombre (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Nombre))
            {
                if (dto.Nombre.Length < 2 || dto.Nombre.Length > 30)
                {
                    errors["nombre"] = new List<string> { "El Nombre debe tener entre 2 y 30 caracteres" };
                }
                else if (!Regex.IsMatch(dto.Nombre, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                {
                    errors["nombre"] = new List<string> { "El Nombre debe contener solo letras" };
                }
            }

            // Validar Apellido (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Apellido))
            {
                if (dto.Apellido.Length < 2 || dto.Apellido.Length > 30)
                {
                    errors["apellido"] = new List<string> { "El Apellido debe tener entre 2 y 30 caracteres" };
                }
                else if (!Regex.IsMatch(dto.Apellido, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                {
                    errors["apellido"] = new List<string> { "El Apellido debe contener solo letras" };
                }
            }

            // Validar Segundo Apellido (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Segundo_Apellido))
            {
                if (dto.Segundo_Apellido.Length > 30)
                {
                    errors["segundo_Apellido"] = new List<string> { "El Segundo Apellido no puede exceder 30 caracteres" };
                }
                else if (!Regex.IsMatch(dto.Segundo_Apellido, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                {
                    errors["segundo_Apellido"] = new List<string> { "El Segundo Apellido debe contener solo letras" };
                }
            }

            // Validar Documento de Identidad (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Documento_Identidad))
            {
                if (dto.Documento_Identidad.Length < 5 || dto.Documento_Identidad.Length > 20)
                {
                    errors["documento_Identidad"] = new List<string> { "El Documento de Identidad debe tener entre 5 y 20 caracteres" };
                }
                else if (!Regex.IsMatch(dto.Documento_Identidad, @"^\d+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                {
                    errors["documento_Identidad"] = new List<string> { "El Documento de Identidad debe contener solo números" };
                }
                else
                {
                    var docExists = await _context.Huespedes
                        .AnyAsync(h => h.Documento_Identidad == dto.Documento_Identidad && !h.ID.SequenceEqual(guidBytes));
                    if (docExists)
                    {
                        errors["documento_Identidad"] = new List<string> { $"Ya existe otro huésped con el Documento de Identidad: {dto.Documento_Identidad}" };
                    }
                }
            }

            // Validar Teléfono (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Telefono))
            {
                if (dto.Telefono.Length < 7 || dto.Telefono.Length > 20)
                {
                    errors["telefono"] = new List<string> { "El Teléfono debe tener entre 7 y 20 caracteres" };
                }
                else if (!Regex.IsMatch(dto.Telefono, @"^[0-9+\-\s()]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                {
                    errors["telefono"] = new List<string> { "El Teléfono debe contener solo números y caracteres válidos" };
                }
            }

            // Validar Fecha de Nacimiento (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Fecha_Nacimiento))
            {
                if (!DateTime.TryParse(dto.Fecha_Nacimiento, out var fechaNacimiento))
                {
                    errors["fecha_Nacimiento"] = new List<string> { "La Fecha de Nacimiento tiene un formato inválido. Use formato: YYYY-MM-DD" };
                }
                else
                {
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

            if (errors.Any())
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

        private void ValidateText(string fieldName, string value, string label, Dictionary<string, List<string>> errors, bool isOptional = false)
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
                errors["documento_Identidad"] = new List<string> { "El Documento de Identidad es obligatorio" };
                return;
            }

            if (documento.Length < 5 || documento.Length > 20)
            {
                errors["documento_Identidad"] = new List<string> { "El Documento de Identidad debe tener entre 5 y 20 caracteres" };
            }

            if (!Regex.IsMatch(documento, @"^\d+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
            {
                errors["documento_Identidad"] = new List<string> { "El Documento de Identidad debe contener solo números" };
                return;
            }

            var docExists = await _context.Huespedes.AnyAsync(h => h.Documento_Identidad == documento);
            if (docExists)
            {
                errors["documento_Identidad"] = new List<string> { $"Ya existe un huésped con el Documento de Identidad: {documento}" };
            }
        }

        private void ValidateTelefono(string? telefono, Dictionary<string, List<string>> errors)
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
    }
}
