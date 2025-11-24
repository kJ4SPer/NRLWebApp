# Content Security Policy (CSP) Implementation

## Oversikt

Dette prosjektet implementerer en streng Content Security Policy (CSP) for å beskytte mot XSS (Cross-Site Scripting), data injection og clickjacking-angrep.

## Hva er CSP?

Content Security Policy er en sikkerhetsfunksjon som lar deg kontrollere hvilke ressurser nettleseren har lov til å laste. Det fungerer som en "hviteliste" av tillatte kilder for scripts, styles, bilder, osv.

## Truslene CSP beskytter mot

### 1. XSS (Cross-Site Scripting)
- **Problem**: Angripere injiserer ondsinnet JavaScript i applikasjonen
- **CSP-beskyttelse**: Blokkerer inline scripts og kun tillater scripts fra godkjente kilder

### 2. Data Injection
- **Problem**: Uønskede scripts lastes fra eksterne kilder
- **CSP-beskyttelse**: Kun ressurser fra whitelistede domener kan lastes

### 3. Clickjacking
- **Problem**: Nettstedet blir lagt i en iframe på ondsinnede sider
- **CSP-beskyttelse**: `frame-ancestors 'none'` blokkerer all iframe-embedding

## Implementeringsdetaljer

### Arkitektur

```
┌─────────────────────────────────────────────┐
│  HTTP Request                               │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  CspMiddleware (FirstWebApplication/        │
│   Middleware/CspMiddleware.cs)              │
│                                             │
│  1. Genererer kryptografisk sikker nonce   │
│  2. Lagrer i HttpContext.Items             │
│  3. Bygger CSP header                      │
│  4. Legger til security headers            │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  Razor Views                                │
│                                             │
│  Bruker @Html.NonceAttribute() for å       │
│  hente nonce og legge til script-tags      │
└─────────────────────────────────────────────┘
```

### Filstruktur

```
FirstWebApplication/
├── Middleware/
│   └── CspMiddleware.cs          # CSP middleware med nonce-generering
├── Helpers/
│   └── CspHelper.cs               # HTML Helper for å hente nonce i views
├── Views/
│   ├── _ViewImports.cshtml        # Importerer CspHelper
│   └── Shared/
│       └── _Layout.cshtml          # Hovedlayout uten inline kode
├── wwwroot/
│   ├── css/
│   │   ├── layout.css             # Styles fra _Layout.cshtml
│   │   └── mapview.css            # Styles fra MapView.cshtml
│   └── js/
│       ├── layout.js              # Menu og dark mode JavaScript
│       └── mapview.js             # Kart-funksjonalitet
└── Program.cs                      # Registrerer CSP middleware
```

### CSP Header-konfigurasjon

Middlewaren genererer følgende CSP policy:

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' 'nonce-{random}' https://unpkg.com;
  style-src 'self' 'nonce-{random}' https://unpkg.com;
  img-src 'self' data: https://*.tile.openstreetmap.org;
  font-src 'self';
  connect-src 'self';
  media-src 'self';
  object-src 'none';
  frame-ancestors 'none';
  base-uri 'self';
  form-action 'self';
  block-all-mixed-content;
  upgrade-insecure-requests;
```

#### Forklaring av hver direktiv:

| Direktiv | Verdi | Forklaring |
|----------|-------|------------|
| `default-src` | `'self'` | Standard: kun samme origin |
| `script-src` | `'self' 'nonce-{random}' https://unpkg.com` | Scripts fra samme origin, med nonce, eller Leaflet CDN |
| `style-src` | `'self' 'nonce-{random}' https://unpkg.com` | Styles fra samme origin, med nonce, eller Leaflet CDN |
| `img-src` | `'self' data: https://*.tile.openstreetmap.org` | Bilder fra samme origin, data URIs, og OSM tiles |
| `font-src` | `'self'` | Fonter kun fra samme origin |
| `connect-src` | `'self'` | AJAX/fetch kun til samme origin |
| `media-src` | `'self'` | Media kun fra samme origin |
| `object-src` | `'none'` | Blokkerer Flash, Java, etc. |
| `frame-ancestors` | `'none'` | Blokkerer alle iframes (clickjacking-beskyttelse) |
| `base-uri` | `'self'` | Begrenser `<base>` tag |
| `form-action` | `'self'` | Forms kan kun submitte til samme origin |
| `block-all-mixed-content` | - | Blokkerer HTTP på HTTPS-sider |
| `upgrade-insecure-requests` | - | Oppgraderer HTTP til HTTPS |

### Andre Security Headers

I tillegg til CSP, setter middlewaren følgende headers:

| Header | Verdi | Beskyttelse |
|--------|-------|-------------|
| `X-Content-Type-Options` | `nosniff` | Forhindrer MIME-sniffing |
| `X-Frame-Options` | `DENY` | Ekstra clickjacking-beskyttelse |
| `X-XSS-Protection` | `0` | Deaktivert (moderne browsere bruker CSP) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Kontrollerer referrer-informasjon |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains; preload` | Tvinger HTTPS (kun i produksjon) |

## Bruk av Nonce i Views

### Eksempel 1: Inline script med konfigurasjon

```cshtml
@section Scripts {
    <script @Html.Raw(Html.NonceAttribute())>
        window.appConfig = {
            apiUrl: '@Url.Action("GetData", "Api")'
        };
    </script>
    <script src="~/js/app.js" asp-append-version="true"></script>
}
```

### Eksempel 2: Inline style (unngå hvis mulig)

```cshtml
<style @Html.Raw(Html.NonceAttribute())>
    .custom-style {
        color: red;
    }
</style>
```

**Merk**: Prøv alltid å unngå inline styles. Legg dem i separate CSS-filer i stedet.

## Subresource Integrity (SRI)

For eksterne scripts og stylesheets bruker vi SRI-hashes for å verifisere integriteten:

```cshtml
<!-- Leaflet CSS -->
<link rel="stylesheet"
      href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"
      integrity="sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY="
      crossorigin="anonymous" />

<!-- Leaflet JS -->
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"
        integrity="sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo="
        crossorigin="anonymous"></script>
```

### Generere SRI-hash

Hvis du trenger å legge til nye eksterne ressurser:

1. Besøk https://www.srihash.org/
2. Lim inn URL-en til ressursen
3. Kopier den genererte integrity-attributten

Eller bruk kommandolinjen:

```bash
curl -s https://unpkg.com/leaflet@1.9.4/dist/leaflet.js | \
  openssl dgst -sha256 -binary | \
  openssl base64 -A
```

## Utviklingsguide

### Legge til nye inline scripts

❌ **FEIL**:
```cshtml
<script>
    // Dette vil bli blokkert av CSP
    console.log('Hello');
</script>
```

✅ **RIKTIG**:
```cshtml
<!-- Alternativ 1: Bruk nonce -->
<script @Html.Raw(Html.NonceAttribute())>
    console.log('Hello');
</script>

<!-- Alternativ 2: Flytt til ekstern fil (ANBEFALT) -->
<script src="~/js/myfile.js" asp-append-version="true"></script>
```

### Legge til nye eksterne CDN-er

1. Oppdater `CspMiddleware.cs`:
```csharp
$"script-src 'self' 'nonce-{nonce}' https://unpkg.com https://cdn.example.com",
```

2. Legg til SRI-hash i view:
```cshtml
<script src="https://cdn.example.com/library.js"
        integrity="sha256-..."
        crossorigin="anonymous"></script>
```

### Legge til nye inline event handlers

❌ **FEIL**:
```cshtml
<button onclick="handleClick()">Click me</button>
```

✅ **RIKTIG**:
```javascript
// I ekstern JS-fil
document.addEventListener('DOMContentLoaded', function() {
    document.getElementById('myButton').addEventListener('click', handleClick);
});
```

```cshtml
<button id="myButton">Click me</button>
```

## Testing av CSP

### 1. Sjekk browser console

Åpne Developer Tools (F12) og sjekk Console-fanen for CSP-violations:

```
Refused to load the script 'https://evil.com/bad.js'
because it violates the following Content Security Policy directive: ...
```

### 2. CSP Report

Du kan legge til `report-uri` eller `report-to` for å logge violations:

```csharp
// I CspMiddleware.cs
policies.Add($"report-uri /csp-violation-report");
```

### 3. Test med CSP Evaluator

Besøk https://csp-evaluator.withgoogle.com/ og lim inn CSP-policyen din.

## Debugging CSP-problemer

### Problem: Script blir blokkert

**Symptom**: `Refused to execute inline script`

**Løsning**:
1. Flytt scriptet til en ekstern fil (anbefalt)
2. Eller legg til nonce-attributt: `@Html.Raw(Html.NonceAttribute())`

### Problem: Style blir blokkert

**Symptom**: `Refused to apply inline style`

**Løsning**:
1. Flytt styles til ekstern CSS-fil (anbefalt)
2. Eller legg til nonce-attributt (hvis absolutt nødvendig)

### Problem: Ekstern ressurs blir blokkert

**Symptom**: `Refused to load the resource from 'https://...'`

**Løsning**:
1. Legg til domenet i relevant CSP-direktiv i `CspMiddleware.cs`
2. Legg til SRI-hash for scripts og stylesheets

### Problem: Bilder fra external source blir blokkert

**Symptom**: Bilder vises ikke

**Løsning**:
Oppdater `img-src` i `CspMiddleware.cs`:
```csharp
"img-src 'self' data: https://*.tile.openstreetmap.org https://external-domain.com",
```

## Beste praksis

### ✅ DO:
- Bruk eksterne JavaScript og CSS-filer
- Bruk nonce for nødvendige inline scripts
- Bruk SRI for alle eksterne ressurser
- Test CSP i alle browsere
- Dokumenter alle CSP-endringer

### ❌ DON'T:
- Bruk `'unsafe-inline'` eller `'unsafe-eval'`
- Legg til `*` (wildcard) i CSP-direktiver
- Ignorer CSP-violations i console
- Bruk inline event handlers (`onclick`, `onerror`, etc.)
- Lag inline styles (bruk CSS-klasser i stedet)

## Produksjonsmiljø

### Før deploy:

1. ✅ Verifiser at alle tester passerer
2. ✅ Sjekk browser console for CSP-violations
3. ✅ Test funksjonalitet i ulike browsere
4. ✅ Verifiser at HTTPS er aktivert
5. ✅ Sjekk at SRI-hashes er korrekte

### Overvåking:

Legg til CSP reporting for å fange violations i produksjon:

```csharp
// I CspMiddleware.cs
policies.Add("report-uri /api/csp-report");
policies.Add("report-to csp-endpoint");
```

## Ytterligere ressurser

- [MDN: Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
- [CSP Cheat Sheet (OWASP)](https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html)
- [Content Security Policy Reference](https://content-security-policy.com/)
- [SRI Hash Generator](https://www.srihash.org/)
- [CSP Evaluator](https://csp-evaluator.withgoogle.com/)

## Kontakt

For spørsmål om CSP-implementasjonen, kontakt utviklingsteamet eller opprett en issue i prosjektets repository.
