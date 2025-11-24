# CSP Implementation - Quick Start Guide

## Hva er gjort?

Vi har implementert en omfattende Content Security Policy (CSP) for å beskytte applikasjonen mot XSS, data injection og clickjacking.

## Endringer i denne implementasjonen

### ✅ Nye filer opprettet:

1. **Middleware/**
   - `CspMiddleware.cs` - Genererer CSP headers og nonces

2. **Helpers/**
   - `CspHelper.cs` - HTML Helper for nonce i Razor views

3. **wwwroot/css/**
   - `layout.css` - Styles fra _Layout.cshtml
   - `mapview.css` - Styles fra MapView.cshtml

4. **wwwroot/js/**
   - `layout.js` - Navigation og dark mode funksjonalitet
   - `mapview.js` - Kart-funksjonalitet

5. **Dokumentasjon/**
   - `SECURITY_CSP.md` - Omfattende dokumentasjon
   - `CSP_IMPLEMENTATION.md` - Denne filen

### 🔄 Modifiserte filer:

1. **Program.cs**
   - Fjernet gammel inline CSP header
   - Registrert ny CspMiddleware

2. **Views/_ViewImports.cshtml**
   - Importerer CspHelper namespace

3. **Views/Shared/_Layout.cshtml**
   - Fjernet alle inline styles (flyttet til layout.css)
   - Fjernet alle inline scripts (flyttet til layout.js)
   - Lagt til SRI for Leaflet CSS/JS

4. **Views/Registerforer/MapView.cshtml**
   - Fjernet alle inline styles (flyttet til mapview.css)
   - Fjernet alle inline scripts (flyttet til mapview.js)
   - Lagt til nonce for konfigurasjonsskript

## Hvordan bruke CSP i nye features

### For inline scripts (bruk kun når nødvendig):

```cshtml
<script @Html.Raw(Html.NonceAttribute())>
    // Din kode her
</script>
```

### For eksterne scripts (anbefalt):

```cshtml
<script src="~/js/minscript.js" asp-append-version="true"></script>
```

### For eksterne CDN-er:

1. Legg til domenet i `CspMiddleware.cs`
2. Bruk SRI-hash:
```cshtml
<script src="https://cdn.example.com/lib.js"
        integrity="sha256-..."
        crossorigin="anonymous"></script>
```

## Neste steg (valgfritt)

Det finnes fortsatt noen inline styles og onclick-handlers i andre views som kan optimaliseres:

- `Views/Pilot/QuickRegister.cshtml`
- `Views/Pilot/FullRegister.cshtml`
- `Views/Pilot/CompleteQuickRegister.cshtml`
- `Views/Registerforer/ReviewObstacle.cshtml`
- `Views/Registerforer/ViewObstacle.cshtml`
- `Views/Home/Index.cshtml`

Følg samme pattern som for _Layout.cshtml og MapView.cshtml for å refaktorere disse.

## Testing

1. Kjør applikasjonen: `dotnet run`
2. Åpne browser console (F12)
3. Sjekk for CSP violations
4. Verifiser at all funksjonalitet virker

## Support

Se `SECURITY_CSP.md` for fullstendig dokumentasjon.

## Sikkerhetsforbedringer

### Før:
- ❌ Inline scripts og styles overalt
- ❌ `'unsafe-inline'` i CSP
- ❌ Ingen SRI for eksterne ressurser
- ❌ Svake security headers

### Etter:
- ✅ Alle scripts i eksterne filer
- ✅ Nonce-basert CSP (ingen `'unsafe-inline'`)
- ✅ SRI for Leaflet CDN
- ✅ Strenge security headers
- ✅ Beskyttelse mot XSS, clickjacking og data injection
