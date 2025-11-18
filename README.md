# NRLAPPfinal

# NRLApp – ASP.NET Core MVC + MariaDB + Leaflet

En enkel webapp for å registrere luftfartshindre. Brukeren fyller ut skjema, klikker i kartet for posisjon, og sender inn. Data lagres i MariaDB og vises i en bekreftelsesside + i en liste.

## Hvordan kjøre koden

### Forutsetninger
- Docker Desktop
- (Valgfritt) Rider (Mac) / VS (Windows) for lokal kjøring

### A) Kjør alt med Docker Compose

docker compose down -v          # resetter DB-volumet (bruk ved passordendring). Sørg for at containere er slettet/ikke i bruk i Docker Desktop før koden kjøres. 
docker compose up --build

### Systemarkitektur

Nettleser (Razor + Leaflet)
   │  GET/POST
ASP.NET Core MVC
   ├─ Controller: ObstacleController (GET/POST + lagring)
   ├─ Views: DataForm / Overview / List
   ├─ Model: ObstacleData (validering)
   └─ Dapper + MySqlConnector
         └─ MariaDB (Docker volume)


Teknologi: .NET 9, MariaDB 11, Dapper, Leaflet/OSM, Bootstrap, Docker/Compose.

### Testing og resultater

Åpne skjema: Når jeg går til /Obstacle/DataForm lastes et responsivt skjema med et Leaflet-kart.

Velge posisjon i kartet: Når jeg klikker i kartet, vises en marker, og skjulte felter (Latitude/Longitude) fylles automatisk.

Gyldig innsending: Når jeg fyller inn navn/høyde/beskrivelse, klikker i kartet og sender inn, kommer jeg til Overview. Der ser jeg alle verdier jeg sendte inn, og et lite kart med markøren på riktig posisjon.

Uten kartklikk: Hvis jeg prøver å sende inn uten å klikke i kartet, får jeg en tydelig feilmelding (“Klikk i kartet for å velge posisjon.”), og skjemaet vises igjen.

Validering av høyde: Setter jeg høyde til 0 eller negativt, får jeg valideringsfeil og kan ikke sende inn før det er rettet.

Desimaler: Høyde med desimal (10,5 eller 10.5) aksepteres — appen er satt til en-US kultur og input-feltet bruker step="any".

### Bootstrap

Bootstrap brukes for enkel og responsiv layout sammen med KI (ChatGPT) 
 
 ## Appsettings.json

 Appsettings.json brukes til å konfigurere applikasjonen i ASP.NET Core.

 ###
 Denne delen definerer tilkoblingen til MariaDB-databasen.

 **Server** er satt til "127.0.0.1". 
 **Port** er satt til 3307.
 **Database** er satt til "nrl"
 **User ID** for å koble til databasen, satt til "nrl_user".
 **Password** for brukeren, satt til "nrl_pass".
 **SslMode** er satt til "None" fordi den ikke brukes.

 ### Logging
 Definerer hvor detaljerte loggene skal være under kjøring av applikasjonen.

 **Default** Styrer standard loggnivå til "Information". Dette gjør at advarsler og feil vil bli logget.
 **Microsoft.AspNetCore** Angir loggnivå for ASP.NET core.

 ### AllowedHosts
 Angir hvilke verter som er tillatt å få tilgang til applikasjonen. "*" betyr at alle verter er tillatt.
 Bør begrenses for bedre sikkerhet i produksjon.

 ## Appsettings.Development.json

 Appsettings.Development.json brukes til å overstyre innstillinger i Appsettings.json.

 ### ConnectionStrings

 Denne seksjonen definerer hvordan applikasjonen kobler til databasen under lokal utvikling.

 **Server** Satt til "localhost" for lokal utvikling.
 **Port** Porten databasen bruker, satt til 3307, en alternativ MySQL-port for å unngå konflikt med standardporten 3306
 **Database** Navnet på databasen.
 **User ID** Brukernavnet for å koble til databasen, satt til root.
 **Password** Passordet for root-brukeren, satt til "nrl_pass".
 **SslMode** Angir at SSL ikke brukes, satt til "None".

