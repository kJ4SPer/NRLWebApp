using FirstWebApplication.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace NRLWebApp.Tests.Mocks
{
    // Hjelpeklasse for å opprette en ApplicationDbContext i minnet
    public static class TestDbContext
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                // Feilmeldingen din indikerte at denne metoden manglet.
                // Løsningen er å ha using Microsoft.EntityFrameworkCore; og sørge for at pakken er installert.
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
