using Xunit;
using HotelManagement.DTOs;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Datos.Config;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Aplicacion.Exceptions;
using HotelManagement.Models;
using System;
using System.Threading.Tasks;

namespace HotelManagement.Tests.Unit.Validators
{
    public class HuespedValidatorTests : IDisposable
    {
        private readonly HotelDbContext _context;
        private readonly HuespedValidator _validator;

        public HuespedValidatorTests()
        {
            // Creamos una base de datos en memoria para aislar los tests de la BD real
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: $"HuespedValidatorTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new HotelDbContext(options);
            _validator = new HuespedValidator(_context);
        }

        // Helper para inyectar un huésped falso en la base de datos de prueba
        private async Task<Huesped> SeedHuespedAsync(
            string documento = "12345678",
            string nombre = "Juan",
            string apellido = "Perez")
        {
            var huesped = new Huesped
            {
                ID = Guid.NewGuid().ToByteArray(),
                Nombre = nombre,
                Apellido = apellido,
                Documento_Identidad = documento,
                Activo = true,
                Fecha_Creacion = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now
            };
            _context.Huespedes.Add(huesped);
            await _context.SaveChangesAsync();
            return huesped;
        }

        // -------------------------------------------------------------
        // Tests para ValidateDeleteAsync (CC = 4 caminos distintos)
        // -------------------------------------------------------------

        [Fact]
        public async Task ValidateDeleteAsync_GuidInvalido_LanzaBadRequestException()
        {
            // Arrange
            var invalidId = "no-es-un-guid";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(
                () => _validator.ValidateDeleteAsync(invalidId));
            
            Assert.Contains("UUID válido", ex.Message);
        }

        [Fact]
        public async Task ValidateDeleteAsync_HuespedNoExiste_LanzaNotFoundException()
        {
            // Arrange
            var idInexistente = Guid.NewGuid().ToString();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(
                () => _validator.ValidateDeleteAsync(idInexistente));
                
            Assert.Contains("No se encontró el huésped", ex.Message);
        }

        [Fact]
        public async Task ValidateDeleteAsync_HuespedConDetallesReserva_LanzaConflictException()
        {
            // Arrange: Creamos un huésped y le asociamos un Detalle de Reserva
            var huesped = await SeedHuespedAsync();
            var huespedIdString = new Guid(huesped.ID).ToString();

            _context.DetalleReservas.Add(new DetalleReserva
            {
                ID = Guid.NewGuid().ToByteArray(),
                Reserva_ID = Guid.NewGuid().ToByteArray(),
                Huesped_ID = huesped.ID, // ASOCIADO
                Habitacion_ID = Guid.NewGuid().ToByteArray()
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConflictException>(
                () => _validator.ValidateDeleteAsync(huespedIdString));
                
            Assert.Contains("tiene detalles de reserva asociados", ex.Message);
        }

        [Fact]
        public async Task ValidateDeleteAsync_HuespedSinReservas_EliminacionValida()
        {
            // Arrange: Creamos un huésped sin detalles asociados
            var huesped = await SeedHuespedAsync();
            var huespedIdString = new Guid(huesped.ID).ToString();

            // Act
            var exception = await Record.ExceptionAsync(
                () => _validator.ValidateDeleteAsync(huespedIdString));

            // Assert
            Assert.Null(exception); // No se debe lanzar ninguna excepción
        }

        // -------------------------------------------------------------
        // Tests para ValidateUpdateAsync (Múltiples caminos)
        // -------------------------------------------------------------

        [Fact]
        public async Task ValidateUpdateAsync_GuidInvalido_LanzaValidationException()
        {
            var dto = new HuespedUpdateDto { Nombre = "Carlos" };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync("no-es-guid", dto));

            Assert.True(ex.Errors.ContainsKey("id"));
        }

        [Fact]
        public async Task ValidateUpdateAsync_HuespedNoExiste_LanzaNotFoundException()
        {
            var dto = new HuespedUpdateDto { Nombre = "Carlos" };
            var idInexistente = Guid.NewGuid().ToString();

            await Assert.ThrowsAsync<NotFoundException>(
                () => _validator.ValidateUpdateAsync(idInexistente, dto));
        }

        [Fact]
        public async Task ValidateUpdateAsync_DatosCompletosYValidos_ActualizacionValida()
        {
            var huesped = await SeedHuespedAsync();
            var id = new Guid(huesped.ID).ToString();

            var dto = new HuespedUpdateDto
            {
                Nombre = "Carlos",
                Apellido = "Gomez",
                Documento_Identidad = "88990011",
                Telefono = "77766655",
                Fecha_Nacimiento = "1990-01-01"
            };

            var exception = await Record.ExceptionAsync(
                () => _validator.ValidateUpdateAsync(id, dto));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidateUpdateAsync_NombreInvalidoMenoresA2Chars_LanzaValidationException()
        {
            var huesped = await SeedHuespedAsync();
            var id = new Guid(huesped.ID).ToString();
            var dto = new HuespedUpdateDto { Nombre = "C" }; // Inválido, mínimo 2

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(id, dto));

            Assert.True(ex.Errors.ContainsKey("nombre"));
        }

        [Fact]
        public async Task ValidateUpdateAsync_DocumentoDuplicadoOtroHuesped_LanzaValidationException()
        {
            // Huesped original que queremos actualizar
            var huesped1 = await SeedHuespedAsync(documento: "11111111");
            var id = new Guid(huesped1.ID).ToString();

            // OTRO huésped con el documento que intentaremos robar
            await SeedHuespedAsync(documento: "99999999");

            var dto = new HuespedUpdateDto { Documento_Identidad = "99999999" };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(id, dto));

            Assert.True(ex.Errors.ContainsKey("documento_Identidad"));
            Assert.Contains("Ya existe otro huésped", ex.Errors["documento_Identidad"][0]);
        }

        [Fact]
        public async Task ValidateUpdateAsync_DocumentoDelMismoHuesped_NoLanzaExcepcion()
        {
            var huesped = await SeedHuespedAsync(documento: "55554444");
            var id = new Guid(huesped.ID).ToString();

            // Actualizamos enviando el mismo documento que ya tiene (no debe dar error de duplicado)
            var dto = new HuespedUpdateDto { Documento_Identidad = "55554444" };

            var exception = await Record.ExceptionAsync(
                () => _validator.ValidateUpdateAsync(id, dto));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidateUpdateAsync_FechaFutura_LanzaValidationException()
        {
            var huesped = await SeedHuespedAsync();
            var id = new Guid(huesped.ID).ToString();

            // Fecha en el futuro
            var dto = new HuespedUpdateDto { Fecha_Nacimiento = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd") };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(id, dto));

            Assert.True(ex.Errors.ContainsKey("fecha_Nacimiento"));
            Assert.Contains("futura", ex.Errors["fecha_Nacimiento"][0]);
        }

        [Fact]
        public async Task ValidateUpdateAsync_MultiplesErroresAcumulados_LanzaValidationException()
        {
            var huesped = await SeedHuespedAsync();
            var id = new Guid(huesped.ID).ToString();

            var dto = new HuespedUpdateDto
            {
                Nombre = "1", // Invalido
                Fecha_Nacimiento = "9999-99-99", // Invalida
                Telefono = "a", // Invalido
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(id, dto));

            Assert.True(ex.Errors.ContainsKey("nombre"));
            Assert.True(ex.Errors.ContainsKey("fecha_Nacimiento"));
            Assert.True(ex.Errors.ContainsKey("telefono"));
        }

        // -------------------------------------------------------------
        // Tests para ValidateCreateAsync
        // -------------------------------------------------------------

        [Fact]
        public async Task ValidateCreateAsync_DatosValidos_NoLanzaExcepcion()
        {
            var dto = new HuespedCreateDto
            {
                Nombre = "Ana",
                Apellido = "Rios",
                Documento_Identidad = "77665544",
                Telefono = "77788899",
                Fecha_Nacimiento = "1995-10-10"
            };

            var exception = await Record.ExceptionAsync(() => _validator.ValidateCreateAsync(dto));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidateCreateAsync_NombreVacio_LanzaValidationException()
        {
            var dto = new HuespedCreateDto { Nombre = "", Apellido = "Rios", Documento_Identidad = "77665544" };
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nombre"));
        }

        [Fact]
        public async Task ValidateCreateAsync_NombreMenorA2Chars_LanzaValidationException()
        {
            var dto = new HuespedCreateDto { Nombre = "A", Apellido = "Rios", Documento_Identidad = "77665544" };
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nombre"));
        }

        [Fact]
        public async Task ValidateCreateAsync_NombreMayorA30Chars_LanzaValidationException()
        {
            var dto = new HuespedCreateDto { Nombre = "EstebanJulioRicardoMontoyaDeLaRosaRamirez", Apellido = "Rios", Documento_Identidad = "77665544" };
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nombre"));
        }

        [Fact]
        public async Task ValidateCreateAsync_NombreConNumeros_LanzaValidationException()
        {
            var dto = new HuespedCreateDto { Nombre = "Ana123", Apellido = "Rios", Documento_Identidad = "77665544" };
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nombre"));
        }

        [Fact]
        public async Task ValidateCreateAsync_DocumentoVacio_LanzaValidationException()
        {
            var dto = new HuespedCreateDto { Nombre = "Ana", Apellido = "Rios", Documento_Identidad = "" };
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("documento_Identidad"));
        }

        [Fact]
        public async Task ValidateCreateAsync_DocumentoDuplicado_LanzaValidationException()
        {
            await SeedHuespedAsync(documento: "99887766");
            var dto = new HuespedCreateDto { Nombre = "Ana", Apellido = "Rios", Documento_Identidad = "99887766" };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            
            Assert.True(ex.Errors.ContainsKey("documento_Identidad"));
            Assert.Contains("Ya existe un huésped", ex.Errors["documento_Identidad"][0]);
        }

        [Fact]
        public async Task ValidateCreateAsync_TelefonoInvalido_LanzaValidationException()
        {
            var dto = new HuespedCreateDto { Nombre = "Ana", Apellido = "Rios", Documento_Identidad = "77665544", Telefono = "123" };
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("telefono"));
        }

        [Fact]
        public async Task ValidateCreateAsync_FechaNacimientoInvalida_LanzaValidationException()
        {
            var dto = new HuespedCreateDto { Nombre = "Ana", Apellido = "Rios", Documento_Identidad = "77665544", Fecha_Nacimiento = "FechaMala" };
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("fecha_Nacimiento"));
        }

        [Fact]
        public async Task ValidateCreateAsync_FechaNacimientoEdadInvalida_LanzaValidationException()
        {
            // Más de 150 años
            var dto = new HuespedCreateDto { Nombre = "Ana", Apellido = "Rios", Documento_Identidad = "77665544", Fecha_Nacimiento = "1800-01-01" };
            var ex = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("fecha_Nacimiento"));
        }

        public void Dispose()
        {
            // Limpieza para no afectar otros tests
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
