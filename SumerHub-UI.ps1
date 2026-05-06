<#
╔══════════════════════════════════════════════════════════════════════════════╗
║                    SUMER HUB - WİNFORMS ARAYÜZ UYGULAMASI                    ║
║               localhost:8189 servisi ile haberleşir                            ║
╚══════════════════════════════════════════════════════════════════════════════╝

Açıklama:
    - SumerHub-Servis.ps1 (localhost:8189) ile HTTP üzerinden haberleşir
    - Trendyol API entegrasyonu
    - LAN tarama
    - Fiş yazdırma
    - POS kontrol paneli

Çalıştırma:
    powershell -ExecutionPolicy Bypass -File .\SumerHub-UI.ps1

Yazar: SAMER Teknoloji
Sürüm: 2.0
#>

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Continue"

# ═══════════════════════════════════════════════════════════════════════════════
# ASSEMBLY VE TİP YÜKLEME
# ═══════════════════════════════════════════════════════════════════════════════
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Net.Http

# ═══════════════════════════════════════════════════════════════════════════════
# KÜRESEL AYARLAR
# ═══════════════════════════════════════════════════════════════════════════════
$script:UygulamaAdi = "SUMER HUB v2.0"
$script:ServisUrl = "http://localhost:8189"
$script:ConfigDir = Join-Path $env:ProgramData "SumerHub"
$script:ConfigPath = Join-Path $ConfigDir "sumerhub-ui-config.json"
$script:LogoPath = Join-Path $PSScriptRoot "logo.png"

if (-not (Test-Path $ConfigDir)) {
    New-Item -Path $ConfigDir -ItemType Directory -Force | Out-Null
}

# ═══════════════════════════════════════════════════════════════════════════════
# YARDIMCI FONKSİYONLAR
# ═══════════════════════════════════════════════════════════════════════════════

function Yonetici-Mi {
    try {
        $kimlik = [Security.Principal.WindowsIdentity]::GetCurrent()
        $prensip = New-Object Security.Principal.WindowsPrincipal($kimlik)
        return $prensip.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    } catch { return $false }
}

function Metin-Sifrele {
    param([string]$Metin)
    if ([string]::IsNullOrWhiteSpace($Metin)) { return "" }
    try {
        $guvenli = ConvertTo-SecureString $Metin -AsPlainText -Force
        return ConvertFrom-SecureString $guvenli
    } catch { return "" }
}

function Metin-Coz {
    param([string]$Sifreli)
    if ([string]::IsNullOrWhiteSpace($Sifreli)) { return "" }
    try {
        $guvenli = ConvertTo-SecureString $Sifreli
        $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($guvenli)
        try {
            return [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
        } finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    } catch { return "" }
}

function Varsayilan-Ayarlari-Al {
    return [pscustomobject]@{
        # Ağ
        PcIpMod = "Otomatik"
        PcIpManuel = ""
        YerelApiPort = 8189

        # POS
        PosIp = "192.168.1.39"
        PosPort = 7778
        PosTimeout = 30000

        # Trendyol
        TrendyolSupplierId = ""
        TrendyolApiKey = ""
        TrendyolApiSecretSifreli = ""
        TrendyolBaseUrl = "https://api.trendyol.com/sapigw/suppliers/{supplierId}/orders?status=Created"

        # Yazıcı / Fiş
        YaziciAdi = ""
        FisBaslik = "KARACABEY GROSS MARKET"
        FisAltYazi = "Teşekkür ederiz."

        # Tarama
        TaramaSubnet = "192.168.1"
        TaramaBaslangic = 1
        TaramaBitis = 254
        TaramaPortlar = "80,443,9100,5577,8080,8090,7777,7778"
    }
}

function Ayarlari-Yukle {
    try {
        if (Test-Path $script:ConfigPath) {
            $json = Get-Content $script:ConfigPath -Raw -Encoding UTF8
            if (-not [string]::IsNullOrWhiteSpace($json)) {
                $cfg = $json | ConvertFrom-Json
                $def = Varsayilan-Ayarlari-Al
                foreach ($p in $def.PSObject.Properties.Name) {
                    if (-not ($cfg.PSObject.Properties.Name -contains $p)) {
                        $cfg | Add-Member -MemberType NoteProperty -Name $p -Value $def.$p
                    }
                }
                return $cfg
            }
        }
    } catch { }
    return Varsayilan-Ayarlari-Al
}

function Ayarlari-Kaydet {
    param($Config)
    try {
        $Config | ConvertTo-Json -Depth 10 | Set-Content -Path $script:ConfigPath -Encoding UTF8
        return $true
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Ayar kaydedilemedi:`r`n$($_.Exception.Message)", "Hata", "OK", "Error") | Out-Null
        return $false
    }
}

$script:Ayarlar = Ayarlari-Yukle
$script:SonSiparisler = @()
$script:SonFisMetni = ""

# ═══════════════════════════════════════════════════════════════════════════════
# HTTP SERVİS İLETİŞİMİ (localhost:8189)
# ═══════════════════════════════════════════════════════════════════════════════

function Servis-Get {
    param([string]$Endpoint)
    try {
        $url = "$script:ServisUrl$Endpoint"
        $response = Invoke-RestMethod -Uri $url -Method GET -TimeoutSec 5 -ErrorAction Stop
        return @{ Basarili = $true; Veri = $response }
    } catch {
        return @{ Basarili = $false; Hata = $_.Exception.Message }
    }
}

function Servis-Post {
    param([string]$Endpoint, [object]$Govde)
    try {
        $url = "$script:ServisUrl$Endpoint"
        $json = $Govde | ConvertTo-Json -Depth 10 -Compress
        $response = Invoke-RestMethod -Uri $url -Method POST -Body $json -ContentType "application/json" -TimeoutSec 30 -ErrorAction Stop
        return @{ Basarili = $true; Veri = $response }
    } catch {
        return @{ Basarili = $false; Hata = $_.Exception.Message }
    }
}

function Servis-Durum-Kontrol {
    $sonuc = Servis-Get -Endpoint "/durum"
    return $sonuc.Basarili
}

# ═══════════════════════════════════════════════════════════════════════════════
# AĞ YARDIMCILARI
# ═══════════════════════════════════════════════════════════════════════════════

function Ana-IPv4-Bilgisi {
    try {
        $cfgs = Get-NetIPConfiguration -ErrorAction Stop |
            Where-Object {
                $_.IPv4Address -and
                $_.NetAdapter.Status -eq "Up" -and
                $_.IPv4DefaultGateway
            } | Select-Object -First 1

        if ($cfgs) {
            return [pscustomobject]@{
                IP = $cfgs.IPv4Address.IPAddress
                Prefix = $cfgs.IPv4Address.PrefixLength
                Gateway = $cfgs.IPv4DefaultGateway.NextHop
                Kart = $cfgs.InterfaceAlias
                Dns = ($cfgs.DNSServer.ServerAddresses -join ", ")
            }
        }
    } catch { }

    try {
        $ip = [System.Net.Dns]::GetHostAddresses($env:COMPUTERNAME) |
            Where-Object { $_.AddressFamily -eq "InterNetwork" -and $_.IPAddressToString -notlike "169.254.*" } |
            Select-Object -First 1
        if ($ip) {
            return [pscustomobject]@{ IP = $ip.IPAddressToString; Prefix = 24; Gateway = ""; Kart = ""; Dns = "" }
        }
    } catch { }

    return [pscustomobject]@{ IP = ""; Prefix = 24; Gateway = ""; Kart = ""; Dns = "" }
}

function Ozel-Ip-Kontrol {
    param([string]$Ip)
    $parsed = $null
    if (-not [System.Net.IPAddress]::TryParse($Ip, [ref]$parsed)) { return $false }
    $parcalar = $Ip.Split(".") | ForEach-Object { [int]$_ }
    if ($parcalar.Count -ne 4) { return $false }
    if ($parcalar[0] -eq 10) { return $true }
    if ($parcalar[0] -eq 172 -and $parcalar[1] -ge 16 -and $parcalar[1] -le 31) { return $true }
    if ($parcalar[0] -eq 192 -and $parcalar[1] -eq 168) { return $true }
    if ($parcalar[0] -eq 127) { return $true }
    if ($parcalar[0] -eq 169 -and $parcalar[1] -eq 254) { return $true }
    return $false
}

function Tcp-Port-Test {
    param([string]$Ip, [int]$Port, [int]$TimeoutMs = 800)
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($Ip, $Port, $null, $null)
        $ok = $async.AsyncWaitHandle.WaitOne($TimeoutMs, $false)
        if (-not $ok) { return $false }
        $client.EndConnect($async)
        return $true
    } catch { return $false } finally {
        try { $client.Close() } catch { }
    }
}

function Hizli-Ping {
    param([string]$Ip, [int]$TimeoutMs = 350)
    try {
        $ping = New-Object System.Net.NetworkInformation.Ping
        $reply = $ping.Send($Ip, $TimeoutMs)
        return ($reply.Status -eq [System.Net.NetworkInformation.IPStatus]::Success)
    } catch { return $false }
}

function Arp-Mac-Al {
    param([string]$Ip)
    try {
        $cikti = arp -a $Ip 2>$null
        foreach ($satir in $cikti) {
            if ($satir -match "([0-9A-Fa-f]{2}[-:]){5}[0-9A-Fa-f]{2}") {
                return $Matches[0].ToUpper()
            }
        }
    } catch { }
    return ""
}

function Hostname-Al {
    param([string]$Ip)
    try { return ([System.Net.Dns]::GetHostEntry($Ip)).HostName } catch { return "" }
}

function Subnet-Taban-Al {
    param([string]$Ip)
    if ($Ip -match "^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.\d{1,3}$") {
        return "$($Matches[1]).$($Matches[2]).$($Matches[3])"
    }
    return "192.168.1"
}

# ═══════════════════════════════════════════════════════════════════════════════
# TRENDYOL API
# ═══════════════════════════════════════════════════════════════════════════════

function Trendyol-Siparis-Cek {
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
        "User-Agent" = "$SupplierId - Sumer Hub"
        "Accept" = "application/json"
    }

    $resp = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -TimeoutSec 30
    if ($null -eq $resp) { return @() }
    if ($resp.PSObject.Properties.Name -contains "content") { return @($resp.content) }
    if ($resp.PSObject.Properties.Name -contains "orders") { return @($resp.orders) }
    if ($resp -is [array]) { return @($resp) }
    return @($resp)
}

function Ornek-Siparis-Al {
    return [pscustomobject]@{
        id = "MOCK-1001"
        orderNumber = "TY-MOCK-1001"
        customerFirstName = "Samet"
        customerLastName = "Er"
        paymentType = "Online Ödeme"
        totalPrice = 329.65
        cargoProviderName = "Mağaza Teslim / Kurye"
        customerNote = "Zile basmayın, arayın."
        shipmentAddress = [pscustomobject]@{
            fullAddress = "Örnek Mah. Market Sok. No: 10"
            district = "Karacabey / Bursa"
        }
        lines = @(
            [pscustomobject]@{ productName = "Domates 1 KG"; quantity = 2; price = 39.90; vatRate = "%1" }
            [pscustomobject]@{ productName = "Ayçiçek Yağı 5 LT"; quantity = 1; price = 249.90; vatRate = "%10" }
            [pscustomobject]@{ productName = "Market Poşeti"; quantity = 2; price = 0.25; vatRate = "%20" }
        )
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# FİŞ / YAZICI İŞLEMLERİ
# ═══════════════════════════════════════════════════════════════════════════════

function Para-Formatla {
    param($Deger)
    try { return ("{0:N2} TL" -f ([decimal]$Deger)) } catch { return "$Deger TL" }
}

function Ozellik-Al {
    param($Nesne, [string[]]$Isimler, $Varsayilan = "")
    if ($null -eq $Nesne) { return $Varsayilan }
    foreach ($n in $Isimler) {
        if ($Nesne.PSObject.Properties.Name -contains $n -and $null -ne $Nesne.$n -and "$($Nesne.$n)" -ne "") {
            return $Nesne.$n
        }
    }
    return $Varsayilan
}

function Satirlari-Al {
    param($Siparis)
    if ($null -eq $Siparis) { return @() }
    foreach ($alan in @("lines", "items", "products", "orderLines")) {
        if ($Siparis.PSObject.Properties.Name -contains $alan -and $null -ne $Siparis.$alan) {
            return @($Siparis.$alan)
        }
    }
    return @()
}

function Fis-Olustur {
    param($Siparis, [string]$Baslik, [string]$AltYazi)
    $nl = [Environment]::NewLine
    $sb = New-Object System.Text.StringBuilder

    $siparisNo = Ozellik-Al $Siparis @("orderNumber", "orderNo", "id", "packageId") "-"
    $musteri = (Ozellik-Al $Siparis @("customerFirstName", "firstName", "buyerName") "") + " " + (Ozellik-Al $Siparis @("customerLastName", "lastName", "buyerSurname") "")
    $musteri = $musteri.Trim()
    if ([string]::IsNullOrWhiteSpace($musteri)) { $musteri = Ozellik-Al $Siparis @("customer", "customerName") "-" }

    $odeme = Ozellik-Al $Siparis @("paymentType", "paymentTypeDescription", "paymentMethod") "-"
    $toplam = Ozellik-Al $Siparis @("totalPrice", "totalAmount", "grossAmount", "amount") 0
    $kargo = Ozellik-Al $Siparis @("cargoProviderName", "shipmentCompany", "cargoCompany") ""

    [void]$sb.AppendLine($Baslik)
    [void]$sb.AppendLine(("=" * 38))
    [void]$sb.AppendLine("Tarih       : $(Get-Date -Format 'dd.MM.yyyy HH:mm')")
    [void]$sb.AppendLine("Sipariş No  : $siparisNo")
    [void]$sb.AppendLine("Müşteri     : $musteri")
    [void]$sb.AppendLine("Ödeme       : $odeme")
    if (-not [string]::IsNullOrWhiteSpace($kargo)) { [void]$sb.AppendLine("Kargo       : $kargo") }
    [void]$sb.AppendLine(("-" * 38))
    [void]$sb.AppendLine("ÜRÜN                         KDV  TUTAR")
    [void]$sb.AppendLine(("-" * 38))

    $satirlar = Satirlari-Al $Siparis
    $araToplam = [decimal]0

    if ($satirlar.Count -eq 0) {
        [void]$sb.AppendLine("Ürün satırı bulunamadı.")
    } else {
        foreach ($satir in $satirlar) {
            $ad = Ozellik-Al $satir @("productName", "name", "title", "barcode") "Ürün"
            $adet = Ozellik-Al $satir @("quantity", "qty", "count") 1
            $fiyat = Ozellik-Al $satir @("price", "amount", "salePrice", "unitPrice") 0
            $kdv = Ozellik-Al $satir @("vatRate", "vat", "taxRate", "vatBaseAmount") "-"

            $adetDec = [decimal]1; $fiyatDec = [decimal]0
            try { $adetDec = [decimal]$adet } catch { }
            try { $fiyatDec = [decimal]$fiyat } catch { }

            $altToplam = $adetDec * $fiyatDec
            $araToplam += $altToplam

            $kisa = $ad
            if ($kisa.Length -gt 24) { $kisa = $kisa.Substring(0, 24) }

            [void]$sb.AppendLine($kisa)
            [void]$sb.AppendLine(("  {0} x {1}    KDV:{2}   {3}" -f $adet, (Para-Formatla $fiyatDec), $kdv, (Para-Formatla $altToplam)))
        }
    }

    [void]$sb.AppendLine(("-" * 38))
    if ($araToplam -gt 0) { [void]$sb.AppendLine(("Ara Toplam  : {0}" -f (Para-Formatla $araToplam))) }
    [void]$sb.AppendLine(("Genel Toplam: {0}" -f (Para-Formatla $toplam)))

    $adres = Ozellik-Al $Siparis @("shipmentAddress", "address", "deliveryAddress") $null
    if ($adres) {
        [void]$sb.AppendLine(("-" * 38))
        [void]$sb.AppendLine("ADRES")
        if ($adres -is [string]) {
            [void]$sb.AppendLine($adres)
        } else {
            $tamAdres = Ozellik-Al $adres @("fullAddress", "address1", "address") ""
            $ilce = Ozellik-Al $adres @("district", "city", "neighborhood") ""
            [void]$sb.AppendLine($tamAdres)
            if (-not [string]::IsNullOrWhiteSpace($ilce)) { [void]$sb.AppendLine($ilce) }
        }
    }

    $not = Ozellik-Al $Siparis @("customerNote", "note", "description") ""
    if (-not [string]::IsNullOrWhiteSpace($not)) {
        [void]$sb.AppendLine(("-" * 38))
        [void]$sb.AppendLine("NOT: $not")
    }

    [void]$sb.AppendLine(("=" * 38))
    [void]$sb.AppendLine($AltYazi)
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("")

    return $sb.ToString()
}

function Yazdir-Metin {
    param([string]$YaziciAdi, [string]$Metin)
    if ([string]::IsNullOrWhiteSpace($Metin)) { throw "Yazdırılacak fiş metni boş." }

    $doc = New-Object System.Drawing.Printing.PrintDocument
    if (-not [string]::IsNullOrWhiteSpace($YaziciAdi)) {
        $doc.PrinterSettings.PrinterName = $YaziciAdi
    }

    $font = New-Object System.Drawing.Font("Consolas", 9)
    $brush = [System.Drawing.Brushes]::Black
    $script:printLines = $Metin -split "`r?`n"
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

function Yazicilari-Al {
    $yazicilar = @()
    try {
        $yazicilar = Get-Printer -ErrorAction Stop | Select-Object -ExpandProperty Name
    } catch {
        try {
            $yazicilar = [System.Drawing.Printing.PrinterSettings]::InstalledPrinters
        } catch { }
    }
    return @($yazicilar)
}

# ═══════════════════════════════════════════════════════════════════════════════
# UI YARDIMCILARI
# ═══════════════════════════════════════════════════════════════════════════════

function Etiket-Olustur {
    param([string]$Metin, [int]$X, [int]$Y, [int]$W = 140, [int]$H = 24)
    $l = New-Object System.Windows.Forms.Label
    $l.Text = $Metin
    $l.Location = New-Object System.Drawing.Point($X, $Y)
    $l.Size = New-Object System.Drawing.Size($W, $H)
    $l.AutoSize = $false
    return $l
}

function MetinKutusu-Olustur {
    param([string]$Metin, [int]$X, [int]$Y, [int]$W = 220, [int]$H = 24)
    $t = New-Object System.Windows.Forms.TextBox
    $t.Text = $Metin
    $t.Location = New-Object System.Drawing.Point($X, $Y)
    $t.Size = New-Object System.Drawing.Size($W, $H)
    return $t
}

function Buton-Olustur {
    param([string]$Metin, [int]$X, [int]$Y, [int]$W = 130, [int]$H = 32)
    $b = New-Object System.Windows.Forms.Button
    $b.Text = $Metin
    $b.Location = New-Object System.Drawing.Point($X, $Y)
    $b.Size = New-Object System.Drawing.Size($W, $H)
    return $b
}

function LogKutusu-Olustur {
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

function Grid-Ayarla {
    param([System.Windows.Forms.DataGridView]$Grid)
    $Grid.AllowUserToAddRows = $false
    $Grid.AllowUserToDeleteRows = $false
    $Grid.ReadOnly = $true
    $Grid.SelectionMode = "FullRowSelect"
    $Grid.MultiSelect = $false
    $Grid.AutoSizeColumnsMode = "Fill"
    $Grid.RowHeadersVisible = $false
}

function Log-Yaz {
    param([System.Windows.Forms.TextBox]$Kutu, [string]$Mesaj)
    if ($null -eq $Kutu) { return }
    $zaman = Get-Date -Format "HH:mm:ss"
    $Kutu.AppendText("[$zaman] $Mesaj`r`n")
    $Kutu.SelectionStart = $Kutu.TextLength
    $Kutu.ScrollToCaret()
}

# ═══════════════════════════════════════════════════════════════════════════════
# ANA PENCERE
# ═══════════════════════════════════════════════════════════════════════════════

$form = New-Object System.Windows.Forms.Form
$form.Text = $script:UygulamaAdi
$form.Size = New-Object System.Drawing.Size(1200, 800)
$form.StartPosition = "CenterScreen"
$form.MinimumSize = New-Object System.Drawing.Size(1000, 700)

# Logo yükle
if (Test-Path $script:LogoPath) {
    try {
        $logoImage = [System.Drawing.Image]::FromFile($script:LogoPath)
        $form.Icon = [System.Drawing.Icon]::FromHandle($logoImage.GetHicon())
    } catch { }
}

$tabs = New-Object System.Windows.Forms.TabControl
$tabs.Dock = "Fill"
$form.Controls.Add($tabs)

$tabDurum = New-Object System.Windows.Forms.TabPage
$tabDurum.Text = "1) Genel Durum"

$tabLan = New-Object System.Windows.Forms.TabPage
$tabLan.Text = "2) LAN Tarama"

$tabPos = New-Object System.Windows.Forms.TabPage
$tabPos.Text = "3) POS Kontrol"

$tabTrendyol = New-Object System.Windows.Forms.TabPage
$tabTrendyol.Text = "4) Trendyol API"

$tabFis = New-Object System.Windows.Forms.TabPage
$tabFis.Text = "5) Fiş / Yazıcı"

$tabs.TabPages.AddRange(@($tabDurum, $tabLan, $tabPos, $tabTrendyol, $tabFis))

# ═══════════════════════════════════════════════════════════════════════════════
# SEKME 1: GENEL DURUM
# ═══════════════════════════════════════════════════════════════════════════════

$lblBaslik = Etiket-Olustur "SUMER HUB Yerel Ağ ve POS Merkezi" 20 20 600 32
$lblBaslik.Font = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Bold)
$tabDurum.Controls.Add($lblBaslik)

# Logo resmi
if (Test-Path $script:LogoPath) {
    try {
        $picLogo = New-Object System.Windows.Forms.PictureBox
        $picLogo.Location = New-Object System.Drawing.Point(650, 15)
        $picLogo.Size = New-Object System.Drawing.Size(80, 80)
        $picLogo.SizeMode = "Zoom"
        $picLogo.Image = [System.Drawing.Image]::FromFile($script:LogoPath)
        $tabDurum.Controls.Add($picLogo)
    } catch { }
}

$lblPcIp = Etiket-Olustur "PC IP:" 25 75 180
$valPcIp = Etiket-Olustur "-" 210 75 600
$lblGateway = Etiket-Olustur "Gateway:" 25 110 180
$valGateway = Etiket-Olustur "-" 210 110 600
$lblKart = Etiket-Olustur "Ağ Kartı:" 25 145 180
$valKart = Etiket-Olustur "-" 210 145 600
$lblAdmin = Etiket-Olustur "Yönetici:" 25 180 180
$valAdmin = Etiket-Olustur "-" 210 180 600
$lblServis = Etiket-Olustur "POS Servisi:" 25 215 180
$valServis = Etiket-Olustur "Kontrol ediliyor..." 210 215 600
$lblConfig = Etiket-Olustur "Ayar Dosyası:" 25 250 180
$valConfig = Etiket-Olustur $script:ConfigPath 210 250 800

$tabDurum.Controls.AddRange(@(
    $lblPcIp, $valPcIp, $lblGateway, $valGateway, 
    $lblKart, $valKart, $lblAdmin, $valAdmin,
    $lblServis, $valServis, $lblConfig, $valConfig
))

$btnYenile = Buton-Olustur "Durumu Yenile" 25 300 150
$btnKlasor = Buton-Olustur "Ayar Klasörünü Aç" 190 300 160
$btnAdmin = Buton-Olustur "Yönetici Olarak Aç" 365 300 170
$btnServisBaslat = Buton-Olustur "POS Servisini Başlat" 550 300 170
$tabDurum.Controls.AddRange(@($btnYenile, $btnKlasor, $btnAdmin, $btnServisBaslat))

$durumLog = LogKutusu-Olustur 25 360 1120 340
$tabDurum.Controls.Add($durumLog)

function Durum-Yenile {
    $bilgi = Ana-IPv4-Bilgisi
    $valPcIp.Text = if ($bilgi.IP) { "$($bilgi.IP) /$($bilgi.Prefix)" } else { "Bulunamadı" }
    $valGateway.Text = if ($bilgi.Gateway) { $bilgi.Gateway } else { "-" }
    $valKart.Text = if ($bilgi.Kart) { $bilgi.Kart } else { "-" }
    $valAdmin.Text = if (Yonetici-Mi) { "Evet - Tam yetki" } else { "Hayır - Sınırlı yetki" }

    # Servis durumu kontrolü
    $servisAktif = Servis-Durum-Kontrol
    if ($servisAktif) {
        $valServis.Text = "Çalışıyor (localhost:8189)"
        $valServis.ForeColor = [System.Drawing.Color]::Green
    } else {
        $valServis.Text = "Çalışmıyor - Başlatın"
        $valServis.ForeColor = [System.Drawing.Color]::Red
    }

    Log-Yaz $durumLog "PC: $env:COMPUTERNAME"
    Log-Yaz $durumLog "IP: $($valPcIp.Text)"
    Log-Yaz $durumLog "Gateway: $($valGateway.Text)"
    Log-Yaz $durumLog "Ağ kartı: $($valKart.Text)"
    Log-Yaz $durumLog "Admin: $($valAdmin.Text)"
    Log-Yaz $durumLog "POS Servisi: $($valServis.Text)"
}

$btnYenile.Add_Click({ Durum-Yenile })
$btnKlasor.Add_Click({
    try { Start-Process explorer.exe $script:ConfigDir } catch { }
})
$btnAdmin.Add_Click({
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
})
$btnServisBaslat.Add_Click({
    try {
        $servisYol = Join-Path $PSScriptRoot "SumerHub-Servis.ps1"
        if (Test-Path $servisYol) {
            Start-Process powershell.exe -ArgumentList "-ExecutionPolicy Bypass -WindowStyle Hidden -File `"$servisYol`"" -WindowStyle Hidden
            Log-Yaz $durumLog "POS Servisi başlatma komutu gönderildi..."
            Start-Sleep -Seconds 2
            Durum-Yenile
        } else {
            [System.Windows.Forms.MessageBox]::Show("SumerHub-Servis.ps1 bulunamadı!`r`n$PSScriptRoot klasöründe olmalı.", "Hata", "OK", "Error") | Out-Null
        }
    } catch {
        Log-Yaz $durumLog "Servis başlatma hatası: $($_.Exception.Message)"
    }
})

# ═══════════════════════════════════════════════════════════════════════════════
# SEKME 2: LAN TARAMA
# ═══════════════════════════════════════════════════════════════════════════════

$anaBilgi = Ana-IPv4-Bilgisi
$varsayilanSubnet = Subnet-Taban-Al $anaBilgi.IP

$tabLan.Controls.Add((Etiket-Olustur "Subnet /24:" 20 20 100))
$txtSubnet = MetinKutusu-Olustur $varsayilanSubnet 125 18 140
$tabLan.Controls.Add($txtSubnet)

$tabLan.Controls.Add((Etiket-Olustur "Başlangıç:" 285 20 80))
$txtBaslangic = MetinKutusu-Olustur "1" 365 18 55
$tabLan.Controls.Add($txtBaslangic)

$tabLan.Controls.Add((Etiket-Olustur "Bitiş:" 440 20 60))
$txtBitis = MetinKutusu-Olustur "254" 495 18 55
$tabLan.Controls.Add($txtBitis)

$tabLan.Controls.Add((Etiket-Olustur "Portlar:" 570 20 60))
$txtPortlar = MetinKutusu-Olustur "80,443,9100,5577,8080,8090,7777,7778" 630 18 260
$tabLan.Controls.Add($txtPortlar)

$btnTarama = Buton-Olustur "Taramayı Başlat" 910 15 150
$tabLan.Controls.Add($btnTarama)

$gridLan = New-Object System.Windows.Forms.DataGridView
$gridLan.Location = New-Object System.Drawing.Point(20, 65)
$gridLan.Size = New-Object System.Drawing.Size(1140, 480)
Grid-Ayarla $gridLan
[void]$gridLan.Columns.Add("IP", "IP")
[void]$gridLan.Columns.Add("Hostname", "Hostname")
[void]$gridLan.Columns.Add("MAC", "MAC")
[void]$gridLan.Columns.Add("Ping", "Ping")
[void]$gridLan.Columns.Add("Ports", "Açık Portlar")
$tabLan.Controls.Add($gridLan)

$lanLog = LogKutusu-Olustur 20 560 1140 120
$tabLan.Controls.Add($lanLog)

$btnTarama.Add_Click({
    $gridLan.Rows.Clear()
    $subnet = $txtSubnet.Text.Trim()
    $baslangic = [int]$txtBaslangic.Text
    $bitis = [int]$txtBitis.Text
    $portMetni = $txtPortlar.Text.Trim()
    $portlar = @()

    foreach ($p in ($portMetni -split ",")) {
        $port = 0
        if ([int]::TryParse($p.Trim(), [ref]$port) -and $port -gt 0 -and $port -le 65535) {
            $portlar += $port
        }
    }

    if ($subnet -notmatch "^\d{1,3}\.\d{1,3}\.\d{1,3}$") {
        [System.Windows.Forms.MessageBox]::Show("Subnet örneği: 192.168.1", "Hatalı subnet", "OK", "Warning") | Out-Null
        return
    }

    if ($baslangic -lt 1) { $baslangic = 1 }
    if ($bitis -gt 254) { $bitis = 254 }
    if ($baslangic -gt $bitis) {
        [System.Windows.Forms.MessageBox]::Show("Başlangıç bitişten büyük olamaz.", "Hata", "OK", "Warning") | Out-Null
        return
    }

    Log-Yaz $lanLog "Tarama başladı: $subnet.$baslangic - $subnet.$bitis"
    Log-Yaz $lanLog "Port kontrolü sadece ping veren cihazlarda yapılır."

    $btnTarama.Enabled = $false
    try {
        for ($i = $baslangic; $i -le $bitis; $i++) {
            $ip = "$subnet.$i"

            if (-not (Ozel-Ip-Kontrol $ip)) {
                Log-Yaz $lanLog "Güvenlik: private olmayan IP atlandı: $ip"
                continue
            }

            $canli = Hizli-Ping $ip 300
            if ($canli) {
                $mac = Arp-Mac-Al $ip
                $hn = Hostname-Al $ip
                $acik = @()

                foreach ($port in $portlar) {
                    if (Tcp-Port-Test $ip $port 250) {
                        $acik += $port
                    }
                    [System.Windows.Forms.Application]::DoEvents()
                }

                $idx = $gridLan.Rows.Add()
                $gridLan.Rows[$idx].Cells[0].Value = $ip
                $gridLan.Rows[$idx].Cells[1].Value = $hn
                $gridLan.Rows[$idx].Cells[2].Value = $mac
                $gridLan.Rows[$idx].Cells[3].Value = "OK"
                $gridLan.Rows[$idx].Cells[4].Value = ($acik -join ", ")
                Log-Yaz $lanLog "Bulundu: $ip MAC=$mac Ports=$($acik -join ',')"
            }

            if (($i % 5) -eq 0) {
                [System.Windows.Forms.Application]::DoEvents()
            }
        }
        Log-Yaz $lanLog "Tarama bitti. Bulunan cihaz: $($gridLan.Rows.Count)"
    } finally {
        $btnTarama.Enabled = $true
    }
})

# ═══════════════════════════════════════════════════════════════════════════════
# SEKME 3: POS KONTROL (Servis üzerinden)
# ═══════════════════════════════════════════════════════════════════════════════

$chkPcOto = New-Object System.Windows.Forms.CheckBox
$chkPcOto.Text = "PC IP otomatik algılansın"
$chkPcOto.Location = New-Object System.Drawing.Point(25, 25)
$chkPcOto.Size = New-Object System.Drawing.Size(220, 24)
$chkPcOto.Checked = ($script:Ayarlar.PcIpMod -eq "Otomatik")
$tabPos.Controls.Add($chkPcOto)

$tabPos.Controls.Add((Etiket-Olustur "PC IP:" 25 65 120))
$txtPcIp = MetinKutusu-Olustur "" 160 62 220
$tabPos.Controls.Add($txtPcIp)

$tabPos.Controls.Add((Etiket-Olustur "POS IP:" 25 105 120))
$txtPosIp = MetinKutusu-Olustur $script:Ayarlar.PosIp 160 102 220
$tabPos.Controls.Add($txtPosIp)

$tabPos.Controls.Add((Etiket-Olustur "POS TCP Port:" 25 145 120))
$txtPosPort = MetinKutusu-Olustur "$($script:Ayarlar.PosPort)" 160 142 220
$tabPos.Controls.Add($txtPosPort)

$tabPos.Controls.Add((Etiket-Olustur "Servis Port:" 25 185 120))
$txtServisPort = MetinKutusu-Olustur "$($script:Ayarlar.YerelApiPort)" 160 182 220
$tabPos.Controls.Add($txtServisPort)

$btnPcAlgila = Buton-Olustur "PC IP Algıla" 410 60 130
$btnPosBaglan = Buton-Olustur "POS Bağlan" 410 100 130
$btnPosDurum = Buton-Olustur "POS Durum Sorgula" 410 140 160
$btnPosSatis = Buton-Olustur "Test Satış" 410 180 130
$btnPosGunSonu = Buton-Olustur "Gün Sonu" 560 100 130
$btnPosAyarKaydet = Buton-Olustur "Ayarları Kaydet" 560 140 150
$tabPos.Controls.AddRange(@($btnPcAlgila, $btnPosBaglan, $btnPosDurum, $btnPosSatis, $btnPosGunSonu, $btnPosAyarKaydet))

$posBilgiKutu = New-Object System.Windows.Forms.GroupBox
$posBilgiKutu.Text = "Bilgi"
$posBilgiKutu.Location = New-Object System.Drawing.Point(25, 240)
$posBilgiKutu.Size = New-Object System.Drawing.Size(1140, 100)
$tabPos.Controls.Add($posBilgiKutu)

$posNot = New-Object System.Windows.Forms.Label
$posNot.Text = "SUMER HUB Servisi (localhost:8189) aracılığıyla Ingenico POS terminaline bağlanır.`r`nPOS IP: örn. 192.168.1.39, Port: 7778 (ECR mod) veya 7777 (stand-alone).`r`nServis çalışmıyorsa 'Genel Durum' sekmesinden başlatın."
$posNot.Location = New-Object System.Drawing.Point(15, 25)
$posNot.Size = New-Object System.Drawing.Size(1100, 60)
$posBilgiKutu.Controls.Add($posNot)

$posLog = LogKutusu-Olustur 25 360 1140 320
$tabPos.Controls.Add($posLog)

function PcIp-Yenile {
    $bilgi = Ana-IPv4-Bilgisi
    if ($chkPcOto.Checked) {
        $txtPcIp.Text = $bilgi.IP
        $txtPcIp.ReadOnly = $true
    } else {
        $txtPcIp.ReadOnly = $false
        if (-not [string]::IsNullOrWhiteSpace($script:Ayarlar.PcIpManuel)) {
            $txtPcIp.Text = $script:Ayarlar.PcIpManuel
        }
    }
}

$chkPcOto.Add_CheckedChanged({ PcIp-Yenile })
$btnPcAlgila.Add_Click({
    PcIp-Yenile
    Log-Yaz $posLog "PC IP algılandı: $($txtPcIp.Text)"
})

$btnPosBaglan.Add_Click({
    $ip = $txtPosIp.Text.Trim()
    $port = [int]$txtPosPort.Text

    if (-not (Ozel-Ip-Kontrol $ip)) {
        [System.Windows.Forms.MessageBox]::Show("POS IP private/local ağda olmalı. Örnek: 192.168.1.39", "Güvenlik", "OK", "Warning") | Out-Null
        return
    }

    Log-Yaz $posLog "POS bağlantı isteği gönderiliyor: $ip`:$port"
    $sonuc = Servis-Post -Endpoint "/pos/baglan" -Govde @{ Ip = $ip; Port = $port }

    if ($sonuc.Basarili) {
        if ($sonuc.Veri.Basarili) {
            Log-Yaz $posLog "✅ POS bağlantısı başarılı! Kayıt: $($sonuc.Veri.Kayit.Mesaj)"
            [System.Windows.Forms.MessageBox]::Show("POS bağlantısı başarılı!`r`nIP: $ip`:$port", "POS Bağlantı", "OK", "Information") | Out-Null
        } else {
            Log-Yaz $posLog "❌ POS kayıt hatası: $($sonuc.Veri.Hata)"
        }
    } else {
        Log-Yaz $posLog "❌ Servis hatası: $($sonuc.Hata)"
        [System.Windows.Forms.MessageBox]::Show("Servise ulaşılamıyor. Servisin çalıştığından emin olun.`r`n$($sonuc.Hata)", "Hata", "OK", "Error") | Out-Null
    }
})

$btnPosDurum.Add_Click({
    Log-Yaz $posLog "POS durum sorgulanıyor..."
    $sonuc = Servis-Get -Endpoint "/pos/durum"

    if ($sonuc.Basarili) {
        Log-Yaz $posLog "POS Durum: $($sonuc.Veri.Mesaj)"
        if ($sonuc.Veri.TerminalHazir) {
            Log-Yaz $posLog "✅ Terminal hazır ve işlem yapabilir."
        } else {
            Log-Yaz $posLog "⚠️ Terminal meşgul veya hata durumunda."
        }
    } else {
        Log-Yaz $posLog "❌ Durum sorgu hatası: $($sonuc.Hata)"
    }
})

$btnPosSatis.Add_Click({
    $tutar = [decimal]0
    $girdi = [System.Windows.Forms.InputBox]::Show("Test satış tutarı (TL):", "Test Satış", "1.00")
    if (-not [decimal]::TryParse($girdi, [ref]$tutar)) {
        Log-Yaz $posLog "Geçersiz tutar girildi."
        return
    }

    Log-Yaz $posLog "Test satış gönderiliyor: $tutar TL"
    $sonuc = Servis-Post -Endpoint "/pos/satis" -Govde @{ Tutar = $tutar; IslemNo = "TEST001"; Taksit = 0 }

    if ($sonuc.Basarili) {
        if ($sonuc.Veri.Basarili) {
            Log-Yaz $posLog "✅ Satış başarılı! Onay: $($sonuc.Veri.Detay.OnayKodu)"
        } else {
            Log-Yaz $posLog "❌ Satış hatası: $($sonuc.Veri.Hata)"
        }
    } else {
        Log-Yaz $posLog "❌ Servis hatası: $($sonuc.Hata)"
    }
})

$btnPosGunSonu.Add_Click({
    $cevap = [System.Windows.Forms.MessageBox]::Show("Gün sonu işlemi yapılacak. Emin misiniz?", "Gün Sonu", "YesNo", "Question")
    if ($cevap -eq "Yes") {
        Log-Yaz $posLog "Gün sonu gönderiliyor..."
        $sonuc = Servis-Get -Endpoint "/pos/gunsonu"
        if ($sonuc.Basarili) {
            if ($sonuc.Veri.Basarili) {
                Log-Yaz $posLog "✅ Gün sonu başarılı!"
            } else {
                Log-Yaz $posLog "❌ Gün sonu hatası: $($sonuc.Veri.Hata)"
            }
        } else {
            Log-Yaz $posLog "❌ Servis hatası: $($sonuc.Hata)"
        }
    }
})

$btnPosAyarKaydet.Add_Click({
    $script:Ayarlar.PcIpMod = if ($chkPcOto.Checked) { "Otomatik" } else { "Manuel" }
    $script:Ayarlar.PcIpManuel = $txtPcIp.Text.Trim()
    $script:Ayarlar.PosIp = $txtPosIp.Text.Trim()
    $script:Ayarlar.PosPort = [int]$txtPosPort.Text
    $script:Ayarlar.YerelApiPort = [int]$txtServisPort.Text

    # Servis ayarlarını da güncelle
    $servisAyar = @{
        PosIp = $script:Ayarlar.PosIp
        PosPort = $script:Ayarlar.PosPort
        HttpPort = $script:Ayarlar.YerelApiPort
    }
    $null = Servis-Post -Endpoint "/pos/ayarlar" -Govde $servisAyar

    if (Ayarlari-Kaydet $script:Ayarlar) {
        Log-Yaz $posLog "POS/Ağ ayarları kaydedildi."
    }
})

# ═══════════════════════════════════════════════════════════════════════════════
# SEKME 4: TRENDYOL API
# ═══════════════════════════════════════════════════════════════════════════════

$tabTrendyol.Controls.Add((Etiket-Olustur "Supplier ID:" 25 25 120))
$txtSupplier = MetinKutusu-Olustur $script:Ayarlar.TrendyolSupplierId 160 22 260
$tabTrendyol.Controls.Add($txtSupplier)

$tabTrendyol.Controls.Add((Etiket-Olustur "API Key:" 25 65 120))
$txtApiKey = MetinKutusu-Olustur $script:Ayarlar.TrendyolApiKey 160 62 360
$tabTrendyol.Controls.Add($txtApiKey)

$tabTrendyol.Controls.Add((Etiket-Olustur "API Secret:" 25 105 120))
$txtApiSecret = MetinKutusu-Olustur (Metin-Coz $script:Ayarlar.TrendyolApiSecretSifreli) 160 102 360
$txtApiSecret.UseSystemPasswordChar = $true
$tabTrendyol.Controls.Add($txtApiSecret)

$tabTrendyol.Controls.Add((Etiket-Olustur "Base URL:" 25 145 120))
$txtBaseUrl = MetinKutusu-Olustur $script:Ayarlar.TrendyolBaseUrl 160 142 650
$tabTrendyol.Controls.Add($txtBaseUrl)

$btnTrendyolKaydet = Buton-Olustur "API Ayar Kaydet" 840 22 150
$btnSiparisCek = Buton-Olustur "Siparişleri Çek" 840 62 150
$btnOrnekSiparis = Buton-Olustur "Örnek Sipariş" 840 102 150
$btnFiseAktar = Buton-Olustur "Seçileni Fişe Aktar" 840 142 150
$tabTrendyol.Controls.AddRange(@($btnTrendyolKaydet, $btnSiparisCek, $btnOrnekSiparis, $btnFiseAktar))

$gridSiparisler = New-Object System.Windows.Forms.DataGridView
$gridSiparisler.Location = New-Object System.Drawing.Point(25, 190)
$gridSiparisler.Size = New-Object System.Drawing.Size(1140, 260)
Grid-Ayarla $gridSiparisler
[void]$gridSiparisler.Columns.Add("SiparisNo", "Sipariş No")
[void]$gridSiparisler.Columns.Add("Musteri", "Müşteri")
[void]$gridSiparisler.Columns.Add("Odeme", "Ödeme")
[void]$gridSiparisler.Columns.Add("Toplam", "Toplam")
[void]$gridSiparisler.Columns.Add("Durum", "Durum")
$tabTrendyol.Controls.Add($gridSiparisler)

$trendyolLog = LogKutusu-Olustur 25 470 1140 210
$tabTrendyol.Controls.Add($trendyolLog)

function Siparis-Grid-Yenile {
    $gridSiparisler.Rows.Clear()
    for ($i = 0; $i -lt $script:SonSiparisler.Count; $i++) {
        $o = $script:SonSiparisler[$i]
        $siparisNo = Ozellik-Al $o @("orderNumber", "orderNo", "id", "packageId") "-"
        $musteri = ((Ozellik-Al $o @("customerFirstName", "firstName") "") + " " + (Ozellik-Al $o @("customerLastName", "lastName") "")).Trim()
        if ([string]::IsNullOrWhiteSpace($musteri)) { $musteri = Ozellik-Al $o @("customer", "customerName") "-" }
        $odeme = Ozellik-Al $o @("paymentType", "paymentTypeDescription", "paymentMethod") "-"
        $toplam = Ozellik-Al $o @("totalPrice", "totalAmount", "grossAmount", "amount") 0
        $durum = Ozellik-Al $o @("status", "packageStatus", "orderStatus") "-"

        $idx = $gridSiparisler.Rows.Add()
        $gridSiparisler.Rows[$idx].Cells[0].Value = $siparisNo
        $gridSiparisler.Rows[$idx].Cells[1].Value = $musteri
        $gridSiparisler.Rows[$idx].Cells[2].Value = $odeme
        $gridSiparisler.Rows[$idx].Cells[3].Value = Para-Formatla $toplam
        $gridSiparisler.Rows[$idx].Cells[4].Value = $durum
        $gridSiparisler.Rows[$idx].Tag = $i
    }
}

$btnTrendyolKaydet.Add_Click({
    $script:Ayarlar.TrendyolSupplierId = $txtSupplier.Text.Trim()
    $script:Ayarlar.TrendyolApiKey = $txtApiKey.Text.Trim()
    $script:Ayarlar.TrendyolApiSecretSifreli = Metin-Sifrele $txtApiSecret.Text
    $script:Ayarlar.TrendyolBaseUrl = $txtBaseUrl.Text.Trim()
    if (Ayarlari-Kaydet $script:Ayarlar) {
        Log-Yaz $trendyolLog "Trendyol API ayarları kaydedildi. Secret şifrelendi."
    }
})

$btnSiparisCek.Add_Click({
    try {
        Log-Yaz $trendyolLog "Trendyol sipariş çekme başladı..."
        $siparisler = Trendyol-Siparis-Cek `
            -SupplierId $txtSupplier.Text.Trim() `
            -ApiKey $txtApiKey.Text.Trim() `
            -ApiSecret $txtApiSecret.Text `
            -BaseUrl $txtBaseUrl.Text.Trim()

        $script:SonSiparisler = @($siparisler)
        Siparis-Grid-Yenile
        Log-Yaz $trendyolLog "Sipariş çekme tamamlandı. Adet: $($script:SonSiparisler.Count)"
    } catch {
        Log-Yaz $trendyolLog "Trendyol hata: $($_.Exception.Message)"
        [System.Windows.Forms.MessageBox]::Show("Trendyol sipariş çekilemedi:`r`n$($_.Exception.Message)", "Trendyol API", "OK", "Warning") | Out-Null
    }
})

$btnOrnekSiparis.Add_Click({
    $script:SonSiparisler = @(Ornek-Siparis-Al)
    Siparis-Grid-Yenile
    Log-Yaz $trendyolLog "Örnek sipariş yüklendi."
})

# ═══════════════════════════════════════════════════════════════════════════════
# SEKME 5: FİŞ / YAZICI
# ═══════════════════════════════════════════════════════════════════════════════

$tabFis.Controls.Add((Etiket-Olustur "Yazıcı:" 25 25 100))
$cmbYazicilar = New-Object System.Windows.Forms.ComboBox
$cmbYazicilar.Location = New-Object System.Drawing.Point(125, 22)
$cmbYazicilar.Size = New-Object System.Drawing.Size(360, 24)
$cmbYazicilar.DropDownStyle = "DropDownList"
$tabFis.Controls.Add($cmbYazicilar)

$btnYaziciYenile = Buton-Olustur "Yazıcı Yenile" 505 18 130
$btnYaziciKaydet = Buton-Olustur "Yazıcı Kaydet" 650 18 130
$btnOrnekFis = Buton-Olustur "Örnek Fiş" 795 18 110
$btnYazdir = Buton-Olustur "Yazdır" 920 18 100
$tabFis.Controls.AddRange(@($btnYaziciYenile, $btnYaziciKaydet, $btnOrnekFis, $btnYazdir))

$tabFis.Controls.Add((Etiket-Olustur "Başlık:" 25 65 100))
$txtFisBaslik = MetinKutusu-Olustur $script:Ayarlar.FisBaslik 125 62 360
$tabFis.Controls.Add($txtFisBaslik)

$tabFis.Controls.Add((Etiket-Olustur "Alt Yazı:" 505 65 100))
$txtFisAltYazi = MetinKutusu-Olustur $script:Ayarlar.FisAltYazi 605 62 360
$tabFis.Controls.Add($txtFisAltYazi)

$txtFis = New-Object System.Windows.Forms.TextBox
$txtFis.Location = New-Object System.Drawing.Point(25, 105)
$txtFis.Size = New-Object System.Drawing.Size(1140, 575)
$txtFis.Multiline = $true
$txtFis.ScrollBars = "Both"
$txtFis.Font = New-Object System.Drawing.Font("Consolas", 10)
$txtFis.WordWrap = $false
$tabFis.Controls.Add($txtFis)

function Yazicilari-Yenile {
    $cmbYazicilar.Items.Clear()
    $yazicilar = Yazicilari-Al
    foreach ($p in $yazicilar) { [void]$cmbYazicilar.Items.Add($p) }

    if (-not [string]::IsNullOrWhiteSpace($script:Ayarlar.YaziciAdi)) {
        $idx = $cmbYazicilar.Items.IndexOf($script:Ayarlar.YaziciAdi)
        if ($idx -ge 0) { $cmbYazicilar.SelectedIndex = $idx }
    }
    if ($cmbYazicilar.SelectedIndex -lt 0 -and $cmbYazicilar.Items.Count -gt 0) {
        $cmbYazicilar.SelectedIndex = 0
    }
}

$btnYaziciYenile.Add_Click({ Yazicilari-Yenile })

$btnYaziciKaydet.Add_Click({
    if ($cmbYazicilar.SelectedItem) {
        $script:Ayarlar.YaziciAdi = "$($cmbYazicilar.SelectedItem)"
    }
    $script:Ayarlar.FisBaslik = $txtFisBaslik.Text
    $script:Ayarlar.FisAltYazi = $txtFisAltYazi.Text
    if (Ayarlari-Kaydet $script:Ayarlar) {
        [System.Windows.Forms.MessageBox]::Show("Fiş/yazıcı ayarları kaydedildi.", "Ayar", "OK", "Information") | Out-Null
    }
})

$btnOrnekFis.Add_Click({
    $siparis = Ornek-Siparis-Al
    $txtFis.Text = Fis-Olustur -Siparis $siparis -Baslik $txtFisBaslik.Text -AltYazi $txtFisAltYazi.Text
    $script:SonFisMetni = $txtFis.Text
})

$btnFiseAktar.Add_Click({
    if ($gridSiparisler.SelectedRows.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show("Önce Trendyol sipariş listesinden bir satır seçin.", "Fiş", "OK", "Information") | Out-Null
        return
    }

    $idx = [int]$gridSiparisler.SelectedRows[0].Tag
    if ($idx -lt 0 -or $idx -ge $script:SonSiparisler.Count) { return }

    $siparis = $script:SonSiparisler[$idx]
    $fis = Fis-Olustur -Siparis $siparis -Baslik $txtFisBaslik.Text -AltYazi $txtFisAltYazi.Text
    $txtFis.Text = $fis
    $script:SonFisMetni = $fis

    $tabs.SelectedTab = $tabFis
    Log-Yaz $trendyolLog "Seçili sipariş fişe aktarıldı."
})

$btnYazdir.Add_Click({
    try {
        $yazici = ""
        if ($cmbYazicilar.SelectedItem) { $yazici = "$($cmbYazicilar.SelectedItem)" }
        Yazdir-Metin -YaziciAdi $yazici -Metin $txtFis.Text
        [System.Windows.Forms.MessageBox]::Show("Fiş yazıcıya gönderildi.", "Yazdır", "OK", "Information") | Out-Null
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Yazdırma hatası:`r`n$($_.Exception.Message)", "Yazdır", "OK", "Warning") | Out-Null
    }
})

# ═══════════════════════════════════════════════════════════════════════════════
# İLK YÜKLEME
# ═══════════════════════════════════════════════════════════════════════════════

$form.Add_Shown({
    Durum-Yenile
    PcIp-Yenile
    Yazicilari-Yenile

    Log-Yaz $durumLog "SUMER HUB v2.0 hazır."
    Log-Yaz $durumLog "POS Servisi localhost:$($script:Ayarlar.YerelApiPort) üzerinde çalışmalı."
    Log-Yaz $durumLog "Servis çalışmıyorsa 'Genel Durum' sekmesinden başlatın."
    Log-Yaz $posLog "POS IP: $($script:Ayarlar.PosIp), Port: $($script:Ayarlar.PosPort)"
    Log-Yaz $trendyolLog "Trendyol API bilgilerini girip 'Siparişleri Çek' kullanabilirsiniz."
})

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::Run($form)
