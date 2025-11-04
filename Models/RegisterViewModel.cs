using System.ComponentModel.DataAnnotations;

namespace NRLApp.Models
{
    public class RegisterViewModel
    {

        [Required(ErrorMessage = "E-post er påkrevd")]
        [EmailAddress(ErrorMessage = "Ugyldig e-postadresse")]
        [Display(Name = "E-post")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passord er påkrevd")]
        [StringLength(100, ErrorMessage = "Passord må være minst {2} tegn", MinimumLength = 8)]
        [Display(Name = "Passord")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bekreft passord")]
        [Compare(nameof(Password), ErrorMessage = "Passordene er ikke like")]
        [Display(Name = "Bekreft passord")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

