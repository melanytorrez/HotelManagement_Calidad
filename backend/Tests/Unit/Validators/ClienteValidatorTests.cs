using Xunit;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Datos.Config;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Aplicacion.Exceptions;
using HotelManagement.DTOs;
using HotelManagement.Models;

namespace HotelManagement.Tests.Unit.Validators
{
    public class ClienteValidatorTests : IDisposable
    {
        private readonly HotelDbContext _context;
        private readonly ClienteValidator _validator;

        public ClienteValidatorTests()
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: $"ClienteValidatorTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new HotelDbContext(options);
            _validator = new ClienteValidator(_context);
        }

        private async Task<Cliente> SeedClienteAsync(
            string razonSocial = "HOTEL TEST",
            string nit = "12345678",
            string email = "test@hotel.com")
        {
            var cliente = new Cliente
            {
                ID = Guid.NewGuid().ToByteArray(),
                Razon_Social = razonSocial,
                NIT = nit,
                Email = email,
                Activo = true,
                Fecha_Creacion = DateTime.Now,
                Fecha_Actualizacion = DateTime.Now
            };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        private static ClienteCreateDTO ValidCreateDto(
            string razonSocial = "HOTEL TEST",
            string nit = "12345678",
            string email = "test@hotel.com")
            => new() { Razon_Social = razonSocial, NIT = nit, Email = email };

        // ValidateCreateAsync — HAPPY PATH

        [Fact]
        public async Task ValidateCreateAsync_DatosValidos_NoLanzaExcepcion()
        {
            var dto = ValidCreateDto();
            var exception = await Record.ExceptionAsync(() => _validator.ValidateCreateAsync(dto));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidateCreateAsync_NitEnLimiteSuperior_NoLanzaExcepcion()
        {
            var dto = ValidCreateDto(nit: "12345678901234567890"); // 20 chars
            var exception = await Record.ExceptionAsync(() => _validator.ValidateCreateAsync(dto));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidateCreateAsync_NitEnLimiteInferior_NoLanzaExcepcion()
        {
            var dto = ValidCreateDto(nit: "1234567"); // 7 chars
            var exception = await Record.ExceptionAsync(() => _validator.ValidateCreateAsync(dto));
            Assert.Null(exception);
        }

        // ValidateCreateAsync

        [Fact]
        public async Task ValidateCreateAsync_RazonSocialVacia_LanzaValidationException()
        {
            var dto = ValidCreateDto(razonSocial: "");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("razon_Social"));
        }

        [Fact]
        public async Task ValidateCreateAsync_RazonSocialNull_LanzaValidationException()
        {
            var dto = new ClienteCreateDTO { Razon_Social = null!, NIT = "12345678", Email = "test@hotel.com" };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("razon_Social"));
        }

        [Fact]
        public async Task ValidateCreateAsync_RazonSocialMenorA3Chars_LanzaValidationException()
        {
            var dto = ValidCreateDto(razonSocial: "AB"); // 2 chars
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("razon_Social"));
            Assert.Contains("3 caracteres", ex.Errors["razon_Social"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_RazonSocialMayorA20Chars_LanzaValidationException()
        {
            var dto = ValidCreateDto(razonSocial: "ABCDEFGHIJKLMNOPQRSTU"); // 21 chars
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("razon_Social"));
            Assert.Contains("20 caracteres", ex.Errors["razon_Social"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_RazonSocialEnLimiteSuperior_NoLanzaExcepcion()
        {
            var dto = ValidCreateDto(razonSocial: "ABCDEFGHIJKLMNOPQRST"); // 20 chars exactos
            var exception = await Record.ExceptionAsync(() => _validator.ValidateCreateAsync(dto));
            Assert.Null(exception);
        }

        // ValidateCreateAsync — NIT

        [Fact]
        public async Task ValidateCreateAsync_NitVacio_LanzaValidationException()
        {
            var dto = ValidCreateDto(nit: "");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nit"));
            Assert.Contains("obligatorio", ex.Errors["nit"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_NitMenorA7Chars_LanzaValidationException()
        {
            var dto = ValidCreateDto(nit: "123456"); // 6 chars
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nit"));
            Assert.Contains("7 y 20 caracteres", ex.Errors["nit"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_NitMayorA20Chars_LanzaValidationException()
        {
            var dto = ValidCreateDto(nit: "123456789012345678901"); // 21 chars
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nit"));
        }

        [Fact]
        public async Task ValidateCreateAsync_NitConLetras_LanzaValidationException()
        {
            var dto = ValidCreateDto(nit: "1234ABC890");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nit"));
            Assert.Contains("solo números", ex.Errors["nit"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_NitDuplicado_LanzaValidationException()
        {
            await SeedClienteAsync(nit: "99887766");
            var dto = ValidCreateDto(nit: "99887766", email: "otro@hotel.com");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("nit"));
            Assert.Contains("99887766", ex.Errors["nit"].First());
        }

        // ValidateCreateAsync — EMAIL

        [Fact]
        public async Task ValidateCreateAsync_EmailVacio_LanzaValidationException()
        {
            var dto = ValidCreateDto(email: "");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("email"));
            Assert.Contains("obligatorio", ex.Errors["email"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_EmailSinArroba_LanzaValidationException()
        {
            var dto = ValidCreateDto(email: "emailsinformato");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("email"));
            Assert.Contains("formato válido", ex.Errors["email"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_EmailSinDominio_LanzaValidationException()
        {
            var dto = ValidCreateDto(email: "email@sindominio");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("email"));
        }

        [Fact]
        public async Task ValidateCreateAsync_EmailMayorA30Chars_LanzaValidationException()
        {
            var dto = ValidCreateDto(email: "abcdefghijklmno@dominiotest.ext");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("email"));
            Assert.Contains("30 caracteres", ex.Errors["email"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_EmailDuplicado_LanzaValidationException()
        {
            await SeedClienteAsync(email: "duplicado@hotel.com");
            var dto = ValidCreateDto(nit: "99887766", email: "duplicado@hotel.com");
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("email"));
            Assert.Contains("duplicado@hotel.com", ex.Errors["email"].First());
        }

        [Fact]
        public async Task ValidateCreateAsync_MultiplesErrores_CapturaTodos()
        {
            var dto = new ClienteCreateDTO
            {
                Razon_Social = "",   
                NIT = "ABC",         
                Email = ""           
            };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateCreateAsync(dto));
            Assert.True(ex.Errors.ContainsKey("razon_Social"));
            Assert.True(ex.Errors.ContainsKey("nit"));
            Assert.True(ex.Errors.ContainsKey("email"));
        }

        // ValidateUpdateAsync


        [Fact]
        public async Task ValidateUpdateAsync_GuidInvalido_LanzaValidationException()
        {
            var dto = new ClienteUpdateDTO { Razon_Social = "HOTEL NUEVO" };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync("no-es-guid", dto));
            Assert.True(ex.Errors.ContainsKey("id"));
        }

        [Fact]
        public async Task ValidateUpdateAsync_ClienteInexistente_LanzaNotFoundException()
        {
            var dto = new ClienteUpdateDTO { Razon_Social = "HOTEL NUEVO" };
            await Assert.ThrowsAsync<NotFoundException>(
                () => _validator.ValidateUpdateAsync(Guid.NewGuid().ToString(), dto));
        }

        [Fact]
        public async Task ValidateUpdateAsync_DatosValidos_NoLanzaExcepcion()
        {
            var cliente = await SeedClienteAsync();
            var id = new Guid(cliente.ID).ToString();
            var dto = new ClienteUpdateDTO
            {
                Razon_Social = "NUEVO NOMBRE",
                NIT = "11112222",
                Email = "nuevo@hotel.com"
            };
            var exception = await Record.ExceptionAsync(
                () => _validator.ValidateUpdateAsync(id, dto));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidateUpdateAsync_MismoNitDelMismoCliente_NoLanzaExcepcion()
        {
            var cliente = await SeedClienteAsync(nit: "55556666");
            var id = new Guid(cliente.ID).ToString();
            var dto = new ClienteUpdateDTO
            {
                NIT = "55556666",
                Razon_Social = "NUEVO NOMBRE",
                Email = "nuevo@hotel.com"
            };
            var exception = await Record.ExceptionAsync(
                () => _validator.ValidateUpdateAsync(id, dto));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidateUpdateAsync_NitDuplicadoDeOtroCliente_LanzaValidationException()
        {
            await SeedClienteAsync(nit: "11110000", email: "otro@hotel.com");
            var cliente2 = await SeedClienteAsync(nit: "22220000", email: "cliente2@hotel.com");
            var id = new Guid(cliente2.ID).ToString();
            var dto = new ClienteUpdateDTO
            {
                Razon_Social = "HOTEL DOS",
                NIT = "11110000",
                Email = "cliente2@hotel.com"
            };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateUpdateAsync(id, dto));
            Assert.True(ex.Errors.ContainsKey("nit"));
        }

        // ValidatePartialUpdateAsync

        [Fact]
        public async Task ValidatePartialUpdateAsync_GuidInvalido_LanzaValidationException()
        {
            var dto = new ClienteUpdateDTO { Razon_Social = "NUEVO" };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidatePartialUpdateAsync("guid-invalido", dto));
            Assert.True(ex.Errors.ContainsKey("id"));
        }

        [Fact]
        public async Task ValidatePartialUpdateAsync_ClienteInexistente_LanzaNotFoundException()
        {
            var dto = new ClienteUpdateDTO { Razon_Social = "NUEVO" };
            await Assert.ThrowsAsync<NotFoundException>(
                () => _validator.ValidatePartialUpdateAsync(Guid.NewGuid().ToString(), dto));
        }

        [Fact]
        public async Task ValidatePartialUpdateAsync_SoloRazonSocial_NoLanzaExcepcion()
        {
            var cliente = await SeedClienteAsync();
            var id = new Guid(cliente.ID).ToString();
            var dto = new ClienteUpdateDTO { Razon_Social = "NUEVO NOMBRE" };
            var exception = await Record.ExceptionAsync(
                () => _validator.ValidatePartialUpdateAsync(id, dto));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidatePartialUpdateAsync_DtoVacio_NoLanzaExcepcion()
        {
            var cliente = await SeedClienteAsync();
            var id = new Guid(cliente.ID).ToString();
            var dto = new ClienteUpdateDTO();
            var exception = await Record.ExceptionAsync(
                () => _validator.ValidatePartialUpdateAsync(id, dto));
            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidatePartialUpdateAsync_RazonSocialInvalida_LanzaValidationException()
        {
            var cliente = await SeedClienteAsync();
            var id = new Guid(cliente.ID).ToString();
            var dto = new ClienteUpdateDTO { Razon_Social = "AB" }; 
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidatePartialUpdateAsync(id, dto));
            Assert.True(ex.Errors.ContainsKey("razon_Social"));
        }

        // ValidateDeleteAsync

        [Fact]
        public async Task ValidateDeleteAsync_GuidInvalido_LanzaBadRequestException()
        {
            await Assert.ThrowsAsync<BadRequestException>(
                () => _validator.ValidateDeleteAsync("no-es-guid"));
        }

        [Fact]
        public async Task ValidateDeleteAsync_ClienteInexistente_LanzaNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(
                () => _validator.ValidateDeleteAsync(Guid.NewGuid().ToString()));
        }

        [Fact]
        public async Task ValidateDeleteAsync_ClienteConReservas_LanzaConflictException()
        {
            var cliente = await SeedClienteAsync();

            _context.Reservas.Add(new Reserva
            {
                ID = Guid.NewGuid().ToByteArray(),
                Cliente_ID = cliente.ID,
                Estado_Reserva = "Pendiente",
                Monto_Total = 100,
                Fecha_Creacion = DateTime.Now
            });
            await _context.SaveChangesAsync();

            var id = new Guid(cliente.ID).ToString();
            await Assert.ThrowsAsync<ConflictException>(
                () => _validator.ValidateDeleteAsync(id));
        }

        [Fact]
        public async Task ValidateDeleteAsync_ClienteSinReservas_NoLanzaExcepcion()
        {
            var cliente = await SeedClienteAsync();
            var id = new Guid(cliente.ID).ToString();
            var exception = await Record.ExceptionAsync(
                () => _validator.ValidateDeleteAsync(id));
            Assert.Null(exception);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
