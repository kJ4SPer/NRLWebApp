using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace FirstWebApplication.Services
{
    public class RoleInitializerService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RoleInitializerService> _logger;

        public RoleInitializerService(RoleManager<IdentityRole> roleManager, ILogger<RoleInitializerService> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            string[] roleNames = { "Admin", "Pilot", "Registerfører" };

            // Fetch existing role names in a single query to avoid multiple store lookups
            var existingRoleNames = await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync();

            var existingSet = new HashSet<string>(existingRoleNames ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            foreach (var roleName in roleNames)
            {
                if (!existingSet.Contains(roleName))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Role '{roleName}' created successfully.");
                        existingSet.Add(roleName);
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to create role '{roleName}': {string.Join(';', result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }
}