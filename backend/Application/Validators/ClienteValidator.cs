using HotelManagement.DTOs;
using HotelManagement.Aplicacion.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HotelManagement.Aplicacion.Validators
{
    public class ClienteValidator : IClienteValidator
    {
        private readonly Datos.Config.HotelDbContext _context;
        private static readonly TimeSpan timeout = TimeSpan.FromMilliseconds(100);

        public ClienteValidator(Datos.Config.HotelDbContext context)
        {
            _context = context;
        }

        public async Task ValidateCreateAsync(ClienteCreateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            // Validar Razón Social
            if (string.IsNullOrWhiteSpace(dto.Razon_Social))
            {
                errors["razon_Social"] = new List<string> { "La Razón Social es obligatoria" };
            }
            else if (dto.Razon_Social.Length < 3)
            {
                errors["razon_Social"] = new List<string> { "La Razón Social debe tener al menos 3 caracteres" };
            }
            else if (dto.Razon_Social.Length > 20)
            {
                errors["razon_Social"] = new List<string> { "La Razón Social no puede exceder 20 caracteres" };
            }
            
            // Validar NIT
            if (string.IsNullOrWhiteSpace(dto.NIT))
            {
                errors["nit"] = new List<string> { "El NIT es obligatorio" };
            }
            else if (dto.NIT.Length < 7)
            {
                errors["nit"] = new List<string> { "El NIT debe tener al menos 7 caracteres" };
            }
            else if (dto.NIT.Length > 20)
            {
                errors["nit"] = new List<string> { "El NIT no puede exceder 20 caracteres" };
            }
            else if (!Regex.IsMatch(dto.NIT, @"^[0-9]+$", RegexOptions.IgnoreCase, timeout))
            {
                errors["nit"] = new List<string> { "El NIT debe contener solo números" };
            }
            else
            {
                // Verificar si el NIT ya existe
                var nitExists = await _context.Clientes.AnyAsync(c => c.NIT == dto.NIT);
                if (nitExists)
                {
                    errors["nit"] = new List<string> { $"Ya existe un cliente con el NIT: {dto.NIT}" };
                }
            }

            // Validar Email
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                errors["email"] = new List<string> { "El Email es obligatorio" };
            }
            else if (!IsValidEmail(dto.Email))
            {
                errors["email"] = new List<string> { "El Email debe tener un formato válido (ejemplo@dominio.com)" };
            }
            else if (dto.Email.Length > 30)
            {
                errors["email"] = new List<string> { "El Email no puede exceder 30 caracteres" };
            }
            else
            {
                // Verificar si el email ya existe
                var emailExists = await _context.Clientes.AnyAsync(c => c.Email == dto.Email);
                if (emailExists)
                {
                    errors["email"] = new List<string> { $"Ya existe un cliente con el Email: {dto.Email}" };
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidateUpdateAsync(string id, ClienteUpdateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            // Validar ID
            if (!IsValidUuid(id))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = Guid.Parse(id).ToByteArray();
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.ID.SequenceEqual(guidBytes));

            if (cliente == null)
                throw new NotFoundException($"No se encontró el cliente con ID: {id}", "id");

            // Validar Razón Social (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Razon_Social))
            {
                if (dto.Razon_Social.Length < 3)
                {
                    errors["razon_Social"] = new List<string> { "La Razón Social debe tener al menos 3 caracteres" };
                }
                else if (dto.Razon_Social.Length > 20)
                {
                    errors["razon_Social"] = new List<string> { "La Razón Social no puede exceder 20 caracteres" };
                }
            }

            // Validar NIT (si se proporciona)
            if (!string.IsNullOrEmpty(dto.NIT))
            {
                if (dto.NIT.Length < 7 || dto.NIT.Length > 20)
                {
                    errors["nit"] = new List<string> { "El NIT debe tener entre 7 y 20 caracteres" };
                }
                else if (!Regex.IsMatch(dto.NIT, @"^[0-9]+$", RegexOptions.IgnoreCase, timeout))
                {
                    errors["nit"] = new List<string> { "El NIT debe contener solo números" };
                }
                else
                {
                    var nitExists = await _context.Clientes
                        .AnyAsync(c => c.NIT == dto.NIT && !c.ID.SequenceEqual(guidBytes));
                    if (nitExists)
                    {
                        errors["nit"] = new List<string> { $"Ya existe otro cliente con el NIT: {dto.NIT}" };
                    }
                }
            }

            // Validar Email (si se proporciona)
            if (!string.IsNullOrEmpty(dto.Email))
            {
                if (!IsValidEmail(dto.Email))
                {
                    errors["email"] = new List<string> { "El Email debe tener un formato válido (ejemplo@dominio.com)" };
                }
                else if (dto.Email.Length > 30)
                {
                    errors["email"] = new List<string> { "El Email no puede exceder 30 caracteres" };
                }
                else
                {
                    var emailExists = await _context.Clientes
                        .AnyAsync(c => c.Email == dto.Email && !c.ID.SequenceEqual(guidBytes));
                    if (emailExists)
                    {
                        errors["email"] = new List<string> { $"Ya existe otro cliente con el Email: {dto.Email}" };
                    }
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidateDeleteAsync(string id)
        {
            if (!IsValidUuid(id))
                throw new BadRequestException("El ID debe ser un UUID válido", "id");

            var guidBytes = ConvertToGuid(id);
            var exists = await _context.Clientes
                .AnyAsync(c => c.ID.SequenceEqual(guidBytes));
            
            if (!exists)
                throw new NotFoundException($"No se encontró el cliente con ID: {id}", "id");

            var hasReservas = await _context.Reservas
                .AnyAsync(r => r.Cliente_ID.SequenceEqual(guidBytes));
            
            if (hasReservas)
                throw new ConflictException("No se puede eliminar el cliente porque tiene reservas asociadas", "id");
        }

        private bool IsValidUuid(string value) => Guid.TryParse(value, out _);

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, timeout);
        }

        private byte[] ConvertToGuid(string uuid) => Guid.Parse(uuid).ToByteArray();
    }
}