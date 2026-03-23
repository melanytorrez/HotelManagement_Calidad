using HotelManagement.DTOs;
using HotelManagement.Models;
using HotelManagement.Repositories;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Aplicacion.Exceptions;

namespace HotelManagement.Services
{
    public class DetalleReservaService : IDetalleReservaService
    {
        private readonly IDetalleReservaRepository _repository;
        private readonly IDetalleReservaValidator _validator;

        public DetalleReservaService(
            IDetalleReservaRepository repository,
            IDetalleReservaValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<List<DetalleReservaDto>> GetAllAsync()
        {
            var detalles = await _repository.GetAllAsync();
            return detalles.Select(MapToDTO).ToList();
        }

        public async Task<DetalleReservaDto> GetByIdAsync(string id)
        {
            var guidBytes = Guid.Parse(id).ToByteArray();
            var detalle = await _repository.GetByIdAsync(guidBytes);

            if (detalle == null)
                throw new NotFoundException($"No se encontró el detalle de reserva con ID: {id}");

            return MapToDTO(detalle);
        }

        public async Task<DetalleReservaDto> CreateAsync(DetalleReservaCreateDto dto)
        {
            await _validator.ValidateCreateAsync(dto);

            var detalle = new DetalleReserva
            {
                ID = Guid.NewGuid().ToByteArray(),
                Reserva_ID = Guid.Parse(dto.Reserva_ID).ToByteArray(),
                Habitacion_ID = Guid.Parse(dto.Habitacion_ID).ToByteArray(),
                Huesped_ID = Guid.Parse(dto.Huesped_ID).ToByteArray(),
                Fecha_Entrada = dto.Fecha_Entrada.Value,
                Fecha_Salida = dto.Fecha_Salida.Value
            };

            var created = await _repository.CreateAsync(detalle);
            return MapToDTO(created);
        }

        public async Task<DetalleReservaDto> UpdateAsync(string id, DetalleReservaUpdateDto dto)
        {
            await _validator.ValidateUpdateAsync(id, dto);

            var guidBytes = Guid.Parse(id).ToByteArray();
            var detalle = await _repository.GetByIdAsync(guidBytes);

            if (detalle == null)
                throw new NotFoundException($"No se encontró el detalle de reserva con ID: {id}");

            if (!string.IsNullOrEmpty(dto.Habitacion_ID))
                detalle.Habitacion_ID = Guid.Parse(dto.Habitacion_ID).ToByteArray();

            if (!string.IsNullOrEmpty(dto.Huesped_ID))
                detalle.Huesped_ID = Guid.Parse(dto.Huesped_ID).ToByteArray();

            if (dto.Fecha_Entrada.HasValue)
                detalle.Fecha_Entrada = dto.Fecha_Entrada.Value;

            if (dto.Fecha_Salida.HasValue)
                detalle.Fecha_Salida = dto.Fecha_Salida.Value;

            var updated = await _repository.UpdateAsync(detalle);
            return MapToDTO(updated);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _validator.ValidateDeleteAsync(id);
            var guidBytes = Guid.Parse(id).ToByteArray();
            return await _repository.DeleteAsync(guidBytes);
        }

        public async Task<List<DetalleReservaDto>> GetByReservaIdAsync(string reservaId)
        {
            var guidBytes = Guid.Parse(reservaId).ToByteArray();
            var detalles = await _repository.GetByReservaIdAsync(guidBytes);
            return detalles.Select(MapToDTO).ToList();
        }

        private static DetalleReservaDto MapToDTO(DetalleReserva detalle)
        {
            string nombreHuesped = string.Empty;
            if (detalle.Huesped != null)
            {
                nombreHuesped = $"{detalle.Huesped.Nombre} {detalle.Huesped.Apellido}";
                if (!string.IsNullOrWhiteSpace(detalle.Huesped.Segundo_Apellido))
                {
                    nombreHuesped += $" {detalle.Huesped.Segundo_Apellido}";
                }
            }

            return new DetalleReservaDto
            {
                ID = ByteArrayToGuid(detalle.ID),
                Reserva_ID = ByteArrayToGuid(detalle.Reserva_ID),
                Habitacion_ID = ByteArrayToGuid(detalle.Habitacion_ID),
                Huesped_ID = ByteArrayToGuid(detalle.Huesped_ID),
                Fecha_Entrada = detalle.Fecha_Entrada,
                Fecha_Salida = detalle.Fecha_Salida,
                Numero_Habitacion = detalle.Habitacion?.Numero_Habitacion,
                Nombre_Huesped = nombreHuesped
            };
        }

        private static string ByteArrayToGuid(byte[] bytes)
        {
            return new Guid(bytes).ToString();
        }
    }
}
