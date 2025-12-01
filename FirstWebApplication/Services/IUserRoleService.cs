using FirstWebApplication.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstWebApplication.Services
{
    public interface IUserRoleService
    {
        Task<bool> CreateRoleAsync(string roleName);
        Task<bool> AssignRoleToUserAsync(string userEmail, string roleName);
        Task<bool> RemoveRoleFromUserAsync(string userEmail, string roleName);
        Task<IList<string>> GetUserRolesAsync(string userEmail);
        Task<bool> IsUserInRoleAsync(string userEmail, string roleName);

        // FIKS 1: Lagt til metoden som manglet
        Task<List<string>> GetAllRolesAsync();

        // FIKS 2: Endret returtype til IList<ApplicationUser> for å matche UserRoleService
        Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName);
    }
}