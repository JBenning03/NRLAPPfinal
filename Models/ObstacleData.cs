using System.ComponentModel.DataAnnotations;

namespace NRLApp.Models
{
    /// ViewModel for NRL obstacle reporting
    public class ObstacleData
    {
        // Navnet på hindret må være mellom 1-100 tegn
        [Required, StringLength(100)]
        [Display(Name = "Navn på hinder")]
        public string ObstacleName { get; set; } = string.Empty;
       
        // Høyde på hindret i meter. Må være positiv verdi for å representere fysisk høyde
        // Maksverdi på 100000 meter dekker alle realistiske hinder inkludert høye fjell
        [Range(1, 100000, ErrorMessage = "Høyden må være > 0")]
        [Display(Name = "Høyde (meter)")]
        public double ObstacleHeight { get; set; }
        
        // Valgfri beskrivelse som kan inneholde detaljer om hindrets type, materiale
        [StringLength(500)]
        [Display(Name = "Beskrivelse")]
        public string? ObstacleDescription { get; set; }
        
        // Validert innenfor gyldige geografiske koordinater (-90 til 90 grader)
        [Range(-90, 90)]
        [Display(Name = "Breddegrad (lat)")]
        public double Latitude { get; set; }
        
        // Validert innenfor gyldige geografiske koordinater (-180 til 180 grader)
        [Range(-180, 180)]
        [Display(Name = "Lengdegrad (lng)")]
        public double Longitude { get; set; }
    }
}
