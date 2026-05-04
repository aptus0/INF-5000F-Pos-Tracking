# =====================================================================
# SAMER Hub - Local API Service
# File: SAMER-Hub-ApiService.ps1
# Port: http://127.0.0.1:8189
#
# Bu dosya arka planda çalışan yerel HTTP API servisidir.
# Windows GUI dosyası bu servisle konuşur:
#   SAMER-Hub-App.ps1  ->  http://127.0.0.1:8189
#
# Not:
# - Ingenico MOVE/5000F gerçek ödeme mesaj formatı, bankanın/Ingenico'nun
#   ECR/TCP-IP dokümanındaki protokole göre doldurulmalıdır.
# - Bu servis güvenlik için varsayılan olarak sadece 127.0.0.1 üzerinde dinler.
# =====================================================================

param(
    [int]$Port = 8189,
    [string]$BindAddress = "127.0.0.1"
)

$ErrorActionPreference = "Continue"

$ServiceName = "SAMER Hub Local API Service"
$Version = "1.0.0"
$BaseDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ConfigDir = Join-Path $env:ProgramData "SAMERHub\PowerShellPro"
$ConfigPath = Join-Path $ConfigDir "config.json"
$LogPath = Join-Path $ConfigDir "api-service.log"

if (-not (Test-Path $ConfigDir)) {
    New-Item -Path $ConfigDir -ItemType Directory -Force | Out-Null
}

function Write-ServiceLog {
    param([string]$Message)
    try {
        $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
        Add-Content -Path $LogPath -Value $line -Encoding UTF8
    } catch { }
}

function Test-IsAdmin {
    try {
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = New-Object Security.Principal.WindowsPrincipal($identity)
        return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    } catch {
        return $false
    }
}

function Protect-PlainText {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return "" }
    try {
        $secure = ConvertTo-SecureString $Text -AsPlainText -Force
        return ConvertFrom-SecureString $secure
    } catch {
        return ""
    }
}

function Unprotect-ToPlainText {
    param([string]$Cipher)
    if ([string]::IsNullOrWhiteSpace($Cipher)) { return "" }
    try {
        $secure = ConvertTo-SecureString $Cipher
        $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try {
            return [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
        } finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    } catch {
        return ""
    }
}

function Get-DefaultConfig {
    [pscustomobject]@{
        pcIpMode = "Auto"
        pcIpManual = ""
        posIp = "192.168.1.39"
        posPort = 5577
        posProtocolMode = "FakeSimulation"
        posMessageTemplate = "SALE|{orderNo}|{amountCents}|{currency}"
        posUseStxEtx = $false
        localApiPort = 8189

        trendyolSupplierId = ""
        trendyolApiKey = ""
        trendyolApiSecretProtected = ""
        trendyolBaseUrl = "https://api.trendyol.com/sapigw/suppliers/{supplierId}/orders?status=Created"

        printerName = ""
        receiptTitle = "KARACABEY GROSS MARKET"
        receiptFooter = "Teşekkür ederiz."
    }
}

function Load-Config {
    $def = Get-DefaultConfig

    try {
        if (Test-Path $ConfigPath) {
            $json = Get-Content -Path $ConfigPath -Raw -Encoding UTF8
            if (-not [string]::IsNullOrWhiteSpace($json)) {
                $cfg = $json | ConvertFrom-Json
                foreach ($p in $def.PSObject.Properties.Name) {
                    if (-not ($cfg.PSObject.Properties.Name -contains $p)) {
                        $cfg | Add-Member -MemberType NoteProperty -Name $p -Value $def.$p
                    }
                }
                return $cfg
            }
        }
    } catch {
        Write-ServiceLog "Config load error: $($_.Exception.Message)"
    }

    return $def
}

function Save-Config {
    param($Config)
    try {
        $Config | ConvertTo-Json -Depth 20 | Set-Content -Path $ConfigPath -Encoding UTF8
        return $true
    } catch {
        Write-ServiceLog "Config save error: $($_.Exception.Message)"
        return $false
    }
}

$Global:Config = Load-Config

function ConvertTo-HashtableDeep {
    param($InputObject)

    if ($null -eq $InputObject) { return $null }

    if ($InputObject -is [System.Collections.IEnumerable] -and -not ($InputObject -is [string]) -and -not ($InputObject -is [System.Management.Automation.PSCustomObject])) {
        $arr = @()
        foreach ($item in $InputObject) { $arr += (ConvertTo-HashtableDeep $item) }
        return $arr
    }

    if ($InputObject -is [System.Management.Automation.PSCustomObject]) {
        $hash = @{}
        foreach ($p in $InputObject.PSObject.Properties) {
            $hash[$p.Name] = ConvertTo-HashtableDeep $p.Value
        }
        return $hash
    }

    return $InputObject
}

function Send-Json {
    param(
        [System.Net.HttpListenerContext]$Context,
        $Data,
        [int]$StatusCode = 200
    )

    try {
        $Context.Response.StatusCode = $StatusCode
        $Context.Response.ContentType = "application/json; charset=utf-8"
        $Context.Response.Headers.Add("Access-Control-Allow-Origin", "http://127.0.0.1:$Port")
        $Context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type")
        $Context.Response.Headers.Add("Access-Control-Allow-Methods", "GET,POST,OPTIONS")

        $json = $Data | ConvertTo-Json -Depth 30
        $bytes = [Text.Encoding]::UTF8.GetBytes($json)
        $Context.Response.ContentLength64 = $bytes.Length
        $Context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
    } catch {
        Write-ServiceLog "Send json error: $($_.Exception.Message)"
    } finally {
        try { $Context.Response.OutputStream.Close() } catch { }
    }
}

function Send-ErrorJson {
    param(
        [System.Net.HttpListenerContext]$Context,
        [string]$Message,
        [int]$StatusCode = 500
    )

    Send-Json -Context $Context -StatusCode $StatusCode -Data ([pscustomobject]@{
        ok = $false
        error = $Message
        time = (Get-Date).ToString("s")
    })
}

function Read-JsonBody {
    param([System.Net.HttpListenerRequest]$Request)

    try {
        $reader = New-Object IO.StreamReader($Request.InputStream, $Request.ContentEncoding)
        $raw = $reader.ReadToEnd()
        if ([string]::IsNullOrWhiteSpace($raw)) { return [pscustomobject]@{} }
        return $raw | ConvertFrom-Json
    } catch {
        return [pscustomobject]@{}
    }
}

function Get-PrimaryIPv4Info {
    try {
        $cfgs = Get-NetIPConfiguration -ErrorAction Stop |
            Where-Object {
                $_.IPv4Address -and
                $_.NetAdapter.Status -eq "Up" -and
                $_.IPv4DefaultGateway
            } |
            Select-Object -First 1

        if ($cfgs) {
            return [pscustomobject]@{
                ip = $cfgs.IPv4Address.IPAddress
                prefix = $cfgs.IPv4Address.PrefixLength
                gateway = $cfgs.IPv4DefaultGateway.NextHop
                adapter = $cfgs.InterfaceAlias
                dns = ($cfgs.DNSServer.ServerAddresses -join ", ")
            }
        }
    } catch { }

    try {
        $ip = [System.Net.Dns]::GetHostAddresses($env:COMPUTERNAME) |
            Where-Object { $_.AddressFamily -eq "InterNetwork" -and $_.IPAddressToString -notlike "169.254.*" } |
            Select-Object -First 1

        if ($ip) {
            return [pscustomobject]@{
                ip = $ip.IPAddressToString
                prefix = 24
                gateway = ""
                adapter = ""
                dns = ""
            }
        }
    } catch { }

    return [pscustomobject]@{
        ip = ""
        prefix = 24
        gateway = ""
        adapter = ""
        dns = ""
    }
}

function Test-PrivateIPv4 {
    param([string]$Ip)

    $parsed = $null
    if (-not [System.Net.IPAddress]::TryParse($Ip, [ref]$parsed)) { return $false }

    $parts = $Ip.Split(".") | ForEach-Object { [int]$_ }
    if ($parts.Count -ne 4) { return $false }

    if ($parts[0] -eq 10) { return $true }
    if ($parts[0] -eq 172 -and $parts[1] -ge 16 -and $parts[1] -le 31) { return $true }
    if ($parts[0] -eq 192 -and $parts[1] -eq 168) { return $true }
    if ($parts[0] -eq 127) { return $true }
    if ($parts[0] -eq 169 -and $parts[1] -eq 254) { return $true }

    return $false
}

function Test-TcpPort {
    param(
        [string]$Ip,
        [int]$Port,
        [int]$TimeoutMs = 1200
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($Ip, $Port, $null, $null)
        $ok = $async.AsyncWaitHandle.WaitOne($TimeoutMs, $false)
        if (-not $ok) { return $false }
        $client.EndConnect($async)
        return $true
    } catch {
        return $false
    } finally {
        try { $client.Close() } catch { }
    }
}

function Test-PingFast {
    param(
        [string]$Ip,
        [int]$TimeoutMs = 350
    )

    try {
        $ping = New-Object System.Net.NetworkInformation.Ping
        $reply = $ping.Send($Ip, $TimeoutMs)
        return ($reply.Status -eq [System.Net.NetworkInformation.IPStatus]::Success)
    } catch {
        return $false
    }
}

function Get-MacFromArp {
    param([string]$Ip)

    try {
        $out = arp -a $Ip 2>$null
        foreach ($line in $out) {
            if ($line -match "([0-9A-Fa-f]{2}[-:]){5}[0-9A-Fa-f]{2}") {
                return $Matches[0].ToUpper()
            }
        }
    } catch { }

    return ""
}

function Get-HostnameSafe {
    param([string]$Ip)
    try {
        return ([System.Net.Dns]::GetHostEntry($Ip)).HostName
    } catch {
        return ""
    }
}

function Invoke-LanScan {
    param($Body)

    $subnet = "$($Body.subnet)".Trim()
    $start = [int]$Body.start
    $end = [int]$Body.end
    $timeoutMs = if ($Body.timeoutMs) { [int]$Body.timeoutMs } else { 300 }

    if ($subnet -notmatch "^\d{1,3}\.\d{1,3}\.\d{1,3}$") {
        throw "Subnet formatı hatalı. Örnek: 192.168.1"
    }

    if ($start -lt 1) { $start = 1 }
    if ($end -gt 254) { $end = 254 }
    if ($start -gt $end) { throw "Başlangıç IP değeri bitişten büyük olamaz." }

    $ports = @()
    foreach ($p in @($Body.ports)) {
        try {
            $port = [int]$p
            if ($port -gt 0 -and $port -le 65535) { $ports += $port }
        } catch { }
    }

    $scanPortsWhenNoPing = $false
    try { $scanPortsWhenNoPing = [bool]$Body.scanPortsWhenNoPing } catch { }

    $devices = @()

    for ($i = $start; $i -le $end; $i++) {
        $ip = "$subnet.$i"

        if (-not (Test-PrivateIPv4 $ip)) {
            continue
        }

        $alive = Test-PingFast -Ip $ip -TimeoutMs $timeoutMs
        $open = @()

        if ($alive -or $scanPortsWhenNoPing) {
            foreach ($port in $ports) {
                if (Test-TcpPort -Ip $ip -Port $port -TimeoutMs 250) {
                    $open += $port
                }
            }
        }

        if ($alive -or $open.Count -gt 0) {
            $devices += [pscustomobject]@{
                ip = $ip
                hostname = Get-HostnameSafe $ip
                mac = Get-MacFromArp $ip
                ping = $alive
                openPorts = $open
                cableInfo = "Fiziksel switch portu için yönetilebilir switch + LLDP/SNMP gerekir."
            }
        }
    }

    return $devices
}

function Add-PosFirewallRule {
    param(
        [string]$PosIp,
        [int]$PosPort
    )

    if (-not (Test-IsAdmin)) {
        throw "Firewall kuralı eklemek için uygulamayı Yönetici olarak çalıştırmalısın."
    }

    if (-not (Test-PrivateIPv4 $PosIp)) {
        throw "POS IP adresi private/local ağda değil. Güvenlik için kural eklenmedi: $PosIp"
    }

    $name = "SAMER Hub POS ECR Outbound $PosIp`:$PosPort"
    $existing = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue

    if ($existing) {
        return [pscustomobject]@{
            ok = $true
            message = "Firewall kuralı zaten mevcut."
            ruleName = $name
        }
    }

    New-NetFirewallRule `
        -DisplayName $name `
        -Direction Outbound `
        -Action Allow `
        -Protocol TCP `
        -RemoteAddress $PosIp `
        -RemotePort $PosPort `
        -Profile Private,Domain `
        -Description "SAMER Hub PC -> Ingenico/POS ECR TCP izin kuralı" | Out-Null

    return [pscustomobject]@{
        ok = $true
        message = "Firewall kuralı eklendi."
        ruleName = $name
    }
}

function Add-LocalApiFirewallRule {
    param([int]$ApiPort)

    if (-not (Test-IsAdmin)) {
        throw "Firewall kuralı eklemek için uygulamayı Yönetici olarak çalıştırmalısın."
    }

    $name = "SAMER Hub Local API Inbound TCP $ApiPort"
    $existing = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue

    if ($existing) {
        return [pscustomobject]@{
            ok = $true
            message = "Local API firewall kuralı zaten mevcut."
            ruleName = $name
        }
    }

    New-NetFirewallRule `
        -DisplayName $name `
        -Direction Inbound `
        -Action Allow `
        -Protocol TCP `
        -LocalPort $ApiPort `
        -Profile Private,Domain `
        -Description "SAMER Hub yerel API için LAN içi izin. Varsayılan kullanımda servis sadece 127.0.0.1 dinler." | Out-Null

    return [pscustomobject]@{
        ok = $true
        message = "Local API firewall kuralı eklendi."
        ruleName = $name
    }
}

function Format-Money {
    param($Value)
    try {
        return ("{0:N2} TL" -f ([decimal]$Value))
    } catch {
        return "$Value TL"
    }
}

function Get-PropOr {
    param($Obj, [string[]]$Names, $Default = "")
    if ($null -eq $Obj) { return $Default }
    foreach ($n in $Names) {
        if ($Obj.PSObject.Properties.Name -contains $n -and $null -ne $Obj.$n -and "$($Obj.$n)" -ne "") {
            return $Obj.$n
        }
    }
    return $Default
}

function Get-OrderLinesSafe {
    param($Order)

    if ($null -eq $Order) { return @() }

    foreach ($field in @("lines", "items", "products", "orderLines")) {
        if ($Order.PSObject.Properties.Name -contains $field -and $null -ne $Order.$field) {
            return @($Order.$field)
        }
    }

    return @()
}

function Build-ReceiptFromOrder {
    param(
        $Order,
        [string]$Title,
        [string]$Footer
    )

    $sb = New-Object System.Text.StringBuilder

    $orderNo = Get-PropOr $Order @("orderNumber", "orderNo", "id", "packageId") "-"
    $customer = ((Get-PropOr $Order @("customerFirstName", "firstName", "buyerName") "") + " " + (Get-PropOr $Order @("customerLastName", "lastName", "buyerSurname") "")).Trim()
    if ([string]::IsNullOrWhiteSpace($customer)) { $customer = Get-PropOr $Order @("customer", "customerName") "-" }

    $payment = Get-PropOr $Order @("paymentType", "paymentTypeDescription", "paymentMethod") "-"
    $total = Get-PropOr $Order @("totalPrice", "totalAmount", "grossAmount", "amount") 0
    $cargo = Get-PropOr $Order @("cargoProviderName", "shipmentCompany", "cargoCompany") ""

    [void]$sb.AppendLine($Title)
    [void]$sb.AppendLine(("=" * 42))
    [void]$sb.AppendLine("Tarih        : $(Get-Date -Format 'dd.MM.yyyy HH:mm')")
    [void]$sb.AppendLine("Sipariş No   : $orderNo")
    [void]$sb.AppendLine("Müşteri      : $customer")
    [void]$sb.AppendLine("Ödeme Tipi   : $payment")
    if (-not [string]::IsNullOrWhiteSpace($cargo)) { [void]$sb.AppendLine("Teslimat     : $cargo") }
    [void]$sb.AppendLine(("-" * 42))
    [void]$sb.AppendLine("ÜRÜN DETAYI")
    [void]$sb.AppendLine(("-" * 42))

    $lines = Get-OrderLinesSafe $Order
    $lineTotal = [decimal]0

    if ($lines.Count -eq 0) {
        [void]$sb.AppendLine("Ürün satırı bulunamadı.")
    } else {
        foreach ($line in $lines) {
            $name = Get-PropOr $line @("productName", "name", "title", "barcode") "Ürün"
            $qty = Get-PropOr $line @("quantity", "qty", "count") 1
            $price = Get-PropOr $line @("price", "amount", "salePrice", "unitPrice") 0
            $vat = Get-PropOr $line @("vatRate", "vat", "taxRate", "vatBaseAmount") "-"

            $qtyDec = [decimal]1
            $priceDec = [decimal]0
            try { $qtyDec = [decimal]$qty } catch { }
            try { $priceDec = [decimal]$price } catch { }

            $sub = $qtyDec * $priceDec
            $lineTotal += $sub

            [void]$sb.AppendLine("$name")
            [void]$sb.AppendLine(("  {0} x {1} | KDV: {2} | {3}" -f $qty, (Format-Money $priceDec), $vat, (Format-Money $sub)))
        }
    }

    [void]$sb.AppendLine(("-" * 42))
    if ($lineTotal -gt 0) {
        [void]$sb.AppendLine(("Ara Toplam   : {0}" -f (Format-Money $lineTotal)))
    }
    [void]$sb.AppendLine(("Genel Toplam : {0}" -f (Format-Money $total)))

    $addr = Get-PropOr $Order @("shipmentAddress", "address", "deliveryAddress") $null
    if ($addr) {
        [void]$sb.AppendLine(("-" * 42))
        [void]$sb.AppendLine("TESLİMAT ADRESİ")
        if ($addr -is [string]) {
            [void]$sb.AppendLine($addr)
        } else {
            $fullAddr = Get-PropOr $addr @("fullAddress", "address1", "address") ""
            $district = Get-PropOr $addr @("district", "city", "neighborhood") ""
            if (-not [string]::IsNullOrWhiteSpace($fullAddr)) { [void]$sb.AppendLine($fullAddr) }
            if (-not [string]::IsNullOrWhiteSpace($district)) { [void]$sb.AppendLine($district) }
        }
    }

    $note = Get-PropOr $Order @("customerNote", "note", "description") ""
    if (-not [string]::IsNullOrWhiteSpace($note)) {
        [void]$sb.AppendLine(("-" * 42))
        [void]$sb.AppendLine("Müşteri Notu : $note")
    }

    [void]$sb.AppendLine(("=" * 42))
    [void]$sb.AppendLine($Footer)
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("")

    return $sb.ToString()
}

function Get-MockTrendyolOrder {
    [pscustomobject]@{
        id = "MOCK-1001"
        orderNumber = "TY-MOCK-1001"
        customerFirstName = "Samet"
        customerLastName = "Er"
        paymentType = "Online Ödeme"
        totalPrice = 329.65
        cargoProviderName = "Mağaza Teslim / Kurye"
        customerNote = "Zile basmayın, telefonla arayın."
        shipmentAddress = [pscustomobject]@{
            fullAddress = "Örnek Mah. Market Sok. No: 10"
            district = "Karacabey / Bursa"
        }
        lines = @(
            [pscustomobject]@{ productName = "Domates 1 KG"; quantity = 2; price = 39.90; vatRate = "%1" },
            [pscustomobject]@{ productName = "Ayçiçek Yağı 5 LT"; quantity = 1; price = 249.90; vatRate = "%10" },
            [pscustomobject]@{ productName = "Market Poşeti"; quantity = 2; price = 0.25; vatRate = "%20" }
        )
    }
}

function Fetch-TrendyolOrders {
    param(
        [string]$SupplierId,
        [string]$ApiKey,
        [string]$ApiSecret,
        [string]$BaseUrl
    )

    if ([string]::IsNullOrWhiteSpace($SupplierId)) { throw "Supplier ID boş." }
    if ([string]::IsNullOrWhiteSpace($ApiKey)) { throw "API Key boş." }
    if ([string]::IsNullOrWhiteSpace($ApiSecret)) { throw "API Secret boş." }
    if ([string]::IsNullOrWhiteSpace($BaseUrl)) { throw "Base URL boş." }

    $url = $BaseUrl.Replace("{supplierId}", $SupplierId)

    $pair = "$ApiKey`:$ApiSecret"
    $encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
    $headers = @{
        "Authorization" = "Basic $encoded"
        "User-Agent"    = "$SupplierId - SAMER Hub"
        "Accept"        = "application/json"
    }

    $resp = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -TimeoutSec 30

    if ($null -eq $resp) { return @() }
    if ($resp.PSObject.Properties.Name -contains "content") { return @($resp.content) }
    if ($resp.PSObject.Properties.Name -contains "orders") { return @($resp.orders) }
    if ($resp -is [array]) { return @($resp) }

    return @($resp)
}

function Convert-AmountToCents {
    param($Amount)
    try {
        return [int]([Math]::Round(([decimal]$Amount) * 100, 0))
    } catch {
        return 0
    }
}

function New-IngenicoSaleMessage {
    param(
        $Request,
        $Config
    )

    $amount = Get-PropOr $Request @("amount", "total", "totalPrice") 0
    $amountCents = Convert-AmountToCents $amount
    $currency = Get-PropOr $Request @("currency") "TRY"
    $orderNo = Get-PropOr $Request @("orderNo", "orderNumber", "reference") ("SAMER-" + (Get-Date -Format "yyyyMMddHHmmss"))

    $template = "$($Config.posMessageTemplate)"
    if ([string]::IsNullOrWhiteSpace($template)) {
        $template = "SALE|{orderNo}|{amountCents}|{currency}"
    }

    $message = $template.
        Replace("{orderNo}", "$orderNo").
        Replace("{amount}", "$amount").
        Replace("{amountCents}", "$amountCents").
        Replace("{currency}", "$currency")

    if ($Config.posUseStxEtx -eq $true) {
        $stx = [char]0x02
        $etx = [char]0x03
        $message = "$stx$message$etx"
    }

    return [Text.Encoding]::ASCII.GetBytes($message)
}

function Invoke-PosTcpRawSale {
    param(
        [string]$Ip,
        [int]$Port,
        [byte[]]$MessageBytes,
        [int]$TimeoutMs = 8000
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($Ip, $Port, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne($TimeoutMs, $false)) {
            throw "POS bağlantı zaman aşımı: $Ip`:$Port"
        }

        $client.EndConnect($async)
        $client.ReceiveTimeout = $TimeoutMs
        $client.SendTimeout = $TimeoutMs

        $stream = $client.GetStream()
        $stream.Write($MessageBytes, 0, $MessageBytes.Length)

        Start-Sleep -Milliseconds 200

        $buffer = New-Object byte[] 4096
        $read = 0
        try {
            $read = $stream.Read($buffer, 0, $buffer.Length)
        } catch { }

        $rawResponse = ""
        if ($read -gt 0) {
            $rawResponse = [Text.Encoding]::ASCII.GetString($buffer, 0, $read)
        }

        return [pscustomobject]@{
            ok = $true
            sentBytes = $MessageBytes.Length
            rawResponse = $rawResponse
            approved = $false
            message = "TCP mesajı POS cihazına gönderildi. Gerçek onay/ret ayrıştırması için Ingenico ECR cevap formatı gereklidir."
        }
    } finally {
        try { $client.Close() } catch { }
    }
}

function Invoke-PosSale {
    param($Body)

    $cfg = $Global:Config

    $posIp = if ($Body.posIp) { "$($Body.posIp)" } else { "$($cfg.posIp)" }
    $posPort = if ($Body.posPort) { [int]$Body.posPort } else { [int]$cfg.posPort }
    $mode = if ($Body.mode) { "$($Body.mode)" } else { "$($cfg.posProtocolMode)" }

    if (-not (Test-PrivateIPv4 $posIp)) {
        throw "POS IP adresi private/local ağda olmalıdır."
    }

    if ($mode -eq "FakeSimulation") {
        return [pscustomobject]@{
            ok = $true
            mode = $mode
            approved = $true
            authCode = "SIM" + (Get-Random -Minimum 100000 -Maximum 999999)
            rrn = "SIM" + (Get-Date -Format "MMddHHmmss")
            message = "Simülasyon onayı üretildi. Gerçek POS işlemi yapılmadı."
        }
    }

    if ($mode -eq "TcpRaw") {
        $messageBytes = New-IngenicoSaleMessage -Request $Body -Config $cfg
        return Invoke-PosTcpRawSale -Ip $posIp -Port $posPort -MessageBytes $messageBytes
    }

    return [pscustomobject]@{
        ok = $false
        approved = $false
        mode = $mode
        message = "Bu protokol modu henüz uygulanmadı. Ingenico ECR/TCP dokümanındaki mesaj formatı gereklidir."
    }
}

function Get-SafeConfigForUi {
    $cfg = $Global:Config | ConvertTo-HashtableDeep
    $plain = Unprotect-ToPlainText $Global:Config.trendyolApiSecretProtected
    $cfg["trendyolApiSecretPlain"] = $plain
    return [pscustomobject]$cfg
}

function Merge-ConfigFromBody {
    param($Body)

    $cfg = $Global:Config
    foreach ($p in $Body.PSObject.Properties) {
        if ($p.Name -eq "trendyolApiSecretPlain") {
            $cfg.trendyolApiSecretProtected = Protect-PlainText "$($p.Value)"
            continue
        }

        if ($cfg.PSObject.Properties.Name -contains $p.Name) {
            $cfg.$($p.Name) = $p.Value
        }
    }

    $Global:Config = $cfg
    Save-Config $Global:Config | Out-Null
    return Get-SafeConfigForUi
}

# -----------------------------
# HTTP Listener
# -----------------------------

Add-Type -AssemblyName System.Net.HttpListener

$prefix = "http://$BindAddress`:$Port/"
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($prefix)

try {
    $listener.Start()
    Write-ServiceLog "$ServiceName started on $prefix"
    Write-Host "$ServiceName started on $prefix"
} catch {
    Write-ServiceLog "Listener start error: $($_.Exception.Message)"
    Write-Host "Servis başlatılamadı: $($_.Exception.Message)"
    Write-Host "Çözüm: PowerShell'i Yönetici olarak aç veya farklı port dene."
    exit 1
}

$Global:ShouldStop = $false

while ($listener.IsListening -and -not $Global:ShouldStop) {
    try {
        $context = $listener.GetContext()
        $request = $context.Request
        $path = $request.Url.AbsolutePath.TrimEnd("/")
        if ([string]::IsNullOrWhiteSpace($path)) { $path = "/" }
        $method = $request.HttpMethod.ToUpperInvariant()

        if ($method -eq "OPTIONS") {
            Send-Json -Context $context -Data ([pscustomobject]@{ ok = $true })
            continue
        }

        try {
            switch ("$method $path") {
                "GET /" {
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        service = $ServiceName
                        version = $Version
                        port = $Port
                        documentation = "Local API endpoints: /health, /api/status, /api/config, /api/lan/scan, /api/pos/test, /api/pos/sale"
                    })
                }

                "GET /health" {
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        service = $ServiceName
                        version = $Version
                        port = $Port
                        time = (Get-Date).ToString("s")
                    })
                }

                "GET /api/status" {
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        computerName = $env:COMPUTERNAME
                        userName = $env:USERNAME
                        isAdmin = Test-IsAdmin
                        ip = Get-PrimaryIPv4Info
                        configPath = $ConfigPath
                        logPath = $LogPath
                        service = $ServiceName
                        version = $Version
                    })
                }

                "GET /api/config" {
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        config = Get-SafeConfigForUi
                    })
                }

                "POST /api/config/save" {
                    $body = Read-JsonBody $request
                    $newCfg = Merge-ConfigFromBody $body
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        message = "Ayarlar kaydedildi."
                        config = $newCfg
                    })
                }

                "POST /api/lan/scan" {
                    $body = Read-JsonBody $request
                    $devices = Invoke-LanScan $body
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        count = $devices.Count
                        devices = $devices
                    })
                }

                "POST /api/pos/test" {
                    $body = Read-JsonBody $request
                    $ip = "$($body.ip)"
                    $portValue = [int]$body.port
                    if (-not (Test-PrivateIPv4 $ip)) { throw "POS IP private/local ağda olmalıdır." }

                    $ok = Test-TcpPort -Ip $ip -Port $portValue -TimeoutMs 2500
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        reachable = $ok
                        ip = $ip
                        port = $portValue
                        message = if ($ok) { "POS TCP portuna erişildi." } else { "POS TCP portuna erişilemedi." }
                    })
                }

                "POST /api/firewall/pos" {
                    $body = Read-JsonBody $request
                    $result = Add-PosFirewallRule -PosIp "$($body.ip)" -PosPort ([int]$body.port)
                    Send-Json -Context $context -Data $result
                }

                "POST /api/firewall/local-api" {
                    $body = Read-JsonBody $request
                    $apiPort = if ($body.port) { [int]$body.port } else { $Port }
                    $result = Add-LocalApiFirewallRule -ApiPort $apiPort
                    Send-Json -Context $context -Data $result
                }

                "POST /api/pos/sale" {
                    $body = Read-JsonBody $request
                    $result = Invoke-PosSale $body
                    Send-Json -Context $context -Data $result
                }

                "POST /api/trendyol/orders" {
                    $body = Read-JsonBody $request
                    $supplier = if ($body.supplierId) { "$($body.supplierId)" } else { "$($Global:Config.trendyolSupplierId)" }
                    $apiKey = if ($body.apiKey) { "$($body.apiKey)" } else { "$($Global:Config.trendyolApiKey)" }
                    $secret = if ($body.apiSecret) { "$($body.apiSecret)" } else { Unprotect-ToPlainText $Global:Config.trendyolApiSecretProtected }
                    $baseUrl = if ($body.baseUrl) { "$($body.baseUrl)" } else { "$($Global:Config.trendyolBaseUrl)" }

                    $orders = Fetch-TrendyolOrders -SupplierId $supplier -ApiKey $apiKey -ApiSecret $secret -BaseUrl $baseUrl
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        count = $orders.Count
                        orders = $orders
                    })
                }

                "GET /api/trendyol/mock" {
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        count = 1
                        orders = @(Get-MockTrendyolOrder)
                    })
                }

                "POST /api/receipt/build" {
                    $body = Read-JsonBody $request
                    $title = if ($body.title) { "$($body.title)" } else { "$($Global:Config.receiptTitle)" }
                    $footer = if ($body.footer) { "$($body.footer)" } else { "$($Global:Config.receiptFooter)" }
                    $receipt = Build-ReceiptFromOrder -Order $body.order -Title $title -Footer $footer
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        receipt = $receipt
                    })
                }

                "POST /api/service/shutdown" {
                    Send-Json -Context $context -Data ([pscustomobject]@{
                        ok = $true
                        message = "Servis kapatılıyor."
                    })
                    $Global:ShouldStop = $true
                }

                default {
                    Send-ErrorJson -Context $context -StatusCode 404 -Message "Endpoint bulunamadı: $method $path"
                }
            }
        } catch {
            Send-ErrorJson -Context $context -StatusCode 500 -Message $_.Exception.Message
        }
    } catch {
        Write-ServiceLog "Listener loop error: $($_.Exception.Message)"
    }
}

try {
    $listener.Stop()
    $listener.Close()
} catch { }

Write-ServiceLog "$ServiceName stopped."
