namespace HotelManagement.DTOs
{
    public class HabitacionDto
    {
        public string ID { get; set; } = string.Empty;
        public string Numero_Habitacion { get; set; } = string.Empty;
        public short Piso { get; set; }
        public string Estado_Habitacion { get; set; } = string.Empty;
        public string? Tipo_Habitacion_ID { get; set; }
        public string? Tipo_Nombre { get; set; }
        public byte? Capacidad_Maxima { get; set; }
        public decimal? Tarifa_Base { get; set; }
        public bool Activo { get; set; }
    }

    public class HabitacionCreateDto
    {
        public string Tipo_Habitacion_ID { get; set; } = string.Empty;
        public string Numero_Habitacion { get; set; } = string.Empty;
        public short? Piso { get; set; }
        public string Estado_Habitacion { get; set; } = "Libre";
    }

    public class HabitacionUpdateDto
    {
        public string? Tipo_Habitacion_ID { get; set; }
        public string? Numero_Habitacion { get; set; }
        public short? Piso { get; set; }
        public string? Estado_Habitacion { get; set; }
        public bool? Activo { get; set; }
    }
}
