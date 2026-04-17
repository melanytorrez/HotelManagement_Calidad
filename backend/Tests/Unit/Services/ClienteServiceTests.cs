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
            _repoMock = new Mock<IClienteRepository>();
            _validatorMock = new Mock<IClienteValidator>();
            
            _service = new ClienteService(_repoMock.Object, _validatorMock.Object);
        }
        #region Pruebas de CreateAsync 

        [Fact]
        public async Task CreateAsync_Path1_ValidationFails_ThrowsValidationException()
        {
            var dto = new ClienteCreateDTO();
            
            var erroresSimulados = new Dictionary<string, List<string>>
            {
                { "Razon_Social", new List<string> { "La Razón Social es obligatoria." } }
            };

            _validatorMock.Setup(v => v.ValidateCreateAsync(dto))
                        .ThrowsAsync(new ValidationException(erroresSimulados));

            await Assert.ThrowsAsync<ValidationException>(() => 
                _service.CreateAsync(dto));
            
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Cliente>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_Path2_ValidData_CreatesAndReturnsDTO()
        {
            var dto = new ClienteCreateDTO { 
                Razon_Social = "HOTEL TEST S.A.", 
                NIT = "987654321"
            };
            
            var cliente = new Cliente { 
                ID = Guid.NewGuid().ToByteArray(), 
                Razon_Social = "HOTEL TEST S.A.",
                Activo = true
            };
            
            _validatorMock.Setup(v => v.ValidateCreateAsync(dto))
                        .Returns(Task.CompletedTask);
            
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Cliente>()))
                    .ReturnsAsync(cliente);

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("HOTEL TEST S.A.", result.Razon_Social);
            Assert.True(result.Activo);
            
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Cliente>()), Times.Once);
        }

        #endregion

        #region Pruebas de GetByIdAsync 

        [Fact]
        public async Task GetByIdAsync_Path1_InvalidGuid_ThrowsBadRequestException()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => 
                _service.GetByIdAsync("formato-incorrecto"));
        }

        [Fact]
        public async Task GetByIdAsync_Path2_ClientNotFound_ThrowsNotFoundException()
        {
            var guid = Guid.NewGuid().ToString();
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((Cliente)null!);

            await Assert.ThrowsAsync<NotFoundException>(() => 
                _service.GetByIdAsync(guid));
        }

        [Fact]
        public async Task GetByIdAsync_Path3_Success_ReturnsDTO()
        {
            var guid = Guid.NewGuid();
            var cliente = new Cliente { ID = guid.ToByteArray(), Razon_Social = "Test" };
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(cliente);

            var result = await _service.GetByIdAsync(guid.ToString());

            Assert.NotNull(result);
            Assert.Equal("Test", result.Razon_Social);
        }

        #endregion

        #region Pruebas de ApplyUpdateAsync

        [Fact]
        public async Task ApplyUpdateAsync_Path1_ClientNotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid().ToString();
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((Cliente)null!);

            await Assert.ThrowsAsync<NotFoundException>(() => 
                _service.UpdateAsync(id, new ClienteUpdateDTO()));
        }

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

        #region Pruebas de DeleteAsync 

        [Fact]
        public async Task DeleteAsync_Path1_ValidationFails_ThrowsValidationException()
        {
            var id = Guid.NewGuid().ToString();
            var errores = new Dictionary<string, List<string>> {
                { "Eliminación", new List<string> { "El cliente tiene facturas pendientes." } }
            };

            _validatorMock.Setup(v => v.ValidateDeleteAsync(id))
                          .ThrowsAsync(new ValidationException(errores));

            await Assert.ThrowsAsync<ValidationException>(() => 
                _service.DeleteAsync(id));
            
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Path2_Success_ReturnsTrue()
        {
            // Camino 2: Eliminación exitosa
            var id = Guid.NewGuid().ToString();
            _validatorMock.Setup(v => v.ValidateDeleteAsync(id)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<byte[]>())).ReturnsAsync(true);

            var result = await _service.DeleteAsync(id);

            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<byte[]>()), Times.Once);
        }

        #endregion
    }
}