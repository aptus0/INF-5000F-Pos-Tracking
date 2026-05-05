# Tek dosya PowerShell WinForms uygulaması
# Windows PowerShell 5.1+ ile çalışır.
#
# Çalıştırma:
#   1) Dosyaya sağ tık > Run with PowerShell
#   2) Firewall kuralı eklemek için PowerShell'i Yönetici olarak açın:
#      powershell -ExecutionPolicy Bypass -File .\SAMER-Hub-LanPos-Trendyol.ps1
#
# Güvenlik:
#   - LAN taraması sadece private/local IP aralıkları için tasarlanmıştır.
#   - Router port forwarding açmaz.
#   - POS için sadece yerel ağda TCP bağlantı testi ve Windows Firewall outbound kuralı ekler.

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Continue"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ---------------------------
# Genel yardımcılar
# ---------------------------

$AppName = "SAMER Hub LAN / POS / Trendyol Merkezi"
$ConfigDir = Join-Path $env:ProgramData "SAMERHub"
$ConfigPath = Join-Path $ConfigDir "samer-hub-config.json"

if (-not (Test-Path $ConfigDir)) {
    New-Item -Path $ConfigDir -ItemType Directory -Force | Out-Null
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
        PcIpMode        = "Auto"
        PcIpManual      = ""
        PosIp           = "192.168.1.39"
        PosPort         = 5577
        TrendyolSupplierId = ""
        TrendyolApiKey     = ""
        TrendyolApiSecretProtected = ""
        TrendyolBaseUrl    = "https://api.trendyol.com/sapigw/suppliers/{supplierId}/orders?status=Created"
        PrinterName     = ""
        ReceiptTitle    = "KARACABEY GROSS MARKET"
        ReceiptFooter   = "Tesekkur ederiz."
        LocalApiPort    = 8090
    }
}

function Load-Config {
    try {
        if (Test-Path $ConfigPath) {
            $json = Get-Content $ConfigPath -Raw -Encoding UTF8
            if (-not [string]::IsNullOrWhiteSpace($json)) {
                $cfg = $json | ConvertFrom-Json
                $def = Get-DefaultConfig
                foreach ($p in $def.PSObject.Properties.Name) {
                    if (-not ($cfg.PSObject.Properties.Name -contains $p)) {
                        $cfg | Add-Member -MemberType NoteProperty -Name $p -Value $def.$p
                    }
                }
                return $cfg
            }
        }
    } catch { }
    return Get-DefaultConfig
}

function Save-Config {
    param($Config)
    try {
        $Config | ConvertTo-Json -Depth 10 | Set-Content -Path $ConfigPath -Encoding UTF8
        return $true
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Ayar kaydedilemedi:`r`n$($_.Exception.Message)", "Hata", "OK", "Error") | Out-Null
        return $false
    }
}

$Global:Config = Load-Config
$Global:LastOrders = @()
$Global:LastReceiptText = ""

function Write-UiLog {
    param(
        [System.Windows.Forms.TextBox]$TextBox,
        [string]$Message
    )
    if ($null -eq $TextBox) { return }
    $stamp = Get-Date -Format "HH:mm:ss"
    $TextBox.AppendText("[$stamp] $Message`r`n")
    $TextBox.SelectionStart = $TextBox.TextLength
    $TextBox.ScrollToCaret()
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
                IP       = $cfgs.IPv4Address.IPAddress
                Prefix   = $cfgs.IPv4Address.PrefixLength
                Gateway  = $cfgs.IPv4DefaultGateway.NextHop
                Adapter  = $cfgs.InterfaceAlias
                Dns      = ($cfgs.DNSServer.ServerAddresses -join ", ")
            }
        }
    } catch { }

    try {
        $ip = [System.Net.Dns]::GetHostAddresses($env:COMPUTERNAME) |
            Where-Object { $_.AddressFamily -eq "InterNetwork" -and $_.IPAddressToString -notlike "169.254.*" } |
            Select-Object -First 1

        if ($ip) {
            return [pscustomobject]@{
                IP      = $ip.IPAddressToString
                Prefix  = 24
                Gateway = ""
                Adapter = ""
                Dns     = ""
            }
        }
    } catch { }

    return [pscustomobject]@{
        IP      = ""
        Prefix  = 24
        Gateway = ""
        Adapter = ""
        Dns     = ""
    }
}

function Test-PrivateIPv4 {
    param([string]$Ip)

    $parsed = $null
    if (-not [System.Net.IPAddress]::TryParse($Ip, [ref]$parsed)) { return $false }

    $parts = $Ip.Split(".") | ForEach-Object { [int]$_ }
    if ($parts.Count -ne 4) { return $false }

    # RFC1918 + link-local/localhost
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
        [int]$TimeoutMs = 800
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

function Get-SubnetBaseFromIp {
    param([string]$Ip)
    if ($Ip -match "^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.\d{1,3}$") {
        return "$($Matches[1]).$($Matches[2]).$($Matches[3])"
    }
    return "192.168.1"
}

function Add-GridRow {
    param(
        [System.Windows.Forms.DataGridView]$Grid,
        [string]$Ip,
        [string]$Hostname,
        [string]$Mac,
        [string]$Ping,
        [string]$OpenPorts
    )
    $idx = $Grid.Rows.Add()
    $Grid.Rows[$idx].Cells[0].Value = $Ip
    $Grid.Rows[$idx].Cells[1].Value = $Hostname
    $Grid.Rows[$idx].Cells[2].Value = $Mac
    $Grid.Rows[$idx].Cells[3].Value = $Ping
    $Grid.Rows[$idx].Cells[4].Value = $OpenPorts
}

function Add-PosFirewallRule {
    param(
        [string]$PosIp,
        [int]$PosPort
    )

    if (-not (Test-IsAdmin)) {
        throw "Firewall kuralı için PowerShell'i Yönetici olarak açmalısın."
    }
    if (-not (Test-PrivateIPv4 $PosIp)) {
        throw "POS IP private/local ağda değil. Güvenlik için kural eklenmedi: $PosIp"
    }

    $name = "SAMER Hub POS ECR Outbound $PosIp`:$PosPort"

    try {
        $existing = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
        if ($existing) {
            return "Kural zaten var: $name"
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

        return "Firewall kuralı eklendi: $name"
    } catch {
        throw $_.Exception.Message
    }
}

function Add-LocalApiFirewallRule {
    param([int]$Port)

    if (-not (Test-IsAdmin)) {
        throw "Firewall kuralı için PowerShell'i Yönetici olarak açmalısın."
    }

    $name = "SAMER Hub Local API Inbound TCP $Port"
    try {
        $existing = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
        if ($existing) {
            return "Kural zaten var: $name"
        }

        New-NetFirewallRule `
            -DisplayName $name `
            -Direction Inbound `
            -Action Allow `
            -Protocol TCP `
            -LocalPort $Port `
            -Profile Private,Domain `
            -Description "SAMER Hub yerel servis/API için LAN içi izin" | Out-Null

        return "Firewall kuralı eklendi: $name"
    } catch {
        throw $_.Exception.Message
    }
}

function Relaunch-AsAdmin {
    try {
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = "powershell.exe"
        $psi.Arguments = "-ExecutionPolicy Bypass -File `"$PSCommandPath`""
        $psi.Verb = "runas"
        [System.Diagnostics.Process]::Start($psi) | Out-Null
        [System.Windows.Forms.Application]::Exit()
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Yönetici olarak açılamadı:`r`n$($_.Exception.Message)", "Hata", "OK", "Error") | Out-Null
    }
}

function Get-InstalledPrintersSafe {
    $printers = @()
    try {
        $printers = Get-Printer -ErrorAction Stop | Select-Object -ExpandProperty Name
    } catch {
        try {
            $printers = [System.Drawing.Printing.PrinterSettings]::InstalledPrinters
        } catch { }
    }
    return @($printers)
}

function Format-Money {
    param($Value)
    try {
        return ("{0:N2} TL" -f ([decimal]$Value))
    } catch {
        return "$Value TL"
    }
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

function Build-ReceiptFromOrder {
    param(
        $Order,
        [string]$Title,
        [string]$Footer
    )

    $nl = [Environment]::NewLine
    $sb = New-Object System.Text.StringBuilder

    $orderNo = Get-PropOr $Order @("orderNumber", "orderNo", "id", "packageId") "-"
    $customer = (Get-PropOr $Order @("customerFirstName", "firstName", "buyerName") "") + " " + (Get-PropOr $Order @("customerLastName", "lastName", "buyerSurname") "")
    $customer = $customer.Trim()
    if ([string]::IsNullOrWhiteSpace($customer)) { $customer = Get-PropOr $Order @("customer", "customerName") "-" }

    $payment = Get-PropOr $Order @("paymentType", "paymentTypeDescription", "paymentMethod") "-"
    $total = Get-PropOr $Order @("totalPrice", "totalAmount", "grossAmount", "amount") 0
    $cargo = Get-PropOr $Order @("cargoProviderName", "shipmentCompany", "cargoCompany") ""

    [void]$sb.AppendLine($Title)
    [void]$sb.AppendLine(("=" * 38))
    [void]$sb.AppendLine("Tarih       : $(Get-Date -Format 'dd.MM.yyyy HH:mm')")
    [void]$sb.AppendLine("Siparis No  : $orderNo")
    [void]$sb.AppendLine("Musteri     : $customer")
    [void]$sb.AppendLine("Odeme       : $payment")
    if (-not [string]::IsNullOrWhiteSpace($cargo)) { [void]$sb.AppendLine("Kargo       : $cargo") }
    [void]$sb.AppendLine(("-" * 38))
    [void]$sb.AppendLine("URUN                         KDV  TUTAR")
    [void]$sb.AppendLine(("-" * 38))

    $lines = Get-OrderLinesSafe $Order
    $taxTotal = [decimal]0
    $lineTotal = [decimal]0

    if ($lines.Count -eq 0) {
        [void]$sb.AppendLine("Urun satiri bulunamadi.")
    } else {
        foreach ($line in $lines) {
            $name = Get-PropOr $line @("productName", "name", "title", "barcode") "Urun"
            $qty = Get-PropOr $line @("quantity", "qty", "count") 1
            $price = Get-PropOr $line @("price", "amount", "salePrice", "unitPrice") 0
            $vat = Get-PropOr $line @("vatRate", "vat", "taxRate", "vatBaseAmount") "-"

            $qtyDec = [decimal]1
            $priceDec = [decimal]0
            try { $qtyDec = [decimal]$qty } catch { }
            try { $priceDec = [decimal]$price } catch { }

            $sub = $qtyDec * $priceDec
            $lineTotal += $sub

            $short = "$name"
            if ($short.Length -gt 24) { $short = $short.Substring(0, 24) }

            [void]$sb.AppendLine($short)
            [void]$sb.AppendLine(("  {0} x {1}    KDV:{2}   {3}" -f $qty, (Format-Money $priceDec), $vat, (Format-Money $sub)))
        }
    }

    [void]$sb.AppendLine(("-" * 38))
    if ($lineTotal -gt 0) {
        [void]$sb.AppendLine(("Ara Toplam  : {0}" -f (Format-Money $lineTotal)))
    }
    [void]$sb.AppendLine(("Genel Toplam: {0}" -f (Format-Money $total)))

    $addr = Get-PropOr $Order @("shipmentAddress", "address", "deliveryAddress") $null
    if ($addr) {
        [void]$sb.AppendLine(("-" * 38))
        [void]$sb.AppendLine("ADRES")
        if ($addr -is [string]) {
            [void]$sb.AppendLine($addr)
        } else {
            $fullAddr = Get-PropOr $addr @("fullAddress", "address1", "address") ""
            $district = Get-PropOr $addr @("district", "city", "neighborhood") ""
            [void]$sb.AppendLine($fullAddr)
            if (-not [string]::IsNullOrWhiteSpace($district)) { [void]$sb.AppendLine($district) }
        }
    }

    $note = Get-PropOr $Order @("customerNote", "note", "description") ""
    if (-not [string]::IsNullOrWhiteSpace($note)) {
        [void]$sb.AppendLine(("-" * 38))
        [void]$sb.AppendLine("NOT: $note")
    }

    [void]$sb.AppendLine(("=" * 38))
    [void]$sb.AppendLine($Footer)
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("")

    return $sb.ToString()
}

function Print-TextDocument {
    param(
        [string]$PrinterName,
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        throw "Yazdirilacak fis metni bos."
    }

    $doc = New-Object System.Drawing.Printing.PrintDocument
    if (-not [string]::IsNullOrWhiteSpace($PrinterName)) {
        $doc.PrinterSettings.PrinterName = $PrinterName
    }

    $font = New-Object System.Drawing.Font("Consolas", 9)
    $brush = [System.Drawing.Brushes]::Black
    $script:printLines = $Text -split "`r?`n"
    $script:printIndex = 0

    $doc.add_PrintPage({
        param($sender, $e)
        $y = $e.MarginBounds.Top
        $lineHeight = $font.GetHeight($e.Graphics) + 2

        while ($script:printIndex -lt $script:printLines.Count) {
            $line = $script:printLines[$script:printIndex]
            $e.Graphics.DrawString($line, $font, $brush, $e.MarginBounds.Left, $y)
            $y += $lineHeight
            $script:printIndex++

            if ($y + $lineHeight -gt $e.MarginBounds.Bottom) {
                $e.HasMorePages = $true
                return
            }
        }

        $e.HasMorePages = $false
    })

    $doc.Print()
}

function Get-MockTrendyolOrder {
    [pscustomobject]@{
        id = "MOCK-1001"
        orderNumber = "TY-MOCK-1001"
        customerFirstName = "Samet"
        customerLastName = "Er"
        paymentType = "Online Odeme"
        totalPrice = 329.65
        cargoProviderName = "Magaza Teslim / Kurye"
        customerNote = "Zile basmayin, arayin."
        shipmentAddress = [pscustomobject]@{
            fullAddress = "Ornek Mah. Market Sok. No: 10"
            district = "Karacabey / Bursa"
        }
        lines = @(
            [pscustomobject]@{ productName = "Domates 1 KG"; quantity = 2; price = 39.90; vatRate = "%1" },
            [pscustomobject]@{ productName = "Ayçiçek Yagi 5 LT"; quantity = 1; price = 249.90; vatRate = "%10" },
            [pscustomobject]@{ productName = "Market Poseti"; quantity = 2; price = 0.25; vatRate = "%20" }
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

    if ([string]::IsNullOrWhiteSpace($SupplierId)) { throw "Supplier ID bos." }
    if ([string]::IsNullOrWhiteSpace($ApiKey)) { throw "API Key bos." }
    if ([string]::IsNullOrWhiteSpace($ApiSecret)) { throw "API Secret bos." }
    if ([string]::IsNullOrWhiteSpace($BaseUrl)) { throw "Base URL bos." }

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

    if ($resp.PSObject.Properties.Name -contains "content") {
        return @($resp.content)
    }
    if ($resp.PSObject.Properties.Name -contains "orders") {
        return @($resp.orders)
    }
    if ($resp -is [array]) {
        return @($resp)
    }

    return @($resp)
}

# ---------------------------
# UI yardımcıları
# ---------------------------

function New-Label {
    param([string]$Text, [int]$X, [int]$Y, [int]$W = 140, [int]$H = 24)
    $l = New-Object System.Windows.Forms.Label
    $l.Text = $Text
    $l.Location = New-Object System.Drawing.Point($X, $Y)
    $l.Size = New-Object System.Drawing.Size($W, $H)
    $l.AutoSize = $false
    return $l
}

function New-TextBox {
    param([string]$Text, [int]$X, [int]$Y, [int]$W = 220, [int]$H = 24)
    $t = New-Object System.Windows.Forms.TextBox
    $t.Text = $Text
    $t.Location = New-Object System.Drawing.Point($X, $Y)
    $t.Size = New-Object System.Drawing.Size($W, $H)
    return $t
}

function New-Button {
    param([string]$Text, [int]$X, [int]$Y, [int]$W = 130, [int]$H = 32)
    $b = New-Object System.Windows.Forms.Button
    $b.Text = $Text
    $b.Location = New-Object System.Drawing.Point($X, $Y)
    $b.Size = New-Object System.Drawing.Size($W, $H)
    return $b
}

function New-LogBox {
    param([int]$X, [int]$Y, [int]$W, [int]$H)
    $t = New-Object System.Windows.Forms.TextBox
    $t.Location = New-Object System.Drawing.Point($X, $Y)
    $t.Size = New-Object System.Drawing.Size($W, $H)
    $t.Multiline = $true
    $t.ScrollBars = "Vertical"
    $t.ReadOnly = $true
    $t.Font = New-Object System.Drawing.Font("Consolas", 9)
    return $t
}

function Configure-Grid {
    param([System.Windows.Forms.DataGridView]$Grid)
    $Grid.AllowUserToAddRows = $false
    $Grid.AllowUserToDeleteRows = $false
    $Grid.ReadOnly = $true
    $Grid.SelectionMode = "FullRowSelect"
    $Grid.MultiSelect = $false
    $Grid.AutoSizeColumnsMode = "Fill"
    $Grid.RowHeadersVisible = $false
}

# ---------------------------
# Ana pencere
# ---------------------------

$form = New-Object System.Windows.Forms.Form
$form.Text = $AppName
$form.Size = New-Object System.Drawing.Size(1120, 760)
$form.StartPosition = "CenterScreen"
$form.MinimumSize = New-Object System.Drawing.Size(980, 680)

$tabs = New-Object System.Windows.Forms.TabControl
$tabs.Dock = "Fill"
$form.Controls.Add($tabs)

$tabStatus = New-Object System.Windows.Forms.TabPage
$tabStatus.Text = "1) Genel Durum"

$tabLan = New-Object System.Windows.Forms.TabPage
$tabLan.Text = "2) LAN Tarama"

$tabPos = New-Object System.Windows.Forms.TabPage
$tabPos.Text = "3) POS + Firewall"

$tabTrendyol = New-Object System.Windows.Forms.TabPage
$tabTrendyol.Text = "4) Trendyol API"

$tabReceipt = New-Object System.Windows.Forms.TabPage
$tabReceipt.Text = "5) Fiş / Yazıcı"

$tabs.TabPages.AddRange(@($tabStatus, $tabLan, $tabPos, $tabTrendyol, $tabReceipt))

# ---------------------------
# 1) Genel Durum
# ---------------------------

$lblStatusTitle = New-Label "SAMER Hub Yerel Ağ Merkezi" 20 20 500 32
$lblStatusTitle.Font = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Bold)
$tabStatus.Controls.Add($lblStatusTitle)

$lblPcIp = New-Label "PC IP:" 25 75 180
$valPcIp = New-Label "-" 210 75 600
$lblGateway = New-Label "Gateway:" 25 110 180
$valGateway = New-Label "-" 210 110 600
$lblAdapter = New-Label "Ağ Kartı:" 25 145 180
$valAdapter = New-Label "-" 210 145 600
$lblAdmin = New-Label "Yönetici:" 25 180 180
$valAdmin = New-Label "-" 210 180 600
$lblConfig = New-Label "Ayar Dosyası:" 25 215 180
$valConfig = New-Label $ConfigPath 210 215 800

$tabStatus.Controls.AddRange(@($lblPcIp, $valPcIp, $lblGateway, $valGateway, $lblAdapter, $valAdapter, $lblAdmin, $valAdmin, $lblConfig, $valConfig))

$btnRefreshStatus = New-Button "Durumu Yenile" 25 270 150
$btnOpenConfig = New-Button "Ayar Klasörünü Aç" 190 270 160
$btnAdmin = New-Button "Yönetici Olarak Aç" 365 270 170
$tabStatus.Controls.AddRange(@($btnRefreshStatus, $btnOpenConfig, $btnAdmin))

$statusInfo = New-LogBox 25 330 1020 310
$tabStatus.Controls.Add($statusInfo)

function Refresh-StatusTab {
    $info = Get-PrimaryIPv4Info
    $valPcIp.Text = if ($info.IP) { "$($info.IP) /$($info.Prefix)" } else { "Bulunamadi" }
    $valGateway.Text = if ($info.Gateway) { $info.Gateway } else { "-" }
    $valAdapter.Text = if ($info.Adapter) { $info.Adapter } else { "-" }
    $valAdmin.Text = if (Test-IsAdmin) { "Evet - Firewall ekleyebilir" } else { "Hayır - Firewall için yönetici aç" }

    Write-UiLog $statusInfo "PC: $env:COMPUTERNAME"
    Write-UiLog $statusInfo "IP: $($valPcIp.Text)"
    Write-UiLog $statusInfo "Gateway: $($valGateway.Text)"
    Write-UiLog $statusInfo "Ağ kartı: $($valAdapter.Text)"
    Write-UiLog $statusInfo "Admin: $($valAdmin.Text)"
}

$btnRefreshStatus.Add_Click({ Refresh-StatusTab })
$btnOpenConfig.Add_Click({
    try { Start-Process explorer.exe $ConfigDir } catch { }
})
$btnAdmin.Add_Click({ Relaunch-AsAdmin })

# ---------------------------
# 2) LAN Tarama
# ---------------------------

$primaryInfo = Get-PrimaryIPv4Info
$defaultSubnet = Get-SubnetBaseFromIp $primaryInfo.IP

$tabLan.Controls.Add((New-Label "Subnet /24:" 20 20 100))
$txtSubnet = New-TextBox $defaultSubnet 125 18 140
$tabLan.Controls.Add($txtSubnet)

$tabLan.Controls.Add((New-Label "Başlangıç:" 285 20 80))
$txtStart = New-TextBox "1" 365 18 55
$tabLan.Controls.Add($txtStart)

$tabLan.Controls.Add((New-Label "Bitiş:" 440 20 60))
$txtEnd = New-TextBox "254" 495 18 55
$tabLan.Controls.Add($txtEnd)

$tabLan.Controls.Add((New-Label "Portlar:" 570 20 60))
$txtScanPorts = New-TextBox "80,443,9100,5577,8080,8090" 630 18 220
$tabLan.Controls.Add($txtScanPorts)

$btnScanLan = New-Button "Taramayı Başlat" 870 15 150
$tabLan.Controls.Add($btnScanLan)

$gridLan = New-Object System.Windows.Forms.DataGridView
$gridLan.Location = New-Object System.Drawing.Point(20, 65)
$gridLan.Size = New-Object System.Drawing.Size(1040, 445)
Configure-Grid $gridLan
[void]$gridLan.Columns.Add("IP", "IP")
[void]$gridLan.Columns.Add("Hostname", "Hostname")
[void]$gridLan.Columns.Add("MAC", "MAC")
[void]$gridLan.Columns.Add("Ping", "Ping")
[void]$gridLan.Columns.Add("Ports", "Açık Portlar")
$tabLan.Controls.Add($gridLan)

$lanLog = New-LogBox 20 525 1040 120
$tabLan.Controls.Add($lanLog)

$btnScanLan.Add_Click({
    $gridLan.Rows.Clear()
    $subnet = $txtSubnet.Text.Trim()
    $start = [int]$txtStart.Text
    $end = [int]$txtEnd.Text
    $portText = $txtScanPorts.Text.Trim()
    $ports = @()

    foreach ($p in ($portText -split ",")) {
        $port = 0
        if ([int]::TryParse($p.Trim(), [ref]$port) -and $port -gt 0 -and $port -le 65535) {
            $ports += $port
        }
    }

    if ($subnet -notmatch "^\d{1,3}\.\d{1,3}\.\d{1,3}$") {
        [System.Windows.Forms.MessageBox]::Show("Subnet örneği: 192.168.1", "Hatalı subnet", "OK", "Warning") | Out-Null
        return
    }

    if ($start -lt 1) { $start = 1 }
    if ($end -gt 254) { $end = 254 }
    if ($start -gt $end) {
        [System.Windows.Forms.MessageBox]::Show("Başlangıç bitişten büyük olamaz.", "Hata", "OK", "Warning") | Out-Null
        return
    }

    Write-UiLog $lanLog "Tarama başladı: $subnet.$start - $subnet.$end"
    Write-UiLog $lanLog "Port kontrolü sadece ping veren cihazlarda yapılır. POS ping kapalıysa POS sekmesinden direkt test et."

    $btnScanLan.Enabled = $false
    try {
        for ($i = $start; $i -le $end; $i++) {
            $ip = "$subnet.$i"

            if (-not (Test-PrivateIPv4 $ip)) {
                Write-UiLog $lanLog "Güvenlik: private olmayan IP atlandı: $ip"
                continue
            }

            $alive = Test-PingFast $ip 300
            if ($alive) {
                $mac = Get-MacFromArp $ip
                $hn = Get-HostnameSafe $ip
                $open = @()

                foreach ($port in $ports) {
                    if (Test-TcpPort $ip $port 250) {
                        $open += $port
                    }
                    [System.Windows.Forms.Application]::DoEvents()
                }

                Add-GridRow $gridLan $ip $hn $mac "OK" ($open -join ", ")
                Write-UiLog $lanLog "Bulundu: $ip MAC=$mac Ports=$($open -join ',')"
            }

            if (($i % 5) -eq 0) {
                [System.Windows.Forms.Application]::DoEvents()
            }
        }
        Write-UiLog $lanLog "Tarama bitti. Bulunan cihaz: $($gridLan.Rows.Count)"
    } finally {
        $btnScanLan.Enabled = $true
    }
})

# ---------------------------
# 3) POS + Firewall
# ---------------------------

$chkPcAuto = New-Object System.Windows.Forms.CheckBox
$chkPcAuto.Text = "PC IP otomatik algılansın"
$chkPcAuto.Location = New-Object System.Drawing.Point(25, 25)
$chkPcAuto.Size = New-Object System.Drawing.Size(220, 24)
$chkPcAuto.Checked = ($Global:Config.PcIpMode -eq "Auto")
$tabPos.Controls.Add($chkPcAuto)

$tabPos.Controls.Add((New-Label "PC IP:" 25 65 120))
$txtPcIp = New-TextBox "" 160 62 220
$tabPos.Controls.Add($txtPcIp)

$tabPos.Controls.Add((New-Label "POS IP:" 25 105 120))
$txtPosIp = New-TextBox $Global:Config.PosIp 160 102 220
$tabPos.Controls.Add($txtPosIp)

$tabPos.Controls.Add((New-Label "POS TCP Port:" 25 145 120))
$txtPosPort = New-TextBox "$($Global:Config.PosPort)" 160 142 220
$tabPos.Controls.Add($txtPosPort)

$tabPos.Controls.Add((New-Label "Yerel API Port:" 25 185 120))
$txtLocalApiPort = New-TextBox "$($Global:Config.LocalApiPort)" 160 182 220
$tabPos.Controls.Add($txtLocalApiPort)

$btnDetectPcIp = New-Button "PC IP Algıla" 410 60 130
$btnTestPos = New-Button "POS Test Et" 410 100 130
$btnAddPosFw = New-Button "POS Firewall İzni" 410 140 160
$btnAddApiFw = New-Button "Local API Firewall" 410 180 160
$btnSavePos = New-Button "Ayarları Kaydet" 590 100 150
$tabPos.Controls.AddRange(@($btnDetectPcIp, $btnTestPos, $btnAddPosFw, $btnAddApiFw, $btnSavePos))

$posInfoBox = New-Object System.Windows.Forms.GroupBox
$posInfoBox.Text = "Not"
$posInfoBox.Location = New-Object System.Drawing.Point(25, 240)
$posInfoBox.Size = New-Object System.Drawing.Size(1020, 120)
$tabPos.Controls.Add($posInfoBox)

$posNote = New-Object System.Windows.Forms.Label
$posNote.Text = "PC IP otomatik: örn. 192.168.1.10. POS IP elle: örn. 192.168.1.39. POS portu Ingenico/ECR dokümanındaki gerçek TCP port olmalı. Router port yönlendirme açılmaz; sadece LAN içinde PC -> POS izin verilir."
$posNote.Location = New-Object System.Drawing.Point(15, 30)
$posNote.Size = New-Object System.Drawing.Size(980, 70)
$posInfoBox.Controls.Add($posNote)

$posLog = New-LogBox 25 380 1020 260
$tabPos.Controls.Add($posLog)

function Refresh-PcIpTextbox {
    $info = Get-PrimaryIPv4Info
    if ($chkPcAuto.Checked) {
        $txtPcIp.Text = $info.IP
        $txtPcIp.ReadOnly = $true
    } else {
        $txtPcIp.ReadOnly = $false
        if (-not [string]::IsNullOrWhiteSpace($Global:Config.PcIpManual)) {
            $txtPcIp.Text = $Global:Config.PcIpManual
        }
    }
}

$chkPcAuto.Add_CheckedChanged({ Refresh-PcIpTextbox })
$btnDetectPcIp.Add_Click({
    Refresh-PcIpTextbox
    Write-UiLog $posLog "PC IP algılandı: $($txtPcIp.Text)"
})

$btnTestPos.Add_Click({
    $posIp = $txtPosIp.Text.Trim()
    $port = [int]$txtPosPort.Text

    if (-not (Test-PrivateIPv4 $posIp)) {
        [System.Windows.Forms.MessageBox]::Show("POS IP private/local ağda olmalı. Örnek: 192.168.1.39", "Güvenlik", "OK", "Warning") | Out-Null
        return
    }

    Write-UiLog $posLog "POS TCP test başladı: $posIp`:$port"
    $ok = Test-TcpPort $posIp $port 2500
    if ($ok) {
        Write-UiLog $posLog "BAŞARILI: $posIp`:$port açık ve erişilebilir."
        [System.Windows.Forms.MessageBox]::Show("POS bağlantısı başarılı: $posIp`:$port", "POS Test", "OK", "Information") | Out-Null
    } else {
        Write-UiLog $posLog "BAŞARISIZ: $posIp`:$port erişilemedi. IP/port, POS ECR modu, aynı LAN ve firewall kontrol et."
        [System.Windows.Forms.MessageBox]::Show("POS bağlantısı başarısız.`r`nIP/port, POS ECR modu ve aynı ağ durumunu kontrol et.", "POS Test", "OK", "Warning") | Out-Null
    }
})

$btnAddPosFw.Add_Click({
    try {
        $msg = Add-PosFirewallRule -PosIp $txtPosIp.Text.Trim() -PosPort ([int]$txtPosPort.Text)
        Write-UiLog $posLog $msg
        [System.Windows.Forms.MessageBox]::Show($msg, "Firewall", "OK", "Information") | Out-Null
    } catch {
        Write-UiLog $posLog "Firewall hatası: $($_.Exception.Message)"
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, "Firewall Hatası", "OK", "Warning") | Out-Null
    }
})

$btnAddApiFw.Add_Click({
    try {
        $msg = Add-LocalApiFirewallRule -Port ([int]$txtLocalApiPort.Text)
        Write-UiLog $posLog $msg
        [System.Windows.Forms.MessageBox]::Show($msg, "Firewall", "OK", "Information") | Out-Null
    } catch {
        Write-UiLog $posLog "Firewall hatası: $($_.Exception.Message)"
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, "Firewall Hatası", "OK", "Warning") | Out-Null
    }
})

$btnSavePos.Add_Click({
    $Global:Config.PcIpMode = if ($chkPcAuto.Checked) { "Auto" } else { "Manual" }
    $Global:Config.PcIpManual = $txtPcIp.Text.Trim()
    $Global:Config.PosIp = $txtPosIp.Text.Trim()
    $Global:Config.PosPort = [int]$txtPosPort.Text
    $Global:Config.LocalApiPort = [int]$txtLocalApiPort.Text
    if (Save-Config $Global:Config) {
        Write-UiLog $posLog "POS/Ağ ayarları kaydedildi."
    }
})

# ---------------------------
# 4) Trendyol API
# ---------------------------

$tabTrendyol.Controls.Add((New-Label "Supplier ID:" 25 25 120))
$txtSupplier = New-TextBox $Global:Config.TrendyolSupplierId 160 22 260
$tabTrendyol.Controls.Add($txtSupplier)

$tabTrendyol.Controls.Add((New-Label "API Key:" 25 65 120))
$txtApiKey = New-TextBox $Global:Config.TrendyolApiKey 160 62 360
$tabTrendyol.Controls.Add($txtApiKey)

$tabTrendyol.Controls.Add((New-Label "API Secret:" 25 105 120))
$txtApiSecret = New-TextBox (Unprotect-ToPlainText $Global:Config.TrendyolApiSecretProtected) 160 102 360
$txtApiSecret.UseSystemPasswordChar = $true
$tabTrendyol.Controls.Add($txtApiSecret)

$tabTrendyol.Controls.Add((New-Label "Base URL:" 25 145 120))
$txtBaseUrl = New-TextBox $Global:Config.TrendyolBaseUrl 160 142 650
$tabTrendyol.Controls.Add($txtBaseUrl)

$btnSaveTrendyol = New-Button "API Ayar Kaydet" 840 22 150
$btnFetchOrders = New-Button "Siparişleri Çek" 840 62 150
$btnMockOrders = New-Button "Örnek Sipariş" 840 102 150
$btnReceiptFromOrder = New-Button "Seçileni Fişe Aktar" 840 142 150
$tabTrendyol.Controls.AddRange(@($btnSaveTrendyol, $btnFetchOrders, $btnMockOrders, $btnReceiptFromOrder))

$gridOrders = New-Object System.Windows.Forms.DataGridView
$gridOrders.Location = New-Object System.Drawing.Point(25, 190)
$gridOrders.Size = New-Object System.Drawing.Size(1020, 245)
Configure-Grid $gridOrders
[void]$gridOrders.Columns.Add("OrderNo", "Sipariş No")
[void]$gridOrders.Columns.Add("Customer", "Müşteri")
[void]$gridOrders.Columns.Add("Payment", "Ödeme")
[void]$gridOrders.Columns.Add("Total", "Toplam")
[void]$gridOrders.Columns.Add("Status", "Durum")
$tabTrendyol.Controls.Add($gridOrders)

$trendyolLog = New-LogBox 25 450 1020 190
$tabTrendyol.Controls.Add($trendyolLog)

function Refresh-OrdersGrid {
    $gridOrders.Rows.Clear()
    for ($i = 0; $i -lt $Global:LastOrders.Count; $i++) {
        $o = $Global:LastOrders[$i]
        $orderNo = Get-PropOr $o @("orderNumber", "orderNo", "id", "packageId") "-"
        $customer = ((Get-PropOr $o @("customerFirstName", "firstName") "") + " " + (Get-PropOr $o @("customerLastName", "lastName") "")).Trim()
        if ([string]::IsNullOrWhiteSpace($customer)) { $customer = Get-PropOr $o @("customer", "customerName") "-" }
        $payment = Get-PropOr $o @("paymentType", "paymentTypeDescription", "paymentMethod") "-"
        $total = Get-PropOr $o @("totalPrice", "totalAmount", "grossAmount", "amount") 0
        $status = Get-PropOr $o @("status", "packageStatus", "orderStatus") "-"

        $idx = $gridOrders.Rows.Add()
        $gridOrders.Rows[$idx].Cells[0].Value = $orderNo
        $gridOrders.Rows[$idx].Cells[1].Value = $customer
        $gridOrders.Rows[$idx].Cells[2].Value = $payment
        $gridOrders.Rows[$idx].Cells[3].Value = Format-Money $total
        $gridOrders.Rows[$idx].Cells[4].Value = $status
        $gridOrders.Rows[$idx].Tag = $i
    }
}

$btnSaveTrendyol.Add_Click({
    $Global:Config.TrendyolSupplierId = $txtSupplier.Text.Trim()
    $Global:Config.TrendyolApiKey = $txtApiKey.Text.Trim()
    $Global:Config.TrendyolApiSecretProtected = Protect-PlainText $txtApiSecret.Text
    $Global:Config.TrendyolBaseUrl = $txtBaseUrl.Text.Trim()
    if (Save-Config $Global:Config) {
        Write-UiLog $trendyolLog "Trendyol API ayarları kaydedildi. Secret Windows kullanıcı hesabına göre şifrelendi."
    }
})

$btnFetchOrders.Add_Click({
    try {
        Write-UiLog $trendyolLog "Trendyol sipariş çekme başladı..."
        $orders = Fetch-TrendyolOrders `
            -SupplierId $txtSupplier.Text.Trim() `
            -ApiKey $txtApiKey.Text.Trim() `
            -ApiSecret $txtApiSecret.Text `
            -BaseUrl $txtBaseUrl.Text.Trim()

        $Global:LastOrders = @($orders)
        Refresh-OrdersGrid
        Write-UiLog $trendyolLog "Sipariş çekme tamamlandı. Adet: $($Global:LastOrders.Count)"
    } catch {
        Write-UiLog $trendyolLog "Trendyol hata: $($_.Exception.Message)"
        [System.Windows.Forms.MessageBox]::Show("Trendyol sipariş çekilemedi:`r`n$($_.Exception.Message)", "Trendyol API", "OK", "Warning") | Out-Null
    }
})

$btnMockOrders.Add_Click({
    $Global:LastOrders = @(Get-MockTrendyolOrder)
    Refresh-OrdersGrid
    Write-UiLog $trendyolLog "Örnek sipariş yüklendi."
})

# ---------------------------
# 5) Fiş / Yazıcı
# ---------------------------

$tabReceipt.Controls.Add((New-Label "Yazıcı:" 25 25 100))
$cmbPrinters = New-Object System.Windows.Forms.ComboBox
$cmbPrinters.Location = New-Object System.Drawing.Point(125, 22)
$cmbPrinters.Size = New-Object System.Drawing.Size(360, 24)
$cmbPrinters.DropDownStyle = "DropDownList"
$tabReceipt.Controls.Add($cmbPrinters)

$btnRefreshPrinters = New-Button "Yazıcı Yenile" 505 18 130
$btnSavePrinter = New-Button "Yazıcı Kaydet" 650 18 130
$btnExampleReceipt = New-Button "Örnek Fiş" 795 18 110
$btnPrintReceipt = New-Button "Yazdır" 920 18 100
$tabReceipt.Controls.AddRange(@($btnRefreshPrinters, $btnSavePrinter, $btnExampleReceipt, $btnPrintReceipt))

$tabReceipt.Controls.Add((New-Label "Başlık:" 25 65 100))
$txtReceiptTitle = New-TextBox $Global:Config.ReceiptTitle 125 62 360
$tabReceipt.Controls.Add($txtReceiptTitle)

$tabReceipt.Controls.Add((New-Label "Alt Yazı:" 505 65 100))
$txtReceiptFooter = New-TextBox $Global:Config.ReceiptFooter 605 62 360
$tabReceipt.Controls.Add($txtReceiptFooter)

$txtReceipt = New-Object System.Windows.Forms.TextBox
$txtReceipt.Location = New-Object System.Drawing.Point(25, 105)
$txtReceipt.Size = New-Object System.Drawing.Size(1020, 535)
$txtReceipt.Multiline = $true
$txtReceipt.ScrollBars = "Both"
$txtReceipt.Font = New-Object System.Drawing.Font("Consolas", 10)
$txtReceipt.WordWrap = $false
$tabReceipt.Controls.Add($txtReceipt)

function Refresh-Printers {
    $cmbPrinters.Items.Clear()
    $printers = Get-InstalledPrintersSafe
    foreach ($p in $printers) { [void]$cmbPrinters.Items.Add($p) }

    if (-not [string]::IsNullOrWhiteSpace($Global:Config.PrinterName)) {
        $idx = $cmbPrinters.Items.IndexOf($Global:Config.PrinterName)
        if ($idx -ge 0) { $cmbPrinters.SelectedIndex = $idx }
    }

    if ($cmbPrinters.SelectedIndex -lt 0 -and $cmbPrinters.Items.Count -gt 0) {
        $cmbPrinters.SelectedIndex = 0
    }
}

$btnRefreshPrinters.Add_Click({ Refresh-Printers })

$btnSavePrinter.Add_Click({
    if ($cmbPrinters.SelectedItem) {
        $Global:Config.PrinterName = "$($cmbPrinters.SelectedItem)"
    }
    $Global:Config.ReceiptTitle = $txtReceiptTitle.Text
    $Global:Config.ReceiptFooter = $txtReceiptFooter.Text
    if (Save-Config $Global:Config) {
        [System.Windows.Forms.MessageBox]::Show("Fiş/yazıcı ayarları kaydedildi.", "Ayar", "OK", "Information") | Out-Null
    }
})

$btnExampleReceipt.Add_Click({
    $order = Get-MockTrendyolOrder
    $txtReceipt.Text = Build-ReceiptFromOrder -Order $order -Title $txtReceiptTitle.Text -Footer $txtReceiptFooter.Text
    $Global:LastReceiptText = $txtReceipt.Text
})

$btnReceiptFromOrder.Add_Click({
    if ($gridOrders.SelectedRows.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show("Önce Trendyol sipariş listesinden bir satır seç.", "Fiş", "OK", "Information") | Out-Null
        return
    }

    $idx = [int]$gridOrders.SelectedRows[0].Tag
    if ($idx -lt 0 -or $idx -ge $Global:LastOrders.Count) { return }

    $order = $Global:LastOrders[$idx]
    $receipt = Build-ReceiptFromOrder -Order $order -Title $txtReceiptTitle.Text -Footer $txtReceiptFooter.Text
    $txtReceipt.Text = $receipt
    $Global:LastReceiptText = $receipt

    $tabs.SelectedTab = $tabReceipt
    Write-UiLog $trendyolLog "Seçili sipariş fişe aktarıldı."
})

$btnPrintReceipt.Add_Click({
    try {
        $printer = ""
        if ($cmbPrinters.SelectedItem) { $printer = "$($cmbPrinters.SelectedItem)" }
        Print-TextDocument -PrinterName $printer -Text $txtReceipt.Text
        [System.Windows.Forms.MessageBox]::Show("Fiş yazıcıya gönderildi.", "Yazdır", "OK", "Information") | Out-Null
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Yazdırma hatası:`r`n$($_.Exception.Message)", "Yazdır", "OK", "Warning") | Out-Null
    }
})

# ---------------------------
# İlk yükleme
# ---------------------------

$form.Add_Shown({
    Refresh-StatusTab
    Refresh-PcIpTextbox
    Refresh-Printers

    Write-UiLog $statusInfo "Hazır. Önce POS IP/port test et, sonra firewall iznini yönetici olarak ekle."
    Write-UiLog $posLog "Örnek: PC IP 192.168.1.10, POS IP 192.168.1.39. POS port gerçek Ingenico/ECR portu olmalı."
    Write-UiLog $trendyolLog "Trendyol endpoint ayarlanabilir bırakıldı. API bilgilerini girip 'Siparişleri Çek' kullanabilirsin."
})

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::Run($form)
