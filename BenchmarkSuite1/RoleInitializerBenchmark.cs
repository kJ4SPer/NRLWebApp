using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace FirstWebApplication.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class RoleInitializerBenchmark
    {
        private readonly string[] _roleNames = new[]
        {
            "Admin",
            "Pilot",
            "Registerfører"
        };
        private FakeRoleManager _fakeManager;
        [GlobalSetup]
        public void Setup()
        {
            _fakeManager = new FakeRoleManager();
        }

        [Benchmark]
        public async Task InitializeAsync()
        {
            foreach (var roleName in _roleNames)
            {
                var roleExist = await _fakeManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await _fakeManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        // Lightweight in-memory stand-in for RoleManager to keep the benchmark self-contained
        public class FakeRoleManager
        {
            private readonly HashSet<string> _roles = new();
            public Task<bool> RoleExistsAsync(string roleName)
            {
                return Task.FromResult(_roles.Contains(roleName));
            }

            public Task<IdentityResult> CreateAsync(IdentityRole role)
            {
                _roles.Add(role.Name ?? string.Empty);
                return Task.FromResult(IdentityResult.Success);
            }
        }
    }
}