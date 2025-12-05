# NRLWebApp Testsuite - Dokumentasjon

## Oversikt

Denne testsuitene validerer funksjonaliteten i **NRLWebApp** - en ASP.NET Core Razor Pages-applikasjon for registrering og godkjenning av flyhindringer (obstacles).

Testene er strukturert etter controller-arkitektur og middleware-komponenter, og bruker **xUnit**, **Moq** og **InMemory Database**.

---

## 📁 Teststruktur

```
NRLWebApp.Tests/
├── Controllers/
│   ├── PilotControllerTests.cs          # Pilot-bruker: registrering av hindringer
│   ├── RegisterforerControllerTests.cs  # Registerfører: godkjenning av hindringer
│   ├── AdminControllerTests.cs          # Admin: brukerbehandling
│   └── HomeControllerTests.cs           # Offentlige sider
├── Middleware/
│   └── CspMiddlewareTests.cs            # Content Security Policy validering
└── Mocks/
    ├── MockUserManager.cs               # Mock av UserManager
    ├── MockRoleManager.cs               # Mock av RoleManager
    ├── MockLoggerFactory.cs             # Mock av logger
    └── TestDbContext.cs                 # In-memory testdatabase
```

---

## 🧪 Controller-Tester

### 1. PilotControllerTests
**Formål:** Validere at piloter kan registrere flyhindringer

#### Tester:
- **Registrering**: Rask- og fullregistrering av hindringer
- **Validering**: Håndtering av ugyldig geometri
- **Autorisering**: Bare godkjente brukere kan registrere
- **Visning**: Oversikt over egne registreringer

**Eksempel:**
```csharp
[Fact]
public async Task QuickRegister_Post_ValidGeometry_SavesToDatabase()
{
    // Rask registrering av hinder med GPS-koordinater
    // Verifiserer at hinder lagres med status "Registered"
}
```

---

### 2. RegisterforerControllerTests
**Formål:** Validere godkjenning og avvisning av hindringer

#### Tester:
- **Godkjenning**: Status endres fra "Pending" → "Approved"
- **Avvisning**: Hindringer avvises med grunn og kommentarer
- **Filtrering**: Hent ventende, godkjente, avviste hindringer
- **Dashboard**: Statistikk over registreringer
- **Kartvisning**: JSON-API for kartvisning

**Eksempel:**
```csharp
[Fact]
public async Task ApproveObstacle_ValidModel_ChangesStatusToApproved()
{
    // Godkjenner hinder og lagrer kommentarer
    // Verifiserer at status blir "Approved"
}
```

---

### 3. AdminControllerTests
**Formål:** Validere brukerbehandling og rollehåndtering

#### Tester:
- **Godkjenning av brukere**: Ventende brukere godkjennes
- **Avvisning av brukere**: Brukere slettes hvis avvist
- **Brukerredigering**: Oppdatere brukerdata og rolle
- **Brukersletting**: Permanent sletting av bruker
- **Dashboard**: Antall ventende og godkjente brukere

**Eksempel:**
```csharp
[Fact]
public async Task ApproveUser_ValidId_SetsApprovedTrue()
{
    // Godkjenner bruker og setter IsApproved = true
    // Verifiserer lagring i database
}
```

---

### 4. HomeControllerTests
**Formål:** Validere offentlige sider

#### Tester:
- **Startsiden**: Redirect for godkjente brukere basert på rolle
- **Personvernside**: Vises korrekt
- **Feilside**: Viser med request ID

---

## 🔒 Middleware-Tester

### CspMiddlewareTests
**Formål:** Validere sikkerhetshoder og Content Security Policy

#### Tester:
- **Nonce-generering**: Unik, base64-kodet, 32 bytes
- **CSP-direktiver**: Alle kritiske direktiver til stede
- **Sikkerhetshoder**: X-Frame-Options, X-Content-Type-Options, osv.
- **HTTPS-håndtering**: HSTS og upgrade-insecure-requests
- **Pipeline**: Headers settes før next middleware

**Kritiske sikkerhetskrav:**
- ✅ Ingen JavaScript fra eksterne kilder (bortsett fra nonce/CDN)
- ✅ Blokkering av Flash/Java (object-src: 'none')
- ✅ Blokkering av clickjacking (frame-ancestors: 'none')
- ✅ HTTPS-forcing i produksjon (HSTS)

---

## 🛠️ Mock-Hjelpere

### MockUserManager
**Bruk:** Mock av ASP.NET Core UserManager for testing

```csharp
var mockUserManager = MockUserManager.Create();
mockUserManager.Setup(um => um.FindByIdAsync("user1"))
    .ReturnsAsync(new ApplicationUser { ... });
```

### MockRoleManager
**Bruk:** Mock av ASP.NET Core RoleManager for testing

```csharp
var mockRoleManager = MockRoleManager.GetMockRoleManager();
```

### TestDbContext
**Bruk:** In-memory database for integrert testing

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
var context = new ApplicationDbContext(options);
```

---

## 🚀 Kjøring av Tester

### Alle tester
```bash
dotnet test
```

### Spesifikk testklasse
```bash
dotnet test --filter "ClassName=NRLWebApp.Tests.Controllers.PilotControllerTests"
```

### Med coverage-rapportering
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Watch-modus (kjør på filendring)
```bash
dotnet watch test
```

---

## ✅ Best Practices

### Testnavngiving
```
[Method]_[Scenario]_[ExpectedResult]
```

**Eksempler:**
- `QuickRegister_Post_ValidGeometry_SavesToDatabase`
- `ApproveObstacle_ValidModel_ChangesStatusToApproved`
- `AdminDashboard_WithNoUsers_ReturnsZeroCounts`

### Arrange-Act-Assert (AAA) Pattern
```csharp
[Fact]
public async Task TestMethod()
{
    // Arrange: Oppsett
    var testData = new Obstacle { ... };
    
    // Act: Utføring
    var result = await _controller.Method(testData);
    
    // Assert: Validering
    Assert.NotNull(result);
}
```

### Isolerte Tester
- ✅ Hver test står alene
- ✅ Ingen avhengigheter mellom tester
- ✅ In-memory database for hver test
- ✅ Mocked eksterne avhengigheter

---

## 📊 Testdekning

| Komponente | Dekning | Status |
|-----------|---------|--------|
| PilotController | 28 tester | ✅ Komplett |
| RegisterforerController | 11 tester | ✅ Komplett |
| AdminController | 14 tester | ✅ Komplett |
| HomeController | 3 tester | ✅ Komplett |
| CspMiddleware | 7 tester | ✅ Komplett |
| **Total** | **63 tester** | **✅** |

---

## 🐛 Feilsøking

### Problem: Tester feiler med "Database is locked"
**Løsning:** In-memory database er ekslusiv per test. Sørg for at hver test bruker `Guid.NewGuid()` som database-navn.

```csharp
.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
```

### Problem: Mock returnerer null
**Løsning:** Verifiser at `.Setup()` er kalt før handlingen:

```csharp
_mockUserManager.Setup(x => x.FindByIdAsync("id"))
    .ReturnsAsync(user);
```

### Problem: "Missing using directive"
**Løsning:** Husk `using Xunit;` i alle testfiler (konfigurert globalt i `.csproj`)

---

## 📚 Ressurser

- **xUnit Dokumentasjon:** https://xunit.net/docs/getting-started/netcore
- **Moq Dokumentasjon:** https://github.com/moq/moq4
- **Entity Framework Testing:** https://learn.microsoft.com/en-us/ef/core/testing/

---

## 👥 Bidrag

Når du legger til nye tester:
1. ✅ Bruk AAA-pattern
2. ✅ Navngi tester beskrivende
3. ✅ Legg til norske kommentarer for komplekse setups
4. ✅ Verifiser at testen feiler uten koden den tester
5. ✅ Kjør hele testsuitten før push

---

## 📝 Versjon
- **Sist oppdatert:** Desember 2025
- **.NET version:** 9.0
- **xUnit version:** 2.9.2
- **Moq version:** 4.20.72

