using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FirstWebApplication.Entities
{
    /// <summary>
    /// Utvider IdentityUser med tilleggsfelt for organisasjon
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [StringLength(50)]
        public string Fornavn { get; set; } = string.Empty;

        [PersonalData]
        [StringLength(50)]
        public string Etternavn { get; set; } = string.Empty;

        // FK til Organisasjon (Endret til long for å matche din database)
        public long? OrganisasjonId { get; set; }

        public Organisasjon? Organisasjon { get; set; }
    }
}