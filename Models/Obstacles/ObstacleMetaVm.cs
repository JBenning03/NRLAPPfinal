using System.ComponentModel.DataAnnotations;

namespace NRLApp.Models.Obstacles
{
    /// <summary>
    /// Holder GeoJSON mellom trinnene (brukes i TempData i controlleren)
    /// <summary>
    public class DrawState
    {
        public string? GeoJson { get; set; }
    }

    /// <summary>
    /// ViewModel for metadata-skjemaet.
    /// Gjort public slik at ObstacleEditVm kan arve feltene.
    /// <summary
    public class ObstacleMetaVm
    {
        [Display(Name = "Kategori")]
        public string? Category { get; set; }

        [Display(Name = "Hinder")]
        public string? ObstacleName { get; set; } // ikke lenger [Required]

        [Required(ErrorMessage = "Oppgi høyde.")]
        [Range(0, 10000, ErrorMessage = "Høyden må være et tall ≥ 0.")]
        [Display(Name = "Høyde")]
        public double? HeightValue { get; set; }

        // "m" eller "ft"
        [Display(Name = "Enhet")]
        public string HeightUnit { get; set; } = "m";

        [Display(Name = "Beskrivelse")]
        public string? Description { get; set; }

        [Display(Name = "Lagre som utkast")]
        public bool SaveAsDraft { get; set; }
    }
}
