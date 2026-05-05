# =====================================================================
# SAMER Hub - Professional PowerShell App
# File: SAMER-Hub-App.ps1
#
# Bu dosya Windows pencere uygulamasıdır.
# Arka plandaki yerel API servisi:
#   SAMER-Hub-ApiService.ps1 -> http://127.0.0.1:8189
# =====================================================================

$ErrorActionPreference = "Continue"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$AppTitle = "SAMER Hub - LAN, POS ve Trendyol Merkezi"
$Version = "1.0.0"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ApiScriptPath = Join-Path $ScriptDir "SAMER-Hub-ApiService.ps1"
$LogoPath = Join-Path $ScriptDir "samer-hub-logo.png"
$IconPath = Join-Path $ScriptDir "samer-hub.ico"
$ApiPort = 8189
$ApiBase = "http://127.0.0.1:$ApiPort"

$Global:ApiProcess = $null
$Global:Orders = @()
$Global:SelectedOrder = $null

function Show-Message {
    param([string]$Text, [string]$Title = "SAMER Hub", [string]$Icon = "Information")
    [System.Windows.Forms.MessageBox]::Show($Text, $Title, "OK", $Icon) | Out-Null
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

function Invoke-SamerApi {
    param(
        [string]$Method,
        [string]$Path,
        $Body = $null,
        [int]$TimeoutSec = 60
    )

    $uri = "$ApiBase$Path"

    if ($null -eq $Body) {
        return Invoke-RestMethod -Method $Method -Uri $uri -TimeoutSec $TimeoutSec
    }

    $json = $Body | ConvertTo-Json -Depth 30
    return Invoke-RestMethod -Method $Method -Uri $uri -ContentType "application/json; charset=utf-8" -Body $json -TimeoutSec $TimeoutSec
}

function Test-ApiHealth {
    try {
        $h = Invoke-SamerApi -Method "GET" -Path "/health" -TimeoutSec 2
        return ($h.ok -eq $true)
    } catch {
        return $false
    }
}

function Start-ApiService {
    if (Test-ApiHealth) { return $true }

    if (-not (Test-Path $ApiScriptPath)) {
        Show-Message "API servis dosyası bulunamadı:`r`n$ApiScriptPath" "Eksik Dosya" "Error"
        return $false
    }

    try {
        $args = "-NoProfile -ExecutionPolicy Bypass -File `"$ApiScriptPath`" -Port $ApiPort -BindAddress 127.0.0.1"
        $Global:ApiProcess = Start-Process -FilePath "powershell.exe" -ArgumentList $args -WindowStyle Hidden -PassThru

        for ($i = 0; $i -lt 20; $i++) {
            Start-Sleep -Milliseconds 300
            if (Test-ApiHealth) { return $true }
            [System.Windows.Forms.Application]::DoEvents()
        }

        return $false
    } catch {
        Show-Message "API servis başlatılamadı:`r`n$($_.Exception.Message)" "Servis Hatası" "Error"
        return $false
    }
}

function Stop-ApiService {
    try {
        Invoke-SamerApi -Method "POST" -Path "/api/service/shutdown" -Body ([pscustomobject]@{}) -TimeoutSec 2 | Out-Null
    } catch { }

    try {
        if ($Global:ApiProcess -and -not $Global:ApiProcess.HasExited) {
            $Global:ApiProcess.Kill()
        }
    } catch { }
}

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

function Configure-Grid {
    param([System.Windows.Forms.DataGridView]$Grid)
    $Grid.AllowUserToAddRows = $false
    $Grid.AllowUserToDeleteRows = $false
    $Grid.ReadOnly = $true
    $Grid.SelectionMode = "FullRowSelect"
    $Grid.MultiSelect = $false
    $Grid.AutoSizeColumnsMode = "Fill"
    $Grid.RowHeadersVisible = $false
    $Grid.BackgroundColor = [System.Drawing.Color]::White
}

function Get-SubnetBaseFromIp {
    param([string]$Ip)
    if ($Ip -match "^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.\d{1,3}$") {
        return "$($Matches[1]).$($Matches[2]).$($Matches[3])"
    }
    return "192.168.1"
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

function Get-InstalledPrintersSafe {
    $printers = @()
    try {
        $printers = Get-Printer -ErrorAction Stop | Select-Object -ExpandProperty Name
    } catch {
        try {
            foreach ($p in [System.Drawing.Printing.PrinterSettings]::InstalledPrinters) {
                $printers += "$p"
            }
        } catch { }
    }
    return @($printers)
}

function Print-TextDocument {
    param(
        [string]$PrinterName,
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        throw "Yazdırılacak fiş metni boş."
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

# ---------------------------
# Ana pencere
# ---------------------------

[System.Windows.Forms.Application]::EnableVisualStyles()

$form = New-Object System.Windows.Forms.Form
$form.Text = $AppTitle
$form.Size = New-Object System.Drawing.Size(1180, 790)
$form.StartPosition = "CenterScreen"
$form.MinimumSize = New-Object System.Drawing.Size(1040, 700)
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9)

if (Test-Path $IconPath) {
    try { $form.Icon = New-Object System.Drawing.Icon($IconPath) } catch { }
}

$root = New-Object System.Windows.Forms.Panel
$root.Dock = "Fill"
$form.Controls.Add($root)

$header = New-Object System.Windows.Forms.Panel
$header.Height = 92
$header.Dock = "Top"
$header.BackColor = [System.Drawing.Color]::FromArgb(20, 83, 45)
$root.Controls.Add($header)

$logo = New-Object System.Windows.Forms.PictureBox
$logo.Location = New-Object System.Drawing.Point(20, 14)
$logo.Size = New-Object System.Drawing.Size(64, 64)
$logo.SizeMode = "Zoom"
if (Test-Path $LogoPath) {
    try { $logo.Image = [System.Drawing.Image]::FromFile($LogoPath) } catch { }
}
$header.Controls.Add($logo)

$title = New-Object System.Windows.Forms.Label
$title.Text = "SAMER Hub"
$title.ForeColor = [System.Drawing.Color]::White
$title.Font = New-Object System.Drawing.Font("Segoe UI", 20, [System.Drawing.FontStyle]::Bold)
$title.Location = New-Object System.Drawing.Point(100, 15)
$title.Size = New-Object System.Drawing.Size(360, 34)
$header.Controls.Add($title)

$subtitle = New-Object System.Windows.Forms.Label
$subtitle.Text = "LAN keşfi, Ingenico POS bağlantısı, Trendyol siparişleri ve fiş yazdırma merkezi"
$subtitle.ForeColor = [System.Drawing.Color]::FromArgb(220, 255, 230)
$subtitle.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$subtitle.Location = New-Object System.Drawing.Point(103, 53)
$subtitle.Size = New-Object System.Drawing.Size(780, 24)
$header.Controls.Add($subtitle)

$lblApiState = New-Object System.Windows.Forms.Label
$lblApiState.Text = "API: kontrol ediliyor..."
$lblApiState.ForeColor = [System.Drawing.Color]::White
$lblApiState.TextAlign = "MiddleRight"
$lblApiState.Location = New-Object System.Drawing.Point(850, 22)
$lblApiState.Size = New-Object System.Drawing.Size(280, 24)
$header.Controls.Add($lblApiState)

$tabs = New-Object System.Windows.Forms.TabControl
$tabs.Dock = "Fill"
$root.Controls.Add($tabs)

$statusBar = New-Object System.Windows.Forms.StatusStrip
$statusLabel = New-Object System.Windows.Forms.ToolStripStatusLabel
$statusLabel.Text = "Hazırlanıyor..."
$statusBar.Items.Add($statusLabel) | Out-Null
$root.Controls.Add($statusBar)
$statusBar.Dock = "Bottom"

$tabDashboard = New-Object System.Windows.Forms.TabPage
$tabDashboard.Text = "Pano"

$tabLan = New-Object System.Windows.Forms.TabPage
$tabLan.Text = "LAN Haritası"

$tabPos = New-Object System.Windows.Forms.TabPage
$tabPos.Text = "POS / ECR"

$tabTrendyol = New-Object System.Windows.Forms.TabPage
$tabTrendyol.Text = "Trendyol"

$tabReceipt = New-Object System.Windows.Forms.TabPage
$tabReceipt.Text = "Fiş ve Yazıcı"

$tabs.TabPages.AddRange(@($tabDashboard, $tabLan, $tabPos, $tabTrendyol, $tabReceipt))

# ---------------------------
# Pano
# ---------------------------

$lblDash = New-Label "Sistem Durumu" 25 25 400 32
$lblDash.Font = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Bold)
$tabDashboard.Controls.Add($lblDash)

$btnRefreshStatus = New-Button "Durumu Yenile" 25 75 140
$btnStartApi = New-Button "API Servisini Başlat" 180 75 160
$btnStopApi = New-Button "API Servisini Durdur" 355 75 165
$tabDashboard.Controls.AddRange(@($btnRefreshStatus, $btnStartApi, $btnStopApi))

$dashInfo = New-Object System.Windows.Forms.GroupBox
$dashInfo.Text = "Özet"
$dashInfo.Location = New-Object System.Drawing.Point(25, 125)
$dashInfo.Size = New-Object System.Drawing.Size(1085, 155)
$tabDashboard.Controls.Add($dashInfo)

$lblComputer = New-Label "Bilgisayar:" 20 35 140
$valComputer = New-Label "-" 180 35 800
$lblIp = New-Label "PC IP:" 20 70 140
$valIp = New-Label "-" 180 70 800
$lblAdmin = New-Label "Yönetici Yetkisi:" 20 105 140
$valAdmin = New-Label "-" 180 105 800
$dashInfo.Controls.AddRange(@($lblComputer, $valComputer, $lblIp, $valIp, $lblAdmin, $valAdmin))

$dashLog = New-LogBox 25 305 1085 300
$tabDashboard.Controls.Add($dashLog)

# ---------------------------
# LAN Haritası
# ---------------------------

$tabLan.Controls.Add((New-Label "Ağ Bloğu /24:" 25 25 110))
$txtSubnet = New-TextBox "192.168.1" 145 22 140
$tabLan.Controls.Add($txtSubnet)

$tabLan.Controls.Add((New-Label "Başlangıç:" 305 25 80))
$txtLanStart = New-TextBox "1" 385 22 55
$tabLan.Controls.Add($txtLanStart)

$tabLan.Controls.Add((New-Label "Bitiş:" 455 25 50))
$txtLanEnd = New-TextBox "254" 510 22 55
$tabLan.Controls.Add($txtLanEnd)

$tabLan.Controls.Add((New-Label "Kontrol Portları:" 585 25 110))
$txtLanPorts = New-TextBox "80,443,9100,5577,8080,8189" 700 22 210
$tabLan.Controls.Add($txtLanPorts)

$chkNoPingPort = New-Object System.Windows.Forms.CheckBox
$chkNoPingPort.Text = "Ping kapalı cihazlarda port dene"
$chkNoPingPort.Location = New-Object System.Drawing.Point(925, 24)
$chkNoPingPort.Size = New-Object System.Drawing.Size(220, 22)
$tabLan.Controls.Add($chkNoPingPort)

$btnScan = New-Button "Ağı Tara" 25 62 120
$tabLan.Controls.Add($btnScan)

$gridLan = New-Object System.Windows.Forms.DataGridView
$gridLan.Location = New-Object System.Drawing.Point(25, 105)
$gridLan.Size = New-Object System.Drawing.Size(1085, 390)
Configure-Grid $gridLan
[void]$gridLan.Columns.Add("Ip", "IP Adresi")
[void]$gridLan.Columns.Add("Host", "Cihaz Adı")
[void]$gridLan.Columns.Add("Mac", "MAC")
[void]$gridLan.Columns.Add("Ping", "Ping")
[void]$gridLan.Columns.Add("Ports", "Açık Portlar")
[void]$gridLan.Columns.Add("Cable", "Kablo / Switch Bilgisi")
$tabLan.Controls.Add($gridLan)

$lanLog = New-LogBox 25 515 1085 95
$tabLan.Controls.Add($lanLog)

# ---------------------------
# POS / ECR
# ---------------------------

$groupPos = New-Object System.Windows.Forms.GroupBox
$groupPos.Text = "Ingenico MOVE/5000F - LAN/ECR Ayarları"
$groupPos.Location = New-Object System.Drawing.Point(25, 25)
$groupPos.Size = New-Object System.Drawing.Size(1085, 245)
$tabPos.Controls.Add($groupPos)

$groupPos.Controls.Add((New-Label "PC IP Modu:" 20 35 130))
$chkPcAuto = New-Object System.Windows.Forms.CheckBox
$chkPcAuto.Text = "Otomatik algıla"
$chkPcAuto.Location = New-Object System.Drawing.Point(160, 34)
$chkPcAuto.Size = New-Object System.Drawing.Size(160, 22)
$chkPcAuto.Checked = $true
$groupPos.Controls.Add($chkPcAuto)

$groupPos.Controls.Add((New-Label "PC IP:" 20 72 130))
$txtPcIp = New-TextBox "" 160 68 220
$txtPcIp.ReadOnly = $true
$groupPos.Controls.Add($txtPcIp)

$groupPos.Controls.Add((New-Label "POS IP:" 20 109 130))
$txtPosIp = New-TextBox "192.168.1.39" 160 105 220
$groupPos.Controls.Add($txtPosIp)

$groupPos.Controls.Add((New-Label "POS TCP Port:" 20 146 130))
$txtPosPort = New-TextBox "5577" 160 142 220
$groupPos.Controls.Add($txtPosPort)

$groupPos.Controls.Add((New-Label "Protokol Modu:" 420 35 130))
$cmbProtocol = New-Object System.Windows.Forms.ComboBox
$cmbProtocol.Location = New-Object System.Drawing.Point(560, 32)
$cmbProtocol.Size = New-Object System.Drawing.Size(220, 24)
$cmbProtocol.DropDownStyle = "DropDownList"
[void]$cmbProtocol.Items.Add("FakeSimulation")
[void]$cmbProtocol.Items.Add("TcpRaw")
$cmbProtocol.SelectedIndex = 0
$groupPos.Controls.Add($cmbProtocol)

$groupPos.Controls.Add((New-Label "Mesaj Şablonu:" 420 72 130))
$txtTemplate = New-TextBox "SALE|{orderNo}|{amountCents}|{currency}" 560 68 430
$groupPos.Controls.Add($txtTemplate)

$chkStxEtx = New-Object System.Windows.Forms.CheckBox
$chkStxEtx.Text = "STX/ETX çerçevesi ekle"
$chkStxEtx.Location = New-Object System.Drawing.Point(560, 105)
$chkStxEtx.Size = New-Object System.Drawing.Size(220, 22)
$groupPos.Controls.Add($chkStxEtx)

$btnSavePos = New-Button "Ayarları Kaydet" 20 190 140
$btnTestPos = New-Button "POS Bağlantısını Test Et" 175 190 190
$btnPosFirewall = New-Button "Firewall İzni Ekle" 380 190 160
$btnFakeSale = New-Button "Örnek Ödeme Simülasyonu" 555 190 200
$groupPos.Controls.AddRange(@($btnSavePos, $btnTestPos, $btnPosFirewall, $btnFakeSale))

$posNote = New-Object System.Windows.Forms.Label
$posNote.Text = "Gerçek Ingenico ödeme akışı için bankanın/Ingenico'nun ECR/TCP mesaj formatı gerekir. Bu ekranda IP, port, firewall ve TCP haberleşme altyapısı hazırdır."
$posNote.Location = New-Object System.Drawing.Point(25, 285)
$posNote.Size = New-Object System.Drawing.Size(1085, 38)
$tabPos.Controls.Add($posNote)

$posLog = New-LogBox 25 335 1085 275
$tabPos.Controls.Add($posLog)

# ---------------------------
# Trendyol
# ---------------------------

$groupTy = New-Object System.Windows.Forms.GroupBox
$groupTy.Text = "Trendyol API Bilgileri"
$groupTy.Location = New-Object System.Drawing.Point(25, 25)
$groupTy.Size = New-Object System.Drawing.Size(1085, 170)
$tabTrendyol.Controls.Add($groupTy)

$groupTy.Controls.Add((New-Label "Supplier ID:" 20 35 120))
$txtSupplier = New-TextBox "" 150 32 250
$groupTy.Controls.Add($txtSupplier)

$groupTy.Controls.Add((New-Label "API Key:" 20 72 120))
$txtApiKey = New-TextBox "" 150 69 360
$groupTy.Controls.Add($txtApiKey)

$groupTy.Controls.Add((New-Label "API Secret:" 20 109 120))
$txtApiSecret = New-TextBox "" 150 106 360
$txtApiSecret.UseSystemPasswordChar = $true
$groupTy.Controls.Add($txtApiSecret)

$groupTy.Controls.Add((New-Label "Base URL:" 535 35 90))
$txtBaseUrl = New-TextBox "https://api.trendyol.com/sapigw/suppliers/{supplierId}/orders?status=Created" 625 32 430
$groupTy.Controls.Add($txtBaseUrl)

$btnSaveTy = New-Button "API Ayarlarını Kaydet" 535 105 170
$btnFetchOrders = New-Button "Siparişleri Çek" 720 105 130
$btnMockOrders = New-Button "Örnek Sipariş Yükle" 865 105 160
$groupTy.Controls.AddRange(@($btnSaveTy, $btnFetchOrders, $btnMockOrders))

$gridOrders = New-Object System.Windows.Forms.DataGridView
$gridOrders.Location = New-Object System.Drawing.Point(25, 215)
$gridOrders.Size = New-Object System.Drawing.Size(1085, 285)
Configure-Grid $gridOrders
[void]$gridOrders.Columns.Add("OrderNo", "Sipariş No")
[void]$gridOrders.Columns.Add("Customer", "Müşteri")
[void]$gridOrders.Columns.Add("Payment", "Ödeme Tipi")
[void]$gridOrders.Columns.Add("Total", "Toplam")
[void]$gridOrders.Columns.Add("Status", "Durum")
$tabTrendyol.Controls.Add($gridOrders)

$btnBuildReceipt = New-Button "Seçili Siparişten Fiş Oluştur" 25 515 230
$btnSendPosFromOrder = New-Button "Seçili Siparişi POS Simülasyonuna Gönder" 270 515 295
$tabTrendyol.Controls.AddRange(@($btnBuildReceipt, $btnSendPosFromOrder))

$tyLog = New-LogBox 25 560 1085 50
$tabTrendyol.Controls.Add($tyLog)

# ---------------------------
# Fiş ve Yazıcı
# ---------------------------

$tabReceipt.Controls.Add((New-Label "Yazıcı:" 25 25 90))
$cmbPrinters = New-Object System.Windows.Forms.ComboBox
$cmbPrinters.Location = New-Object System.Drawing.Point(115, 22)
$cmbPrinters.Size = New-Object System.Drawing.Size(360, 24)
$cmbPrinters.DropDownStyle = "DropDownList"
$tabReceipt.Controls.Add($cmbPrinters)

$btnRefreshPrinters = New-Button "Yazıcıları Yenile" 495 18 145
$btnSavePrinter = New-Button "Yazıcı Ayarını Kaydet" 655 18 170
$btnPrint = New-Button "Fişi Yazdır" 840 18 120
$tabReceipt.Controls.AddRange(@($btnRefreshPrinters, $btnSavePrinter, $btnPrint))

$tabReceipt.Controls.Add((New-Label "Fiş Başlığı:" 25 65 90))
$txtReceiptTitle = New-TextBox "KARACABEY GROSS MARKET" 115 62 360
$tabReceipt.Controls.Add($txtReceiptTitle)

$tabReceipt.Controls.Add((New-Label "Alt Yazı:" 495 65 90))
$txtReceiptFooter = New-TextBox "Teşekkür ederiz." 585 62 360
$tabReceipt.Controls.Add($txtReceiptFooter)

$txtReceipt = New-Object System.Windows.Forms.TextBox
$txtReceipt.Location = New-Object System.Drawing.Point(25, 105)
$txtReceipt.Size = New-Object System.Drawing.Size(1085, 505)
$txtReceipt.Multiline = $true
$txtReceipt.ScrollBars = "Both"
$txtReceipt.Font = New-Object System.Drawing.Font("Consolas", 10)
$txtReceipt.WordWrap = $false
$tabReceipt.Controls.Add($txtReceipt)

# ---------------------------
# UI işlevleri
# ---------------------------

function Refresh-ApiState {
    if (Test-ApiHealth) {
        $lblApiState.Text = "API: çalışıyor - $ApiBase"
        $lblApiState.ForeColor = [System.Drawing.Color]::FromArgb(220, 255, 220)
        $statusLabel.Text = "Yerel API hazır."
        return $true
    }

    $lblApiState.Text = "API: kapalı"
    $lblApiState.ForeColor = [System.Drawing.Color]::FromArgb(255, 220, 220)
    $statusLabel.Text = "Yerel API çalışmıyor."
    return $false
}

function Refresh-Dashboard {
    try {
        $st = Invoke-SamerApi -Method "GET" -Path "/api/status" -TimeoutSec 5
        $valComputer.Text = "$($st.computerName) / Kullanıcı: $($st.userName)"
        $valIp.Text = "$($st.ip.ip) /$($st.ip.prefix) - Gateway: $($st.ip.gateway) - Adaptör: $($st.ip.adapter)"
        $valAdmin.Text = if ($st.isAdmin) { "Evet, firewall kuralları eklenebilir." } else { "Hayır. Firewall işlemleri için Yönetici olarak çalıştır." }

        if ($st.ip.ip) {
            $txtSubnet.Text = Get-SubnetBaseFromIp $st.ip.ip
            $txtPcIp.Text = $st.ip.ip
        }

        Write-UiLog $dashLog "Durum yenilendi. API: $ApiBase"
        Refresh-ApiState | Out-Null
    } catch {
        Write-UiLog $dashLog "Durum alınamadı: $($_.Exception.Message)"
        Refresh-ApiState | Out-Null
    }
}

function Load-ConfigToUi {
    try {
        $res = Invoke-SamerApi -Method "GET" -Path "/api/config" -TimeoutSec 5
        if (-not $res.ok) { return }

        $cfg = $res.config

        $chkPcAuto.Checked = ($cfg.pcIpMode -eq "Auto")
        $txtPosIp.Text = "$($cfg.posIp)"
        $txtPosPort.Text = "$($cfg.posPort)"
        $txtTemplate.Text = "$($cfg.posMessageTemplate)"
        $chkStxEtx.Checked = ($cfg.posUseStxEtx -eq $true)

        $modeIndex = $cmbProtocol.Items.IndexOf("$($cfg.posProtocolMode)")
        if ($modeIndex -ge 0) { $cmbProtocol.SelectedIndex = $modeIndex }

        $txtSupplier.Text = "$($cfg.trendyolSupplierId)"
        $txtApiKey.Text = "$($cfg.trendyolApiKey)"
        $txtApiSecret.Text = "$($cfg.trendyolApiSecretPlain)"
        $txtBaseUrl.Text = "$($cfg.trendyolBaseUrl)"
        $txtReceiptTitle.Text = "$($cfg.receiptTitle)"
        $txtReceiptFooter.Text = "$($cfg.receiptFooter)"

        Refresh-Printers
        if ($cfg.printerName) {
            $pi = $cmbPrinters.Items.IndexOf("$($cfg.printerName)")
            if ($pi -ge 0) { $cmbPrinters.SelectedIndex = $pi }
        }
    } catch {
        Write-UiLog $dashLog "Ayarlar yüklenemedi: $($_.Exception.Message)"
    }
}

function Save-ConfigFromUi {
    $printer = ""
    if ($cmbPrinters.SelectedItem) { $printer = "$($cmbPrinters.SelectedItem)" }

    $body = [pscustomobject]@{
        pcIpMode = if ($chkPcAuto.Checked) { "Auto" } else { "Manual" }
        pcIpManual = $txtPcIp.Text
        posIp = $txtPosIp.Text.Trim()
        posPort = [int]$txtPosPort.Text
        posProtocolMode = "$($cmbProtocol.SelectedItem)"
        posMessageTemplate = $txtTemplate.Text
        posUseStxEtx = $chkStxEtx.Checked
        trendyolSupplierId = $txtSupplier.Text.Trim()
        trendyolApiKey = $txtApiKey.Text.Trim()
        trendyolApiSecretPlain = $txtApiSecret.Text
        trendyolBaseUrl = $txtBaseUrl.Text.Trim()
        printerName = $printer
        receiptTitle = $txtReceiptTitle.Text
        receiptFooter = $txtReceiptFooter.Text
    }

    $res = Invoke-SamerApi -Method "POST" -Path "/api/config/save" -Body $body -TimeoutSec 10
    return $res
}

function Refresh-Printers {
    $cmbPrinters.Items.Clear()
    foreach ($p in (Get-InstalledPrintersSafe)) {
        [void]$cmbPrinters.Items.Add($p)
    }
    if ($cmbPrinters.Items.Count -gt 0 -and $cmbPrinters.SelectedIndex -lt 0) {
        $cmbPrinters.SelectedIndex = 0
    }
}

function Refresh-OrdersGrid {
    $gridOrders.Rows.Clear()

    for ($i = 0; $i -lt $Global:Orders.Count; $i++) {
        $o = $Global:Orders[$i]
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

function Get-SelectedOrder {
    if ($gridOrders.SelectedRows.Count -eq 0) { return $null }
    $idx = [int]$gridOrders.SelectedRows[0].Tag
    if ($idx -lt 0 -or $idx -ge $Global:Orders.Count) { return $null }
    return $Global:Orders[$idx]
}

# ---------------------------
# Olaylar
# ---------------------------

$btnStartApi.Add_Click({
    if (Start-ApiService) {
        Refresh-ApiState | Out-Null
        Refresh-Dashboard
        Load-ConfigToUi
        Write-UiLog $dashLog "API servisi başlatıldı."
    } else {
        Show-Message "API servisi başlatılamadı. Port $ApiPort kullanımda olabilir veya PowerShell izni gerekebilir." "API Hatası" "Warning"
    }
})

$btnStopApi.Add_Click({
    Stop-ApiService
    Start-Sleep -Milliseconds 500
    Refresh-ApiState | Out-Null
    Write-UiLog $dashLog "API servisi durdurma isteği gönderildi."
})

$btnRefreshStatus.Add_Click({ Refresh-Dashboard })

$btnScan.Add_Click({
    try {
        $gridLan.Rows.Clear()

        $ports = @()
        foreach ($p in ($txtLanPorts.Text -split ",")) {
            try {
                $port = [int]$p.Trim()
                if ($port -gt 0) { $ports += $port }
            } catch { }
        }

        $body = [pscustomobject]@{
            subnet = $txtSubnet.Text.Trim()
            start = [int]$txtLanStart.Text
            end = [int]$txtLanEnd.Text
            ports = $ports
            timeoutMs = 300
            scanPortsWhenNoPing = $chkNoPingPort.Checked
        }

        $btnScan.Enabled = $false
        Write-UiLog $lanLog "LAN taraması başladı: $($body.subnet).$($body.start)-$($body.end)"

        $res = Invoke-SamerApi -Method "POST" -Path "/api/lan/scan" -Body $body -TimeoutSec 180

        foreach ($d in @($res.devices)) {
            $idx = $gridLan.Rows.Add()
            $gridLan.Rows[$idx].Cells[0].Value = $d.ip
            $gridLan.Rows[$idx].Cells[1].Value = $d.hostname
            $gridLan.Rows[$idx].Cells[2].Value = $d.mac
            $gridLan.Rows[$idx].Cells[3].Value = if ($d.ping) { "Evet" } else { "Hayır" }
            $gridLan.Rows[$idx].Cells[4].Value = (@($d.openPorts) -join ", ")
            $gridLan.Rows[$idx].Cells[5].Value = $d.cableInfo
        }

        Write-UiLog $lanLog "LAN taraması tamamlandı. Bulunan cihaz: $($res.count)"
    } catch {
        Write-UiLog $lanLog "Tarama hatası: $($_.Exception.Message)"
        Show-Message "LAN taraması tamamlanamadı:`r`n$($_.Exception.Message)" "LAN Hatası" "Warning"
    } finally {
        $btnScan.Enabled = $true
    }
})

$btnSavePos.Add_Click({
    try {
        Save-ConfigFromUi | Out-Null
        Write-UiLog $posLog "POS/ECR ayarları kaydedildi."
        Show-Message "POS/ECR ayarları kaydedildi." "Ayarlar"
    } catch {
        Write-UiLog $posLog "Ayar kaydetme hatası: $($_.Exception.Message)"
    }
})

$btnTestPos.Add_Click({
    try {
        $body = [pscustomobject]@{
            ip = $txtPosIp.Text.Trim()
            port = [int]$txtPosPort.Text
        }

        Write-UiLog $posLog "POS bağlantı testi başladı: $($body.ip):$($body.port)"
        $res = Invoke-SamerApi -Method "POST" -Path "/api/pos/test" -Body $body -TimeoutSec 10

        if ($res.reachable) {
            Write-UiLog $posLog "Başarılı: POS portuna erişildi."
            Show-Message "POS bağlantısı başarılı.`r`n$($body.ip):$($body.port)" "POS Testi"
        } else {
            Write-UiLog $posLog "Başarısız: POS portuna erişilemedi."
            Show-Message "POS bağlantısı başarısız.`r`nIP, port, aynı LAN ve POS ECR modunu kontrol et." "POS Testi" "Warning"
        }
    } catch {
        Write-UiLog $posLog "POS test hatası: $($_.Exception.Message)"
    }
})

$btnPosFirewall.Add_Click({
    try {
        $body = [pscustomobject]@{
            ip = $txtPosIp.Text.Trim()
            port = [int]$txtPosPort.Text
        }

        $res = Invoke-SamerApi -Method "POST" -Path "/api/firewall/pos" -Body $body -TimeoutSec 20
        Write-UiLog $posLog "$($res.message) $($res.ruleName)"
        Show-Message "$($res.message)`r`n$($res.ruleName)" "Firewall"
    } catch {
        Write-UiLog $posLog "Firewall hatası: $($_.Exception.Message)"
        Show-Message "Firewall kuralı eklenemedi:`r`n$($_.Exception.Message)" "Firewall" "Warning"
    }
})

$btnFakeSale.Add_Click({
    try {
        Save-ConfigFromUi | Out-Null
        $body = [pscustomobject]@{
            posIp = $txtPosIp.Text.Trim()
            posPort = [int]$txtPosPort.Text
            mode = "$($cmbProtocol.SelectedItem)"
            orderNo = "TEST-" + (Get-Date -Format "yyyyMMddHHmmss")
            amount = 100.50
            currency = "TRY"
        }

        $res = Invoke-SamerApi -Method "POST" -Path "/api/pos/sale" -Body $body -TimeoutSec 20
        Write-UiLog $posLog "Ödeme sonucu: approved=$($res.approved), message=$($res.message)"
        Show-Message "Ödeme sonucu:`r`n$($res.message)" "POS Ödeme"
    } catch {
        Write-UiLog $posLog "Ödeme simülasyonu hatası: $($_.Exception.Message)"
    }
})

$btnSaveTy.Add_Click({
    try {
        Save-ConfigFromUi | Out-Null
        Write-UiLog $tyLog "Trendyol API ayarları kaydedildi."
        Show-Message "Trendyol API ayarları kaydedildi." "Trendyol"
    } catch {
        Write-UiLog $tyLog "Ayar kaydetme hatası: $($_.Exception.Message)"
    }
})

$btnFetchOrders.Add_Click({
    try {
        Save-ConfigFromUi | Out-Null
        $body = [pscustomobject]@{
            supplierId = $txtSupplier.Text.Trim()
            apiKey = $txtApiKey.Text.Trim()
            apiSecret = $txtApiSecret.Text
            baseUrl = $txtBaseUrl.Text.Trim()
        }

        Write-UiLog $tyLog "Trendyol siparişleri çekiliyor..."
        $res = Invoke-SamerApi -Method "POST" -Path "/api/trendyol/orders" -Body $body -TimeoutSec 60
        $Global:Orders = @($res.orders)
        Refresh-OrdersGrid
        Write-UiLog $tyLog "Siparişler alındı. Adet: $($res.count)"
    } catch {
        Write-UiLog $tyLog "Trendyol hatası: $($_.Exception.Message)"
        Show-Message "Trendyol siparişleri çekilemedi:`r`n$($_.Exception.Message)" "Trendyol" "Warning"
    }
})

$btnMockOrders.Add_Click({
    try {
        $res = Invoke-SamerApi -Method "GET" -Path "/api/trendyol/mock" -TimeoutSec 10
        $Global:Orders = @($res.orders)
        Refresh-OrdersGrid
        Write-UiLog $tyLog "Örnek sipariş yüklendi."
    } catch {
        Write-UiLog $tyLog "Örnek sipariş hatası: $($_.Exception.Message)"
    }
})

$btnBuildReceipt.Add_Click({
    try {
        $order = Get-SelectedOrder
        if ($null -eq $order) {
            Show-Message "Lütfen önce sipariş listesinden bir satır seç." "Fiş" "Warning"
            return
        }

        Save-ConfigFromUi | Out-Null

        $body = [pscustomobject]@{
            title = $txtReceiptTitle.Text
            footer = $txtReceiptFooter.Text
            order = $order
        }

        $res = Invoke-SamerApi -Method "POST" -Path "/api/receipt/build" -Body $body -TimeoutSec 20
        $txtReceipt.Text = $res.receipt
        $tabs.SelectedTab = $tabReceipt
        Write-UiLog $tyLog "Seçili siparişten fiş oluşturuldu."
    } catch {
        Write-UiLog $tyLog "Fiş oluşturma hatası: $($_.Exception.Message)"
    }
})

$btnSendPosFromOrder.Add_Click({
    try {
        $order = Get-SelectedOrder
        if ($null -eq $order) {
            Show-Message "Lütfen önce sipariş listesinden bir satır seç." "POS" "Warning"
            return
        }

        Save-ConfigFromUi | Out-Null

        $orderNo = Get-PropOr $order @("orderNumber", "orderNo", "id", "packageId") ("ORDER-" + (Get-Date -Format "yyyyMMddHHmmss"))
        $total = Get-PropOr $order @("totalPrice", "totalAmount", "grossAmount", "amount") 0

        $body = [pscustomobject]@{
            posIp = $txtPosIp.Text.Trim()
            posPort = [int]$txtPosPort.Text
            mode = "$($cmbProtocol.SelectedItem)"
            orderNo = $orderNo
            amount = $total
            currency = "TRY"
        }

        $res = Invoke-SamerApi -Method "POST" -Path "/api/pos/sale" -Body $body -TimeoutSec 30
        Write-UiLog $tyLog "POS sonucu: approved=$($res.approved), message=$($res.message)"
        Show-Message "POS sonucu:`r`n$($res.message)" "POS"
    } catch {
        Write-UiLog $tyLog "POS gönderim hatası: $($_.Exception.Message)"
    }
})

$btnRefreshPrinters.Add_Click({ Refresh-Printers })

$btnSavePrinter.Add_Click({
    try {
        Save-ConfigFromUi | Out-Null
        Show-Message "Yazıcı ve fiş ayarları kaydedildi." "Yazıcı"
    } catch {
        Show-Message "Yazıcı ayarı kaydedilemedi:`r`n$($_.Exception.Message)" "Yazıcı" "Warning"
    }
})

$btnPrint.Add_Click({
    try {
        $printer = ""
        if ($cmbPrinters.SelectedItem) { $printer = "$($cmbPrinters.SelectedItem)" }
        Print-TextDocument -PrinterName $printer -Text $txtReceipt.Text
        Show-Message "Fiş yazıcıya gönderildi." "Yazdır"
    } catch {
        Show-Message "Fiş yazdırılamadı:`r`n$($_.Exception.Message)" "Yazdır" "Warning"
    }
})

$form.Add_FormClosing({
    # API servisini otomatik kapatmıyoruz; uygulama yeniden açıldığında aynı servisi kullanabilir.
})

$form.Add_Shown({
    $statusLabel.Text = "Yerel API başlatılıyor..."
    if (Start-ApiService) {
        Refresh-ApiState | Out-Null
        Refresh-Dashboard
        Load-ConfigToUi
        Write-UiLog $dashLog "Uygulama hazır. API: $ApiBase"
    } else {
        Refresh-ApiState | Out-Null
        Write-UiLog $dashLog "API servisi başlatılamadı. Yönetici yetkisi veya port kontrolü gerekebilir."
        Show-Message "Yerel API servisi başlatılamadı.`r`nPort $ApiPort kullanımda olabilir." "API Hatası" "Warning"
    }

    if (Test-IsAdmin) {
        Write-UiLog $dashLog "Uygulama yönetici yetkisiyle çalışıyor."
    } else {
        Write-UiLog $dashLog "Uygulama normal kullanıcı yetkisiyle çalışıyor. Firewall için yönetici başlatıcıyı kullan."
    }
})

[System.Windows.Forms.Application]::Run($form)
