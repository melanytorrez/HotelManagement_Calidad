using Moq;
using Xunit;
using HotelManagement.Aplicacion.Exceptions;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Application.Services;
using HotelManagement.Repositories;
using HotelManagement.Datos.Repositories;
using HotelManagement.Datos.Config;
using HotelManagement.Models;
using HotelManagement.DTOs;
using System;
using System.Threading.Tasks;

namespace HotelManagement.Tests.Unit.Services
{
    public class ReservaServiceTests : IDisposable
    {
        private readonly Mock<IReservaRepository> _reservaRepoMock;
        private readonly Mock<IClienteRepository> _clienteRepoMock;
        private readonly HotelDbContext _context;
        private readonly ReservaService _service;

        public ReservaServiceTests()
        {
            // 1. Instanciar los Mocks de los Repositorios
            _reservaRepoMock = new Mock<IReservaRepository>();
            _clienteRepoMock = new Mock<IClienteRepository>();

            // 2. Crear un Context en Memoria (es necesario porque el GetAll hace consultas directas)
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: $"ReservaServiceTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new HotelDbContext(options);

            // 3. Inyectar todo al Servicio Real
            _service = new ReservaService(
                _reservaRepoMock.Object, 
                _clienteRepoMock.Object, 
                _context);
        }

        #region Pruebas de GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_ReservaNoExiste_RetornaNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _reservaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                            .ReturnsAsync((Reserva)null!);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ReservaExiste_RetornaReservaDTO()
        {
            // Arrange
            var idReserva = Guid.NewGuid();
            var idCliente = Guid.NewGuid();

            var reservaEntity = new Reserva
            {
                ID = idReserva.ToByteArray(),
                Cliente_ID = idCliente.ToByteArray(),
                Fecha_Creacion = DateTime.Now,
                Estado_Reserva = "Confirmada",
                Monto_Total = 1500.50M,
                Cliente = new Cliente { Razon_Social = "Empresa Hotelera" } // Mockeando la asociación
            };

            _reservaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                            .ReturnsAsync(reservaEntity);

            // Act
            var result = await _service.GetByIdAsync(idReserva);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(idReserva.ToString(), result.ID);
            Assert.Equal("Empresa Hotelera", result.Cliente_Nombre);
            Assert.Equal("Confirmada", result.Estado_Reserva);
            Assert.Equal(1500.50M, result.Monto_Total);
        }

        #endregion

        #region Pruebas de DeleteAsync

        [Fact]
        public async Task DeleteAsync_ReservaNoExiste_RetornaFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _reservaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>()))
                            .ReturnsAsync((Reserva)null!);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.False(result); // En vez de lanzar Exception, devuelve false amablemente
            _reservaRepoMock.Verify(r => r.DeleteAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ReservaExiste_EliminaYRetornaTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new Reserva { ID = id.ToByteArray() };

            _reservaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(entity);
            _reservaRepoMock.Setup(r => r.DeleteAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);
            _reservaRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.True(result);
            _reservaRepoMock.Verify(r => r.DeleteAsync(It.IsAny<byte[]>()), Times.Once);
            _reservaRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region Pruebas de GetAllAsync

        [Fact]
        public async Task GetAllAsync_ListaVacia_RetornaListaVaciaDto()
        {
            // Arrange
            _reservaRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Reserva>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ConDatos_MapeaCorrectamenteYBuscaDetalles()
        {
            // Arrange
            var reservaId = Guid.NewGuid();
            var reservaBytes = reservaId.ToByteArray();
            var fechaEntradaReal = DateTime.Now.AddDays(1);

            var reservaEntity = new Reserva
            {
                ID = reservaBytes,
                Cliente_ID = Guid.NewGuid().ToByteArray(),
                Monto_Total = 500,
                Cliente = new Cliente { Razon_Social = "Juan Perez" }
            };

            // Insertamos un detalle real en la BD en memoria para simular el cruce de tablas
            _context.DetalleReservas.Add(new DetalleReserva
            {
                ID = Guid.NewGuid().ToByteArray(),
                Reserva_ID = reservaBytes,
                Huesped_ID = Guid.NewGuid().ToByteArray(),
                Habitacion_ID = Guid.NewGuid().ToByteArray(),
                Fecha_Entrada = fechaEntradaReal,
                Fecha_Salida = fechaEntradaReal.AddDays(3)
            });
            await _context.SaveChangesAsync();

            _reservaRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Reserva> { reservaEntity });

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Single(result);
            var first = result.GetEnumerator();
            first.MoveNext();
            
            Assert.Equal("Juan Perez", first.Current.Cliente_Nombre);
            Assert.Equal(fechaEntradaReal, first.Current.Fecha_Entrada);
            Assert.Equal(500, first.Current.Monto_Total);
        }

        #endregion

        #region Pruebas de AddAsync

        [Fact]
        public async Task AddAsync_MontoNegativo_LanzaValidationException()
        {
            var dto = new ReservaCreateDTO { Monto_Total = -50 };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.AddAsync(dto));
            Assert.True(ex.Errors.ContainsKey("Monto_Total"));
            Assert.Contains("mayor o igual a cero", ex.Errors["Monto_Total"][0]);
        }

        [Fact]
        public async Task AddAsync_ClienteNoExiste_LanzaNotFoundException()
        {
            var dto = new ReservaCreateDTO { Cliente_ID = Guid.NewGuid().ToString(), Monto_Total = 100 };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync((Cliente)null!);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.AddAsync(dto));
            Assert.Contains("No se encontró un cliente", ex.Message);
        }

        [Fact]
        public async Task AddAsync_DatosValidos_LlamaRepositoryAdd()
        {
            var dto = new ReservaCreateDTO { Cliente_ID = Guid.NewGuid().ToString(), Monto_Total = 200, Estado_Reserva = "Nueva" };
            
            // Simulamos que el cliente sí existe
            _clienteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(new Cliente());
            
            _reservaRepoMock.Setup(r => r.AddAsync(It.IsAny<Reserva>())).Returns(Task.CompletedTask);
            _reservaRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.AddAsync(dto);

            _reservaRepoMock.Verify(r => r.AddAsync(It.Is<Reserva>(res => res.Monto_Total == 200 && res.Estado_Reserva == "Nueva")), Times.Once);
            _reservaRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region Pruebas de UpdateAsync

        [Fact]
        public async Task UpdateAsync_ReservaNoExiste_RetornaFalse()
        {
            _reservaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync((Reserva)null!);

            var result = await _service.UpdateAsync(Guid.NewGuid(), new ReservaUpdateDTO());

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ClienteNuevoNoExiste_LanzaNotFoundException()
        {
            // Reserva existe
            _reservaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(new Reserva());
            // Cliente nuevo NO existe
            _clienteRepoMock.Setup(c => c.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync((Cliente)null!);

            var dto = new ReservaUpdateDTO { Cliente_ID = Guid.NewGuid().ToString() };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdateAsync_ActualizacionParcial_SinCambiarCliente_ActualizaYRetornaTrue()
        {
            var reservaOriginal = new Reserva { Monto_Total = 100, Estado_Reserva = "Viejo" };
            _reservaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(reservaOriginal);

            // DTO SIN cliente
            var dto = new ReservaUpdateDTO { Monto_Total = 300, Estado_Reserva = "Actualizado" };

            var result = await _service.UpdateAsync(Guid.NewGuid(), dto);

            Assert.True(result);
            Assert.Equal(300, reservaOriginal.Monto_Total);
            Assert.Equal("Actualizado", reservaOriginal.Estado_Reserva);
            
            // Nunca debió buscar al cliente porque no se envió
            _clienteRepoMock.Verify(c => c.GetByIdAsync(It.IsAny<byte[]>()), Times.Never);
            _reservaRepoMock.Verify(r => r.UpdateAsync(reservaOriginal), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ActualizacionCompleta_ActualizaYRetornaTrue()
        {
            var reservaOriginal = new Reserva { Cliente_ID = Guid.NewGuid().ToByteArray() };
            var nuevoClienteId = Guid.NewGuid();

            _reservaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(reservaOriginal);
            _clienteRepoMock.Setup(c => c.GetByIdAsync(It.IsAny<byte[]>())).ReturnsAsync(new Cliente()); // El nuevo cliente SI existe

            var dto = new ReservaUpdateDTO { Cliente_ID = nuevoClienteId.ToString() };

            var result = await _service.UpdateAsync(Guid.NewGuid(), dto);

            Assert.True(result);
            Assert.Equal(nuevoClienteId.ToByteArray(), reservaOriginal.Cliente_ID); // Se debió reasignar el arreglo de bytes
        }

        #endregion

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
