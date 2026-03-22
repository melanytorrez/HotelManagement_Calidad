using HotelManagement.DTOs;

namespace HotelManagement.Services
{
    public interface IClienteService
    {
        Task<List<ClienteDTO>> GetAllAsync();
        Task<ClienteDTO> GetByIdAsync(string id);
        Task<ClienteDTO> CreateAsync(ClienteCreateDTO dto);
        Task<ClienteDTO> UpdateAsync(string id, ClienteUpdateDTO dto);
        Task<ClienteDTO> PartialUpdateAsync(string id, ClienteUpdateDTO dto);
        Task<bool> DeleteAsync(string id);
    }
}