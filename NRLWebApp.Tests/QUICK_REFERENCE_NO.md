# NRLWebApp Testsuite - Quick Reference

## 🚀 Raskt Startsett

### Installer avhengigheter
```bash
dotnet restore
```

### Kjør alle tester
```bash
dotnet test
```

### Kjør spesifikk test
```bash
dotnet test --filter "ClassName=NRLWebApp.Tests.Controllers.PilotControllerTests"
```

### Kjør med detaljert output
```bash
dotnet test -v normal
```

---

## 📂 Filstruktur

```
NRLWebApp.Tests/
├── Controllers/
│   ├── PilotControllerTests.cs          [28 tester]  Pilot registrering
│   ├── RegisterforerControllerTests.cs  [12 tester]  Godkjenning
│   ├── AdminControllerTests.cs          [13 tester]  Brukerbehandling
│   └── HomeControllerTests.cs           [3 tester]   Offentlige sider
├── Middleware/
│   └── CspMiddlewareTests.cs            [12 tester]  Sikkerhetshoder
├── Mocks/
│   ├── MockUserManager.cs               Mock UserManager
│   ├── MockRoleManager.cs               Mock RoleManager
│   └── TestDbContext.cs                 In-memory DB
├── README_NO.md                         📖 Full dokumentasjon (norsk)
└── ENDRINGER_SAMMENFATNING.md           📋 Oversikt over endringer
```

---

## 🧪 Testnavngiving Pattern

```
[MethodName]_[Scenario]_[ExpectedResult]
```

### Eksempler
| Test | Hva den tester |
|------|-------------|
| `QuickRegister_Post_ValidGeometry_SavesToDatabase` | Rask registrering lagres |
| `ApproveObstacle_ValidModel_ChangesStatusToApproved` | Godkjenning endrer status |
| `AdminDashboard_WithNoUsers_ReturnsZeroCounts` | Tom liste gir 0-teller |

---

## 📋 Hver Testklasse

### PilotControllerTests
| Test | Formål |
|------|--------|
| RegisterType_Get | Viser registreringstype-valg |
| QuickRegister | Rask registrering med GPS |
| FullRegister | Komplett registrering |
| MyRegistrations | Viser pilotens hindringer |
| Overview | Detaljer om hindring |
| DeleteRegistration | Sletter hindring |

### RegisterforerControllerTests  
| Test | Formål |
|------|--------|
| ApproveObstacle | Godkjenner hindring |
| RejectObstacle | Avviser hindring |
| PendingObstacles | Viser ventende hindringer |
| ApprovedObstacles | Viser godkjente hindringer |

### AdminControllerTests
| Test | Formål |
|------|--------|
| AdminDashboard | Brukerstatistikk |
| ApproveUser | Godkjenner bruker |
| RejectUser | Avviser bruker |
| EditUser | Oppdaterer brukerdata |
| DeleteUser | Sletter bruker |
| AdminUsers | Viser alle brukere |

### CspMiddlewareTests
| Test | Formål |
|------|--------|
| Nonce-generering | Unik, sikker nonce |
| CSP-direktiver | Sikkerhetspolicy er satt |
| Sikkerhetshoder | X-Frame-Options, osv |
| HTTPS-håndtering | HSTS på HTTPS |

---

## 💾 Eksempel: Skrive Ny Test

```csharp
[Fact]
public async Task MyFeature_GivenScenario_ReturnsExpected()
{
    // ARRANGE - Oppsett
    var testData = new MyEntity { Id = 1, Name = "Test" };
    _testContext.MyEntities.Add(testData);
    await _testContext.SaveChangesAsync();
    
    // ACT - Utførelse
    var result = await _testController.MyMethod(1);
    
    // ASSERT - Validering
    Assert.NotNull(result);
    Assert.Equal("Test", result.Name);
}
```

---

## 🔧 Mock-Eksempler

### Mock UserManager
```csharp
var mockUserManager = MockUserManager.Create();

// Mock spesifikk bruker
mockUserManager
    .Setup(um => um.FindByIdAsync("user1"))
    .ReturnsAsync(new ApplicationUser { Id = "user1", Email = "test@test.no" });

// Mock brukererliste
var users = new List<ApplicationUser> { /* ... */ };
MockUserManager.SetupUsersList(mockUserManager, users);
```

### Mock RoleManager
```csharp
var mockRoleManager = MockRoleManager.GetMockRoleManager();

// Alle standard operasjoner er allerede satt opp
```

### In-Memory Database
```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
var context = new ApplicationDbContext(options);
```

---

## 🐛 Vanlige Problemer

| Problem | Løsning |
|---------|---------|
| Test feiler med timeout | Sjekk at mocks er satt opp før test kjører |
| Mock returnerer null | Verifiser `.Setup()` kalles før handlingen |
| Database locked | Bruk `Guid.NewGuid()` som database-navn |
| Test finnes ikke | Sjekk at testklasse er public og har `[Fact]` |

---

## ✅ Før Du Pusher Kode

```bash
# 1. Kjør alle tester
dotnet test

# 2. Verifiser ingen ny warnings
# 3. Verifiser dine nye tester kjører
# 4. Sjekk norske kommentarer er lagt til
# 5. Verifiser AAA-pattern brukes
# 6. Push!
```

---

## 📞 Ressurser

- 📖 **Full dokumentasjon**: `README_NO.md`
- 📋 **Endringsoversikt**: `ENDRINGER_SAMMENFATNING.md`
- 🔗 **xUnit docs**: https://xunit.net
- 🔗 **Moq docs**: https://github.com/moq/moq4
- 🔗 **EF Core testing**: https://docs.microsoft.com/en-us/ef/core/testing/

---

**Status**: ✅ Alle 68 tester kjører | **Coverage**: ~85% | **Dokumentasjon**: norsk

