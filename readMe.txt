RUBRICA TELEFONICA — React, C#, ASP.NET Core 10 e PostgreSQL

ANALISI E ARCHITETTURA
Il frontend React/Vite chiama il backend HTTP, che valida gli input e interroga
PostgreSQL. Docker Compose avvia frontend, backend, database e pgAdmin.
Per questa piccola rubrica CRUD sono sufficienti responsabilità separate e
query esplicite, senza introdurre ulteriori livelli o un ORM.

Il backend Express è stato sostituito da C# su .NET 10. Database e API restano
compatibili: richieste con firstName e lastName, risposte con first_name,
last_name e created_at; errori nel formato { "error": ... }.
I dati esistenti e i volumi PostgreSQL non richiedono conversioni.

FILE PRINCIPALI
- backend/Program.cs: dependency injection, CORS e pipeline HTTP.
- backend/ContactEndpoints.cs: Minimal API per elenco, ricerca e CRUD.
- backend/ContactContracts.cs: DTO immutabili, normalizzazione e validazione.
- backend/ContactRepository.cs: query parametrizzate, I/O asincrono e
  propagazione della cancellazione delle richieste.
- backend/DatabaseConfiguration.cs: connection string costruita dalle variabili
  DB_* oppure letta da ConnectionStrings__Contacts, senza credenziali nel codice.
- backend/ApiExceptionHandler.cs: errori centralizzati, dettagli tecnici nei log.
- backend/appsettings.json: log e origini CORS configurabili.
- backend/Rubrica.Api.csproj: nullable abilitato e warning trattati come errori;
  unica dipendenza applicativa: Npgsql.
- backend/Dockerfile: build multi-stage e runtime con utente non privilegiato.
- database/init.sql: schema e dati dimostrativi, eseguiti solo su volumi nuovi.
- compose.yaml: servizi e porte pubblicate solo su loopback.

PRATICHE ADOTTATE
NpgsqlDataSource è un singleton gestito dal container DI e riutilizza il pool
delle connessioni. Comandi e reader vengono rilasciati con await using.
Le query non concatenano input utente. Il vincolo UNIQUE garantisce l'unicità
del telefono anche con richieste concorrenti; le violazioni restituiscono 409.
La validazione controlla campi obbligatori, email e lunghezze delle colonne,
restituendo 400. Gli ID devono essere interi positivi; ID non validi o contatti
non trovati restituiscono 404. POST restituisce 201 con Location, DELETE 204.
/health interroga il database e restituisce 503 se non è disponibile.
Gli errori conservano il formato atteso dal frontend invece di passare a
Problem Details, per evitare modifiche incompatibili al client esistente.

AVVIO CON DOCKER
Prerequisito: Docker Desktop con Docker Compose.
Configurare nel proprio .env (ignorato da Git): POSTGRES_USER,
POSTGRES_PASSWORD, POSTGRES_DB, PGADMIN_DEFAULT_EMAIL, PGADMIN_DEFAULT_PASSWORD.
Poi, dalla directory del progetto:

    docker compose up --build -d

Frontend: http://localhost:5174
API: http://localhost:3001/contacts
Health: http://localhost:3001/health
pgAdmin: http://localhost:5051
PostgreSQL dal computer: localhost:5433; dai container: postgres:5432.

Il backend usa binari pubblicati: dopo una modifica al C# eseguire:
    docker compose up --build -d backend
Il frontend mantiene il server di sviluppo Vite con aggiornamento automatico.
Non eliminare i volumi per aggiornare il backend: contengono i contatti.

SVILUPPO LOCALE DEL BACKEND
Prerequisito: SDK .NET 10. Avviare PostgreSQL e configurare nella shell
DB_HOST=localhost, DB_PORT=5433, DB_USER, DB_PASSWORD e DB_NAME con i propri
valori. .NET non legge automaticamente il file .env di Compose.
In alternativa impostare ConnectionStrings__Contacts con una connection string
Npgsql. Avviare con:

    dotnet run --project backend/Rubrica.Api.csproj --urls http://localhost:3001

Per cambiare le origini consentite usare Cors__AllowedOrigins__0, __1, ecc.

TEST DI INTEGRAZIONE
Da PowerShell, con Docker Desktop attivo:
    ./tests/Test-Backend.ps1

I test costruiscono l'immagine ed eseguono l'API con PostgreSQL reale in un
progetto Compose separato (rubrica-api-tests), una porta locale casuale e un
database effimero. Verificano CRUD, ricerca, JSON, normalizzazione, duplicati,
validazione, richieste malformate, ID non validi, CORS e guasti al database.
I container di test vengono rimossi anche in caso di errore. Non usano .env
o i volumi della rubrica. Non avviare due esecuzioni dei test contemporaneamente.

LIMITI ATTUALI
Il progetto resta una demo locale: manca l'autenticazione e l'elenco non è
paginato. Per una distribuzione pubblica servono autorizzazione, HTTPS sul punto
di ingresso e una build di produzione del frontend.

RIFERIMENTI
https://www.npgsql.org/doc/basic-usage.html
https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0
