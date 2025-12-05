using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstWebApplication.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Random _random = new Random();

        public DatabaseSeeder(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            // 1. Seed Roller
            await SeedRolesAsync();

            // 2. Seed Organisasjoner
            await SeedOrganizationsAsync();

            // 3. Seed StatusTyper
            await SeedStatusTypesAsync();

            // 4. Seed ObstacleTyper
            await SeedObstacleTypesAsync();

            // 5. Seed Brukere (Nå med fiks for eksisterende admin!)
            await SeedUsersAsync();

            // 6. Seed Hindre
            await SeedObstaclesAsync(50);
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = { "Admin", "Registerfører", "Pilot" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task SeedOrganizationsAsync()
        {
            if (!_context.Organisasjoner.Any())
            {
                var orgs = new List<Organisasjon>
                {
                    new Organisasjon { Name = "Kartverket", Active = true, ContactEmail = "post@kartverket.no" },
                    new Organisasjon { Name = "Luftambulansen", Active = true, ContactEmail = "info@nla.no" },
                    new Organisasjon { Name = "Luftforsvaret", Active = true, ContactEmail = "forsvaret@mil.no" },
                    new Organisasjon { Name = "Politiet", Active = true, ContactEmail = "politiet@politiet.no" },
                    new Organisasjon { Name = "Norsk Luftsportforbund", Active = true, ContactEmail = "nlf@nlf.no" },
                    new Organisasjon { Name = "Privat", Active = true, ContactEmail = "" }
                };

                await _context.Organisasjoner.AddRangeAsync(orgs);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedStatusTypesAsync()
        {
            if (!await _context.StatusTypes.AnyAsync())
            {
                var statuses = new List<StatusType>
                {
                    new StatusType { Id = 1, Name = "Registered", Description = "Quick Register saved - incomplete" },
                    new StatusType { Id = 2, Name = "Pending", Description = "Awaiting approval from Registerfører" },
                    new StatusType { Id = 3, Name = "Approved", Description = "Approved by Registerfører" },
                    new StatusType { Id = 4, Name = "Rejected", Description = "Rejected by Registerfører" }
                };
                await _context.StatusTypes.AddRangeAsync(statuses);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedObstacleTypesAsync()
        {
            if (!_context.ObstacleTypes.Any())
            {
                var types = new List<ObstacleType>
                {
                    new ObstacleType { Name = "Mast", Description = "Radio/TV mast", MinHeight = 10, MaxHeight = 500 },
                    new ObstacleType { Name = "Tower", Description = "Tower structure", MinHeight = 20, MaxHeight = 300 },
                    new ObstacleType { Name = "Power Line", Description = "Power line/cables", MinHeight = 5, MaxHeight = 100 },
                    new ObstacleType { Name = "Wind Turbine", Description = "Wind turbine", MinHeight = 50, MaxHeight = 250 },
                    new ObstacleType { Name = "Building", Description = "Building", MinHeight = 10, MaxHeight = 200 },
                    new ObstacleType { Name = "Crane", Description = "Construction crane", MinHeight = 20, MaxHeight = 150 },
                    new ObstacleType { Name = "Bridge", Description = "Bridge", MinHeight = 10, MaxHeight = 100 },
                    new ObstacleType { Name = "Other", Description = "Other obstacles", MinHeight = 1, MaxHeight = 1000 }
                };
                await _context.ObstacleTypes.AddRangeAsync(types);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedUsersAsync()
        {
            var kartverket = await _context.Organisasjoner.FirstOrDefaultAsync(o => o.Name == "Kartverket");
            var luftambulansen = await _context.Organisasjoner.FirstOrDefaultAsync(o => o.Name == "Luftambulansen");

            // 1. ADMIN USER
            await CreateSpecificUser("admin@test.com", "Admin123!", "Admin", "Admin", "User", kartverket?.Id);

            // 2. REGISTERFØRER USER
            await CreateSpecificUser("reg@test.com", "Reg123!", "Registerfører", "Register", "Fører", kartverket?.Id);

            // 3. PILOT USER
            await CreateSpecificUser("pilot@test.com", "Pilot123!", "Pilot", "Peder", "Pilot", luftambulansen?.Id);

            // 4. RANDOM PILOTS
            var firstNames = new[] { "Ole", "Kari", "Lars", "Marit", "Jan", "Ingrid" };
            var lastNames = new[] { "Hansen", "Johansen", "Olsen", "Larsen", "Berg" };
            var pilotOrgs = await _context.Organisasjoner.Where(o => o.Name != "Kartverket").ToListAsync();

            if (pilotOrgs.Any())
            {
                for (int i = 0; i < 10; i++)
                {
                    var fName = firstNames[_random.Next(firstNames.Length)];
                    var lName = lastNames[_random.Next(lastNames.Length)];
                    var org = pilotOrgs[_random.Next(pilotOrgs.Count)];

                    await CreateSpecificUser(
                        $"{fName.ToLower()}.{lName.ToLower()}{i}@pilot.no",
                        "Pilot123!", "Pilot", fName, lName, org.Id
                    );
                }
            }
        }

        
        private async Task CreateSpecificUser(string email, string password, string role, string fornavn, string etternavn, long? orgId)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Opprett ny bruker (Godkjent)
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Fornavn = fornavn,
                    Etternavn = etternavn,
                    EmailConfirmed = true,
                    IsApproved = true, // <--- VIKTIG
                    RegisteredDate = DateTime.Now,
                    OrganisasjonId = orgId
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }
            else
            {
                // FIKS FOR EKSISTERENDE BRUKERE (Hvis admin ble opprettet før vi la til godkjenning)
                if (!user.IsApproved)
                {
                    user.IsApproved = true;
                    await _userManager.UpdateAsync(user);
                }

                // Sjekk også at de har riktig rolle
                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }
        }

        private async Task SeedObstaclesAsync(int count)
        {
            if (await _context.Obstacles.AnyAsync()) return;

            var pilots = await _userManager.GetUsersInRoleAsync("Pilot");
            if (!pilots.Any()) return;

            var types = await _context.ObstacleTypes.ToListAsync();
            var locations = new[] { (59.9139, 10.7522), (60.3913, 5.3221), (63.4305, 10.3951), (69.6492, 18.9553) };

            for (int i = 0; i < count; i++)
            {
                var pilot = pilots[_random.Next(pilots.Count)];
                var type = types[_random.Next(types.Count)];
                var locBase = locations[_random.Next(locations.Length)];

                var lat = locBase.Item1 + (_random.NextDouble() - 0.5) * 0.1;
                var lng = locBase.Item2 + (_random.NextDouble() - 0.5) * 0.1;

                var obstacle = new Obstacle
                {
                    Height = _random.Next(20, 300),
                    Description = "Automatisk generert testdata",
                    Location = $"POINT({lng.ToString(System.Globalization.CultureInfo.InvariantCulture)} {lat.ToString(System.Globalization.CultureInfo.InvariantCulture)})",
                    ObstacleTypeId = type.Id,
                    RegisteredByUserId = pilot.Id,
                    RegisteredDate = DateTime.Now.AddDays(-_random.Next(1, 100))
                };

                _context.Obstacles.Add(obstacle);
                await _context.SaveChangesAsync();

                var statusId = _random.Next(2, 4); // 2=Pending, 3=Approved
                var status = new ObstacleStatus
                {
                    ObstacleId = obstacle.Id,
                    StatusTypeId = statusId,
                    ChangedByUserId = pilot.Id,
                    ChangedDate = DateTime.Now,
                    Comments = "Initial status",
                    IsActive = true
                };

                _context.ObstacleStatuses.Add(status);
                await _context.SaveChangesAsync();

                obstacle.CurrentStatusId = status.Id;
                _context.Obstacles.Update(obstacle);
            }
            await _context.SaveChangesAsync();
        }
    }
}