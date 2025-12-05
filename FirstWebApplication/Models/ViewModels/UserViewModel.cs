using System.Collections.Generic;

namespace FirstWebApplication.Models.ViewModels
{
    public class UserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Fornavn { get; set; } = string.Empty;
        public string Etternavn { get; set; } = string.Empty;
        public string OrganisasjonNavn { get; set; } = string.Empty;
        public IList<string> Roller { get; set; } = new List<string>();
    }
}