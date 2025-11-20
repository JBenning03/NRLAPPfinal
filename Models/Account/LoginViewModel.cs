using System.ComponentModel.DataAnnotations;

namespace NRLApp.Models.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-post er påkrevd")]
        [EmailAddress(ErrorMessage = "Ugyldig e-postadresse")]
        [Display(Name = "E-post")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passord er påkrevd")]
        [DataType(DataType.Password)]
        [Display(Name = "Passord")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Husk meg")]
        public bool RememberMe { get; set; }
    }
}
