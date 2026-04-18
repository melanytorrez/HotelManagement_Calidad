using HotelManagement.Aplicacion.Exceptions;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Application.Services;
using HotelManagement.DTOs;
using HotelManagement.Models;
using HotelManagement.Repositories;
using Moq;
using Xunit;

namespace HotelManagement.Tests.Unit.Services
{
    public class HuespedServiceTests
    {
        private readonly Mock<IHuespedRepository> _repoMock;
        private readonly Mock<IHuespedValidator> _validatorMock;
        private readonly HuespedService _service;

        public HuespedServiceTests()
        {
            _repoMock = new Mock<IHuespedRepository>();
            _validatorMock = new Mock<IHuespedValidator>();
            _service = new HuespedService(_repoMock.Object, _validatorMock.Object);
        }

        #region Pruebas de GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_Path1_InvalidGuidFormat_ThrowsBadRequestException()
        {
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => 
                _service.GetByIdAsync("esto-no-es-un-guid"));
            
            Assert.Equal("id", ex.Field);
        }

        [Fact]
        public async Task GetByIdAsync_Path2_HuespedNotFound_ThrowsNotFoundException()
        {
            var guid = Guid.NewGuid().ToString();
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((Huesped)null!);

            await Assert.ThrowsAsync<NotFoundException>(() => 
                _service.GetByIdAsync(guid));
        }

        [Fact]
        public async Task GetByIdAsync_Path3_Success_ReturnsHuespedDto()
        {
            var guid = Guid.NewGuid();
            var entity = new Huesped 
            { 
                ID = guid.ToByteArray(), 
                Nombre = "Test", 
                Apellido = "Huesped" 
            };

            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync(entity);

            var result = await _service.GetByIdAsync(guid.ToString());

            Assert.NotNull(result);
            Assert.Equal("Test", result.Nombre);
            Assert.Equal(guid.ToString(), result.ID);
        }

        #endregion

        #region Pruebas de CreateAsync
        [Fact]
        public async Task CreateAsync_Path1_BusinessValidationFails_ThrowsException()
        {
            var dto = new HuespedCreateDto { Nombre = "Test" };
            _validatorMock.Setup(v => v.ValidateCreateAsync(dto))
                          .ThrowsAsync(new ValidationException(new Dictionary<string, List<string>> { { "Documento", new List<string> { "Duplicado" } } }));

            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Huesped>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_Path2_InvalidDate_ThrowsValidationException()
        {
            var dto = new HuespedCreateDto { 
                Nombre = "Luis", 
                Fecha_Nacimiento = "fecha-invalida" 
            };
            _validatorMock.Setup(v => v.ValidateCreateAsync(dto)).Returns(Task.CompletedTask);

            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_Path3_Success_ReturnsHuespedDto()
        {
            // Arrange
            var dto = new HuespedCreateDto { 
                Nombre = "Luis", 
                Apellido = "Sosa",
                Fecha_Nacimiento = "1995-05-20"
            };
            var entityCreada = new Huesped { 
                ID = Guid.NewGuid().ToByteArray(), 
                Nombre = "Luis" 
            };

            _validatorMock.Setup(v => v.ValidateCreateAsync(dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Huesped>())).ReturnsAsync(entityCreada);

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("Luis", result.Nombre);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Huesped>(h => h.Activo == true)), Times.Once);
        }

        #endregion

        #region Pruebas de UpdateAsync

        [Fact]
        public async Task UpdateAsync_Path1_ValidationFails_ThrowsException()
        {
            var id = Guid.NewGuid().ToString();
            var dto = new HuespedUpdateDto();
            _validatorMock.Setup(v => v.ValidateUpdateAsync(id, dto))
                          .ThrowsAsync(new ValidationException(new Dictionary<string, List<string>> { { "General", new List<string> { "Error" } } }));

            await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_Path2_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid().ToString();
            var dto = new HuespedUpdateDto();
            _validatorMock.Setup(v => v.ValidateUpdateAsync(id, dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync((Huesped)null!);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_Path3_InvalidDateFormat_ThrowsValidationException()
        {
            var guid = Guid.NewGuid();
            var dto = new HuespedUpdateDto { Fecha_Nacimiento = "fecha-loca" };
            var entity = new Huesped { ID = guid.ToByteArray() };

            _validatorMock.Setup(v => v.ValidateUpdateAsync(guid.ToString(), dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(entity);

            await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(guid.ToString(), dto));
        }

        [Fact]
        public async Task UpdateAsync_Path4_FullUpdateSuccess_ReturnsDto()
        {
            var guid = Guid.NewGuid();
            var idStr = guid.ToString();
            var dto = new HuespedUpdateDto {
                Nombre = "NuevoNombre",
                Apellido = "NuevoApellido",
                Segundo_Apellido = "NuevoSegundo",
                Documento_Identidad = "123",
                Telefono = "555",
                Fecha_Nacimiento = "2000-01-01",
                Activo = false
            };
            var entity = new Huesped { ID = guid.ToByteArray(), Nombre = "Viejo" };

            _validatorMock.Setup(v => v.ValidateUpdateAsync(idStr, dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(entity);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Huesped>())).ReturnsAsync(entity);

            var result = await _service.UpdateAsync(idStr, dto);

            Assert.Equal(dto.Nombre, result.Nombre);
            Assert.Equal(dto.Apellido, result.Apellido);
            Assert.False(result.Activo);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Huesped>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Path5_PartialFields_OnlyNames()
        {
            var guid = Guid.NewGuid();
            var dto = new HuespedUpdateDto { Nombre = "Juan", Apellido = "Perez" }; 
            var entity = new Huesped { ID = guid.ToByteArray() };

            _validatorMock.Setup(v => v.ValidateUpdateAsync(It.IsAny<string>(), dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(entity);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Huesped>())).ReturnsAsync(entity);

            await _service.UpdateAsync(guid.ToString(), dto);

            Assert.Equal("Juan", entity.Nombre);
            Assert.Null(entity.Telefono); 
        }

        [Fact]
        public async Task UpdateAsync_Path6_OptionalFields()
        {
            var guid = Guid.NewGuid();
            var dto = new HuespedUpdateDto { Segundo_Apellido = "Zuñiga", Telefono = "777888" };
            var entity = new Huesped { ID = guid.ToByteArray() };

            _validatorMock.Setup(v => v.ValidateUpdateAsync(It.IsAny<string>(), dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(entity);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Huesped>())).ReturnsAsync(entity);

            await _service.UpdateAsync(guid.ToString(), dto);

            Assert.Equal("Zuñiga", entity.Segundo_Apellido);
            Assert.Equal("777888", entity.Telefono);
        }

        [Fact]
        public async Task UpdateAsync_Path7_ToggleActive()
        {
            var guid = Guid.NewGuid();
            var dto = new HuespedUpdateDto { Activo = false };
            var entity = new Huesped { ID = guid.ToByteArray(), Activo = true };

            _validatorMock.Setup(v => v.ValidateUpdateAsync(It.IsAny<string>(), dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(entity);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Huesped>())).ReturnsAsync(entity);

            await _service.UpdateAsync(guid.ToString(), dto);

            Assert.False(entity.Activo);
        }

        [Fact]
        public async Task UpdateAsync_Path8_NullDate_ClearsField()
        {
            var guid = Guid.NewGuid();
            var fechaOriginal = new DateTime(1990, 1, 1);
            var dto = new HuespedUpdateDto { Fecha_Nacimiento = null }; 
            var entity = new Huesped { ID = guid.ToByteArray(), Fecha_Nacimiento = fechaOriginal };

            _validatorMock.Setup(v => v.ValidateUpdateAsync(It.IsAny<string>(), dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(entity);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Huesped>())).ReturnsAsync(entity);

            await _service.UpdateAsync(guid.ToString(), dto);

            Assert.Equal(fechaOriginal, entity.Fecha_Nacimiento);
        }

        #endregion

        #region Pruebas de DeleteAsync 

        [Fact]
        public async Task DeleteAsync_Path1_ValidationFails_ThrowsException()
        {
            var id = Guid.NewGuid().ToString();
            _validatorMock.Setup(v => v.ValidateDeleteAsync(id))
                          .ThrowsAsync(new ValidationException(new Dictionary<string, List<string>> { { "General", new List<string> { "Huésped con deuda" } } }));

            await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(id));
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Path2_Success_ReturnsTrue()
        {
            var guid = Guid.NewGuid();
            _validatorMock.Setup(v => v.ValidateDeleteAsync(guid.ToString())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<byte[]>())).ReturnsAsync(true);

            var result = await _service.DeleteAsync(guid.ToString());

            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<byte[]>()), Times.Once);
        }

        #endregion
    }
}