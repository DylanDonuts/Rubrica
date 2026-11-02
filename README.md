# Rubrica

Applicazione web per gestire una rubrica: creazione, consultazione, ricerca,
modifica ed eliminazione dei contatti. Realizzata con React, ASP.NET Core e
PostgreSQL; l'intero ambiente si avvia con Docker Compose.

## Funzionalità

- Contatti con nome, cognome, telefono, email e note.
- Ricerca senza distinzione tra maiuscole e minuscole su nome, cognome,
  telefono ed email.
- Elenco ordinato per cognome e nome.
- Validazione dei dati e gestione dei numeri di telefono duplicati.
- Conferma prima dell'eliminazione e interfaccia adattabile agli schermi piccoli.
- Persistenza dei contatti e amministrazione del database tramite pgAdmin.

## Tecnologie e architettura

| Componente | Tecnologia | Ruolo |
| --- | --- | --- |
| Frontend | React 19, Vite 8, Node.js 24 nel container | Interfaccia e chiamate HTTP |
| Backend | C#, ASP.NET Core 10 Minimal API, Npgsql 10 | Validazione e accesso ai dati |
| Database | PostgreSQL 16 | Memorizzazione dei contatti |
| Amministrazione | pgAdmin 4 | Consultazione e gestione del database |
| Ambiente | Docker Compose | Build, rete e volumi dei quattro servizi |

Il browser carica il frontend da `http://localhost:5174` e chiama l'API su
`http://localhost:3001`. L'API comunica con PostgreSQL tramite la rete interna
Docker, usando `postgres:5432`.

Il backend separa endpoint, contratti JSON, validazione e repository. Usa query
SQL parametrizzate e asincrone, un pool di connessioni gestito da
`NpgsqlDataSource` e un gestore centralizzato degli errori. Non usa un ORM.
L'immagine backend viene costruita in più stadi ed esegue il processo con un
utente non privilegiato.

## Prerequisiti

- Docker Desktop avviato e configurato per i container Linux, con Docker Compose.
- Git per clonare il repository.
- PowerShell per eseguire lo script di test incluso.

**Non occorre installare Node.js, npm, .NET o PostgreSQL sul computer.**
Compilazione e dipendenze applicative vengono gestite nei container.

Verificare Docker:

```powershell
docker --version
docker compose version
docker info
```

## Avvio con Docker

### 1. Ottenere il progetto

Clonare il repository usando il suo URL GitHub ed entrare nella cartella
`rubrica`. Se il progetto è già sul computer, aprire un terminale
nella cartella che contiene `compose.yaml`.

### 2. Configurare le variabili d'ambiente

Al primo avvio, creare il file locale `.env` dal modello:

```powershell
Copy-Item .env.example .env
```

Se `.env` esiste già, conservarlo. Aprirlo con un editor e impostare:

| Variabile | Significato |
| --- | --- |
| `POSTGRES_USER` | Utente del database |
| `POSTGRES_PASSWORD` | Password del database |
| `POSTGRES_DB` | Nome del database |
| `PGADMIN_DEFAULT_EMAIL` | Email per accedere a pgAdmin |
| `PGADMIN_DEFAULT_PASSWORD` | Password per accedere a pgAdmin |

Sostituire le password dimostrative con credenziali dedicate. Il file `.env` è
escluso da Git; `.env.example` documenta le variabili richieste.
Compose passa le credenziali PostgreSQL al backend come variabili `DB_*`.

### 3. Costruire e avviare i container

```powershell
docker compose up --build -d
docker compose ps
```

La prima esecuzione scarica le immagini e installa le dipendenze nei container.
PostgreSQL viene inizializzato prima dell'avvio del backend e di pgAdmin.
Attendere che l'API risponda al controllo di salute.

### 4. Aprire l'applicazione

| Servizio | Indirizzo dal computer |
| --- | --- |
| Rubrica | [http://localhost:5174](http://localhost:5174) |
| Elenco contatti JSON | [http://localhost:3001/contacts](http://localhost:3001/contacts) |
| Stato API e database | [http://localhost:3001/health](http://localhost:3001/health) |
| pgAdmin | [http://localhost:5051](http://localhost:5051) |
| PostgreSQL | `localhost:5433` |

Le porte sono pubblicate su `127.0.0.1`, quindi sono accessibili soltanto dal
computer locale. Usare `localhost` nel browser: è l'origine prevista dalla
configurazione CORS.

Su un database nuovo sono presenti due contatti dimostrativi: Mario Rossi e
Giulia Bianchi.

## Collegare pgAdmin a PostgreSQL

1. Aprire `http://localhost:5051` e accedere con email e password pgAdmin del `.env`.
2. Selezionare **Register > Server** e scegliere un nome, ad esempio `Rubrica`.
3. Nella scheda **Connection** inserire:
   - **Host name/address:** `postgres.internal`
   - **Port:** `5432`
   - **Maintenance database:** il valore di `POSTGRES_DB`
   - **Username:** il valore di `POSTGRES_USER`
   - **Password:** il valore di `POSTGRES_PASSWORD`
4. Salvare la connessione.

pgAdmin gira in un container: il suo `localhost` indica quel container. L'alias
`postgres.internal` configurato in Compose identifica il database nella rete Docker.
Per un client installato sul computer, usare invece `localhost:5433`.

## Comandi utili

Eseguire questi comandi dalla cartella principale:

```powershell
# Stato dei servizi
docker compose ps

# Log del backend (Ctrl+C per uscire)
docker compose logs -f backend

# Fermare e rimuovere i container conservando i volumi
docker compose down

# Riavviare i servizi
docker compose up -d

# Ricostruire il backend dopo modifiche al codice C#
docker compose up --build -d backend

# Ricostruire il frontend dopo modifiche a dipendenze o Dockerfile
docker compose up --build -d --renew-anon-volumes frontend

# Controllo statico e build del frontend, dentro Docker
docker compose exec frontend npm run lint
docker compose exec frontend npm run build
```

Il frontend monta la cartella locale `frontend/` e usa Vite con aggiornamento
automatico delle modifiche ai sorgenti. Il backend esegue i binari pubblicati
nell'immagine e richiede una ricostruzione per applicare le modifiche.

## Persistenza dei dati

I volumi nominati `rubrica-postgres-data` e `rubrica-pgadmin-data` conservano
rispettivamente database e configurazione di pgAdmin; Docker Compose può
anteporre ai nomi il nome del progetto.

`database/init.sql` crea la tabella `contacts` e inserisce i dati dimostrativi
soltanto quando PostgreSQL inizializza una directory dati vuota. Modificare il
file SQL non aggiorna automaticamente un database già esistente.

Anche le credenziali di inizializzazione non vengono aggiornate semplicemente
modificando `.env` quando i volumi sono già popolati: occorre aggiornare gli
account esistenti e mantenere coerente la configurazione.

**Attenzione:** `docker compose down --volumes` elimina i volumi del progetto,
compresi tutti i contatti e la configurazione di pgAdmin. Non serve per
aggiornare l'applicazione.

## API REST

URL base locale: `http://localhost:3001`.

| Metodo | Percorso | Risultato |
| --- | --- | --- |
| `GET` | `/health` | Verifica API e connessione al database |
| `GET` | `/contacts` | Elenco dei contatti |
| `GET` | `/contacts?search=rossi` | Ricerca dei contatti |
| `GET` | `/contacts/{id}` | Dettaglio di un contatto |
| `POST` | `/contacts` | Creazione, risposta `201` con header `Location` |
| `PUT` | `/contacts/{id}` | Modifica completa, risposta `200` |
| `DELETE` | `/contacts/{id}` | Eliminazione, risposta `204` senza corpo |

Corpo JSON per creazione e modifica:

```json
{
  "firstName": "Ada",
  "lastName": "Lovelace",
  "phone": "+39 333 1234567",
  "email": "ada@example.com",
  "notes": "Contatto di esempio"
}
```

Le risposte dei contatti contengono `id`, `first_name`, `last_name`, `phone`,
`email`, `notes` e `created_at`. Email e note vuote vengono salvate come `null`.

Nome, cognome e telefono sono obbligatori; i limiti sono 80 caratteri per nome
e cognome, 30 per telefono e 150 per email. Se presente, l'email deve avere un
formato valido. Gli spazi iniziali e finali vengono rimossi; il telefono è
univoco come stringa e non viene convertito in un formato telefonico standard.

Gli errori hanno il formato `{"error":"messaggio"}`: `400` per input non valido,
`404` per contatto o rotta inesistente, `409` per telefono duplicato e `500`
per errori interni. `/health` risponde `503` se il database non è disponibile.
Gli ID devono essere interi positivi.

## Test di integrazione

Con Docker Desktop attivo, dalla cartella principale in PowerShell:

```powershell
./tests/Test-Backend.ps1
```

Lo script costruisce il backend e lo esegue con un PostgreSQL reale nel progetto
Compose separato `rubrica-api-tests`, con porta API locale casuale e database
effimero. Verifica CRUD, ricerca, contratto JSON, normalizzazione, duplicati,
validazione, richieste malformate, ID non validi, CORS e indisponibilità del database.

I container di test vengono rimossi nel blocco finale anche in caso di errore.
I test non usano le credenziali applicative del `.env` né i volumi della rubrica.
Non eseguire due istanze dello script contemporaneamente, perché usano lo stesso
nome di progetto Compose. Non è richiesta un'installazione locale di .NET.

## Struttura del progetto

```text
rubrica/
|-- backend/
|   |-- Program.cs                 # Configurazione API, DI, CORS e health
|   |-- ContactEndpoints.cs        # Endpoint CRUD
|   |-- ContactContracts.cs        # DTO, normalizzazione e validazione
|   |-- ContactRepository.cs       # Query SQL parametrizzate
|   |-- DatabaseConfiguration.cs   # Configurazione connessione
|   |-- ApiExceptionHandler.cs     # Gestione centralizzata errori
|   |-- appsettings.json
|   |-- Rubrica.Api.csproj
|   `-- Dockerfile
|-- frontend/
|   |-- src/App.jsx                # Interfaccia e chiamate API
|   |-- src/App.css                # Stili responsive
|   |-- package.json
|   |-- package-lock.json
|   `-- Dockerfile
|-- database/init.sql              # Schema e dati dimostrativi
|-- tests/
|   |-- compose.yaml               # Ambiente isolato dei test
|   `-- Test-Backend.ps1
|-- compose.yaml
|-- .env.example
|-- .gitignore
`-- README.md
```

## Risoluzione dei problemi

- **Docker non risponde:** avviare Docker Desktop e attendere che il motore
  Linux sia pronto; verificare con `docker info`.
- **Porta già occupata:** controllare le porte `5174`, `3001`, `5433` e `5051`.
  Se si cambiano le porte HTTP, aggiornare anche l'URL API in
  `frontend/src/App.jsx` e le origini CORS in `backend/appsettings.json`.
- **Errore durante il caricamento dei contatti:** verificare `/health`,
  `docker compose ps` e `docker compose logs backend postgres`.
- **Credenziali rifiutate dopo una modifica al `.env`:** controllare gli account
  già presenti nei volumi; le variabili servono anche all'inizializzazione e non
  cambiano automaticamente le password esistenti.

## Ambito attuale

La configurazione inclusa è pensata per l'esecuzione locale. Il frontend usa il server di sviluppo
Vite e l'URL dell'API è impostato su `localhost`; il backend è pubblicato in
configurazione Debug. Non sono implementati autenticazione, autorizzazione o
paginazione dell'elenco.

Per una distribuzione pubblica occorrono una build di produzione del frontend,
configurazione degli indirizzi, HTTPS, gestione dei segreti, controllo degli
accessi e backup.
