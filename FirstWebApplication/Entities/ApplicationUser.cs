using Microsoft.AspNetCore.Identity;
using System; 
using System.ComponentModel.DataAnnotations;

namespace FirstWebApplication.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [StringLength(50)]
        public string Fornavn { get; set; } = string.Empty;

        [PersonalData]
        [StringLength(50)]
        public string Etternavn { get; set; } = string.Empty;

        // NYTT: Sjekk om bruker er godkjent av admin
        public bool IsApproved { get; set; } = false;

        // NYTT: Dato for registrering (kjekt for admin å se)
        public DateTime RegisteredDate { get; set; } = DateTime.Now;

        public long? OrganisasjonId { get; set; }
        public Organisasjon? Organisasjon { get; set; }
    }
}