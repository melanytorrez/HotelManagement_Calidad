using System.ComponentModel.DataAnnotations;

namespace HotelManagement.DTOs
{
    public class HuespedDto
    {
        public string ID { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Segundo_Apellido { get; set; }
        public string Documento_Identidad { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public DateTime? Fecha_Nacimiento { get; set; }
        public bool Activo { get; set; }
        
        // Campo calculado para el frontend
        public string? Nombre_Completo => 
            $"{Nombre} {Apellido}" + 
            (string.IsNullOrWhiteSpace(Segundo_Apellido) ? "" : $" {Segundo_Apellido}");
    }

    public class HuespedCreateDto
    {
        [Required]
        [StringLength(30)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Apellido { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Segundo_Apellido { get; set; }

        [Required]
        [StringLength(20)]
        public string Documento_Identidad { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Telefono { get; set; }

        public string? Fecha_Nacimiento { get; set; }
    }

    public class HuespedUpdateDto
    {
        [StringLength(30)]
        public string? Nombre { get; set; }

        [StringLength(30)]
        public string? Apellido { get; set; }

        [StringLength(30)]
        public string? Segundo_Apellido { get; set; }

        [StringLength(20)]
        public string? Documento_Identidad { get; set; }

        [Phone]
        [StringLength(20)]
        public string? Telefono { get; set; }

        public string? Fecha_Nacimiento { get; set; }

        public bool? Activo { get; set; }
    }
}
