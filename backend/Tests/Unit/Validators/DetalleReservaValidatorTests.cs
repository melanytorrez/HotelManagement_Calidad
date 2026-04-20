using Xunit;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Datos.Config;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Aplicacion.Exceptions;
using HotelManagement.DTOs;
using HotelManagement.Models;

namespace HotelManagement.Tests.Unit.Validators
{
    public class DetalleReservaValidatorTests : IDisposable
    {
        private readonly HotelDbContext _context;
        private readonly DetalleReservaValidator _validator;

        public DetalleReservaValidatorTests()
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: $"DetalleReservaValidatorDb_{Guid.NewGuid()}")
                .Options;

            _context   = new HotelDbContext(options);
            _validator = new DetalleReservaValidator(_context);
        }

        // HELPERS

        private static byte[] ToMySqlBytes(Guid guid)
        {
            var bytes = guid.ToByteArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 0, 4);
                Array.Reverse(bytes, 4, 2);
                Array.Reverse(bytes, 6, 2);
            }
            return bytes;
        }

        private async Task<(Guid reservaId, Guid habitacionId, Guid huespedId)> SeedEntidadesValidasAsync()
        {
            var clienteId     = Guid.NewGuid();
            var tipoHabId     = Guid.NewGuid();
            var reservaId     = Guid.NewGuid();
            var habitacionId  = Guid.NewGuid();
            var huespedId     = Guid.NewGuid();

            _context.Clientes.Add(new Cliente
            {
                ID           = ToMySqlBytes(clienteId),
                Razon_Social = "CLIENTE TEST",
                NIT          = "12345678",
                Email        = "test@hotel.com",
                Activo       = true,
                Fecha_Creacion      = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now
            });

            _context.TipoHabitaciones.Add(new TipoHabitacion
            {
                ID         = ToMySqlBytes(tipoHabId),
                Nombre     = "Suite",
                Precio_Base       = 200m,
                Capacidad_Maxima  = 2
            });

            _context.Reservas.Add(new Reserva
            {
                ID             = ToMySqlBytes(reservaId),
                Cliente_ID     = ToMySqlBytes(clienteId),
                Estado_Reserva = "Pendiente",
                Monto_Total    = 400m,
                Fecha_Creacion = DateTime.Now
            });

            _context.Habitaciones.Add(new Habitacion
            {
                ID                  = ToMySqlBytes(habitacionId),
                Tipo_Habitacion_ID  = ToMySqlBytes(tipoHabId),
                Numero_Habitacion   = "101",
                Piso                = 1,
                Estado_Habitacion   = "Libre",
                Fecha_Creacion      = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now
            });

            _context.Huespedes.Add(new Huesped
            {
                ID                  = ToMySqlBytes(huespedId),
                Nombre              = "Juan",
                Apellido            = "Perez",
                Documento_Identidad = "12345678",
                Activo              = true,
                Fecha_Creacion      = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return (reservaId, habitacionId, huespedId);
        }


        // ValidateCreateAsync — VALIDACIÓN DE UUIDs

        [Fact]
        public async Task ValidateCreateAsync_ReservaIdInvalido_LanzaValidationException()
        {
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = "no-es-guid",
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("reserva_ID"));
        }

        [Fact]
        public async Task ValidateCreateAsync_HabitacionIdInvalido_LanzaValidationException()
        {
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = Guid.NewGuid().ToString(),
                Habitacion_ID = "no-es-guid",
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("habitacion_ID"));
        }

        [Fact]
        public async Task ValidateCreateAsync_HuespedIdInvalido_LanzaValidationException()
        {
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = Guid.NewGuid().ToString(),
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = "no-es-guid",
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("huesped_ID"));
        }

        [Fact]
        public async Task ValidateCreateAsync_TodosLosIdsInvalidos_CapturaMultiplesErrores()
        {
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = "xxx",
                Habitacion_ID = "yyy",
                Huesped_ID    = "zzz",
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("reserva_ID"));
            Assert.True(ex.Errors.ContainsKey("habitacion_ID"));
            Assert.True(ex.Errors.ContainsKey("huesped_ID"));
        }

        
        // ValidateCreateAsync — VALIDACIÓN DE FECHAS
        [Fact]
        public async Task ValidateCreateAsync_FechaEntradaMuyAnteriorAHoy_LanzaValidationException()
        {
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = Guid.NewGuid().ToString(),
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = DateTime.Today.AddDays(-3),
                Fecha_Salida  = DateTime.Today.AddDays(1)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("fecha_Entrada"));
            Assert.Contains("anterior", ex.Errors["fecha_Entrada"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_FechaSalidaIgualAEntrada_LanzaValidationException()
        {
        
            var fechaHoy = DateTime.Today.AddDays(2);
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = Guid.NewGuid().ToString(),
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = fechaHoy,
                Fecha_Salida  = fechaHoy
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("fecha_Salida"));
            Assert.Contains("posterior", ex.Errors["fecha_Salida"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_FechaSalidaAnteriorAEntrada_LanzaValidationException()
        {
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = Guid.NewGuid().ToString(),
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = DateTime.Today.AddDays(5),
                Fecha_Salida  = DateTime.Today.AddDays(2)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("fecha_Salida"));
        }

        // ValidateCreateAsync — EXISTENCIA DE ENTIDADES EN BD

        [Fact]
        public async Task ValidateCreateAsync_ReservaNoExiste_LanzaValidationException()
        {
            
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = Guid.NewGuid().ToString(), 
                Habitacion_ID = Guid.NewGuid().ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("reserva_ID"));
            Assert.Contains("No existe", ex.Errors["reserva_ID"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_HabitacionNoExiste_LanzaValidationException()
        {
            var (reservaId, _, _) = await SeedEntidadesValidasAsync();
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = reservaId.ToString(),
                Habitacion_ID = Guid.NewGuid().ToString(), // inexistente
                Huesped_ID    = Guid.NewGuid().ToString(),
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("habitacion_ID"));
            Assert.Contains("No existe", ex.Errors["habitacion_ID"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_HuespedNoExiste_LanzaValidationException()
        {
            var (reservaId, habitacionId, _) = await SeedEntidadesValidasAsync();
            var dto = new DetalleReservaCreateDto
            {
                Reserva_ID    = reservaId.ToString(),
                Habitacion_ID = habitacionId.ToString(),
                Huesped_ID    = Guid.NewGuid().ToString(), // inexistente
                Fecha_Entrada = DateTime.Today.AddDays(1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));

            Assert.True(ex.Errors.ContainsKey("huesped_ID"));
            Assert.Contains("No existe", ex.Errors["huesped_ID"].First());
        }

       
        // ValidateUpdateAsync

        [Fact]
        public async Task ValidateUpdateAsync_IdInvalido_LanzaValidationException()
        {

            var dto = new DetalleReservaUpdateDto();

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync("no-es-guid", dto));

            Assert.True(ex.Errors.ContainsKey("id"));
        }

        [Fact]
        public async Task ValidateUpdateAsync_FechaEntradaEnElPasado_LanzaValidationException()
        {
            var dto = new DetalleReservaUpdateDto
            {
                Fecha_Entrada = DateTime.Today.AddDays(-1),
                Fecha_Salida  = DateTime.Today.AddDays(3)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(Guid.NewGuid().ToString(), dto));

            Assert.True(ex.Errors.ContainsKey("fecha_Entrada"));
        }

        [Fact]
        public async Task ValidateUpdateAsync_FechaSalidaAnteriorAEntrada_LanzaValidationException()
        {

            var dto = new DetalleReservaUpdateDto
            {
                Fecha_Entrada = DateTime.Today.AddDays(5),
                Fecha_Salida  = DateTime.Today.AddDays(2)
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(Guid.NewGuid().ToString(), dto));

            Assert.True(ex.Errors.ContainsKey("fecha_Salida"));
            Assert.Contains("posterior", ex.Errors["fecha_Salida"].First());
        }

        [Fact]
        public async Task ValidateUpdateAsync_HabitacionIdInvalido_LanzaValidationException()
        {
            var dto = new DetalleReservaUpdateDto { Habitacion_ID = "no-es-guid" };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(Guid.NewGuid().ToString(), dto));

            Assert.True(ex.Errors.ContainsKey("habitacion_ID"));
        }

        [Fact]
        public async Task ValidateUpdateAsync_HabitacionNoExiste_LanzaValidationException()
        {
        
            var dto = new DetalleReservaUpdateDto
            {
                Habitacion_ID = Guid.NewGuid().ToString()
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(Guid.NewGuid().ToString(), dto));

            Assert.True(ex.Errors.ContainsKey("habitacion_ID"));
            Assert.Contains("No existe", ex.Errors["habitacion_ID"].First());
        }

        [Fact]
        public async Task ValidateUpdateAsync_HuespedIdInvalido_LanzaValidationException()
        {

            var dto = new DetalleReservaUpdateDto { Huesped_ID = "no-es-guid" };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(Guid.NewGuid().ToString(), dto));

            Assert.True(ex.Errors.ContainsKey("huesped_ID"));
        }

        [Fact]
        public async Task ValidateUpdateAsync_HuespedNoExiste_LanzaValidationException()
        {
            
            var dto = new DetalleReservaUpdateDto { Huesped_ID = Guid.NewGuid().ToString() };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(Guid.NewGuid().ToString(), dto));

            Assert.True(ex.Errors.ContainsKey("huesped_ID"));
            Assert.Contains("No existe", ex.Errors["huesped_ID"].First());
        }


        // ValidateDeleteAsync

        [Fact]
        public async Task ValidateDeleteAsync_IdInvalido_LanzaBadRequestException()
        {
            await Assert.ThrowsAsync<BadRequestException>(
                () => _validator.ValidateDeleteAsync("no-es-guid"));
        }

        [Fact]
        public async Task ValidateDeleteAsync_DetalleInexistente_LanzaNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(
                () => _validator.ValidateDeleteAsync(Guid.NewGuid().ToString()));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
