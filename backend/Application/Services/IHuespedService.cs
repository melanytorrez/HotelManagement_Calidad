using HotelManagement.DTOs;

namespace HotelManagement.Application.Services
{
    public interface IHuespedService
    {
        Task<List<HuespedDto>> GetAllAsync();
        Task<HuespedDto> GetByIdAsync(string id);
        Task<HuespedDto> CreateAsync(HuespedCreateDto dto);
        Task<HuespedDto> UpdateAsync(string id, HuespedUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
