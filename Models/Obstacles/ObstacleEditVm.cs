using System.ComponentModel.DataAnnotations;

namespace NRLApp.Models.Obstacles
{
    // Brukes når vi redigerer et eksisterende hinder
    // Arver fra ObstacleMetaVm for å gjenbruke felter og validering
    public class ObstacleEditVm : ObstacleMetaVm
    {
        public int Id { get; set; }
    }
}
