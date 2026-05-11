# SAMER Hub

**SAMER Hub**, restoran, kafe, market ve paket servis işletmeleri için geliştirilen Windows tabanlı bir **kasa, masa, paket servis, POS, YNÖKC ve fatura entegrasyon platformudur**.

Sistem; masa içi sipariş yönetimi, paket servis operasyonu, Getir / Yemeksepeti / Trendyol Yemek entegrasyon altyapısı, Ingenico MOVE/5000F benzeri YNÖKC/POS cihazlarıyla LAN/ECR haberleşme omurgası, MySQL veritabanı, kurulum sihirbazı ve yerel servis mimarisi üzerine tasarlanmıştır.

> Durum: Production Candidate / Aktif geliştirme aşaması  
> Hedef platform: Windows 10 / Windows 11  
> Ana teknoloji: .NET 8, Avalonia UI, ASP.NET Core Local Service, MySQL, Native C ECR Engine

---

## Amaç

SAMER Hub’ın amacı işletmedeki tüm satış kanallarını tek merkezde toplamaktır:

- Dükkan içi masa siparişleri
- Paket servis siparişleri
- Getir, Yemeksepeti ve Trendyol Yemek sipariş altyapısı
- Nakit, kart, online ödeme ve yemek kartı ayrımı
- Ingenico MOVE/5000F POS / YNÖKC bağlantısı
- Mali fiş, bilgi fişi, e-Arşiv ve e-Fatura akışları için altyapı
- Yazıcı ve mutfak fişi yönetimi
- Gün sonu, ödeme, POS ve fatura raporları

---

![Ekran Resmi 1](/im1.png)
![Ekran Resmi 1](/imm.png)
![Ekran Resmi 1](/im.png)
![Ekran Resmi 1](/ınego.jpg)

## Kullanılan Resmi / Teknik Dokümanlar

Proje tasarımında aşağıdaki dokümanlardaki iş kuralları ve cihaz davranışları dikkate alınmıştır:

1. **GİB YNÖKC Bilgi Fişleri Teknik Kılavuzu**  
   Bilgi fişi düzenlenmesi gereken durumlar, faturalı satışlar, yemek kartı işlemleri, avans, cari hesap tahsilatı, bilgi fişi üzerinde bulunması gereken bilgiler, POS slip ile birleşik çıktı ve bilgi fişinin mali belgeye dönüşmesi kuralları için kullanılmıştır.

2. **Ingenico iWE280 / iDE280 Kullanım Kılavuzu**  
   Ethernet bağlantı ayarları, DHCP / Static IP, IP / MASK / Gateway, ödeme tipleri, KDV / kısım, PLU ve yazarkasa kullanım modeli için referans alınmıştır.

3. **Ingenico MOVE/5000F / iDE280 Kolay Kullanım Kılavuzu**  
   Faturalı işlem, vergi kimlik numarası / TCKN girişi, fatura / e-Fatura / e-Arşiv seçimi, nakit ve kartlı işlem akışlarının modellenmesi için kullanılmıştır.

4. **Ingenico Android SDK Javadoc**  
   `ApiManagement`, `Transaction`, `SALE`, `VOID`, `REFUND`, terminal bilgisi ve Android context tabanlı SDK yapısını anlamak için incelenmiştir. Ancak ana üretim mimarisi POS içine uygulama yüklemek değil, Windows PC üzerinden LAN/ECR protokolüyle çalışmaktır.

> Not: Bu repository içindeki ECR/TCP-IP motoru, resmi ECR protokol profil değerleri girildiğinde kullanılacak şekilde tasarlanmıştır. Canlı POS ödeme / YNÖKC mali fiş işlemleri için ilgili üretici / entegratör tarafından sağlanan gerçek komut, tag, response ve hata kodu tabloları gereklidir.

---

## Genel Mimari

```text
SAMER Hub Desktop
Avalonia UI / .NET 8
        ↓
SAMER Hub Local Service
ASP.NET Core / Windows Service
http://127.0.0.1:7070
        ↓
MySQL Database
        ↓
POS / YNÖKC / Fatura / Platform / Yazıcı Katmanları
```

### Temel Bileşenler

```text
src/
├── SamerHub.Desktop.Avalonia     # Modern Windows masaüstü arayüzü
├── SamerHub.Service              # Local backend / Windows Service
├── SamerHub.Core                 # Domain modelleri
├── SamerHub.Infrastructure       # MySQL, config, repository, storage
├── SamerHub.Pos                  # POS gateway katmanı
├── SamerHub.Pos.Protocol         # Managed ECR/TCP-IP protokol motoru
├── SamerHub.Pos.NativeBridge     # C# → Native C DLL köprüsü
├── SamerHub.Fiscal               # YNÖKC / mali fiş modelleri
├── SamerHub.Billing              # Fatura / e-Arşiv / e-Fatura altyapısı
├── SamerHub.Platforms            # Getir / Yemeksepeti / Trendyol altyapısı
└── SamerHub.Printing             # Yazıcı / fiş çıktısı altyapısı

native/
└── samer-ecr-native              # C ile yazılmış native ECR byte motoru
```

---

## Masaüstü Uygulama

Masaüstü uygulama Avalonia UI ile geliştirilmiştir. Ana ekranlarda tek menü yapısı, profesyonel header, alt durum çubuğu ve kartlı ekran yapısı kullanılır.

### Ana Pencereler

- Dashboard
- Masalar
- Paket Siparişler
- Kasa
- Ürünler
- Mutfak
- Raporlar
- Ayarlar
- İlk Kurulum Sihirbazı

### Alt Durum Çubuğu

Alt barda işletme için kritik durumlar gösterilir:

- Local service durumu
- POS durumu
- İnternet durumu
- Yazıcı durumu
- Kullanıcı
- Şube
- Saat

---

## Backend Service

`SamerHub.Service`, Windows Service olarak çalışır ve local API sağlar.

Varsayılan servis adresi:

```text
http://127.0.0.1:7070
```

LAN/POS bridge senaryolarında servis aşağıdaki gibi LAN’a da açılabilir:

```text
http://0.0.0.0:7070
```

### Örnek Endpointler

```text
GET  /api/health
GET  /api/system/status
GET  /api/dashboard/summary
GET  /api/tables
POST /api/tables
GET  /api/products
POST /api/products
GET  /api/pos/settings
POST /api/pos/check-connection
POST /api/pos/echo
POST /api/network/scan
GET  /api/fiscal/settings
GET  /api/database/settings
POST /api/database/check
```

---

## Veritabanı

Production hedefinde tercih edilen veritabanı **MySQL**’dir.

Kurulum sihirbazı hedefi:

- MySQL otomatik kurulum
- Mevcut MySQL sunucusuna bağlanma
- Veritabanı oluşturma
- Kullanıcı oluşturma
- Migration çalıştırma
- Config dosyasına güvenli kayıt

Varsayılan üretim yapılandırması:

```json
{
  "Database": {
    "Provider": "MySql",
    "Host": "127.0.0.1",
    "Port": 3306,
    "Database": "samerhub",
    "User": "samerhub_user"
  }
}
```

---

## Config Yönetimi

Production ortamında `.env` yerine JSON config kullanılır.

Windows production config yolu:

```text
C:\ProgramData\SAMER Hub\config\samerhub.config.json
```

Bu dosyada şunlar tutulur:

- İşletme bilgileri
- Şube bilgileri
- MySQL bağlantısı
- Local service URL
- PC LAN IP
- POS IP / POS Port
- POS bridge token
- YNÖKC ayarları
- Platform API ayarları
- Yazıcı ayarları

Gizli değerlerin DPAPI ile şifreli saklanması hedeflenir:

- MySQL parolası
- POS bridge token
- POS ECR secret
- Platform API anahtarları
- Fatura entegratör şifreleri

---

## POS / ECR / YNÖKC Tasarımı

Ana hedef cihaz: **Ingenico MOVE/5000F**

Ana bağlantı yaklaşımı:

```text
Windows PC
   ↓ LAN / Ethernet
Ingenico MOVE/5000F
```

POS içine Android uygulama yüklemek ana yol değildir. Ana üretim yolu LAN/ECR TCP-IP haberleşmesidir.

### POS Ayarları

- PC IP adresi
- POS IP adresi
- POS portu
- Bağlantı tipi: Direkt kablo / Router / Switch
- Timeout
- POS bridge token
- Ağ tarama
- Bağlantı kontrolü

### Ağ Tarama

SAMER Hub, yerel ağdaki cihazları bulmak için ağ tarama altyapısı içerir:

- Ağ kartlarını algılama
- Local subnet seçimi
- IP tarama
- POS port kontrolü
- Bulunan cihazı POS olarak seçme

---

## Native C ECR Motoru

Düşük seviye byte işlemleri için native C motoru eklenmiştir.

C motoru şunları yapar:

- TLV oluşturma
- STX / ETX frame oluşturma
- Length encode/decode
- LRC hesaplama
- Frame parse
- LRC doğrulama
- Hex encode

C# tarafı native DLL’i `P/Invoke` ile çağırır.

```text
C# Service / Gateway
        ↓
SamerHub.Pos.NativeBridge
        ↓
samer_ecr_native.dll
        ↓
Byte-level ECR frame engine
```

---

## YNÖKC / Bilgi Fişi / Fatura Mantığı

SAMER Hub, YNÖKC ve fatura süreçleri için ayrı domain katmanları içerir.

### Desteklenen Belge Tipleri İçin Altyapı

- Mali fiş
- Faturalı satış bilgi fişi
- e-Fatura bilgi fişi
- e-Arşiv bilgi fişi
- Yemek kartı bilgi fişi
- Avans / ön tahsilat bilgi fişi
- Cari hesap tahsilatı
- Fatura tahsilatı

### Ürünlerde Mali Alanlar

Her ürün için aşağıdaki alanlar hedeflenir:

- Ürün adı
- Kategori
- Satış fiyatı
- KDV oranı
- Kısım / departman kodu
- PLU kodu
- Birim tipi
- Aktif / pasif

---

## Platform Entegrasyonları

SAMER Hub, aşağıdaki platformlar için connector mimarisi sağlar:

- Getir
- Yemeksepeti
- Trendyol Yemek

Hedeflenen işlemler:

- Siparişleri çekme
- Sipariş kabul / red
- Hazırlanıyor / hazır / teslim edildi durumları
- Ürünleri çekme
- Menü gönderme
- Fiyat ve stok güncelleme
- Webhook doğrulama

API anahtarları kurulum sırasında girilebilir veya sonradan Ayarlar ekranından eklenebilir.

---

## Kurulum Sihirbazı

Uygulama ilk açıldığında **First Run Wizard** çalışır.

Adımlar:

1. İşletme Bilgileri
2. MySQL / Veritabanı
3. Servis ve PC IP
4. POS Entegrasyonu
5. YNÖKC / Mali Fiş
6. Platformlar
7. Yazıcılar
8. Kontrol ve Kaydet

Kurulum sonucunda config dosyası oluşturulur:

```text
C:\ProgramData\SAMER Hub\config\samerhub.config.json
```

---

## Setup.exe Dağıtımı

Son kullanıcıya kaynak kod dağıtılmaz. Dağıtılacak ürün:

```text
SAMER-Hub-Pro-Setup.exe
```

Kurulum hedefleri:

```text
Service:
C:\Program Files\SAMER Hub\service

Desktop:
C:\Program Files (x86)\SAMER Hub\Desktop

Config / Logs / Data:
C:\ProgramData\SAMER Hub
```

Setup.exe hedefleri:

- Program dosyalarını kurma
- Windows Service kurma
- 7070 portunu kullanan eski süreci kapatma
- SAMER Hub Service başlatma
- MySQL kurulum / bağlantı hazırlığı
- Firewall kuralı
- Masaüstü kısayolu
- İlk kurulum sihirbazını başlatma

---

## Build

### macOS / Linux

```bash
sh publish-samerhub.sh
```

### Windows x64 Self-contained Publish

```bash
sh publish-windows-self-contained.sh
```

### Windows Preflight

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\WINDOWS_BUILD_CHECK.ps1
```

### Native C DLL Build

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\BUILD_NATIVE_WINDOWS.ps1
```

---

## Güvenlik Notları

- Kaynak kod son kullanıcıya dağıtılmaz.
- Gerçek API key, POS secret, MySQL şifresi ve fatura entegratör bilgileri repository’ye eklenmemelidir.
- Production config `C:\ProgramData\SAMER Hub\config` altında tutulmalıdır.
- LAN API varsayılan olarak kısıtlı olmalıdır.
- POS bridge endpointleri token ile korunmalıdır.

---

## Production Durumu

Bu repository bir **production candidate** geliştirme paketidir.

Hazır olanlar:

- Avalonia UI ana ekranları
- Local backend service omurgası
- MySQL config altyapısı
- POS ayar ve bağlantı kontrol yapısı
- Native C ECR frame motoru
- YNÖKC / bilgi fişi / fatura domain altyapısı
- Platform connector mimarisi
- First Run Wizard
- Setup.exe hedef yapısı

Canlı kullanım için doldurulması gerekenler:

- Resmi Ingenico/Intengo ECR TCP-IP komut ve tag tablosu
- Gerçek YNÖKC mali fiş frame formatı
- GİB / özel entegratör fatura API bilgileri
- Getir / Yemeksepeti / Trendyol Yemek resmi API bilgileri
- Gerçek POS cihazında bağlantı ve belge testleri

---

## Lisans

Bu proje özel geliştirme / ticari kullanım hedefiyle hazırlanmıştır. Lisans bilgisi proje sahibinin kararına göre ayrıca belirlenecektir.
