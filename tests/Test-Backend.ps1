$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$composeFile = Join-Path $PSScriptRoot 'compose.yaml'
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(20)
$script:checks = 0

function Invoke-Compose {
    param([string[]]$DockerArguments)
    & docker compose -f $composeFile @DockerArguments
    if ($LASTEXITCODE -ne 0) { throw "Docker Compose ha restituito $LASTEXITCODE" }
}
function Assert-True($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}
function Send-Request {
    param([string]$Method, [string]$Path, [int]$Status, $Body = $null, [string]$Origin = '')
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), "$script:baseUrl$Path")
    try {
        if ($null -ne $Body) {
            $json = if ($Body -is [string]) { $Body } else { ConvertTo-Json -InputObject $Body -Compress }
            $request.Content = [System.Net.Http.StringContent]::new($json, [Text.Encoding]::UTF8, 'application/json')
        }
        if ($Origin) { $request.Headers.Add('Origin', $Origin) }
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Assert-True ([int]$response.StatusCode -eq $Status) "$Method $Path : atteso $Status, ricevuto $([int]$response.StatusCode): $text"
            $data = if ($text) { ConvertFrom-Json -InputObject $text } else { $null }
            return @{ Data = $data; Headers = $response.Headers.ToString() }
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}
try {
    Invoke-Compose -DockerArguments @('up', '--build', '-d', '--wait')
    $binding = Invoke-Compose -DockerArguments @('port', 'api', '3000')
    $script:baseUrl = "http://$($binding.Trim())"
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try { $health = Send-Request GET /health 200; $ready = $true; break }
        catch { Start-Sleep -Seconds 1 }
    }
    Assert-True $ready 'Il backend non risponde'
    Assert-True ($health.Data.status -eq 'ok' -and $health.Data.databaseTime) 'Health check errato'
    $list = Send-Request GET /contacts 200
    Assert-True (@($list.Data).Count -eq 2) 'Dati iniziali non letti'
    $contact = @{ firstName = '  Ada  '; lastName = ' Lovelace '; phone = ' 123456 '; email = ''; notes = ' Test ' }
    $created = Send-Request POST /contacts 201 $contact
    $id = $created.Data.id
    Assert-True ($created.Data.first_name -eq 'Ada' -and $created.Data.last_name -eq 'Lovelace') 'Normalizzazione o contratto JSON errato'
    Assert-True ($created.Data.email -eq $null -and $created.Data.notes -eq 'Test' -and $created.Data.created_at) 'Campi opzionali o timestamp errati'
    Assert-True ($created.Headers -match "Location: /contacts/$id") 'Header Location mancante'
    $read = Send-Request GET "/contacts/$id" 200
    Assert-True ($read.Data.phone -eq '123456') 'Lettura errata'
    $search = Send-Request GET '/contacts?search=LoVeLaCe' 200
    Assert-True (@($search.Data).Count -eq 1 -and $search.Data[0].id -eq $id) 'Ricerca case insensitive errata'
    $injection = Send-Request GET '/contacts?search=%27%20OR%201%3D1%20--' 200
    Assert-True (@($injection.Data).Count -eq 0) 'Input interpretato come SQL'
    $duplicate = Send-Request POST /contacts 409 $contact
    Assert-True ($duplicate.Data.error -eq 'Numero di telefono già presente') 'Errore duplicato incompatibile'
    $contact.phone = '+39 333 1111111'
    $null = Send-Request PUT "/contacts/$id" 409 $contact
    $contact.phone = '654321'
    $contact.email = 'ada@example.com'
    $updated = Send-Request PUT "/contacts/$id" 200 $contact
    Assert-True ($updated.Data.email -eq 'ada@example.com') 'Aggiornamento errato'
    $read = Send-Request GET "/contacts/$id" 200
    Assert-True ($read.Data.phone -eq '654321') 'Aggiornamento non persistito'
    foreach ($invalid in @(
        @{ firstName = ' '; lastName = 'Test'; phone = '123' },
        @{ firstName = ('x' * 81); lastName = 'Test'; phone = '123' },
        @{ firstName = 'Test'; lastName = ('x' * 81); phone = '123' },
        @{ firstName = 'Test'; lastName = 'Test'; phone = ('1' * 31) },
        @{ firstName = 'Test'; lastName = 'Test'; phone = '123'; email = 'non-valida' },
        @{ firstName = 'Test'; lastName = 'Test'; phone = '123'; email = (('x' * 145) + '@a.com') }
    )) {
        $invalidResponse = Send-Request POST /contacts 400 $invalid
        Assert-True ([bool]$invalidResponse.Data.error) 'Validazione senza messaggio'
        $null = Send-Request PUT "/contacts/$id" 400 $invalid
    }
    $null = Send-Request POST /contacts 400 '{'
    $null = Send-Request POST /contacts 400 'null'
    $null = Send-Request POST /contacts 400 '{"firstName":123}'
    foreach ($path in @('/contacts/0', '/contacts/abc', '/contacts/2147483648', '/missing', '/contacts/2147483647')) {
        $null = Send-Request GET $path 404
    }
    $null = Send-Request PUT /contacts/2147483647 404 $contact
    $null = Send-Request DELETE /contacts/2147483647 404
    $cors = Send-Request GET /contacts 200 -Origin 'http://localhost:5174'
    Assert-True ($cors.Headers -match 'Access-Control-Allow-Origin: http://localhost:5174') 'Origine consentita rifiutata'
    $cors = Send-Request GET /contacts 200 -Origin 'http://untrusted.example'
    Assert-True ($cors.Headers -notmatch 'Access-Control-Allow-Origin') 'Origine non consentita accettata'
    $null = Send-Request DELETE "/contacts/$id" 204
    $null = Send-Request GET "/contacts/$id" 404
    Invoke-Compose -DockerArguments @('stop', 'postgres')
    $unavailable = Send-Request GET /health 503
    Assert-True ($unavailable.Data.error -eq 'Database non disponibile') 'Health check non segnala il guasto'
    $failure = Send-Request GET /contacts 500
    Assert-True ($failure.Data.error -eq "Impossibile completare l'operazione") 'Errore espone dettagli interni'
    Write-Host "$script:checks verifiche superate."
} finally {
    $client.Dispose()
    Invoke-Compose -DockerArguments @('down', '--volumes', '--remove-orphans')
}
