using SamerHub.Core.Entities;
using SamerHub.Desktop.Services;

namespace SamerHub.Desktop.Controls.Wizard;

public sealed class PosSetupStepControl : UserControl, IWizardStep
{
    private readonly SamerHubApiClient _apiClient = new();
    private CheckBox _enabledCheckBox = null!;
    private TextBox _pcIpAddressTextBox = null!;
    private TextBox _posIpAddressTextBox = null!;
    private NumericUpDown _posPortNumeric = null!;
    private NumericUpDown _timeoutNumeric = null!;
    private TextBox _currencyCodeTextBox = null!;
    private NumericUpDown _currencyNumericNumeric = null!;
    private TextBox _terminalIdTextBox = null!;
    private TextBox _merchantIdTextBox = null!;
    private TextBox _ecrSecretTextBox = null!;
    private Label _statusLabel = null!;

    public PosSetupStepControl()
    {
        BackColor = Color.White;

        Controls.Add(UiFactory.CreateSubtitle(
            "POS IP, port, timeout ve temel ECR parametreleri bu adimda kaydedilir.",
            new Point(0, 34)));
        Controls.Add(UiFactory.CreateTitle("3. POS Ayarlari", new Point(0, 0)));

        BuildLayout();
    }

    public async Task OnStepActivatedAsync()
    {
        await LoadSettingsAsync();
    }

    private void BuildLayout()
    {
        var settingsGroup = UiFactory.CreateSection("POS Bilgileri", new Point(0, 72), new Size(760, 430));
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            Location = new Point(20, 28)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));

        _enabledCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "POS entegrasyonu aktif"
        };
        AddControlRow(table, 0, "Durum:", _enabledCheckBox);
        AddControlRow(table, 1, "PC IP Adresi:", _pcIpAddressTextBox = CreateTextBox());
        AddControlRow(table, 2, "POS IP Adresi:", _posIpAddressTextBox = CreateTextBox());
        AddControlRow(table, 3, "POS Port:", _posPortNumeric = CreateNumeric(1, 65535, 5000));
        AddControlRow(table, 4, "Timeout (sn):", _timeoutNumeric = CreateNumeric(1, 300, 60));
        AddControlRow(table, 5, "Para Birimi:", _currencyCodeTextBox = CreateTextBox("TRY"));
        AddControlRow(table, 6, "Currency Numeric:", _currencyNumericNumeric = CreateNumeric(1, 999, 949));
        AddControlRow(table, 7, "Terminal ID:", _terminalIdTextBox = CreateTextBox());
        AddControlRow(table, 8, "Merchant ID:", _merchantIdTextBox = CreateTextBox());
        _ecrSecretTextBox = CreateTextBox();
        _ecrSecretTextBox.UseSystemPasswordChar = true;
        AddControlRow(table, 9, "ECR Anahtari:", _ecrSecretTextBox);

        settingsGroup.Controls.Add(table);

        var actionPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 320),
            Size = new Size(700, 48)
        };

        var detectButton = CreateActionButton("PC IP Algila");
        var discoverButton = CreateActionButton("POS'u Bul");
        var saveButton = CreateActionButton("Ayarlari Kaydet");

        detectButton.Click += async (_, _) => await RunBusyActionAsync("PC IP algilaniyor...", async cancellationToken =>
        {
            _pcIpAddressTextBox.Text = PosIntegrationHelpers.DetectLocalIpv4Address() ?? string.Empty;
            UpdateStatus("Yerel PC IP algilandi.");
            await Task.CompletedTask;
        });

        discoverButton.Click += async (_, _) => await RunBusyActionAsync("Ayni agda POS araniyor...", async cancellationToken =>
        {
            var baseIp = string.IsNullOrWhiteSpace(_pcIpAddressTextBox.Text)
                ? PosIntegrationHelpers.DetectLocalIpv4Address()
                : _pcIpAddressTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(baseIp))
            {
                throw new InvalidOperationException("Once PC IP adresi algilanmali veya manuel girilmelidir.");
            }

            var discoveredIp = await PosIntegrationHelpers.ScanSubnetForOpenPortAsync(baseIp, (int)_posPortNumeric.Value, cancellationToken);
            if (string.IsNullOrWhiteSpace(discoveredIp))
            {
                UpdateStatus("POS bulunamadi.");
                MessageBox.Show("POS bulunamadi. IP, port ve ECR modunu kontrol edin.", "POS Ayarlari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _posIpAddressTextBox.Text = discoveredIp;
            UpdateStatus($"POS bulundu: {discoveredIp}");
        });

        saveButton.Click += async (_, _) => await SaveSettingsAsync();

        actionPanel.Controls.Add(detectButton);
        actionPanel.Controls.Add(discoverButton);
        actionPanel.Controls.Add(saveButton);
        settingsGroup.Controls.Add(actionPanel);

        _statusLabel = UiFactory.CreateEmptyState("POS ayarlari henuz yuklenmedi.");
        _statusLabel.Location = new Point(20, 372);
        _statusLabel.Size = new Size(700, 40);
        settingsGroup.Controls.Add(_statusLabel);

        Controls.Add(settingsGroup);
    }

    private async Task LoadSettingsAsync()
    {
        await RunBusyActionAsync("POS ayarlari yukleniyor...", async cancellationToken =>
        {
            var settings = await _apiClient.GetPosSettingsAsync(cancellationToken);
            _enabledCheckBox.Checked = settings.IsEnabled;
            _pcIpAddressTextBox.Text = settings.PcIpAddress;
            _posIpAddressTextBox.Text = settings.PosIpAddress;
            _posPortNumeric.Value = ClampNumeric(_posPortNumeric, settings.PosPort);
            _timeoutNumeric.Value = ClampNumeric(_timeoutNumeric, settings.TimeoutSeconds);
            _currencyCodeTextBox.Text = settings.CurrencyCode;
            _currencyNumericNumeric.Value = ClampNumeric(_currencyNumericNumeric, settings.CurrencyNumericCode);
            _terminalIdTextBox.Text = settings.TerminalId;
            _merchantIdTextBox.Text = settings.MerchantId;
            _ecrSecretTextBox.Text = settings.EcrSecret;
            UpdateStatus("POS ayarlari yuklendi.");
        });
    }

    private async Task SaveSettingsAsync()
    {
        await RunBusyActionAsync("POS ayarlari kaydediliyor...", async cancellationToken =>
        {
            var saved = await _apiClient.SavePosSettingsAsync(new PosSettings
            {
                IsEnabled = _enabledCheckBox.Checked,
                Mode = "IngenicoEcrTcp",
                PcIpAddress = _pcIpAddressTextBox.Text.Trim(),
                PosIpAddress = _posIpAddressTextBox.Text.Trim(),
                PosPort = Decimal.ToInt32(_posPortNumeric.Value),
                TimeoutSeconds = Decimal.ToInt32(_timeoutNumeric.Value),
                CurrencyCode = _currencyCodeTextBox.Text.Trim(),
                CurrencyNumericCode = Decimal.ToInt32(_currencyNumericNumeric.Value),
                TerminalId = _terminalIdTextBox.Text.Trim(),
                MerchantId = _merchantIdTextBox.Text.Trim(),
                EcrSecret = _ecrSecretTextBox.Text
            }, cancellationToken);

            UpdateStatus($"POS ayarlari kaydedildi. Son POS IP: {saved.PosIpAddress}");
            MessageBox.Show("POS ayarlari kaydedildi.", "POS Ayarlari", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async Task RunBusyActionAsync(string message, Func<CancellationToken, Task> action)
    {
        UseWaitCursor = true;
        _statusLabel.Text = message;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await action(cts.Token);
        }
        catch (Exception ex)
        {
            UpdateStatus(ex.Message);
            MessageBox.Show(ex.Message, "POS Ayarlari", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void UpdateStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private static TextBox CreateTextBox(string text = "")
        => new() { Text = text, Width = 360 };

    private static NumericUpDown CreateNumeric(decimal min, decimal max, decimal value)
        => new() { Minimum = min, Maximum = max, Value = value, Width = 140 };

    private static Button CreateActionButton(string text)
        => new() { Text = text, Size = new Size(180, 36), Margin = new Padding(0, 0, 10, 0) };

    private static void AddControlRow(TableLayoutPanel table, int row, string label, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 8, 12, 8),
            Text = label
        }, 0, row);
        control.Margin = new Padding(0, 4, 0, 4);
        table.Controls.Add(control, 1, row);
    }

    private static decimal ClampNumeric(NumericUpDown control, decimal value)
        => value < control.Minimum ? control.Minimum : value > control.Maximum ? control.Maximum : value;
}
