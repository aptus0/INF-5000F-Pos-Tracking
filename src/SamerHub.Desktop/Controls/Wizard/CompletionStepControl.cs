using SamerHub.Core.Entities;
using SamerHub.Desktop.Services;

namespace SamerHub.Desktop.Controls.Wizard;

public sealed class CompletionStepControl : UserControl
{
    private const string CheckMark = "✓";
    private const string Cross = "✗";
    private const string Warning = "⚠";

    private readonly Label _summaryLabel;
    private readonly Label _warningsLabel;
    private readonly ProgressBar _progressBar;

    public CompletionStepControl()
    {
        BackColor = Color.White;

        Controls.Add(UiFactory.CreateSubtitle(
            "Kurulum özeti ve sistem hazırlığı kontrol ediliyor.",
            new Point(0, 34)));
        Controls.Add(UiFactory.CreateTitle("7. Tamamlama", new Point(0, 0)));

        var summarySection = UiFactory.CreateSection("Kurulum Özeti", new Point(0, 72), new Size(760, 200));
        _summaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Yükleniyor...",
            AutoSize = false,
            Padding = new Padding(12),
            BackColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 10)
        };
        summarySection.Controls.Add(_summaryLabel);
        Controls.Add(summarySection);

        var warningsSection = UiFactory.CreateSection("Sistem Durumu", new Point(0, 280), new Size(760, 120));
        _warningsLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Kontrol ediliyor...",
            AutoSize = false,
            Padding = new Padding(12),
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.FromArgb(192, 0, 0)
        };
        warningsSection.Controls.Add(_warningsLabel);
        Controls.Add(warningsSection);

        _progressBar = new ProgressBar
        {
            Location = new Point(0, 408),
            Size = new Size(760, 8),
            Style = ProgressBarStyle.Marquee
        };
        Controls.Add(_progressBar);

        Load += async (s, e) => await LoadSummaryAsync();
    }

    private async Task LoadSummaryAsync()
    {
        var client = new SamerHubApiClient();
        var ct = CancellationToken.None;

        try
        {
            var posSettings = await client.GetPosSettingsAsync(ct);
            var printerSettings = await client.GetPrinterSettingsAsync(ct);
            var platformSettings = await client.GetPlatformSettingsAsync(ct);

            var summaryText = BuildSummary(posSettings, printerSettings, platformSettings);
            var warningsText = BuildWarnings(posSettings, printerSettings, platformSettings);

            _summaryLabel.Text = summaryText;
            _warningsLabel.Text = warningsText;
            _warningsLabel.ForeColor = warningsText.Contains("✗") ? Color.FromArgb(192, 0, 0) : Color.FromArgb(0, 128, 0);
        }
        catch
        {
            _summaryLabel.Text = "Ayarlar yüklenemedi. Lütfen Service'in çalıştığını kontrol edin.";
            _warningsLabel.Text = "✗ Sistem hazır değil";
        }
        finally
        {
            _progressBar.Visible = false;
        }
    }

    private static string BuildSummary(PosSettings pos, PrinterSettings printer, PlatformApiSettings platform)
    {
        return $@"POS Entegrasyonu: {(pos.IsEnabled ? $"{CheckMark} Etkin" : $"{Cross} Pasif")} ({pos.Mode})
  → Terminal ID: {(string.IsNullOrWhiteSpace(pos.TerminalId) ? "Varsayılan" : pos.TerminalId)}
  → IP: {pos.PosIpAddress}:{pos.PosPort}

Yazıcı: {(string.IsNullOrWhiteSpace(printer.KitchenPrinterName) ? $"{Cross} Seçilmedi" : $"{CheckMark} {printer.KitchenPrinterName}")}

Platform Bağlantıları:
  → Trendyol Go: {(platform.TrendyolGo?.IsEnabled == true ? $"{CheckMark} Aktif" : $"{Cross} Pasif")}
  → Getir: {(platform.Getir?.IsEnabled == true ? $"{CheckMark} Aktif" : $"{Cross} Pasif")}
  → Yemeksepeti: {(platform.Yemeksepeti?.IsEnabled == true ? $"{CheckMark} Aktif" : $"{Cross} Pasif")}";
    }

    private static string BuildWarnings(PosSettings pos, PrinterSettings printer, PlatformApiSettings platform)
    {
        var warnings = new List<string>();

        if (!pos.IsEnabled)
            warnings.Add($"{Cross} POS entegrasyonu devre dışı - Kart ödeme çalışmayacak");

        if (string.IsNullOrWhiteSpace(printer.KitchenPrinterName))
            warnings.Add($"{Cross} Mutfak yazıcısı seçilmedi - Fişler yazdırılamayacak");

        if (platform.TrendyolGo?.IsEnabled != true && platform.Getir?.IsEnabled != true && platform.Yemeksepeti?.IsEnabled != true)
            warnings.Add($"{Warning} Hiçbir platform aktif değil - Manuel siparişlerle başlayabilirsiniz");

        return warnings.Any()
            ? string.Join("\r\n", warnings)
            : $"{CheckMark} Sistem kuruluma hazır! SAMER Hub'ı kullanmaya başlayabilirsiniz.";
    }
}
