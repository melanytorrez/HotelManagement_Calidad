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

            ValidateRazonSocial(dto.Razon_Social, errors);
            await ValidateNitAsync(dto.NIT, errors);
            await ValidateEmailAsync(dto.Email, errors);

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidateUpdateAsync(string id, ClienteUpdateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            if (!IsValidUuid(id))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = Guid.Parse(id).ToByteArray();
            var exists = await _context.Clientes.AnyAsync(c => c.ID.SequenceEqual(guidBytes));
            if (!exists)
                throw new NotFoundException($"No se encontró el cliente con ID: {id}", "id");

            ValidateRazonSocial(dto.Razon_Social, errors);
            await ValidateNitAsync(dto.NIT, errors, guidBytes);
            await ValidateEmailAsync(dto.Email, errors, guidBytes);

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task ValidatePartialUpdateAsync(string id, ClienteUpdateDTO dto)
        {
            var errors = new Dictionary<string, List<string>>();

            if (!IsValidUuid(id))
            {
                errors["id"] = new List<string> { "El ID debe ser un UUID válido" };
                throw new ValidationException(errors);
            }

            var guidBytes = Guid.Parse(id).ToByteArray();
            var exists = await _context.Clientes.AnyAsync(c => c.ID.SequenceEqual(guidBytes));
            if (!exists)
                throw new NotFoundException($"No se encontró el cliente con ID: {id}", "id");

            if (!string.IsNullOrEmpty(dto.Razon_Social))
                ValidateRazonSocial(dto.Razon_Social, errors);

            if (!string.IsNullOrEmpty(dto.NIT))
                await ValidateNitAsync(dto.NIT, errors, guidBytes);

            if (!string.IsNullOrEmpty(dto.Email))
                await ValidateEmailAsync(dto.Email, errors, guidBytes);

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private static void ValidateRazonSocial(string? razonSocial, Dictionary<string, List<string>> errors)
        {
            if (string.IsNullOrWhiteSpace(razonSocial))
            {
                errors["razon_Social"] = new List<string> { "La Razón Social es obligatoria" };
            }
            else if (razonSocial.Length < 3)
            {
                errors["razon_Social"] = new List<string> { "La Razón Social debe tener al menos 3 caracteres" };
            }
            else if (razonSocial.Length > 20)
            {
                errors["razon_Social"] = new List<string> { "La Razón Social no puede exceder 20 caracteres" };
            }
        }

        private async Task ValidateNitAsync(string? nit, Dictionary<string, List<string>> errors, byte[]? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(nit))
            {
                errors["nit"] = new List<string> { "El NIT es obligatorio" };
                return;
            }

            if (nit.Length < 7 || nit.Length > 20)
            {
                errors["nit"] = new List<string> { "El NIT debe tener entre 7 y 20 caracteres" };
            }
            else if (!Regex.IsMatch(nit, @"^[0-9]+$", RegexOptions.IgnoreCase, timeout))
            {
                errors["nit"] = new List<string> { "El NIT debe contener solo números" };
            }
            else
            {
                var query = _context.Clientes.AsQueryable();
                if (currentId != null)
                {
                    query = query.Where(c => !c.ID.SequenceEqual(currentId));
                }

                var nitExists = await query.AnyAsync(c => c.NIT == nit);
                if (nitExists)
                {
                    errors["nit"] = new List<string> { $"Ya existe {(currentId == null ? "un" : "otro")} cliente con el NIT: {nit}" };
                }
            }
        }

        private async Task ValidateEmailAsync(string? email, Dictionary<string, List<string>> errors, byte[]? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                errors["email"] = new List<string> { "El Email es obligatorio" };
                return;
            }

            if (!IsValidEmail(email))
            {
                errors["email"] = new List<string> { "El Email debe tener un formato válido (ejemplo@dominio.com)" };
            }
            else if (email.Length > 30)
            {
                errors["email"] = new List<string> { "El Email no puede exceder 30 caracteres" };
            }
            else
            {
                var query = _context.Clientes.AsQueryable();
                if (currentId != null)
                {
                    query = query.Where(c => !c.ID.SequenceEqual(currentId));
                }

                var emailExists = await query.AnyAsync(c => c.Email == email);
                if (emailExists)
                {
                    errors["email"] = new List<string> { $"Ya existe {(currentId == null ? "un" : "otro")} cliente con el Email: {email}" };
                }
            }
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

        private static bool IsValidUuid(string value) => Guid.TryParse(value, out _);

        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, timeout);
        }

        private static byte[] ConvertToGuid(string uuid) => Guid.Parse(uuid).ToByteArray();
    }
}