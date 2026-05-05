using SamerHub.Desktop.Services;

namespace SamerHub.Desktop.Controls.Wizard;

public sealed class PosTestStepControl : UserControl, IWizardStep
{
    private readonly SamerHubApiClient _apiClient = new();
    private Label _settingsSummaryLabel = null!;
    private Label _resultLabel = null!;

    public PosTestStepControl()
    {
        BackColor = Color.White;

        Controls.Add(UiFactory.CreateSubtitle(
            "Kaydedilen POS ayarlari ile canli baglanti testi ve kontrollu test satis cagrisi bu adimda yapilir.",
            new Point(0, 34)));
        Controls.Add(UiFactory.CreateTitle("4. POS Testi", new Point(0, 0)));

        BuildLayout();
    }

    public async Task OnStepActivatedAsync()
    {
        await RefreshSettingsSummaryAsync();
    }

    private void BuildLayout()
    {
        var settingsGroup = UiFactory.CreateSection("Kaydedilen POS Ayarlari", new Point(0, 72), new Size(760, 180));
        _settingsSummaryLabel = UiFactory.CreateEmptyState("POS ayarlari henuz yuklenmedi.");
        settingsGroup.Controls.Add(_settingsSummaryLabel);

        var testGroup = UiFactory.CreateSection("Test ve Dogrulama", new Point(0, 268), new Size(760, 220));
        var actionsFlow = new FlowLayoutPanel
        {
            Location = new Point(20, 30),
            Size = new Size(700, 46)
        };

        var reloadButton = new Button
        {
            Text = "Ayarlari Yenile",
            Size = new Size(160, 36)
        };
        var testConnectionButton = new Button
        {
            Text = "Baglantiyi Test Et",
            Size = new Size(180, 36)
        };
        var testSaleButton = new Button
        {
            Text = "1 TL Test Satis",
            Size = new Size(160, 36)
        };

        reloadButton.Click += async (_, _) => await RefreshSettingsSummaryAsync();
        testConnectionButton.Click += async (_, _) => await TestConnectionAsync();
        testSaleButton.Click += async (_, _) => await TestSaleAsync();

        actionsFlow.Controls.Add(reloadButton);
        actionsFlow.Controls.Add(testConnectionButton);
        actionsFlow.Controls.Add(testSaleButton);
        testGroup.Controls.Add(actionsFlow);

        _resultLabel = UiFactory.CreateEmptyState(
            "Bu adimda su islemler canli endpointlerle calisacak:\r\n\r\n- Baglantiyi Test Et\r\n- 1 TL Test Satis\r\n\r\nNot: ECR protokol dokumani gelmeden gercek satis frame'i gonderilmeyecek.");
        _resultLabel.Location = new Point(20, 90);
        _resultLabel.Size = new Size(700, 110);
        testGroup.Controls.Add(_resultLabel);

        Controls.Add(testGroup);
        Controls.Add(settingsGroup);
    }

    private async Task RefreshSettingsSummaryAsync()
    {
        await RunBusyActionAsync("POS ayarlari yenileniyor...", async cancellationToken =>
        {
            var settings = await _apiClient.GetPosSettingsAsync(cancellationToken);
            _settingsSummaryLabel.Text =
                $"Durum: {(settings.IsEnabled ? "Aktif" : "Pasif")}\r\n" +
                $"PC IP: {Normalize(settings.PcIpAddress)}\r\n" +
                $"POS IP: {Normalize(settings.PosIpAddress)}\r\n" +
                $"Port: {settings.PosPort}\r\n" +
                $"Timeout: {settings.TimeoutSeconds} sn\r\n" +
                $"Terminal ID: {Normalize(settings.TerminalId)}";
        });
    }

    private async Task TestConnectionAsync()
    {
        await RunBusyActionAsync("POS baglantisi test ediliyor...", async cancellationToken =>
        {
            var result = await _apiClient.TestConnectionAsync(cancellationToken);
            _resultLabel.Text =
                $"Baglanti Sonucu: {(result.IsReachable ? "Basarili" : "Basarisiz")}\r\n" +
                $"Yanit suresi: {(result.RoundTripMilliseconds?.ToString() ?? "-")} ms\r\n" +
                $"Terminal ID: {Normalize(result.TerminalId)}\r\n" +
                $"Hata: {Normalize(result.ErrorMessage)}\r\n\r\n" +
                $"Raw: {Normalize(result.RawResponse)}";
        });
    }

    private async Task TestSaleAsync()
    {
        var confirm = MessageBox.Show(
            "Bu adim servis tarafinda test-sale endpoint'ini cagirir. ECR protokol dokumani olmadan gercek satis frame'i gonderilmeyecektir.\n\nDevam etmek istiyor musunuz?",
            "POS Testi",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await RunBusyActionAsync("Test satis endpoint'i cagriliyor...", async cancellationToken =>
        {
            var result = await _apiClient.TestSaleAsync(cancellationToken);
            _resultLabel.Text =
                $"Test Satis Sonucu: {(result.IsSuccessful ? "Basarili" : "Tamamlanmadi")}\r\n" +
                $"Onay Kodu: {Normalize(result.ApprovalCode)}\r\n" +
                $"Fis No: {Normalize(result.ReceiptNumber)}\r\n" +
                $"STAN: {Normalize(result.Stan)}\r\n" +
                $"Hata: {Normalize(result.ErrorMessage)}\r\n\r\n" +
                $"Raw: {Normalize(result.RawResponse)}";
        });
    }

    private async Task RunBusyActionAsync(string message, Func<CancellationToken, Task> action)
    {
        UseWaitCursor = true;
        _resultLabel.Text = message;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await action(cts.Token);
        }
        catch (Exception ex)
        {
            _resultLabel.Text = ex.Message;
            MessageBox.Show(ex.Message, "POS Testi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value;
}
