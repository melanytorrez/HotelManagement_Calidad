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
    public class DetalleReservaServiceTests
    {
        private readonly Mock<IDetalleReservaRepository> _repoMock;
        private readonly Mock<IDetalleReservaValidator>  _validatorMock;
        private readonly DetalleReservaService           _service;

        public DetalleReservaServiceTests()
        {
            _repoMock      = new Mock<IDetalleReservaRepository>();
            _validatorMock = new Mock<IDetalleReservaValidator>();
            _service       = new DetalleReservaService(_repoMock.Object, _validatorMock.Object);
        }

        // HELPER — construye una entidad DetalleReserva con navegación

        private static DetalleReserva BuildDetalle(
            byte[]? id          = null,
            Huesped? huesped    = null,
            Habitacion? hab     = null)
        {
            return new DetalleReserva
            {
                ID            = id ?? Guid.NewGuid().ToByteArray(),
                Reserva_ID    = Guid.NewGuid().ToByteArray(),
                Habitacion_ID = Guid.NewGuid().ToByteArray(),
                Huesped_ID    = Guid.NewGuid().ToByteArray(),
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3),
                Huesped       = huesped,
                Habitacion    = hab
            };
        }

        // GetAllAsync

        [Fact]
        public async Task GetAllAsync_RetornaListaMapeada()
        {
            var lista = new List<DetalleReserva> { BuildDetalle(), BuildDetalle() };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(lista);

            var result = await _service.GetAllAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ListaVacia_RetornaVacia()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DetalleReserva>());

            var result = await _service.GetAllAsync();

            Assert.Empty(result);
        }

        // GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_DetalleExiste_RetornaDto()
        {
            var id      = Guid.NewGuid();
            var detalle = BuildDetalle(id: id.ToByteArray());
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(detalle);

            var result = await _service.GetByIdAsync(id.ToString());

            Assert.NotNull(result);
            Assert.Equal(id.ToString(), result.ID);
        }

        [Fact]
        public async Task GetByIdAsync_DetalleNoExiste_LanzaNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((DetalleReserva?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.GetByIdAsync(Guid.NewGuid().ToString()));
        }

        // CreateAsync

        [Fact]
        public async Task CreateAsync_DatosValidos_LlamaValidatorYRepositorio()
        {
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = Guid.NewGuid().ToString(),
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };
            var creado = BuildDetalle();
            _validatorMock.Setup(v => v.ValidateCreateAsync(dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<DetalleReserva>())).ReturnsAsync(creado);

            var result = await _service.CreateAsync(dto);

            _validatorMock.Verify(v => v.ValidateCreateAsync(dto), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<DetalleReserva>()), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateAsync_DatosValidos_RetornaDtoConFechasCorrectas()
        {
            var entrada = DateTime.Today.AddDays(2);
            var salida  = DateTime.Today.AddDays(5);
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = Guid.NewGuid().ToString(),
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = entrada,
                Fecha_Salida  = salida
            };
            var creado = BuildDetalle();
            creado.Fecha_Entrada = entrada;
            creado.Fecha_Salida  = salida;

            _validatorMock.Setup(v => v.ValidateCreateAsync(dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<DetalleReserva>())).ReturnsAsync(creado);

            var result = await _service.CreateAsync(dto);

            Assert.Equal(entrada, result.Fecha_Entrada);
            Assert.Equal(salida,  result.Fecha_Salida);
        }

        // UpdateAsync

        [Fact]
        public async Task UpdateAsync_DetalleNoExiste_LanzaNotFoundException()
        {
            var id  = Guid.NewGuid().ToString();
            var dto = new DetalleReservaUpdateDto();
            _validatorMock.Setup(v => v.ValidateUpdateAsync(id, dto)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync((DetalleReserva?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_TodosLosCampos_ActualizaYRetornaDto()
        {
            var id      = Guid.NewGuid();
            var detalle = BuildDetalle(id: id.ToByteArray());
            var dto = new DetalleReservaUpdateDto
            {
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = DateTime.Today.AddDays(2),
                Fecha_Salida  = DateTime.Today.AddDays(4)
            };
            var actualizado = BuildDetalle(id: id.ToByteArray());

            _validatorMock.Setup(v => v.ValidateUpdateAsync(id.ToString(), dto))
                          .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(detalle);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<DetalleReserva>())).ReturnsAsync(actualizado);

            var result = await _service.UpdateAsync(id.ToString(), dto);

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<DetalleReserva>()), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_DtoVacio_NoSobrescribeNingunCampo()
        {
            var id      = Guid.NewGuid();
            var detalle = BuildDetalle(id: id.ToByteArray());
            var dto     = new DetalleReservaUpdateDto();

            _validatorMock.Setup(v => v.ValidateUpdateAsync(id.ToString(), dto))
                          .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(detalle);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<DetalleReserva>())).ReturnsAsync(detalle);

            var result = await _service.UpdateAsync(id.ToString(), dto);

            Assert.NotNull(result);
        }

        // DeleteAsync

        [Fact]
        public async Task DeleteAsync_Exitoso_LlamaValidatorYRetornaTrue()
        {
            var id = Guid.NewGuid().ToString();
            _validatorMock.Setup(v => v.ValidateDeleteAsync(id)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<byte[]>())).ReturnsAsync(true);

            var result = await _service.DeleteAsync(id);

            _validatorMock.Verify(v => v.ValidateDeleteAsync(id), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_NoExiste_RetornaFalse()
        {
            var id = Guid.NewGuid().ToString();
            _validatorMock.Setup(v => v.ValidateDeleteAsync(id)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<byte[]>())).ReturnsAsync(false);

            var result = await _service.DeleteAsync(id);

            Assert.False(result);
        }

        // GetByReservaIdAsync

        [Fact]
        public async Task GetByReservaIdAsync_RetornaDetallesDeEsaReserva()
        {
            var reservaId = Guid.NewGuid();
            var lista = new List<DetalleReserva> { BuildDetalle(), BuildDetalle() };
            _repoMock.Setup(r => r.GetByReservaIdAsync(It.IsAny<byte[]>())).ReturnsAsync(lista);

            var result = await _service.GetByReservaIdAsync(reservaId.ToString());

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByReservaIdAsync_SinDetalles_RetornaListaVacia()
        {

            _repoMock.Setup(r => r.GetByReservaIdAsync(It.IsAny<byte[]>()))
                     .ReturnsAsync(new List<DetalleReserva>());

            var result = await _service.GetByReservaIdAsync(Guid.NewGuid().ToString());

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetAllAsync_HuespedNull_NombreHuespedEsVacio()
        {
            var detalle = BuildDetalle(huesped: null);
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DetalleReserva> { detalle });

            var result = await _service.GetAllAsync();

            Assert.Equal(string.Empty, result.First().Nombre_Huesped);
        }

        [Fact]
        public async Task GetAllAsync_HuespedSinSegundoApellido_NombreConDosPartes()
        {
            var huesped = new Huesped
            {
                ID                  = Guid.NewGuid().ToByteArray(),
                Nombre              = "Carlos",
                Apellido            = "Gomez",
                Segundo_Apellido    = null,
                Documento_Identidad = "11223344"
            };
            var detalle = BuildDetalle(huesped: huesped);
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DetalleReserva> { detalle });

            var result = await _service.GetAllAsync();

            Assert.Equal("Carlos Gomez", result.First().Nombre_Huesped);
        }

        [Fact]
        public async Task GetAllAsync_HuespedConSegundoApellido_NombreConTresPartes()
        {
            var huesped = new Huesped
            {
                ID                  = Guid.NewGuid().ToByteArray(),
                Nombre              = "Carlos",
                Apellido            = "Gomez",
                Segundo_Apellido    = "Perez",
                Documento_Identidad = "11223344"
            };
            var detalle = BuildDetalle(huesped: huesped);
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DetalleReserva> { detalle });

            var result = await _service.GetAllAsync();

            Assert.Equal("Carlos Gomez Perez", result.First().Nombre_Huesped);
        }

        [Fact]
        public async Task GetAllAsync_HabitacionConNumero_NumeroMapeadoEnDto()
        {
            var hab = new Habitacion
            {
                ID                  = Guid.NewGuid().ToByteArray(),
                Tipo_Habitacion_ID  = Guid.NewGuid().ToByteArray(),
                Numero_Habitacion   = "205",
                Piso                = 2,
                Estado_Habitacion   = "Libre",
                Fecha_Creacion      = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now
            };
            var detalle = BuildDetalle(hab: hab);
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DetalleReserva> { detalle });

            var result = await _service.GetAllAsync();

            Assert.Equal("205", result.First().Numero_Habitacion);
        }
    }
}
