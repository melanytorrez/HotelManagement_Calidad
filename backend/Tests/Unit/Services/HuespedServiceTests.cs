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
    }
}