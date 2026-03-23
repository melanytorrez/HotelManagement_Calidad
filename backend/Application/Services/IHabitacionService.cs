using HotelManagement.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

// NOTA: Revisa tu estructura. Si tus servicios están bajo 'HotelManagement.Aplicacion',
// usa 'namespace HotelManagement.Aplicacion.Services' en su lugar.
namespace HotelManagement.Application.Services 
{
    public interface IHabitacionService
    {
        // CRUD Básico
        Task<IEnumerable<HabitacionDto>> GetAllAsync();
        Task<HabitacionDto> GetByIdAsync(string id);
        Task<HabitacionDto> CreateAsync(HabitacionCreateDto dto);
        Task<HabitacionDto> UpdateAsync(string id, HabitacionUpdateDto dto);
        
        // Operación de Eliminación
        Task<bool> DeleteAsync(string id);
        
        // Operación de Actualización Parcial (Necesario para el escenario de cambio de estado)
        Task<HabitacionDto> PartialUpdateAsync(string id, HabitacionUpdateDto dto);

        // Consultas específicas
        Task<IEnumerable<HabitacionDto>> GetByTipoHabitacionIdAsync(string tipoHabitacionId);
        
        // Operación de disponibilidad (opcional, pero útil)
        Task<bool> IsHabitacionAvailableAsync(string id, System.DateTime fechaEntrada, System.DateTime fechaSalida);
    }
}