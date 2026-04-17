using Moq;
using Xunit;
using HotelManagement.Services;
using HotelManagement.Repositories;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Aplicacion.Exceptions;
using HotelManagement.Models;
using HotelManagement.DTOs;
using System;
using System.Threading.Tasks;

namespace HotelManagement.Tests.Unit.Services
{
    public class ClienteServiceTests
    {
        private readonly Mock<IClienteRepository> _repoMock;
        private readonly Mock<IClienteValidator> _validatorMock;
        private readonly ClienteService _service;

        public ClienteServiceTests()
        {
            // Paso 1: Infraestructura de Mocks
            _repoMock = new Mock<IClienteRepository>();
            _validatorMock = new Mock<IClienteValidator>();
            
            // Inyección de dependencias mockeadas en el servicio
            _service = new ClienteService(_repoMock.Object, _validatorMock.Object);
        }

        #region Pruebas de ApplyUpdateAsync

        // CAMINO 1: client == null -> True (Lanza excepción)
        [Fact]
        public async Task ApplyUpdateAsync_Path1_ClientNotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid().ToString();
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((Cliente)null!);

            await Assert.ThrowsAsync<NotFoundException>(() => 
                _service.UpdateAsync(id, new ClienteUpdateDTO()));
        }

        // CAMINO 2: client != null -> Todos los campos con valor (True)
        [Fact]
        public async Task ApplyUpdateAsync_Path2_FullUpdate_Success()
        {
            var guid = Guid.NewGuid();
            var cliente = new Cliente { ID = guid.ToByteArray() };
            var dto = new ClienteUpdateDTO { 
                Razon_Social = "NUEVA EMPRESA", 
                NIT = "1234567", 
                Email = "contacto@empresa.com", 
                Activo = false 
            };

            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(cliente);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(cliente);

            var result = await _service.UpdateAsync(guid.ToString(), dto);

            Assert.Equal("NUEVA EMPRESA", cliente.Razon_Social);
            Assert.Equal("1234567", cliente.NIT);
            Assert.False(cliente.Activo);
        }

        // CAMINO 3: client != null -> Solo Razon_Social tiene valor
        [Fact]
        public async Task ApplyUpdateAsync_Path3_OnlyRazonSocial()
        {
            var guid = Guid.NewGuid();
            var cliente = new Cliente { ID = guid.ToByteArray(), NIT = "77777", Email = "old@test.com" };
            var dto = new ClienteUpdateDTO { Razon_Social = "RAZON EDITADA" };

            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(cliente);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(cliente);

            await _service.UpdateAsync(guid.ToString(), dto);

            Assert.Equal("RAZON EDITADA", cliente.Razon_Social);
            Assert.Equal("77777", cliente.NIT);
        }

        // CAMINO 4: client != null -> Solo NIT tiene valor
        [Fact]
        public async Task ApplyUpdateAsync_Path4_OnlyNIT()
        {
            var guid = Guid.NewGuid();
            var cliente = new Cliente { ID = guid.ToByteArray(), Razon_Social = "MANTIENE" };
            var dto = new ClienteUpdateDTO { NIT = "888888" };

            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(cliente);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(cliente);

            await _service.UpdateAsync(guid.ToString(), dto);

            Assert.Equal("MANTIENE", cliente.Razon_Social);
            Assert.Equal("888888", cliente.NIT);
        }

        // CAMINO 5: client != null -> Solo Email tiene valor
        [Fact]
        public async Task ApplyUpdateAsync_Path5_OnlyEmail()
        {
            var guid = Guid.NewGuid();
            var cliente = new Cliente { ID = guid.ToByteArray(), NIT = "111" };
            var dto = new ClienteUpdateDTO { Email = "nuevo@email.com" };

            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(cliente);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(cliente);

            await _service.UpdateAsync(guid.ToString(), dto);

            Assert.Equal("111", cliente.NIT);
            Assert.Equal("nuevo@email.com", cliente.Email);
        }

        // CAMINO 6: client != null -> Solo Activo tiene valor
        [Fact]
        public async Task ApplyUpdateAsync_Path6_OnlyActivoState()
        {
            var guid = Guid.NewGuid();
            var cliente = new Cliente { ID = guid.ToByteArray(), Activo = true };
            var dto = new ClienteUpdateDTO { Activo = false };

            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(cliente);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(cliente);

            await _service.UpdateAsync(guid.ToString(), dto);

            Assert.False(cliente.Activo);
        }

        #endregion
    }
}