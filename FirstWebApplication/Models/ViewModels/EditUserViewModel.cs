using System.ComponentModel.DataAnnotations;

namespace FirstWebApplication.Models.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "E-post")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fornavn")]
        public string Fornavn { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Etternavn")]
        public string Etternavn { get; set; } = string.Empty;

        [Display(Name = "Organisasjon")]
        public long? OrganisasjonId { get; set; }

        [Display(Name = "Rolle")]
        public string? CurrentRole { get; set; }
    }
}