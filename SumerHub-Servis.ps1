
╔══════════════════════════════════════════════════════════════════════════════╗
║                        SUMER HUB - ARKA PLAN SERVİSİ                         ║
║                     Ingenico ECR LAN POS Entegrasyonu                        ║
╚══════════════════════════════════════════════════════════════════════════════╝

Açıklama:
    - localhost:8189 üzerinde HTTP API sunar
    - Ingenico POS terminali ile TCP/IP (ZVT/PT protokolü) üzerinden haberleşir
    - SumerHub-UI.ps1 tarafından kontrol edilir
    - Windows Service gibi çalışabilir (powershell -WindowStyle Hidden)

Ingenico ECR Protokolü:
    - TCP/IP port: 7777 (stand-alone) veya 7778 (ECR-only mod)
    - Binary APDU mesaj yapısı: [CCRC][APRC][LEN_HI][LEN_LO][DATA...]
    - CRC ve DLE/STX framing yok - TCP/IP kendisi garanti eder
    - Kaynak: https://developer.ingenico.com/files/javadoc/

Çalıştırma:
    powershell -ExecutionPolicy Bypass -WindowStyle Hidden -File .\SumerHub-Servis.ps1

Yazar: SAMER Teknoloji
Sürüm: 2.0
#>

# ═══════════════════════════════════════════════════════════════════════════════
# KÜRESEL AYARLAR
# ═══════════════════════════════════════════════════════════════════════════════
Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

# Logo yolu (UI ile paylaşılan)
$script:LogoPath = Join-Path $PSScriptRoot "logo.png"

# Konfigürasyon
$script:ConfigDir = Join-Path $env:ProgramData "SumerHub"
$script:ConfigPath = Join-Path $ConfigDir "sumerhub-service-config.json"
$script:LogPath = Join-Path $ConfigDir "sumerhub-service.log"

if (-not (Test-Path $ConfigDir)) {
    New-Item -Path $ConfigDir -ItemType Directory -Force | Out-Null
}

# ═══════════════════════════════════════════════════════════════════════════════
# YARDIMCI FONKSİYONLAR
# ═══════════════════════════════════════════════════════════════════════════════

function Yaz-Log {
    param([string]$Mesaj, [string]$Seviye = "INFO")
    $zaman = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $satir = "[$zaman] [$Seviye] $Mesaj"
    Add-Content -Path $script:LogPath -Value $satir -Encoding UTF8 -ErrorAction SilentlyContinue
    Write-Host $satir
}

function Varsayilan-Ayarlari-Al {
    return [pscustomobject]@{
        HttpPort = 8189
        PosIp = "192.168.1.39"
        PosPort = 7778
        PosTimeoutMs = 30000
        PosBeklemeMs = 5000
        AutoReconnect = $true
        EcrMod = "ECR"           # "ECR" veya "STANDALONE"
        MerchantId = 1
        TerminalId = ""
        ZvtSifre = ""
        DebugMod = $false
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
    } catch {
        Yaz-Log "Ayar yükleme hatası: $($_.Exception.Message)" "ERROR"
    }
    return Varsayilan-Ayarlari-Al
}

function Ayarlari-Kaydet {
    param($Config)
    try {
        $Config | ConvertTo-Json -Depth 10 | Set-Content -Path $script:ConfigPath -Encoding UTF8
        return $true
    } catch {
        Yaz-Log "Ayar kaydetme hatası: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

$script:Ayarlar = Ayarlari-Yukle

# ═══════════════════════════════════════════════════════════════════════════════
# INGENICO ZVT/PT PROTOKOLÜ - DÜŞÜK SEVİYE TCP İLETİŞİMİ
# ═══════════════════════════════════════════════════════════════════════════════
# Ingenico ECR Interface v1.3.41 - TCP/IP transport
# APDU yapısı: [CCRC][APRC][Uzunluk_Hi][Uzunluk_Lo][Veri...]
# ═══════════════════════════════════════════════════════════════════════════════

# ZVT Komut Kodları (CCRC + APRC)
$script:ZVT = @{
    # ECR -> Terminal
    Kayit = @(0x06, 0x00)           # Registration
    Yetkilendirme = @(0x06, 0x01)   # Authorisation (Satış)
    Iptal = @(0x06, 0x30)           # Reversal
    Iade = @(0x06, 0x31)            # Refund
    GunSonu = @(0x06, 0x50)         # End-of-day
    Tanilama = @(0x06, 0x70)        # Diagnosis
    DurumSorgu = @(0x05, 0x01)      # Status enquiry
    Cikis = @(0x06, 0x02)           # Logoff
    TekrarFis = @(0x06, 0x20)       # Repeat receipt
    # Terminal -> ECR
    Basarili = @(0x80, 0x00)        # Başarılı yanıt
    Hata = @(0x84, 0x00)            # Genel hata
}

# TLV (Tag-Length-Value) BMP kodları
$script:TLV = @{
    Miktar = 0x04
    Taksit = 0x46
    Sifre = 0x06
    IslemNo = 0x0E
    Tarih = 0x0D
    Saat = 0x0C
    KartNo = 0x0F
    Tutar = 0x50
    KDV = 0x52
    UrunAdi = 0x54
    FisMetni = 0x3A
    Durum = 0x0A
}

$script:PosTcpClient = $null
$script:PosStream = $null
$script:PosBagli = $false
$script:PosKilit = New-Object System.Object
$script:SonCevap = $null
$script:SonHata = ""

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

function Pos-Baglan {
    param(
        [string]$Ip = $script:Ayarlar.PosIp,
        [int]$Port = $script:Ayarlar.PosPort,
        [int]$TimeoutMs = $script:Ayarlar.PosTimeoutMs
    )
    
    [System.Threading.Monitor]::Enter($script:PosKilit)
    try {
        if (-not (Ozel-Ip-Kontrol $Ip)) {
            throw "POS IP adresi private/local ağda olmalı: $Ip"
        }
        
        if ($script:PosBagli -and $script:PosTcpClient -and $script:PosTcpClient.Connected) {
            Yaz-Log "POS zaten bağlı: $Ip`:$Port" "WARN"
            return $true
        }
        
        # Mevcut bağlantıyı temizle
        Pos-Baglanti-Kapat
        
        Yaz-Log "POS bağlantısı kuruluyor: $Ip`:$Port (Timeout: ${TimeoutMs}ms)" "INFO"
        
        $script:PosTcpClient = New-Object System.Net.Sockets.TcpClient
        $async = $script:PosTcpClient.BeginConnect($Ip, $Port, $null, $null)
        $bekle = $async.AsyncWaitHandle.WaitOne($TimeoutMs, $false)
        
        if (-not $bekle) {
            throw "POS bağlantı zaman aşımı: $Ip`:$Port"
        }
        
        $script:PosTcpClient.EndConnect($async)
        $script:PosTcpClient.ReceiveTimeout = $TimeoutMs
        $script:PosTcpClient.SendTimeout = $TimeoutMs
        $script:PosTcpClient.NoDelay = $true
        $script:PosStream = $script:PosTcpClient.GetStream()
        $script:PosBagli = $true
        
        Yaz-Log "POS bağlantısı başarılı: $Ip`:$Port" "SUCCESS"
        return $true
    } catch {
        $script:PosBagli = $false
        $script:SonHata = $_.Exception.Message
        Yaz-Log "POS bağlantı hatası: $($_.Exception.Message)" "ERROR"
        Pos-Baglanti-Kapat
        return $false
    } finally {
        [System.Threading.Monitor]::Exit($script:PosKilit)
    }
}

function Pos-Baglanti-Kapat {
    [System.Threading.Monitor]::Enter($script:PosKilit)
    try {
        if ($script:PosStream) {
            try { $script:PosStream.Close() } catch {}
            $script:PosStream = $null
        }
        if ($script:PosTcpClient) {
            try { $script:PosTcpClient.Close() } catch {}
            $script:PosTcpClient = $null
        }
        $script:PosBagli = $false
        Yaz-Log "POS bağlantısı kapatıldı" "INFO"
    } finally {
        [System.Threading.Monitor]::Exit($script:PosKilit)
    }
}

function Pos-Gonder {
    param([byte[]]$Veri)
    
    [System.Threading.Monitor]::Enter($script:PosKilit)
    try {
        if (-not $script:PosBagli -or -not $script:PosStream) {
            throw "POS bağlı değil! Önce Pos-Baglan() çağrılmalı."
        }
        
        $script:PosStream.Write($Veri, 0, $Veri.Length)
        $script:PosStream.Flush()
        
        if ($script:Ayarlar.DebugMod) {
            $hex = ($Veri | ForEach-Object { "0x{0:X2}" -f $_ }) -join " "
            Yaz-Log "POS -> GÖNDER: $hex" "DEBUG"
        }
    } finally {
        [System.Threading.Monitor]::Exit($script:PosKilit)
    }
}

function Pos-Al {
    param([int]$TimeoutMs = 30000)
    
    [System.Threading.Monitor]::Enter($script:PosKilit)
    try {
        if (-not $script:PosBagli -or -not $script:PosStream) {
            throw "POS bağlı değil!"
        }
        
        $buffer = New-Object byte[] 4096
        $okumaBaslama = Get-Date
        $toplamVeri = New-Object System.Collections.Generic.List[byte]
        
        while ($true) {
            $gecenMs = ([DateTime]::Now - $okumaBaslama).TotalMilliseconds
            $kalanMs = [math]::Max(100, $TimeoutMs - $gecenMs)
            
            if ($kalanMs -le 0) {
                throw "POS yanıt zaman aşımı (${TimeoutMs}ms)"
            }
            
            if (-not $script:PosStream.DataAvailable) {
                Start-Sleep -Milliseconds 50
                continue
            }
            
            $okunan = $script:PosStream.Read($buffer, 0, $buffer.Length)
            if ($okunan -eq 0) {
                throw "POS bağlantısı kapatıldı (EOF)"
            }
            
            for ($i = 0; $i -lt $okunan; $i++) {
                $toplamVeri.Add($buffer[$i])
            }
            
            # Minimum APDU: 4 byte (CCRC + APRC + LEN_HI + LEN_LO)
            if ($toplamVeri.Count -ge 4) {
                $uzunluk = ($toplamVeri[2] -shl 8) -bor $toplamVeri[3]
                if ($toplamVeri.Count -ge (4 + $uzunluk)) {
                    break  # Tam mesaj alındı
                }
            }
            
            if ($gecenMs -gt $TimeoutMs) {
                throw "POS tam yanıt zaman aşımı"
            }
        }
        
        $sonuc = $toplamVeri.ToArray()
        
        if ($script:Ayarlar.DebugMod) {
            $hex = ($sonuc | ForEach-Object { "0x{0:X2}" -f $_ }) -join " "
            Yaz-Log "POS <- ALINDI: $hex" "DEBUG"
        }
        
        return $sonuc
    } finally {
        [System.Threading.Monitor]::Exit($script:PosKilit)
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# ZVT APDU OLUŞTURMA / PARSE ETME
# ═══════════════════════════════════════════════════════════════════════════════

function Zvt-Apdu-Olustur {
    param(
        [byte[]]$Komut,      # [CCRC, APRC]
        [byte[]]$Veri = @()   # TLV veya raw data
    )
    
    $uzunluk = $Veri.Length
    $apdu = New-Object byte[] (4 + $uzunluk)
    $apdu[0] = $Komut[0]    # CCRC
    $apdu[1] = $Komut[1]    # APRC
    $apdu[2] = [byte](($uzunluk -shr 8) -band 0xFF)  # LEN_HI
    $apdu[3] = [byte]($uzunluk -band 0xFF)            # LEN_LO
    
    if ($uzunluk -gt 0) {
        [Array]::Copy($Veri, 0, $apdu, 4, $uzunluk)
    }
    
    return $apdu
}

function Zvt-Apdu-Parcala {
    param([byte[]]$Apdu)
    
    if ($Apdu.Length -lt 4) {
        throw "Geçersiz APDU (minimum 4 byte gerekli)"
    }
    
    $ccrc = $Apdu[0]
    $aprc = $Apdu[1]
    $uzunluk = ($Apdu[2] -shl 8) -bor $Apdu[3]
    $veri = @()
    
    if ($uzunluk -gt 0 -and $Apdu.Length -ge (4 + $uzunluk)) {
        $veri = New-Object byte[] $uzunluk
        [Array]::Copy($Apdu, 4, $veri, 0, $uzunluk)
    }
    
    return [pscustomobject]@{
        CCRC = $ccrc
        APRC = $aprc
        Uzunluk = $uzunluk
        Veri = $veri
        Basarili = ($ccrc -eq 0x80 -and $aprc -eq 0x00)
        HataKodu = if ($ccrc -eq 0x84) { $aprc } else { 0 }
    }
}

function Tlv-Olustur {
    param([byte]$Tag, [byte[]]$Deger)
    
    $uzunluk = $Deger.Length
    if ($uzunluk -gt 255) {
        throw "TLV değer 255 byte'dan uzun olamaz"
    }
    
    $tlv = New-Object byte[] (2 + $uzunluk)
    $tlv[0] = $Tag
    $tlv[1] = [byte]$uzunluk
    if ($uzunluk -gt 0) {
        [Array]::Copy($Deger, 0, $tlv, 2, $uzunluk)
    }
    return $tlv
}

function Tlv-Parcala {
    param([byte[]]$Veri, [int]$Baslangic = 0)
    
    if ($Baslangic + 2 -gt $Veri.Length) { return $null }
    
    $tag = $Veri[$Baslangic]
    $uzunluk = $Veri[$Baslangic + 1]
    
    if ($Baslangic + 2 + $uzunluk -gt $Veri.Length) {
        throw "TLV parse hatası: yetersiz veri"
    }
    
    $deger = New-Object byte[] $uzunluk
    if ($uzunluk -gt 0) {
        [Array]::Copy($Veri, $Baslangic + 2, $deger, 0, $uzunluk)
    }
    
    return [pscustomobject]@{
        Tag = $tag
        Uzunluk = $uzunluk
        Deger = $deger
        SonrakiIndeks = $Baslangic + 2 + $uzunluk
    }
}

# BCD (Binary Coded Decimal) dönüşümleri
function Bcd-Yap {
    param([string]$Metin, [int]$Basamak)
    $metin = $Metin.PadLeft($Basamak, '0')
    $byteSayisi = [math]::Ceiling($Basamak / 2)
    $sonuc = New-Object byte[] $byteSayisi
    
    for ($i = 0; $i -lt $byteSayisi; $i++) {
        $sol = [int]::Parse($metin[$i * 2].ToString())
        $sag = if (($i * 2 + 1) -lt $metin.Length) { 
            [int]::Parse($metin[$i * 2 + 1].ToString()) 
        } else { 0 }
        $sonuc[$i] = [byte](($sol -shl 4) -bor $sag)
    }
    return $sonuc
}

function Bcd-Coz {
    param([byte[]]$Bcd)
    $sonuc = ""
    foreach ($b in $Bcd) {
        $sonuc += "{0:X1}{1:X1}" -f (($b -shr 4) -band 0x0F), ($b -band 0x0F)
    }
    return $sonuc
}

# ═══════════════════════════════════════════════════════════════════════════════
# YÜKSEK SEVİYE INGENICO İŞLEMLERİ
# ═══════════════════════════════════════════════════════════════════════════════

function Ingenico-Kayit {
    # Terminal kayıt (Registration) - 06 00
    param(
        [int]$MagazaNo = $script:Ayarlar.MerchantId,
        [string]$Sifre = $script:Ayarlar.ZvtSifre
    )
    
    try {
        Yaz-Log "Ingenico kayıt başlatılıyor..." "INFO"
        
        $veri = New-Object System.Collections.Generic.List[byte]
        
        # TLV: Şifre (opsiyonel)
        if (-not [string]::IsNullOrWhiteSpace($Sifre)) {
            $sifreBytes = [System.Text.Encoding]::ASCII.GetBytes($Sifre)
            $sifreTlv = Tlv-Olustur -Tag $script:TLV.Sifre -Deger $sifreBytes
            $veri.AddRange($sifreTlv)
        }
        
        # TLV: Tarih/Saat (opsiyonel)
        $tarih = Bcd-Yap (Get-Date -Format "yyMMdd") 6
        $saat = Bcd-Yap (Get-Date -Format "HHmmss") 6
        $veri.AddRange((Tlv-Olustur -Tag $script:TLV.Tarih -Deger $tarih))
        $veri.AddRange((Tlv-Olustur -Tag $script:TLV.Saat -Deger $saat))
        
        $apdu = Zvt-Apdu-Olustur -Komut $script:ZVT.Kayit -Veri $veri.ToArray()
        Pos-Gonder -Veri $apdu
        
        $yanit = Pos-Al -TimeoutMs 10000
        $parse = Zvt-Apdu-Parcala -Apdu $yanit
        
        if ($parse.Basarili) {
            Yaz-Log "Ingenico kayıt başarılı" "SUCCESS"
            return @{ Basarili = $true; Mesaj = "Kayıt başarılı" }
        } else {
            $hataMesaji = Zvt-Hata-Mesaji -Kod $parse.HataKodu
            Yaz-Log "Ingenico kayıt hatası: $hataMesaji (0x{0:X2})" -f $parse.HataKodu "ERROR"
            return @{ Basarili = $false; Hata = $hataMesaji; HataKodu = $parse.HataKodu }
        }
    } catch {
        Yaz-Log "Kayıt hatası: $($_.Exception.Message)" "ERROR"
        return @{ Basarili = $false; Hata = $_.Exception.Message }
    }
}

function Ingenico-Satis {
    # Satış işlemi (Authorisation) - 06 01
    param(
        [decimal]$Tutar,
        [string]$IslemNo = "",
        [int]$Taksit = 0
    )
    
    try {
        $tutarKurus = [int]($Tutar * 100)
        Yaz-Log "Satış işlemi: $tutarKurus kuruş (İşlem: $IslemNo)" "INFO"
        
        $veri = New-Object System.Collections.Generic.List[byte]
        
        # TLV: Miktar (BCD, 6 basamak)
        $miktarBcd = Bcd-Yap $tutarKurus.ToString() 6
        $veri.AddRange((Tlv-Olustur -Tag $script:TLV.Miktar -Deger $miktarBcd))
        
        # TLV: İşlem numarası
        if (-not [string]::IsNullOrWhiteSpace($IslemNo)) {
            $islemBytes = [System.Text.Encoding]::ASCII.GetBytes($IslemNo.PadRight(6))
            $veri.AddRange((Tlv-Olustur -Tag $script:TLV.IslemNo -Deger $islemBytes))
        }
        
        # TLV: Taksit
        if ($Taksit -gt 1) {
            $taksitBcd = Bcd-Yap $Taksit.ToString() 2
            $veri.AddRange((Tlv-Olustur -Tag $script:TLV.Taksit -Deger $taksitBcd))
        }
        
        $apdu = Zvt-Apdu-Olustur -Komut $script:ZVT.Yetkilendirme -Veri $veri.ToArray()
        Pos-Gonder -Veri $apdu
        
        # Satış uzun sürer (müşteri kartı okutur, PIN girer)
        $yanit = Pos-Al -TimeoutMs $script:Ayarlar.PosTimeoutMs
        $parse = Zvt-Apdu-Parcala -Apdu $yanit
        
        if ($parse.Basarili) {
            $sonuc = Zvt-Satis-Cozumle -Veri $parse.Veri
            Yaz-Log "Satış başarılı: $($sonuc.OnayKodu)" "SUCCESS"
            return @{ Basarili = $true; Detay = $sonuc }
        } else {
            $hataMesaji = Zvt-Hata-Mesaji -Kod $parse.HataKodu
            Yaz-Log "Satış hatası: $hataMesaji" "ERROR"
            return @{ Basarili = $false; Hata = $hataMesaji; HataKodu = $parse.HataKodu }
        }
    } catch {
        Yaz-Log "Satış hatası: $($_.Exception.Message)" "ERROR"
        return @{ Basarili = $false; Hata = $_.Exception.Message }
    }
}

function Ingenico-Iptal {
    # İptal (Reversal) - 06 30
    param([string]$IslemNo)
    
    try {
        Yaz-Log "İptal işlemi: $IslemNo" "INFO"
        
        $veri = New-Object System.Collections.Generic.List[byte]
        $islemBytes = [System.Text.Encoding]::ASCII.GetBytes($IslemNo.PadRight(6))
        $veri.AddRange((Tlv-Olustur -Tag $script:TLV.IslemNo -Deger $islemBytes))
        
        $apdu = Zvt-Apdu-Olustur -Komut $script:ZVT.Iptal -Veri $veri.ToArray()
        Pos-Gonder -Veri $apdu
        
        $yanit = Pos-Al -TimeoutMs 30000
        $parse = Zvt-Apdu-Parcala -Apdu $yanit
        
        if ($parse.Basarili) {
            Yaz-Log "İptal başarılı" "SUCCESS"
            return @{ Basarili = $true }
        } else {
            return @{ Basarili = $false; Hata = (Zvt-Hata-Mesaji -Kod $parse.HataKodu) }
        }
    } catch {
        return @{ Basarili = $false; Hata = $_.Exception.Message }
    }
}

function Ingenico-Iade {
    # İade (Refund) - 06 31
    param([decimal]$Tutar, [string]$IslemNo = "")
    
    try {
        $tutarKurus = [int]($Tutar * 100)
        Yaz-Log "İade işlemi: $tutarKurus kuruş" "INFO"
        
        $veri = New-Object System.Collections.Generic.List[byte]
        $miktarBcd = Bcd-Yap $tutarKurus.ToString() 6
        $veri.AddRange((Tlv-Olustur -Tag $script:TLV.Miktar -Deger $miktarBcd))
        
        if ($IslemNo) {
            $islemBytes = [System.Text.Encoding]::ASCII.GetBytes($IslemNo.PadRight(6))
            $veri.AddRange((Tlv-Olustur -Tag $script:TLV.IslemNo -Deger $islemBytes))
        }
        
        $apdu = Zvt-Apdu-Olustur -Komut $script:ZVT.Iade -Veri $veri.ToArray()
        Pos-Gonder -Veri $apdu
        
        $yanit = Pos-Al -TimeoutMs 30000
        $parse = Zvt-Apdu-Parcala -Apdu $yanit
        
        if ($parse.Basarili) {
            return @{ Basarili = $true }
        } else {
            return @{ Basarili = $false; Hata = (Zvt-Hata-Mesaji -Kod $parse.HataKodu) }
        }
    } catch {
        return @{ Basarili = $false; Hata = $_.Exception.Message }
    }
}

function Ingenico-DurumSorgula {
    # Durum sorgu - 05 01
    try {
        $apdu = Zvt-Apdu-Olustur -Komut $script:ZVT.DurumSorgu -Veri @()
        Pos-Gonder -Veri $apdu
        
        $yanit = Pos-Al -TimeoutMs 5000
        $parse = Zvt-Apdu-Parcala -Apdu $yanit
        
        return @{
            Basarili = $parse.Basarili
            TerminalHazir = $parse.Basarili
            Mesaj = if ($parse.Basarili) { "Terminal hazır" } else { "Terminal meşgul/hata" }
        }
    } catch {
        return @{ Basarili = $false; TerminalHazir = $false; Mesaj = $_.Exception.Message }
    }
}

function Ingenico-GunSonu {
    # Gün sonu - 06 50
    try {
        Yaz-Log "Gün sonu işlemi başlatılıyor..." "INFO"
        
        $apdu = Zvt-Apdu-Olustur -Komut $script:ZVT.GunSonu -Veri @()
        Pos-Gonder -Veri $apdu
        
        $yanit = Pos-Al -TimeoutMs 60000
        $parse = Zvt-Apdu-Parcala -Apdu $yanit
        
        if ($parse.Basarili) {
            Yaz-Log "Gün sonu başarılı" "SUCCESS"
            return @{ Basarili = $true }
        } else {
            return @{ Basarili = $false; Hata = (Zvt-Hata-Mesaji -Kod $parse.HataKodu) }
        }
    } catch {
        return @{ Basarili = $false; Hata = $_.Exception.Message }
    }
}

function Ingenico-FisTekrar {
    # Son fişi tekrar yazdır - 06 20
    try {
        $apdu = Zvt-Apdu-Olustur -Komut $script:ZVT.TekrarFis -Veri @()
        Pos-Gonder -Veri $apdu
        
        $yanit = Pos-Al -TimeoutMs 10000
        $parse = Zvt-Apdu-Parcala -Apdu $yanit
        
        return @{ Basarili = $parse.Basarili; Hata = if (-not $parse.Basarili) { (Zvt-Hata-Mesaji -Kod $parse.HataKodu) } else { "" } }
    } catch {
        return @{ Basarili = $false; Hata = $_.Exception.Message }
    }
}

function Ingenico-Cikis {
    # Logoff - 06 02
    try {
        $apdu = Zvt-Apdu-Olustur -Komut $script:ZVT.Cikis -Veri @()
        Pos-Gonder -Veri $apdu
        
        $yanit = Pos-Al -TimeoutMs 5000
        $parse = Zvt-Apdu-Parcala -Apdu $yanit
        
        return @{ Basarili = $parse.Basarili }
    } catch {
        return @{ Basarili = $false }
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# ZVT YARDIMCI FONKSİYONLAR
# ═══════════════════════════════════════════════════════════════════════════════

function Zvt-Satis-Cozumle {
    param([byte[]]$Veri)
    
    $sonuc = @{
        OnayKodu = ""
        KartNo = ""
        Tutar = 0
        Tarih = ""
        Saat = ""
        IslemNo = ""
        KalanBakiye = 0
        FisMetni = ""
    }
    
    $i = 0
    while ($i -lt $Veri.Length) {
        $tlv = Tlv-Parcala -Veri $Veri -Baslangic $i
        if ($null -eq $tlv) { break }
        
        switch ($tlv.Tag) {
            $script:TLV.IslemNo { $sonuc.IslemNo = [System.Text.Encoding]::ASCII.GetString($tlv.Deger).Trim() }
            $script:TLV.Tarih { $sonuc.Tarih = Bcd-Coz $tlv.Deger }
            $script:TLV.Saat { $sonuc.Saat = Bcd-Coz $tlv.Deger }
            $script:TLV.KartNo { $sonuc.KartNo = Bcd-Coz $tlv.Deger }
            $script:TLV.FisMetni { $sonuc.FisMetni = [System.Text.Encoding]::ASCII.GetString($tlv.Deger) }
        }
        $i = $tlv.SonrakiIndeks
    }
    
    return $sonuc
}

function Zvt-Hata-Mesaji {
    param([int]$Kod)
    $mesajlar = @{
        0x00 = "Başarılı"
        0x01 = "İptal edildi"
        0x02 = "Zaman aşımı"
        0x03 = "Geçersiz kart"
        0x04 = "PIN hatası"
        0x05 = "Bağlantı hatası"
        0x06 = "Banka reddetti"
        0x07 = "Yetersiz bakiye"
        0x08 = "Kart süresi dolmuş"
        0x09 = "Kart bloke"
        0x0A = "Geçersiz işlem"
        0x0B = "Tekrar deneyin"
        0x0C = "İşlem numarası hatası"
        0x0D = "Şifre hatası"
        0x0E = "Format hatası"
        0x0F = "Bilinmeyen hata"
        0x6F = "Genel hata"
        0x7A = "Tekrar denenmeli"
    }
    return $mesajlar[$Kod] "Bilinmeyen hata kodu: 0x{0:X2}" -f $Kod
}

# ═══════════════════════════════════════════════════════════════════════════════
# HTTP API - localhost:8189
# ═══════════════════════════════════════════════════════════════════════════════
# Endpoints:
#   GET  /durum          -> Servis durumu
#   GET  /pos/durum      -> POS bağlantı durumu
#   POST /pos/baglan     -> POS'a bağlan
#   POST /pos/baglanti-kes -> POS bağlantısını kes
#   POST /pos/satis      -> Satış işlemi
#   POST /pos/iptal      -> İptal işlemi
#   POST /pos/iade       -> İade işlemi
#   POST /pos/gunsonu    -> Gün sonu
#   GET  /pos/fis        -> Son fişi tekrar yazdır
#   POST /pos/ayarlar    -> Ayarları güncelle
#   GET  /pos/ayarlar    -> Mevcut ayarları al
#   GET  /log            -> Son log kayıtları
# ═══════════════════════════════════════════════════════════════════════════════

$script:HttpListener = $null
$script:Calisiyor = $false
$script:HttpKilit = New-Object System.Object

function Http-Yanit-Ver {
    param(
        [System.Net.HttpListenerResponse]$Response,
        [object]$Veri,
        [int]$StatusCode = 200
    )
    
    $json = $Veri | ConvertTo-Json -Depth 10 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    
    $Response.StatusCode = $StatusCode
    $Response.ContentType = "application/json; charset=utf-8"
    $Response.ContentLength64 = $bytes.Length
    $Response.OutputStream.Write($bytes, 0, $bytes.Length)
    $Response.OutputStream.Close()
}

function Http-Yanit-Hata {
    param(
        [System.Net.HttpListenerResponse]$Response,
        [string]$Mesaj,
        [int]$StatusCode = 400
    )
    
    $hata = @{ Hata = $true; Mesaj = $Mesaj; Zaman = (Get-Date -Format "yyyy-MM-dd HH:mm:ss") }
    Http-Yanit-Ver -Response $Response -Veri $hata -StatusCode $StatusCode
}

function Http-Istek-Isle {
    param([System.Net.HttpListenerContext]$Context)
    
    $istek = $Context.Request
    $yanit = $Context.Response
    $yol = $istek.Url.LocalPath.ToLower()
    $metod = $istek.HttpMethod
    
    Yaz-Log "HTTP $metod $yol from $($istek.RemoteEndPoint)" "DEBUG"
    
    # CORS başlıkları
    $yanit.Headers.Add("Access-Control-Allow-Origin", "*")
    $yanit.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
    $yanit.Headers.Add("Access-Control-Allow-Headers", "Content-Type")
    
    if ($metod -eq "OPTIONS") {
        $yanit.StatusCode = 204
        $yanit.OutputStream.Close()
        return
    }
    
    try {
        switch -Regex ($yol) {
            "^/$" {
                Http-Yanit-Ver -Response $yanit -Veri @{
                    Servis = "SumerHub POS Servisi"
                    Surum = "2.0"
                    Durum = "Aktif"
                    Zaman = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
                    PosBagli = $script:PosBagli
                    HttpPort = $script:Ayarlar.HttpPort
                }
            }
            
            "^/durum$" {
                Http-Yanit-Ver -Response $yanit -Veri @{
                    Servis = "Aktif"
                    PosBagli = $script:PosBagli
                    PosIp = $script:Ayarlar.PosIp
                    PosPort = $script:Ayarlar.PosPort
                    SonHata = $script:SonHata
                    Zaman = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
                }
            }
            
            "^/pos/durum$" {
                if (-not $script:PosBagli) {
                    Http-Yanit-Ver -Response $yanit -Veri @{
                        Bagli = $false
                        Mesaj = "POS bağlı değil"
                    }
                    return
                }
                
                $durum = Ingenico-DurumSorgula
                Http-Yanit-Ver -Response $yanit -Veri $durum
            }
            
            "^/pos/baglan$" {
                if ($metod -ne "POST") {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POST gerekli" -StatusCode 405
                    return
                }
                
                $body = ""
                $reader = New-Object System.IO.StreamReader($istek.InputStream, $istek.ContentEncoding)
                $body = $reader.ReadToEnd()
                $reader.Close()
                
                $param = @{} 
                if (-not [string]::IsNullOrWhiteSpace($body)) {
                    $param = $body | ConvertFrom-Json
                }
                
                $ip = if ($param.Ip) { $param.Ip } else { $script:Ayarlar.PosIp }
                $port = if ($param.Port) { $param.Port } else { $script:Ayarlar.PosPort }
                
                $sonuc = Pos-Baglan -Ip $ip -Port $port
                
                if ($sonuc) {
                    # Kayıt yap
                    $kayit = Ingenico-Kayit
                    Http-Yanit-Ver -Response $yanit -Veri @{
                        Basarili = $true
                        Bagli = $true
                        Kayit = $kayit
                        Ip = $ip
                        Port = $port
                    }
                } else {
                    Http-Yanit-Ver -Response $yanit -Veri @{
                        Basarili = $false
                        Bagli = $false
                        Hata = $script:SonHata
                    }
                }
            }
            
            "^/pos/baglanti-kes$" {
                Pos-Baglanti-Kapat
                Http-Yanit-Ver -Response $yanit -Veri @{ Basarili = $true; Bagli = $false }
            }
            
            "^/pos/satis$" {
                if ($metod -ne "POST") {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POST gerekli" -StatusCode 405
                    return
                }
                
                $body = ""
                $reader = New-Object System.IO.StreamReader($istek.InputStream, $istek.ContentEncoding)
                $body = $reader.ReadToEnd()
                $reader.Close()
                
                $param = $body | ConvertFrom-Json
                
                if (-not $param.Tutar) {
                    Http-Yanit-Hata -Response $yanit -Mesaj "Tutar gerekli"
                    return
                }
                
                if (-not $script:PosBagli) {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POS bağlı değil. Önce /pos/baglan çağırın."
                    return
                }
                
                $sonuc = Ingenico-Satis -Tutar $param.Tutar -IslemNo $param.IslemNo -Taksit $param.Taksit
                Http-Yanit-Ver -Response $yanit -Veri $sonuc
            }
            
            "^/pos/iptal$" {
                if ($metod -ne "POST") {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POST gerekli" -StatusCode 405
                    return
                }
                
                $body = ""
                $reader = New-Object System.IO.StreamReader($istek.InputStream, $istek.ContentEncoding)
                $body = $reader.ReadToEnd()
                $reader.Close()
                
                $param = $body | ConvertFrom-Json
                
                if (-not $param.IslemNo) {
                    Http-Yanit-Hata -Response $yanit -Mesaj "IslemNo gerekli"
                    return
                }
                
                if (-not $script:PosBagli) {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POS bağlı değil"
                    return
                }
                
                $sonuc = Ingenico-Iptal -IslemNo $param.IslemNo
                Http-Yanit-Ver -Response $yanit -Veri $sonuc
            }
            
            "^/pos/iade$" {
                if ($metod -ne "POST") {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POST gerekli" -StatusCode 405
                    return
                }
                
                $body = ""
                $reader = New-Object System.IO.StreamReader($istek.InputStream, $istek.ContentEncoding)
                $body = $reader.ReadToEnd()
                $reader.Close()
                
                $param = $body | ConvertFrom-Json
                
                if (-not $param.Tutar) {
                    Http-Yanit-Hata -Response $yanit -Mesaj "Tutar gerekli"
                    return
                }
                
                if (-not $script:PosBagli) {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POS bağlı değil"
                    return
                }
                
                $sonuc = Ingenico-Iade -Tutar $param.Tutar -IslemNo $param.IslemNo
                Http-Yanit-Ver -Response $yanit -Veri $sonuc
            }
            
            "^/pos/gunsonu$" {
                if (-not $script:PosBagli) {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POS bağlı değil"
                    return
                }
                
                $sonuc = Ingenico-GunSonu
                Http-Yanit-Ver -Response $yanit -Veri $sonuc
            }
            
            "^/pos/fis$" {
                if (-not $script:PosBagli) {
                    Http-Yanit-Hata -Response $yanit -Mesaj "POS bağlı değil"
                    return
                }
                
                $sonuc = Ingenico-FisTekrar
                Http-Yanit-Ver -Response $yanit -Veri $sonuc
            }
            
            "^/pos/ayarlar$" {
                if ($metod -eq "GET") {
                    Http-Yanit-Ver -Response $yanit -Veri $script:Ayarlar
                } elseif ($metod -eq "POST") {
                    $body = ""
                    $reader = New-Object System.IO.StreamReader($istek.InputStream, $istek.ContentEncoding)
                    $body = $reader.ReadToEnd()
                    $reader.Close()
                    
                    $param = $body | ConvertFrom-Json
                    
                    foreach ($p in $param.PSObject.Properties.Name) {
                        if ($script:Ayarlar.PSObject.Properties.Name -contains $p) {
                            $script:Ayarlar.$p = $param.$p
                        }
                    }
                    
                    Ayarlari-Kaydet -Config $script:Ayarlar
                    Http-Yanit-Ver -Response $yanit -Veri @{ Basarili = $true; Ayarlar = $script:Ayarlar }
                } else {
                    Http-Yanit-Hata -Response $yanit -Mesaj "GET veya POST gerekli" -StatusCode 405
                }
            }
            
            "^/log$" {
                $satirlar = @()
                if (Test-Path $script:LogPath) {
                    $satirlar = Get-Content $script:LogPath -Tail 100 -Encoding UTF8
                }
                Http-Yanit-Ver -Response $yanit -Veri @{
                    Kayitlar = $satirlar
                    ToplamSatir = $satirlar.Count
                }
            }
            
            default {
                Http-Yanit-Hata -Response $yanit -Mesaj "Bilinmeyen endpoint: $yol" -StatusCode 404
            }
        }
    } catch {
        Yaz-Log "HTTP hatası ($yol): $($_.Exception.Message)" "ERROR"
        try {
            Http-Yanit-Hata -Response $yanit -Mesaj "Sunucu hatası: $($_.Exception.Message)" -StatusCode 500
        } catch {
            # Yanıt zaten kapatılmış olabilir
        }
    }
}

function Servis-Baslat {
    param([int]$Port = $script:Ayarlar.HttpPort)
    
    Yaz-Log "═══════════════════════════════════════════════════════════════" "INFO"
    Yaz-Log "  SUMER HUB POS SERVİSİ BAŞLATILIYOR" "INFO"
    Yaz-Log "  Port: $Port" "INFO"
    Yaz-Log "  Ingenico ECR TCP/IP Entegrasyonu" "INFO"
    Yaz-Log "═══════════════════════════════════════════════════════════════" "INFO"
    
    $script:HttpListener = New-Object System.Net.HttpListener
    $script:HttpListener.Prefixes.Add("http://+:$Port/")
    
    try {
        $script:HttpListener.Start()
        $script:Calisiyor = $true
        Yaz-Log "HTTP listener başlatıldı: http://localhost:$Port/" "SUCCESS"
    } catch {
        Yaz-Log "HTTP listener hatası: $($_.Exception.Message)" "ERROR"
        Yaz-Log "Yönetici olarak çalıştırmanız gerekebilir." "WARN"
        throw
    }
    
    # Arka planda istek dinleme
    while ($script:Calisiyor) {
        try {
            $async = $script:HttpListener.BeginGetContext($null, $null)
            $tamam = $async.AsyncWaitHandle.WaitOne(500, $false)
            
            if (-not $tamam) { continue }
            
            $context = $script:HttpListener.EndGetContext($async)
            Http-Istek-Isle -Context $context
        } catch {
            if ($script:Calisiyor) {
                Yaz-Log "İstek işleme hatası: $($_.Exception.Message)" "ERROR"
            }
        }
    }
}

function Servis-Durdur {
    $script:Calisiyor = $false
    Pos-Baglanti-Kapat
    if ($script:HttpListener) {
        $script:HttpListener.Stop()
        $script:HttpListener.Close()
        $script:HttpListener = $null
    }
    Yaz-Log "Servis durduruldu" "INFO"
}

# ═══════════════════════════════════════════════════════════════════════════════
# SİNYAL YÖNETİMİ (Graceful Shutdown)
# ═══════════════════════════════════════════════════════════════════════════════

$script:KapatmaIstendi = $false

$kapatmaHandler = {
    param($sender, $e)
    $script:KapatmaIstendi = $true
    Yaz-Log "Kapatma sinyali alındı, servis durduruluyor..." "WARN"
    Servis-Durdur
}

[Console]::CancelKeyPress.Add_Event($kapatmaHandler)

# ═══════════════════════════════════════════════════════════════════════════════
# ANA PROGRAM
# ═══════════════════════════════════════════════════════════════════════════════

try {
    Servis-Baslat
} catch {
    Yaz-Log "KRİTİK HATA: $($_.Exception.Message)" "ERROR"
    Yaz-Log "Servis 5 saniye sonra yeniden başlatılacak..." "WARN"
    Start-Sleep -Seconds 5
    # Yeniden başlatma mantığı burada eklenebilir
} finally {
    Servis-Durdur
}
