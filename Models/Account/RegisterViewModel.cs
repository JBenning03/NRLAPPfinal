using System.ComponentModel.DataAnnotations;

namespace NRLApp.Models.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "E-post er påkrevd")]
        [EmailAddress(ErrorMessage = "Ugyldig e-postadresse")]
        [Display(Name = "E-post")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passord er påkrevd")]
        [MinLength(8, ErrorMessage = "Passord må være minst 8 tegn")]
        [DataType(DataType.Password)]
        [Display(Name = "Passord")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bekreft passord")]
        [Compare(nameof(Password), ErrorMessage = "Passordene er ikke like")]
        [DataType(DataType.Password)]
        [Display(Name = "Bekreft passord")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
