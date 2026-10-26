# MealRotator

Een webapp om avondeten te plannen en te roteren. Je beheert een verzameling gerechten, plant per dag één gerecht, bevestigt achteraf wat je echt gegeten hebt, en de app toont
welke gerechten het **langst geleden** gegeten zijn. Zo komt er vanzelf afwisseling op tafel.

Gebouwd als Blazor Web App (.NET 10, Interactive Server, geen prerendering), MudBlazor, EF Core en
SQLite. Eén gebruiker, geen aanmelding. De volledige UI is Nederlandstalig (nl-BE).

## Schermen

- **Kalender**: één week als rij van zeven dagkaarten (maandag tot zondag) met per dag het geplande
  of gegeten gerecht en de status (*Gepland*, *Niet bevestigd*, *Gegeten*). De kalender opent op de
  huidige week; met de pijltjes ga je een week terug of vooruit. Plannen gaat met één klik via
  de suggesties "Lang niet gegeten".
- **Rotatie**: alle gerechten met wanneer en hoe vaak je ze gegeten hebt. Standaard staat wat nooit
  of het langst geleden gegeten is bovenaan; klik op een kolomkop om anders te sorteren. Zoeken kan
  op naam en filteren op tag. Van daaruit plan je meteen in op een datum.
- **Gerechten**: gerechten toevoegen en verwijderen, en je eigen tags beheren (aanmaken, hernoemen,
  verwijderen) en aan gerechten hangen. Een gerecht kan meerdere tags hebben; er zijn geen
  standaardtags. Plannen kan alleen met een gerecht dat hier bestaat.
- **Gegevens**: alles exporteren naar een JSON-bestand (download in de browser) en een export weer
  importeren (bestand kiezen, maximaal 10 MB).

Weet je niet wat je wilt eten, dan kiest de dobbelsteen rechtsboven een willekeurig gerecht, uit alle
gerechten of uit die met een bepaalde tag. Er wordt niets ingepland.

## Projectstructuur

```
MealRotator.slnx
src/MealRotator.Core/    Domein, EF Core-DbContext + migrations, MealPlanningService (alle regels)
src/MealRotator.Web/     Blazor Web App en de MudBlazor-UI
tests/MealRotator.Tests/ xUnit-tests op de service, tegen een echte SQLite-database
```

Alle business- en datalogica zit in `MealRotator.Core`; de webapp bevat enkel UI en host.

## Starten

```powershell
dotnet run --project src/MealRotator.Web
```

Daarna draait de app op <http://localhost:5080>.

## Testen

```powershell
dotnet test MealRotator.slnx
```

Elke test krijgt een eigen tijdelijk SQLite-bestand waarop de echte migrations draaien.

## Data

De database staat standaard in `App_Data/mealrotator.db` onder de projectmap van `MealRotator.Web`.
Een andere map kiezen kan met de instelling `DataDirectory` in `appsettings.json`, of met de
omgevingsvariabele `MEALROTATOR_DATA_DIR` (die wint), bijvoorbeeld om met testdata te werken:

```powershell
$env:MEALROTATOR_DATA_DIR = "C:\temp\maaltijdplanner-test"
dotnet run --project src/MealRotator.Web
```

Het schema wordt bij het starten automatisch bijgewerkt (EF Core-migrations). Een schemawijziging
voeg je toe met:

```powershell
dotnet tool install --global dotnet-ef   # eenmalig
dotnet ef migrations add <Naam> --project src/MealRotator.Core --output-dir Data/Migrations
```

De themakeuze (licht/donker) wordt per browser bewaard in `localStorage`. Bij de eerste keer volgt de
app het thema van het systeem.

### Back-up: exporteren en importeren

Via **Gegevens** exporteer je alle gerechten, tags en ingevulde dagen naar een leesbaar JSON-bestand:

```json
{
  "format": "mealplanner",
  "version": 1,
  "exportedAtUtc": "2026-10-18T14:00:00Z",
  "meals":   [ { "name": "Lasagne", "createdAtUtc": "2026-10-01T18:00:00Z", "tags": [ "pasta" ] } ],
  "entries": [ { "date": "2026-10-12", "meal": "Lasagne", "status": "Eaten" } ],
  "tags":    [ "pasta", "snelklaar" ]
}
```

Gerechten en tags worden op naam gekoppeld, niet op database-id. Importeren **voegt samen**: ontbrekende gerechten,
tags en dagen komen erbij; een bestaand gerecht (ongeacht hoofdletters/spaties) en een dag die al
ingevuld is, blijven ongewijzigd. Hetzelfde bestand twee keer importeren voegt niets toe. Alles wordt
in één keer opgeslagen; bij een fout in het bestand verandert er niets. `version` gaat pas omhoog bij een formaatwijziging die
oudere versies niet meer kunnen lezen; zo'n nieuwere export wordt geweigerd met een duidelijke melding.
