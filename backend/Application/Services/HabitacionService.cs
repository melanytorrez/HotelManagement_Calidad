using HotelManagement.DTOs;
using HotelManagement.Models;
using HotelManagement.Repositories;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Aplicacion.Exceptions;
using HotelManagement.Application.Services;

namespace HotelManagement.Services
{
    public class HabitacionService : IHabitacionService
    {
        private readonly IHabitacionRepository _repository;
        private readonly IHabitacionValidator _validator;

        public HabitacionService(
            IHabitacionRepository repository,
            IHabitacionValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<IEnumerable<HabitacionDto>> GetAllAsync()
        {
            var habitaciones = await _repository.GetAllAsync();
            return habitaciones.Select(MapToDTO);
        }

        public async Task<HabitacionDto> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid guid))
                throw new BadRequestException("El ID proporcionado no es un GUID válido.");

            var guidBytes = guid.ToByteArray();
            var habitacion = await _repository.GetByIdAsync(guidBytes);

            if (habitacion == null)
                throw new NotFoundException($"No se encontró la habitación con ID: {id}");

            return MapToDTO(habitacion);
        }

        public async Task<HabitacionDto> CreateAsync(HabitacionCreateDto dto)
        {
            await _validator.ValidateCreateAsync(dto);

            var tipoHabitacionId = Guid.Parse(dto.Tipo_Habitacion_ID).ToByteArray();

            var habitacion = new Habitacion
            {
                ID = Guid.NewGuid().ToByteArray(),
                Tipo_Habitacion_ID = tipoHabitacionId,
                Numero_Habitacion = dto.Numero_Habitacion,
                Piso = dto.Piso.Value,
                Estado_Habitacion = dto.Estado_Habitacion,
                Fecha_Creacion = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now,
                Usuario_Creacion_ID = null,
                Usuario_Actualizacion_ID = null
            };

            var created = await _repository.CreateAsync(habitacion);
            return MapToDTO(created);
        }

        public async Task<HabitacionDto> UpdateAsync(string id, HabitacionUpdateDto dto)
        {
            await _validator.ValidateUpdateAsync(id, new HabitacionCreateDto 
            { 
                Tipo_Habitacion_ID = dto.Tipo_Habitacion_ID ?? string.Empty,
                Numero_Habitacion = dto.Numero_Habitacion ?? string.Empty,
                Piso = dto.Piso ?? 0,
                Estado_Habitacion = dto.Estado_Habitacion ?? string.Empty
            });

            var guidBytes = Guid.Parse(id).ToByteArray();
            var habitacion = await _repository.GetByIdAsync(guidBytes);

            if (habitacion == null)
                throw new NotFoundException($"No se encontró la habitación con ID: {id}");

            if (!string.IsNullOrEmpty(dto.Tipo_Habitacion_ID))
                habitacion.Tipo_Habitacion_ID = Guid.Parse(dto.Tipo_Habitacion_ID).ToByteArray();

            if (!string.IsNullOrEmpty(dto.Numero_Habitacion))
                habitacion.Numero_Habitacion = dto.Numero_Habitacion;

            if (dto.Piso.HasValue)
                habitacion.Piso = dto.Piso.Value;

            if (!string.IsNullOrEmpty(dto.Estado_Habitacion))
                habitacion.Estado_Habitacion = dto.Estado_Habitacion;

            if (dto.Activo.HasValue)
            {
                // Nota: Habitacion no tiene campo Activo en el modelo actual,
                // si es necesario agregarlo
            }

            // Actualizar campos de auditoría
            habitacion.Fecha_Actualizacion = DateTime.Now;
            habitacion.Usuario_Actualizacion_ID = null;

            var updated = await _repository.UpdateAsync(habitacion);
            return MapToDTO(updated);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _validator.ValidateDeleteAsync(id);
            var guidBytes = Guid.Parse(id).ToByteArray();
            return await _repository.DeleteAsync(guidBytes);
        }

        public async Task<HabitacionDto> PartialUpdateAsync(string id, HabitacionUpdateDto dto)
        {
            await _validator.ValidatePartialUpdateAsync(id, dto);

            var guidBytes = Guid.Parse(id).ToByteArray();
            var habitacion = await _repository.GetByIdAsync(guidBytes);

            if (habitacion == null)
                throw new NotFoundException($"No se encontró la habitación con ID: {id}");

            if (!string.IsNullOrEmpty(dto.Tipo_Habitacion_ID))
                habitacion.Tipo_Habitacion_ID = Guid.Parse(dto.Tipo_Habitacion_ID).ToByteArray();

            if (!string.IsNullOrEmpty(dto.Numero_Habitacion))
                habitacion.Numero_Habitacion = dto.Numero_Habitacion;

            if (dto.Piso.HasValue)
                habitacion.Piso = dto.Piso.Value;

            if (!string.IsNullOrEmpty(dto.Estado_Habitacion))
                habitacion.Estado_Habitacion = dto.Estado_Habitacion;

            habitacion.Fecha_Actualizacion = DateTime.Now;
            habitacion.Usuario_Actualizacion_ID = null;

            var updated = await _repository.UpdateAsync(habitacion);
            return MapToDTO(updated);
        }

        public async Task<IEnumerable<HabitacionDto>> GetByTipoHabitacionIdAsync(string tipoHabitacionId)
        {
            // Método no implementado en IHabitacionRepository
            // Se retorna lista vacía por ahora
            return await Task.FromResult(Enumerable.Empty<HabitacionDto>());
        }

        public async Task<bool> IsHabitacionAvailableAsync(string id, DateTime fechaEntrada, DateTime fechaSalida)
        {
            // Método no implementado en IHabitacionRepository
            // Se retorna true por ahora
            return await Task.FromResult(true);
        }

        private static HabitacionDto MapToDTO(Habitacion habitacion)
        {
            return new HabitacionDto
            {
                ID = ByteArrayToGuid(habitacion.ID),
                Numero_Habitacion = habitacion.Numero_Habitacion,
                Piso = habitacion.Piso,
                Estado_Habitacion = habitacion.Estado_Habitacion,
                Tipo_Habitacion_ID = ByteArrayToGuid(habitacion.Tipo_Habitacion_ID),
                Tipo_Nombre = habitacion.TipoHabitacion?.Nombre,
                Capacidad_Maxima = habitacion.TipoHabitacion?.Capacidad_Maxima,
                Tarifa_Base = habitacion.TipoHabitacion?.Precio_Base,
                Activo = true
            };
        }

        private static string ByteArrayToGuid(byte[] bytes) => new Guid(bytes).ToString();
    }
}
