using HotelManagement.DTOs;

namespace HotelManagement.Services
{
    public interface IDetalleReservaService
    {
        Task<List<DetalleReservaDto>> GetAllAsync();
        Task<DetalleReservaDto> GetByIdAsync(string id);
        Task<DetalleReservaDto> CreateAsync(DetalleReservaCreateDto dto);
        Task<DetalleReservaDto> UpdateAsync(string id, DetalleReservaUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<List<DetalleReservaDto>> GetByReservaIdAsync(string reservaId);
    }
}
