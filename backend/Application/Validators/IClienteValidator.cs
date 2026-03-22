using HotelManagement.DTOs;

namespace HotelManagement.Aplicacion.Validators
{
    public interface IClienteValidator
    {
        Task ValidateCreateAsync(ClienteCreateDTO dto);
        Task ValidateUpdateAsync(string id, ClienteUpdateDTO dto);
        Task ValidatePartialUpdateAsync(string id, ClienteUpdateDTO dto);
        Task ValidateDeleteAsync(string id);
    }
}