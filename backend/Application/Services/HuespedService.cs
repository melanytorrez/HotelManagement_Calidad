using HotelManagement.Aplicacion.Exceptions;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.DTOs;
using HotelManagement.Models;
using HotelManagement.Repositories;
using System.Globalization;

namespace HotelManagement.Application.Services
{
    public class HuespedService : IHuespedService
    {
        private readonly IHuespedRepository _repository;
        private readonly IHuespedValidator _validator;

        public HuespedService(IHuespedRepository repository, IHuespedValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<List<HuespedDTO>> GetAllAsync()
        {
            var huespedes = await _repository.GetAllAsync();
            return huespedes.Select(MapToDto).ToList();
        }

        public async Task<HuespedDTO> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
            {
                throw new BadRequestException("El ID proporcionado no es un GUID válido.", "id");
            }

            var entity = await _repository.GetByIdAsync(guid.ToByteArray());

            if (entity == null)
            {
                throw new NotFoundException($"No se encontró el huésped con ID: {id}", "id");
            }

            return MapToDto(entity);
        }

        public async Task<HuespedDTO> CreateAsync(HuespedCreateDTO dto)
        {
            await _validator.ValidateCreateAsync(dto);

            var entity = new Huesped
            {
                ID = Guid.NewGuid().ToByteArray(),
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Segundo_Apellido = dto.Segundo_Apellido,
                Documento_Identidad = dto.Documento_Identidad,
                Telefono = dto.Telefono,
                Fecha_Nacimiento = ParseNullableDate(dto.Fecha_Nacimiento),
                Activo = true,
                Fecha_Creacion = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now
            };

            var created = await _repository.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<HuespedDTO> UpdateAsync(string id, HuespedUpdateDTO dto)
        {
            await _validator.ValidateUpdateAsync(id, dto);

            var guidBytes = Guid.Parse(id).ToByteArray();
            var entity = await _repository.GetByIdAsync(guidBytes);

            if (entity == null)
            {
                throw new NotFoundException($"No se encontró el huésped con ID: {id}", "id");
            }

            if (!string.IsNullOrWhiteSpace(dto.Nombre))
            {
                entity.Nombre = dto.Nombre;
            }

            if (!string.IsNullOrWhiteSpace(dto.Apellido))
            {
                entity.Apellido = dto.Apellido;
            }

            if (dto.Segundo_Apellido != null)
            {
                entity.Segundo_Apellido = dto.Segundo_Apellido;
            }

            if (!string.IsNullOrWhiteSpace(dto.Documento_Identidad))
            {
                entity.Documento_Identidad = dto.Documento_Identidad;
            }

            if (dto.Telefono != null)
            {
                entity.Telefono = dto.Telefono;
            }

            if (dto.Fecha_Nacimiento != null)
            {
                entity.Fecha_Nacimiento = ParseNullableDate(dto.Fecha_Nacimiento);
            }

            if (dto.Activo.HasValue)
            {
                entity.Activo = dto.Activo.Value;
            }

            entity.Fecha_Actualizacion = DateTime.Now;

            var updated = await _repository.UpdateAsync(entity);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _validator.ValidateDeleteAsync(id);
            var guidBytes = Guid.Parse(id).ToByteArray();
            return await _repository.DeleteAsync(guidBytes);
        }

        private static DateTime? ParseNullableDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }

            throw new ValidationException("fecha_Nacimiento", "La Fecha de Nacimiento tiene un formato inválido.");
        }

        private static HuespedDTO MapToDto(Huesped entity)
        {
            return new HuespedDTO
            {
                ID = new Guid(entity.ID).ToString(),
                Nombre = entity.Nombre,
                Apellido = entity.Apellido,
                Segundo_Apellido = entity.Segundo_Apellido,
                Documento_Identidad = entity.Documento_Identidad,
                Telefono = entity.Telefono,
                Fecha_Nacimiento = entity.Fecha_Nacimiento,
                Activo = entity.Activo
            };
        }
    }
}
