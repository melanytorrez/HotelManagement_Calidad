using System.ComponentModel.DataAnnotations;

namespace HotelManagement.DTOs
{
    public class DetalleReservaDTO
    {
        public string? ID { get; set; }
        public string Reserva_ID { get; set; } = string.Empty;
        public string Habitacion_ID { get; set; } = string.Empty;
        public string Huesped_ID { get; set; } = string.Empty;
        public DateTime Fecha_Entrada { get; set; }
        public DateTime Fecha_Salida { get; set; }
        public string? Numero_Habitacion { get; set; }
        public string? Nombre_Huesped { get; set; }
    }

    public class DetalleReservaCreateDTO
    {
        [Required]
        public string Reserva_ID { get; set; } = string.Empty;
        
        [Required]
        public string Habitacion_ID { get; set; } = string.Empty;
        
        [Required]
        public string Huesped_ID { get; set; } = string.Empty;
        
        [Required]
        public DateTime? Fecha_Entrada { get; set; }
        
        [Required]
        public DateTime? Fecha_Salida { get; set; }
    }

    public class DetalleReservaUpdateDTO
    {
        public string? Habitacion_ID { get; set; }
        public string? Huesped_ID { get; set; }
        public DateTime? Fecha_Entrada { get; set; }
        public DateTime? Fecha_Salida { get; set; }
    }

    // DTO para crear múltiples detalles a la vez
    public class DetalleReservaMultipleCreateDTO
    {
        [Required]
        public string Reserva_ID { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "Debe agregar al menos una habitación")]
        public List<DetalleHabitacionDTO> Habitaciones { get; set; } = new();
    }

    public class DetalleHabitacionDTO
    {
        [Required]
        public string Habitacion_ID { get; set; } = string.Empty;

        [Required]
        public DateTime Fecha_Entrada { get; set; }

        [Required]
        public DateTime Fecha_Salida { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe asignar al menos un huésped")]
        public List<string> Huesped_IDs { get; set; } = new();
    }
}
