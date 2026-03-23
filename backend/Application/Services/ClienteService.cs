using HotelManagement.DTOs;
using HotelManagement.Models;
using HotelManagement.Repositories;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Aplicacion.Exceptions;

namespace HotelManagement.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _repository;
        private readonly IClienteValidator _validator;

        public ClienteService(
            IClienteRepository repository,
            IClienteValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<List<ClienteDTO>> GetAllAsync()
        {
            var clientes = await _repository.GetAllAsync();
            return clientes.Select(MapToDTO).ToList();
        }

        public async Task<ClienteDTO> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid guid))
                throw new BadRequestException("El ID proporcionado no es un GUID válido.");

            var guidBytes = guid.ToByteArray();
            var cliente = await _repository.GetByIdAsync(guidBytes);

            if (cliente == null)
                throw new NotFoundException($"No se encontró el cliente con ID: {id}");

            return MapToDTO(cliente);
        }

        public async Task<ClienteDTO> CreateAsync(ClienteCreateDTO dto)
        {
            await _validator.ValidateCreateAsync(dto);

            var cliente = new Cliente
            {
                ID = Guid.NewGuid().ToByteArray(),
                Razon_Social = dto.Razon_Social,
                NIT = dto.NIT,
                Email = dto.Email,
                Activo = true,
                Fecha_Creacion = DateTime.Now,
                Usuario_Creacion_ID = null
            };

            var created = await _repository.CreateAsync(cliente);
            return MapToDTO(created);
        }

        public async Task<ClienteDTO> UpdateAsync(string id, ClienteUpdateDTO dto)
        {
            await _validator.ValidateUpdateAsync(id, dto);
            return await ApplyUpdateAsync(id, dto);
        }

        public async Task<ClienteDTO> PartialUpdateAsync(string id, ClienteUpdateDTO dto)
        {
            await _validator.ValidatePartialUpdateAsync(id, dto);
            return await ApplyUpdateAsync(id, dto);
        }

        private async Task<ClienteDTO> ApplyUpdateAsync(string id, ClienteUpdateDTO dto)
        {
            var guidBytes = Guid.Parse(id).ToByteArray();
            var cliente = await _repository.GetByIdAsync(guidBytes);

            if (cliente == null)
                throw new NotFoundException($"No se encontró el cliente con ID: {id}");

            if (!string.IsNullOrEmpty(dto.Razon_Social))
                cliente.Razon_Social = dto.Razon_Social;

            if (!string.IsNullOrEmpty(dto.NIT))
                cliente.NIT = dto.NIT;

            if (!string.IsNullOrEmpty(dto.Email))
                cliente.Email = dto.Email;

            if (dto.Activo.HasValue)
                cliente.Activo = dto.Activo.Value;

            cliente.Fecha_Actualizacion = DateTime.Now;
            cliente.Usuario_Actualizacion_ID = null;

            var updated = await _repository.UpdateAsync(cliente);
            return MapToDTO(updated);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _validator.ValidateDeleteAsync(id);
            var guidBytes = Guid.Parse(id).ToByteArray();
            return await _repository.DeleteAsync(guidBytes);
        }

        private static ClienteDTO MapToDTO(Cliente cliente)
        {
            return new ClienteDTO
            {
                ID = ByteArrayToGuid(cliente.ID),
                Razon_Social = cliente.Razon_Social,
                NIT = cliente.NIT,
                Email = cliente.Email,
                Activo = cliente.Activo
            };
        }

        private static string ByteArrayToGuid(byte[] bytes) => new Guid(bytes).ToString();
    }
}