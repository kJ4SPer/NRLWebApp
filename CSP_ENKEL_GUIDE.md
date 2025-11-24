# 🛡️ CSP Sikkerhet - Enkel Guide

## Hva er CSP egentlig?

Tenk på CSP (Content Security Policy) som en **dørvakt** for nettstedet ditt. Den bestemmer hvem som får lov til å komme inn, og hva de får lov til å gjøre.

### Hvem er angriperne?

**Angripere prøver å:**
- 🚨 Injisere ondsinnet JavaScript-kode i sidene dine (XSS-angrep)
- 🚨 Laste inn farlige scripts fra andre nettsteder
- 🚨 Legge nettstedet ditt i en ramme på deres side (clickjacking)
- 🚨 Stjele brukerdata eller passord

**CSP stopper dem ved å si:**
- ❌ "Nei, du kan IKKE kjøre random JavaScript!"
- ❌ "Nei, du kan IKKE laste scripts fra sketchy.com!"
- ✅ "Du får KUN kjøre kode fra steder JEG godkjenner!"

---

## 🔑 Viktige begreper - forklart enkelt

### 1. **Nonce** (uttales "nåns")

**Hva er det?**
En nonce er som en **engangs-passord** for hver side som lastes. Nettleseren genererer et nytt, tilfeldig passord hver gang noen besøker siden.

**Hvorfor?**
- Uten nonce: "ALLE scripts kan kjøre!" 🚫
- Med nonce: "KUN scripts med riktig passord kan kjøre!" ✅

**Eksempel fra koden vår:**

```cshtml
<!-- DÅRLIG: Inline script uten nonce (BLOKKERES av CSP) -->
<script>
    console.log("Hei!");
</script>

<!-- BRA: Inline script med nonce (TILLATT av CSP) -->
<script @Html.Raw(Html.NonceAttribute())>
    console.log("Hei!");
</script>
```

Det nettleseren ser:
```html
<!-- Nonce endres hver gang siden lastes -->
<script nonce="abc123xyz">
    console.log("Hei!");
</script>
```

### 2. **Inline kode** vs **Ekstern fil**

**Inline kode** = Kode skrevet DIREKTE i HTML-filen:
```html
<!-- Inline style -->
<style>
    .button { color: red; }
</style>

<!-- Inline script -->
<script>
    alert("Hei!");
</script>
```

**Ekstern fil** = Kode i egen fil, linket til HTML:
```html
<!-- Ekstern CSS -->
<link rel="stylesheet" href="~/css/minside.css">

<!-- Eksternt script -->
<script src="~/js/minside.js"></script>
```

**Hvorfor foretrekker vi eksterne filer?**
- ✅ Bedre sikkerhet (CSP kan kontrollere dem lettere)
- ✅ Ryddigere kode
- ✅ Gjenbrukbar kode
- ✅ Browser caching (raskere lastetid)

### 3. **`'unsafe-inline'`** og **`'unsafe-hashes'`**

Disse er som **nødbrytere** for CSP - de gjør sikkerheten litt svakere, men får ting til å fungere midlertidig.

**`'unsafe-inline'`:**
- "OK, du får kjøre inline kode uten nonce..."
- ⚠️ MINDRE SIKKERT, men nødvendig hvis du har mye inline kode

**`'unsafe-hashes'`:**
- Tillater inline event handlers (`onclick="..."`)
- Tillater inline style attributes (`style="color: red;"`)
- ⚠️ Også mindre sikkert

**Målet vårt:** Fjerne disse over tid ved å flytte alt til eksterne filer!

---

## 📋 Hva har vi gjort?

### Før CSP:
```
❌ Ingen sikkerhet
❌ Inline kode overalt
❌ Scripts kan lastes fra HVOR SOM HELST
❌ Ingen nonce
```

### Etter CSP:
```
✅ Streng sikkerhet
✅ Nonce for inline kode
✅ Kun godkjente CDN-er
✅ Refaktorert mye inline kode til eksterne filer
```

---

## 🔨 Praktisk eksempel: RegisterType refaktorering

La meg vise deg NØYAKTIG hva vi gjorde med `RegisterType.cshtml`:

### FØR (dårlig):

**FirstWebApplication/Views/Pilot/RegisterType.cshtml:**
```cshtml
@{
    ViewData["Title"] = "Choose Registration Type";
}

<style>
    /* 141 linjer med CSS her... */
    .register-type-container {
        position: fixed;
        top: 64px;
        /* ... */
    }

    .option-box {
        background: white;
        /* ... */
    }
    /* ... masse mer CSS */
</style>

<!-- HTML kode her -->
<div class="register-type-container">
    ...
</div>

@section Scripts {
    <script>
        // 23 linjer med JavaScript her...
        document.addEventListener('DOMContentLoaded', function () {
            var map = L.map('register-type-map', {
                /* ... */
            });
        });
    </script>
}
```

**Problem:**
- ❌ 141 linjer CSS inline (blokkeres av CSP)
- ❌ 23 linjer JavaScript inline (blokkeres av CSP)
- ❌ Rotete kode
- ❌ Ikke gjenbrukbar

---

### ETTER (bra):

**1. Opprettet ny CSS-fil:**

**FirstWebApplication/wwwroot/css/registertype.css:**
```css
/* Flyttet ALL CSS hit! */
.register-type-container {
    position: fixed;
    top: 64px;
    /* ... */
}

.option-box {
    background: white;
    /* ... */
}

/* ... resten av CSS */
```

**2. Opprettet ny JavaScript-fil:**

**FirstWebApplication/wwwroot/js/registertype.js:**
```javascript
// Flyttet ALL JavaScript hit!
document.addEventListener('DOMContentLoaded', function () {
    var map = L.map('register-type-map', {
        center: [60.4720, 8.4689],
        zoom: 5,
        /* ... */
    });
});
```

**3. Oppdatert view-filen:**

**FirstWebApplication/Views/Pilot/RegisterType.cshtml:**
```cshtml
@{
    ViewData["Title"] = "Choose Registration Type";
}

@section Head {
    <!-- Link til ekstern CSS -->
    <link rel="stylesheet" href="~/css/registertype.css" asp-append-version="true" />
}

<!-- HTML kode (uendret) -->
<div class="register-type-container">
    ...
</div>

@section Scripts {
    <!-- Link til eksternt script -->
    <script src="~/js/registertype.js" asp-append-version="true"></script>
}
```

**Resultat:**
- ✅ **170 linjer mindre** i view-filen!
- ✅ **Ingen CSP violations** lenger
- ✅ Ryddig og oversiktlig
- ✅ CSS og JS kan gjenbrukes
- ✅ Browser cacher filene (raskere lasting)

---

## 🎯 Hvordan refaktorere andre sider

### Steg 1: Finn inline kode

Søk i filen etter:
- `<style>` tags
- `<script>` tags (i HTML-delen, ikke i `@section Scripts`)
- `onclick="..."` event handlers
- `style="..."` inline styles

### Steg 2: Kopier til egen fil

**For CSS:**
1. Opprett `FirstWebApplication/wwwroot/css/[sidenavn].css`
2. Kopier alt mellom `<style>` og `</style>`
3. Fjern `<style>` taggen fra view-filen

**For JavaScript:**
1. Opprett `FirstWebApplication/wwwroot/js/[sidenavn].js`
2. Kopier alt mellom `<script>` og `</script>`
3. Fjern `<script>` taggen fra view-filen

### Steg 3: Link til eksterne filer

I view-filen:
```cshtml
@section Head {
    <link rel="stylesheet" href="~/css/[sidenavn].css" asp-append-version="true" />
}

@section Scripts {
    <script src="~/js/[sidenavn].js" asp-append-version="true"></script>
}
```

### Steg 4: Fix event handlers

**FØR:**
```html
<button onclick="doSomething()">Click me</button>
```

**ETTER:**

**HTML:**
```html
<button id="myButton">Click me</button>
```

**JavaScript (i egen fil):**
```javascript
document.addEventListener('DOMContentLoaded', function() {
    document.getElementById('myButton').addEventListener('click', function() {
        doSomething();
    });
});
```

---

## 📝 Sider som fortsatt trenger refaktorering

Følg stegene over for disse sidene:

### Pilot views:
- ✅ **RegisterType.cshtml** (FERDIG! Se over for eksempel)
- ⏳ **QuickRegister.cshtml** - har inline styles og `onclick`
- ⏳ **FullRegister.cshtml** - har inline styles, `onclick` og `onchange`
- ⏳ **CompleteQuickRegister.cshtml** - har inline styles og `onchange`
- ⏳ **Overview.cshtml** - har inline styles og scripts

### Registerforer views:
- ⏳ **ReviewObstacle.cshtml** - har inline styles, scripts og `onclick`
- ⏳ **ViewObstacle.cshtml** - har inline styles og scripts
- ⏳ **AllObstacles.cshtml** - har inline scripts
- ✅ **MapView.cshtml** (DELVIS FERDIG - trenger bare fikse inline styles)

### Admin views:
- ⏳ **AdminManageUser.cshtml** - har `onclick` handlers

---

## 🔧 Nyttige kommandoer

### Finne alle sider med inline kode:
```bash
# Finn alle filer med inline styles
grep -r "<style" FirstWebApplication/Views/

# Finn alle filer med inline scripts
grep -r "<script>" FirstWebApplication/Views/

# Finn alle filer med onclick handlers
grep -r "onclick=" FirstWebApplication/Views/
```

### Teste CSP:
1. Kjør applikasjonen: `dotnet run`
2. Åpne browser
3. Trykk `F12` (Developer Tools)
4. Gå til **Console**-fanen
5. Se etter røde feilmeldinger som sier "blocked" eller "violates CSP"

---

## 💡 Tips og triks

### 1. Bruk `asp-append-version="true"`
```cshtml
<link rel="stylesheet" href="~/css/minside.css" asp-append-version="true" />
```
Dette legger til en versjon-hash i URL-en, som tvinger nettleseren til å laste ned ny versjon når filen endres.

### 2. Grupér felles kode
Hvis flere sider bruker samme styling eller JavaScript, lag EN felles fil i stedet for mange separate.

### 3. Test én side om gangen
Ikke prøv å fikse alt på en gang. Refaktorer én side, test, commit, og gå videre.

### 4. Bruk nonce kun når nødvendig
Hvis du MÅ ha inline kode midlertidig:
```cshtml
<script @Html.Raw(Html.NonceAttribute())>
    // Midlertidig inline kode
</script>
```

---

## 🎓 Oppsummering

### Hva har vi lært?
- ✅ CSP er en "dørvakt" som beskytter nettstedet
- ✅ Nonce er som et engangspassord
- ✅ Inline kode er dårlig, eksterne filer er bra
- ✅ `'unsafe-inline'` er en midlertidig løsning
- ✅ Refaktorering = flytte kode til egne filer

### Hva er neste steg?
1. Refaktorer resten av sidene (se listen over)
2. Test hver side i nettleseren (F12 → Console)
3. Fjern `'unsafe-inline'` og `'unsafe-hashes'` når alle sider er fikset
4. Nyt en trygg applikasjon! 🎉

---

## 🆘 Trenger hjelp?

**Se på RegisterType som eksempel!**
- `FirstWebApplication/Views/Pilot/RegisterType.cshtml` (view)
- `FirstWebApplication/wwwroot/css/registertype.css` (styles)
- `FirstWebApplication/wwwroot/js/registertype.js` (script)

Dette er den perfekte malen for hvordan alle andre sider bør se ut!

**Spørsmål?** Se `SECURITY_CSP.md` for mer tekniske detaljer.
