using FirstWebApplication.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics; // Legg til denne
using System;

namespace NRLWebApp.Tests.Mocks
{
    public static class TestDbContext
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // LEGG TIL DENNE
                .EnableSensitiveDataLogging()
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}