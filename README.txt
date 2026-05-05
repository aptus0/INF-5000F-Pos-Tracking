SAMER Hub - PowerShell Professional Paket
=========================================

Bu paket iki ana PowerShell dosyasından oluşur:

1) SAMER-Hub-ApiService.ps1
   - Arka planda çalışan yerel HTTP API servisidir.
   - Varsayılan adres: http://127.0.0.1:8189
   - LAN tarama, POS TCP test, firewall kuralı, Trendyol API ve fiş metni oluşturma işlemlerini sağlar.

2) SAMER-Hub-App.ps1
   - Windows pencere uygulamasıdır.
   - Açılırken API servisini otomatik başlatır.
   - 5 ana bölüm içerir:
     Pano, LAN Haritası, POS / ECR, Trendyol, Fiş ve Yazıcı.

Çalıştırma
----------

Normal kullanım:
  Start-SAMER-Hub.bat

Firewall kuralı eklemek için:
  Start-SAMER-Hub-Admin.bat

Manuel PowerShell:
  powershell -ExecutionPolicy Bypass -File .\SAMER-Hub-App.ps1

Önemli Notlar
-------------

- POS IP örneği: 192.168.1.39
- PC IP otomatik algılanır.
- POS TCP portu banka/Ingenico ECR dokümanındaki gerçek port olmalıdır.
- Router port yönlendirme açılmaz.
- Varsayılan API sadece localhost üzerinde dinler: 127.0.0.1:8189

Ingenico MOVE/5000F
-------------------

Bu pakette POS haberleşme altyapısı hazırdır:
- IP/port test
- firewall izin kuralı
- simülasyon ödeme
- TcpRaw modu ile POS'a ham TCP mesajı gönderme

Gerçek ödeme başlatma için Ingenico/Banka ECR-TCP dokümanındaki şu bilgiler gerekir:
- Gerçek TCP port numarası
- Satış isteği mesaj formatı
- Cevap mesaj formatı
- Onay/ret kodları
- Slip/receipt alanları
- STX/ETX/LRC gibi çerçeveleme gerekip gerekmediği

Bu bilgiler gelince SAMER-Hub-ApiService.ps1 içindeki:
  New-IngenicoSaleMessage
  Invoke-PosTcpRawSale
  Invoke-PosSale

fonksiyonları gerçek protokole göre doldurulur.
