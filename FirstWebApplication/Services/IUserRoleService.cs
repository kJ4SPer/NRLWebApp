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
        Task<List<string>> GetAllRolesAsync();
        Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName);
    }
}