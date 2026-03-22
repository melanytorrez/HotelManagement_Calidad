using Microsoft.AspNetCore.Mvc;
using HotelManagement.Services;
using HotelManagement.DTOs;
using System.Net;


namespace HotelManagement.Presentacion.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClienteController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ClienteDTO>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? razon_social = null,
            [FromQuery] string? nit = null,
            [FromQuery] string? email = null,
            [FromQuery] bool? activo = null)
        {
            var clientes = await _clienteService.GetAllAsync();
            
            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(razon_social))
                clientes = clientes.Where(c => c.Razon_Social.Contains(razon_social, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrWhiteSpace(nit))
                clientes = clientes.Where(c => c.NIT.Contains(nit, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrWhiteSpace(email))
                clientes = clientes.Where(c => c.Email.Contains(email, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (activo.HasValue)
                clientes = clientes.Where(c => c.Activo == activo.Value).ToList();
            
            return Ok(clientes);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClienteDTO), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            var cliente = await _clienteService.GetByIdAsync(id);
            return Ok(cliente);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ClienteDTO), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)] // Para emails duplicados
        public async Task<IActionResult> Create([FromBody] ClienteCreateDTO dto)
        {
            var cliente = await _clienteService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = cliente.ID }, cliente);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ClienteDTO), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(string id, [FromBody] ClienteUpdateDTO dto)
        {
            var cliente = await _clienteService.UpdateAsync(id, dto);
            return Ok(cliente);
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ClienteDTO), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> PartialUpdate(string id, [FromBody] ClienteUpdateDTO dto)
        {
            var cliente = await _clienteService.PartialUpdateAsync(id, dto);
            return Ok(cliente);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _clienteService.DeleteAsync(id);
            if (!result) return NotFound(); 
            
            return NoContent();
        }
    }
}