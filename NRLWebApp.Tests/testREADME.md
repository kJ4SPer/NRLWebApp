NRLWebApp - Enhetstester og Testdokumentasjon
Denne mappen inneholder testprosjektet for NRLWebApp. Formålet med testene er å verifisere forretningslogikk, sikkerhetsmekanismer og dataintegritet uten å være avhengig av eksterne systemer som SQL-servere eller nettlesere.

🛠 Teknisk Oversikt
Prosjektet benytter følgende teknologier for testing:

Rammeverk: xUnit (.NET 9.0)

Mocking: Moq - For å simulere avhengigheter som UserManager, RoleManager og ILogger.

Database: Microsoft.EntityFrameworkCore.InMemory - For å teste databaseinteraksjoner raskt og isolert.

Assertions: xUnit innebygde assertions.

📂 Prosjektstruktur
Testprosjektet speiler strukturen i hovedapplikasjonen for enkel navigering:

/Controllers: Inneholder enhetstester for MVC-kontrollerne.

AdminControllerTests.cs: Tester administrasjon av brukere, godkjenning og sletting.

PilotControllerTests.cs: Tester registrering av hindre, transaksjonshåndtering og opprydding.

RegisterforerControllerTests.cs: Tester saksbehandling, filtrering og statusendringer.

/Middleware: Tester for HTTP-pipeline og sikkerhet.

CspMiddlewareTests.cs: Verifiserer Content Security Policy (CSP), Nonce-generering og sikkerhetsheadere.

/Mocks: Hjelpeklasser og infrastruktur for testene.

TestDbContext.cs: Konfigurerer en isolert InMemory-database per test.

MockHelpers.cs: Inneholder logikk for å mocke asynkrone Identity-operasjoner.

🗝 Nøkkelkonsepter og Implementerte Prinsipper
For å sikre robuste tester som håndterer asynkron kode og Entity Framework korrekt, er følgende prinsipper implementert:

1. Asynkron Mocking (MockHelpers.cs)
Siden ASP.NET Core Identity (UserManager) bruker asynkrone metoder (f.eks. .CountAsync()), kan man ikke bruke vanlige C#-lister i testene.

Løsning: Vi har implementert TestAsyncEnumerable og TestAsyncQueryProvider. Dette "lurer" systemet til å tro at en in-memory liste er en ekte asynkron databasekilde, slik at LINQ-spørringer fungerer smertefritt i testene.

2. Test-Isolasjon (TestDbContext.cs)
Tester skal aldri påvirke hverandre.

Løsning: Hver gang TestDbContext.Create() kalles, genereres et unikt databasenavn basert på Guid.NewGuid(). Dette garanterer at hver test starter med en tom, isolert database.

Transaksjoner: Siden InMemory-databasen ikke støtter relasjonelle transaksjoner, er advarselen TransactionIgnoredWarning undertrykt globalt i testkonteksten. Dette gjør at vi kan teste kode som bruker BeginTransactionAsync() (f.eks. i PilotController) uten at testen kræsjer.

3. Tilstandshåndtering (State Management)
Entity Framework cacher data i minnet (Local view). Dette kan føre til at tester "består" selv om data ikke ble lagret riktig i databasen.

Løsning: Vi bruker context.ChangeTracker.Clear() før vi verifiserer resultatet (Assert-fasen). Dette tvinger applikasjonen til å hente data på nytt fra databasen, noe som simulerer en ny, ekte HTTP-forespørsel.

4. Sikkerhetstesting (CSP)
Sikkerhet er en funksjonell del av applikasjonen.

Løsning: Vi tester at:

En unik kryptografisk Nonce genereres for hver request (hindrer Replay-angrep).

HSTS (Strict-Transport-Security) kun aktiveres ved HTTPS-tilkoblinger.

Sikkerhetsheadere som X-Frame-Options og X-Content-Type-Options alltid er til stede.

🚀 Hvordan kjøre testene
Du kan kjøre testene via Visual Studio Test Explorer, eller ved å bruke kommandolinjen i rotmappen av løsningen:

Bash

# Kjør alle tester
dotnet test

# Kjør tester med detaljert output (for feilsøking)
dotnet test --logger "console;verbosity=detailed"
✅ Dekningsområder
AdminController
Verifiserer at dashboard-tall (statistikk) er korrekte.

Tester at brukere kan godkjennes (statusendring).

Tester at brukere kan slettes fullstendig fra systemet.

PilotController
Tester "Hurtigregistrering" flyten.

Verifiserer at sletting av en registrering fjerner både hinderet og all tilhørende historikk (Cascade delete-logikk).

Simulerer innlogget bruker ved hjelp av ClaimsPrincipal.

RegisterforerController
Tester filtrering av hindre (Pending vs Approved).

Verifiserer arbeidsflyten for å Avvise (Reject) et hinder, inkludert lagring av begrunnelse.

Sikrer at relasjoner mellom tabeller (Foreign Keys) er korrekte ved oppslag.

CspMiddleware
Sikrer at applikasjonen leverer korrekte sikkerhetsheadere for å beskytte mot XSS og Clickjacking.