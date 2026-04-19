using Xunit;
using Moq;
using HotelManagement.Services;
using HotelManagement.Repositories;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Aplicacion.Exceptions;
using HotelManagement.DTOs;
using HotelManagement.Models;

namespace HotelManagement.Tests.Unit.Services
{
    public class HabitacionServiceTests
    {
        private readonly Mock<IHabitacionRepository> _repoMock;
        private readonly Mock<IHabitacionValidator> _validatorMock;
        private readonly HabitacionService _service;

        public HabitacionServiceTests()
        {
            _repoMock      = new Mock<IHabitacionRepository>();
            _validatorMock = new Mock<IHabitacionValidator>();
            _service       = new HabitacionService(_repoMock.Object, _validatorMock.Object);
        }

        // HELPER
        
        private static Habitacion BuildHabitacion(
            byte[]? id = null,
            string numero = "101",
            short piso = 1,
            string estado = "Libre")
        {
            return new Habitacion
            {
                ID                   = id ?? Guid.NewGuid().ToByteArray(),
                Tipo_Habitacion_ID   = Guid.NewGuid().ToByteArray(),
                Numero_Habitacion    = numero,
                Piso                 = piso,
                Estado_Habitacion    = estado,
                Fecha_Creacion       = DateTime.Now,
                Fecha_Actualizacion  = DateTime.Now
            };
        }

        // GetAllAsync

        [Fact]
        public async Task GetAllAsync_RepositorioConHabitaciones_RetornaListaDeDtos()
        {
            var habitaciones = new List<Habitacion>
            {
                BuildHabitacion(numero: "101", piso: 1),
                BuildHabitacion(numero: "201", piso: 2),
                BuildHabitacion(numero: "301", piso: 3)
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(habitaciones);

            var result = await _service.GetAllAsync();

            Assert.Equal(3, result.Count());
            Assert.Contains(result, h => h.Numero_Habitacion == "101");
            Assert.Contains(result, h => h.Numero_Habitacion == "201");
        }

        [Fact]
        public async Task GetAllAsync_RepositorioVacio_RetornaListaVacia()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Habitacion>());

            var result = await _service.GetAllAsync();

            Assert.Empty(result);
        }

        // GetByIdAsync
        
        [Fact]
        public async Task GetByIdAsync_GuidInvalido_LanzaBadRequestException()
        {
            var idInvalido = "no-es-un-guid";

            await Assert.ThrowsAsync<BadRequestException>(
                () => _service.GetByIdAsync(idInvalido));
        }

        [Fact]
        public async Task GetByIdAsync_HabitacionNoExiste_LanzaNotFoundException()
        {
            var idValido = Guid.NewGuid().ToString();
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((Habitacion?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.GetByIdAsync(idValido));
        }

        [Fact]
        public async Task GetByIdAsync_HabitacionExiste_RetornaDto()
        {
            var id          = Guid.NewGuid();
            var habitacion  = BuildHabitacion(id: id.ToByteArray(), numero: "105", piso: 1, estado: "Libre");
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync(habitacion);

            var result = await _service.GetByIdAsync(id.ToString());
            Assert.NotNull(result);
            Assert.Equal("105", result.Numero_Habitacion);
            Assert.Equal(1, result.Piso);
            Assert.Equal("Libre", result.Estado_Habitacion);
        }

        [Fact]
        public async Task GetByIdAsync_HabitacionExiste_IdMapeadoCorrectamente()
        {
            var id         = Guid.NewGuid();
            var habitacion = BuildHabitacion(id: id.ToByteArray());
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync(habitacion);

            var result = await _service.GetByIdAsync(id.ToString());

            Assert.Equal(id.ToString(), result.ID);
        }

        // CreateAsync

        [Fact]
        public async Task CreateAsync_DatosValidos_LlamaValidatorYRepositorio()
        {
            var dto = new HabitacionCreateDto
            {
                Tipo_Habitacion_ID = Guid.NewGuid().ToString(),
                Numero_Habitacion  = "102",
                Piso               = 1,
                Estado_Habitacion  = "Libre"
            };
            var habitacionCreada = BuildHabitacion(numero: "102", piso: 1);
            _validatorMock.Setup(v => v.ValidateCreateAsync(dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Habitacion>()))
                     .ReturnsAsync(habitacionCreada);

            var result = await _service.CreateAsync(dto);

            _validatorMock.Verify(v => v.ValidateCreateAsync(dto), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Habitacion>()), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateAsync_DatosValidos_RetornaDtoConCamposCorrectos()
        {
            var tipoId = Guid.NewGuid();
            var dto = new HabitacionCreateDto
            {
                Tipo_Habitacion_ID = tipoId.ToString(),
                Numero_Habitacion  = "103",
                Piso               = 2,
                Estado_Habitacion  = "Disponible"
            };
            var habitacionCreada = new Habitacion
            {
                ID                  = Guid.NewGuid().ToByteArray(),
                Tipo_Habitacion_ID  = tipoId.ToByteArray(),
                Numero_Habitacion   = "103",
                Piso                = 2,
                Estado_Habitacion   = "Disponible",
                Fecha_Creacion      = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now
            };
            _validatorMock.Setup(v => v.ValidateCreateAsync(dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Habitacion>()))
                     .ReturnsAsync(habitacionCreada);

            var result = await _service.CreateAsync(dto);

            Assert.Equal("103", result.Numero_Habitacion);
            Assert.Equal(2, result.Piso);
            Assert.Equal("Disponible", result.Estado_Habitacion);
        }

        // UpdateAsync

        [Fact]
        public async Task UpdateAsync_HabitacionNoExiste_LanzaNotFoundException()
        {
            var id  = Guid.NewGuid().ToString();
            var dto = new HabitacionUpdateDto { Numero_Habitacion = "999", Piso = 9, Estado_Habitacion = "Libre", Tipo_Habitacion_ID = Guid.NewGuid().ToString() };
            _validatorMock.Setup(v => v.ValidateUpdateAsync(It.IsAny<string>(), It.IsAny<HabitacionCreateDto>()))
                          .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((Habitacion?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_HabitacionExiste_ActualizaYRetornaDto()
        {
            var id          = Guid.NewGuid();
            var habitacion  = BuildHabitacion(id: id.ToByteArray(), numero: "101", piso: 1, estado: "Libre");
            var dto = new HabitacionUpdateDto
            {
                Numero_Habitacion  = "101-B",
                Piso               = 1,
                Estado_Habitacion  = "Ocupada",
                Tipo_Habitacion_ID = Guid.NewGuid().ToString()
            };
            var habitacionActualizada = BuildHabitacion(id: id.ToByteArray(), numero: "101-B", piso: 1, estado: "Ocupada");

            _validatorMock.Setup(v => v.ValidateUpdateAsync(It.IsAny<string>(), It.IsAny<HabitacionCreateDto>()))
                          .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(habitacion);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Habitacion>())).ReturnsAsync(habitacionActualizada);

            var result = await _service.UpdateAsync(id.ToString(), dto);

            Assert.Equal("101-B", result.Numero_Habitacion);
            Assert.Equal("Ocupada", result.Estado_Habitacion);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Habitacion>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_CamposNulos_NoSobrescribeValoresExistentes()
        {
            var id         = Guid.NewGuid();
            var habitacion = BuildHabitacion(id: id.ToByteArray(), numero: "202", piso: 2, estado: "Libre");
            var dto        = new HabitacionUpdateDto { Estado_Habitacion = "Mantenimiento" };
            var habitacionActualizada = BuildHabitacion(id: id.ToByteArray(), numero: "202", piso: 2, estado: "Mantenimiento");

            _validatorMock.Setup(v => v.ValidateUpdateAsync(It.IsAny<string>(), It.IsAny<HabitacionCreateDto>()))
                          .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(habitacion);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Habitacion>())).ReturnsAsync(habitacionActualizada);

            var result = await _service.UpdateAsync(id.ToString(), dto);
            Assert.Equal("202", result.Numero_Habitacion);
            Assert.Equal("Mantenimiento", result.Estado_Habitacion);
        }

       
        // PartialUpdateAsync

        [Fact]
        public async Task PartialUpdateAsync_HabitacionNoExiste_LanzaNotFoundException()
        {
            var id  = Guid.NewGuid().ToString();
            var dto = new HabitacionUpdateDto { Estado_Habitacion = "Ocupada" };
            _validatorMock.Setup(v => v.ValidatePartialUpdateAsync(It.IsAny<string>(), It.IsAny<HabitacionUpdateDto>()))
                          .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((Habitacion?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.PartialUpdateAsync(id, dto));
        }

        [Fact]
        public async Task PartialUpdateAsync_SoloEstado_ActualizaSoloEseCampo()
        {
        
            var id         = Guid.NewGuid();
            var habitacion = BuildHabitacion(id: id.ToByteArray(), numero: "303", piso: 3, estado: "Libre");
            var dto        = new HabitacionUpdateDto { Estado_Habitacion = "Reservada" };
            var habitacionActualizada = BuildHabitacion(id: id.ToByteArray(), numero: "303", piso: 3, estado: "Reservada");

            _validatorMock.Setup(v => v.ValidatePartialUpdateAsync(It.IsAny<string>(), It.IsAny<HabitacionUpdateDto>()))
                          .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(habitacion);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Habitacion>())).ReturnsAsync(habitacionActualizada);


            var result = await _service.PartialUpdateAsync(id.ToString(), dto);

            Assert.Equal("303", result.Numero_Habitacion);
            Assert.Equal("Reservada", result.Estado_Habitacion);
        }

        [Fact]
        public async Task PartialUpdateAsync_DtoVacio_NoModificaNada()
        {
            var id         = Guid.NewGuid();
            var habitacion = BuildHabitacion(id: id.ToByteArray(), numero: "404", piso: 4, estado: "Libre");
            var dto        = new HabitacionUpdateDto();
            var habitacionSinCambios = BuildHabitacion(id: id.ToByteArray(), numero: "404", piso: 4, estado: "Libre");

            _validatorMock.Setup(v => v.ValidatePartialUpdateAsync(It.IsAny<string>(), It.IsAny<HabitacionUpdateDto>()))
                          .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(habitacion);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Habitacion>())).ReturnsAsync(habitacionSinCambios);

            var result = await _service.PartialUpdateAsync(id.ToString(), dto);

            Assert.Equal("404", result.Numero_Habitacion);
            Assert.Equal(4, result.Piso);
            Assert.Equal("Libre", result.Estado_Habitacion);
        }

        // DeleteAsync

        [Fact]
        public async Task DeleteAsync_HabitacionExiste_LlamaValidatorYRetornaTrue()
        {
            var id = Guid.NewGuid().ToString();
            _validatorMock.Setup(v => v.ValidateDeleteAsync(id)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<byte[]>())).ReturnsAsync(true);

            var result = await _service.DeleteAsync(id);

            _validatorMock.Verify(v => v.ValidateDeleteAsync(id), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_HabitacionNoExiste_RetornaFalse()
        {
            var id = Guid.NewGuid().ToString();
            _validatorMock.Setup(v => v.ValidateDeleteAsync(id)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<byte[]>())).ReturnsAsync(false);

            var result = await _service.DeleteAsync(id);

            Assert.False(result);
        }

        // Métodos Stub (comportamiento documentado
        [Fact]
        public async Task IsHabitacionAvailableAsync_Stub_SiempreRetornaTrue()
        {
            var id = Guid.NewGuid().ToString();

            var result = await _service.IsHabitacionAvailableAsync(
                id, DateTime.Now, DateTime.Now.AddDays(3));

            Assert.True(result);
        }

        [Fact]
        public async Task GetByTipoHabitacionIdAsync_Stub_SiempreRetornaColeccionVacia()
        {
            var tipoId = Guid.NewGuid().ToString();

            var result = await _service.GetByTipoHabitacionIdAsync(tipoId);

            Assert.Empty(result);
        }
    }
}
