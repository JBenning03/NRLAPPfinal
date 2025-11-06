using System.ComponentModel.DataAnnotations;

namespace NRLApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "E-post er påkrevd")]
        [EmailAddress(ErrorMessage = "Ugyldig e-postadresse")]
        [Display(Name = "E-post")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Passord er påkrevd")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Passord må være minst {2} tegn")]
        [DataType(DataType.Password)]
        [Display(Name = "Passord")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Bekreft passord")]
        [DataType(DataType.Password)]
        [Display(Name = "Bekreft passord")]
        [Compare(nameof(Password), ErrorMessage = "Passordene er ikke like")]
        public string ConfirmPassword { get; set; } = "";
    }
}

