using System.Net.NetworkInformation;
using System.Net.Sockets;
using SamerHub.Core.Entities;
using SamerHub.Desktop.Services;

namespace SamerHub.Desktop.Controls;

public sealed class PosIntegrationSettingsControl : UserControl
{
    private readonly SamerHubApiClient _apiClient = new();

    private CheckBox _enabledCheckBox = null!;
    private RadioButton _ingenicoRadioButton = null!;
    private TextBox _pcIpAddressTextBox = null!;
    private TextBox _posIpAddressTextBox = null!;
    private NumericUpDown _posPortNumeric = null!;
    private NumericUpDown _timeoutNumeric = null!;
    private TextBox _currencyCodeTextBox = null!;
    private NumericUpDown _currencyNumericNumeric = null!;
    private ComboBox _defaultPaymentComboBox = null!;
    private NumericUpDown _minimumTestAmountNumeric = null!;
    private TextBox _terminalIdTextBox = null!;
    private TextBox _merchantIdTextBox = null!;
    private TextBox _ecrSecretTextBox = null!;
    private ComboBox _protocolComboBox = null!;
    private Button _detectPcIpButton = null!;
    private Button _discoverPosButton = null!;
    private Button _testConnectionButton = null!;
    private Button _testSaleButton = null!;
    private Button _saveSettingsButton = null!;
    private Label _statusLabel = null!;

    public PosIntegrationSettingsControl()
    {
        BackColor = Color.White;
        BuildLayout();
        Load += PosIntegrationSettingsControl_Load;
    }

    private async void PosIntegrationSettingsControl_Load(object? sender, EventArgs e)
    {
        Load -= PosIntegrationSettingsControl_Load;
        await LoadSettingsAsync();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

        var leftPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        leftPanel.Controls.Add(UiFactory.CreateTitle("POS Entegrasyonu", new Point(0, 0)));

        _enabledCheckBox = new CheckBox
        {
            AutoSize = true,
            Location = new Point(4, 42),
            Text = "POS entegrasyonu aktif"
        };
        leftPanel.Controls.Add(_enabledCheckBox);

        var modeGroup = UiFactory.CreateSection("Baglanti modu", new Point(0, 76), new Size(720, 72));
        _ingenicoRadioButton = new RadioButton
        {
            AutoSize = true,
            Checked = true,
            Location = new Point(18, 31),
            Text = "Ingenico MOVE/5000F LAN / ECR TCP-IP"
        };
        modeGroup.Controls.Add(_ingenicoRadioButton);

        var networkGroup = UiFactory.CreateSection("Ag Ayarlari", new Point(0, 164), new Size(720, 160));
        networkGroup.Controls.Add(CreateNetworkTable());

        var transactionGroup = UiFactory.CreateSection("Islem Ayarlari", new Point(0, 338), new Size(720, 150));
        transactionGroup.Controls.Add(CreateTransactionTable());

        var securityGroup = UiFactory.CreateSection("Guvenlik / ECR", new Point(0, 504), new Size(720, 170));
        securityGroup.Controls.Add(CreateSecurityTable());

        leftPanel.Controls.Add(securityGroup);
        leftPanel.Controls.Add(transactionGroup);
        leftPanel.Controls.Add(networkGroup);
        leftPanel.Controls.Add(modeGroup);

        var rightPanel = new Panel { Dock = DockStyle.Fill };
        var actionsGroup = UiFactory.CreateSection("Islemler", new Point(0, 0), new Size(340, 280));

        var actionsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        _detectPcIpButton = CreateActionButton("PC IP Algila");
        _discoverPosButton = CreateActionButton("POS'u Bul");
        _testConnectionButton = CreateActionButton("Baglantiyi Test Et");
        _testSaleButton = CreateActionButton("1 TL Test Satis");
        _saveSettingsButton = CreateActionButton("Ayarlari Kaydet");

        _detectPcIpButton.Click += async (_, _) => await DetectPcIpAsync();
        _discoverPosButton.Click += async (_, _) => await DiscoverPosAsync();
        _testConnectionButton.Click += async (_, _) => await TestConnectionAsync();
        _testSaleButton.Click += async (_, _) => await TestSaleAsync();
        _saveSettingsButton.Click += async (_, _) => await SaveSettingsAsync();

        actionsFlow.Controls.Add(_detectPcIpButton);
        actionsFlow.Controls.Add(_discoverPosButton);
        actionsFlow.Controls.Add(_testConnectionButton);
        actionsFlow.Controls.Add(_testSaleButton);
        actionsFlow.Controls.Add(_saveSettingsButton);
        actionsGroup.Controls.Add(actionsFlow);

        var statusGroup = UiFactory.CreateSection("Baglanti Ozeti", new Point(0, 296), new Size(340, 230));
        _statusLabel = UiFactory.CreateEmptyState(
            "Son baglanti: Henuz yapilmadi\r\nPOS IP: -\r\nPort: -\r\nYanit suresi: -\r\nSon hata: -\r\n\r\nNot: PC ve POS ayni yerel agda olmalidir.");
        statusGroup.Controls.Add(_statusLabel);

        rightPanel.Controls.Add(statusGroup);
        rightPanel.Controls.Add(actionsGroup);

        layout.Controls.Add(leftPanel, 0, 0);
        layout.Controls.Add(rightPanel, 1, 0);
        Controls.Add(layout);
    }

    private Control CreateNetworkTable()
    {
        var table = CreateTableLayout();
        AddLabelAndControl(table, 0, "PC IP Adresi:", _pcIpAddressTextBox = CreateTextBox());
        AddLabelAndControl(table, 1, "POS IP Adresi:", _posIpAddressTextBox = CreateTextBox());
        AddLabelAndControl(table, 2, "POS Port:", _posPortNumeric = CreateNumeric(1, 65535, 5000));
        AddLabelAndControl(table, 3, "Timeout (sn):", _timeoutNumeric = CreateNumeric(1, 300, 60));
        table.Location = new Point(16, 28);
        return table;
    }

    private Control CreateTransactionTable()
    {
        var table = CreateTableLayout();
        AddLabelAndControl(table, 0, "Para Birimi:", _currencyCodeTextBox = CreateTextBox("TRY"));
        AddLabelAndControl(table, 1, "Currency Numeric:", _currencyNumericNumeric = CreateNumeric(1, 999, 949));
        _defaultPaymentComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 360
        };
        _defaultPaymentComboBox.Items.AddRange(["Kart / Temassiz + Chip", "Kart / Chip", "Temassiz", "Kart / Nakit Karmasi"]);
        _defaultPaymentComboBox.SelectedIndex = 0;
        AddLabelAndControl(table, 2, "Varsayilan odeme:", _defaultPaymentComboBox);
        AddLabelAndControl(table, 3, "Min. test tutari:", _minimumTestAmountNumeric = CreateDecimalNumeric(0.01m, 1000m, 1.00m));
        table.Location = new Point(16, 28);
        return table;
    }

    private Control CreateSecurityTable()
    {
        var table = CreateTableLayout();
        AddLabelAndControl(table, 0, "Terminal ID:", _terminalIdTextBox = CreateTextBox());
        AddLabelAndControl(table, 1, "Merchant ID:", _merchantIdTextBox = CreateTextBox());
        _ecrSecretTextBox = CreateTextBox();
        _ecrSecretTextBox.UseSystemPasswordChar = true;
        AddLabelAndControl(table, 2, "ECR Anahtari:", _ecrSecretTextBox);
        _protocolComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 360
        };
        _protocolComboBox.Items.Add("RawTcp");
        _protocolComboBox.SelectedIndex = 0;
        AddLabelAndControl(table, 3, "Protokol Tipi:", _protocolComboBox);
        table.Location = new Point(16, 28);
        return table;
    }

    private static TableLayoutPanel CreateTableLayout()
    {
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));
        return table;
    }

    private static void AddLabelAndControl(TableLayoutPanel table, int row, string label, Control control)
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

    private static TextBox CreateTextBox(string? text = null)
    {
        return new TextBox
        {
            Text = text ?? string.Empty,
            Width = 360
        };
    }

    private static NumericUpDown CreateNumeric(decimal min, decimal max, decimal value)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            Width = 140
        };
    }

    private static NumericUpDown CreateDecimalNumeric(decimal min, decimal max, decimal value)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            DecimalPlaces = 2,
            Increment = 0.25m,
            Value = value,
            Width = 140
        };
    }

    private static Button CreateActionButton(string text)
    {
        return new Button
        {
            Margin = new Padding(0, 0, 0, 10),
            Size = new Size(220, 38),
            Text = text
        };
    }

    private async Task LoadSettingsAsync()
    {
        await RunBusyActionAsync("POS ayarlari yukleniyor...", async cancellationToken =>
        {
            var settings = await _apiClient.GetPosSettingsAsync(cancellationToken);
            ApplySettingsToForm(settings);
            UpdateStatusFromSettings(settings, "POS ayarlari yuklendi.");
        });
    }

    private async Task SaveSettingsAsync()
    {
        await RunBusyActionAsync("POS ayarlari kaydediliyor...", async cancellationToken =>
        {
            var saved = await _apiClient.SavePosSettingsAsync(BuildSettingsFromForm(), cancellationToken);
            ApplySettingsToForm(saved);
            UpdateStatusFromSettings(saved, "POS ayarlari kaydedildi.");
            ShowInfo("POS ayarlari basariyla kaydedildi.");
        });
    }

    private async Task TestConnectionAsync()
    {
        await SaveSettingsSilentlyAsync();
        await RunBusyActionAsync("POS baglantisi test ediliyor...", async cancellationToken =>
        {
            var result = await _apiClient.TestConnectionAsync(cancellationToken);
            UpdateStatusFromConnectionResult(result);
            ShowInfo(result.IsReachable
                ? $"POS baglantisi basarili. Yanit suresi: {result.RoundTripMilliseconds ?? 0} ms"
                : $"POS baglantisi kurulamadi: {result.ErrorMessage}");
        });
    }

    private async Task TestSaleAsync()
    {
        var confirm = MessageBox.Show(
            "Test satis butonu servis tarafinda test-sale endpoint'ini cagiracak. ECR protokol dokumani olmadigi icin gercek satis frame'i su anda gonderilmiyor.\n\nDevam etmek istiyor musunuz?",
            "1 TL Test Satis",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await SaveSettingsSilentlyAsync();
        await RunBusyActionAsync("Test satis akisi cagriliyor...", async cancellationToken =>
        {
            var result = await _apiClient.TestSaleAsync(cancellationToken);
            var message = result.IsSuccessful
                ? "Test satis basarili."
                : $"Test satis sonucu: {result.ErrorMessage}";

            _statusLabel.Text =
                $"Son baglanti: Test satis cagrildi\r\nPOS IP: {_posIpAddressTextBox.Text}\r\nPort: {_posPortNumeric.Value}\r\nYanit suresi: -\r\nSon hata: {Normalize(result.ErrorMessage)}\r\n\r\nRaw: {Normalize(result.RawResponse)}";

            ShowInfo(message);
        });
    }

    private async Task DetectPcIpAsync()
    {
        await RunBusyActionAsync("PC IP adresi algilaniyor...", cancellationToken =>
        {
            _pcIpAddressTextBox.Text = DetectLocalIpv4Address() ?? string.Empty;
            _statusLabel.Text =
                $"Son baglanti: Yerel IP algilandi\r\nPOS IP: {_posIpAddressTextBox.Text}\r\nPort: {_posPortNumeric.Value}\r\nYanit suresi: -\r\nSon hata: -";
            return Task.CompletedTask;
        });
    }

    private async Task DiscoverPosAsync()
    {
        await SaveSettingsSilentlyAsync();
        await RunBusyActionAsync("Ayni agda POS araniyor...", async cancellationToken =>
        {
            var baseIp = !string.IsNullOrWhiteSpace(_pcIpAddressTextBox.Text)
                ? _pcIpAddressTextBox.Text.Trim()
                : DetectLocalIpv4Address();

            if (string.IsNullOrWhiteSpace(baseIp))
            {
                throw new InvalidOperationException("Once PC IP adresi algilanmali veya manuel girilmelidir.");
            }

            var discoveredIp = await ScanSubnetForOpenPortAsync(baseIp, (int)_posPortNumeric.Value, cancellationToken);
            if (string.IsNullOrWhiteSpace(discoveredIp))
            {
                _statusLabel.Text =
                    $"Son baglanti: POS bulunamadi\r\nPOS IP: -\r\nPort: {_posPortNumeric.Value}\r\nYanit suresi: -\r\nSon hata: Secilen alt agda acik port bulunamadi.";
                ShowInfo("POS bulunamadi. Kablo, IP, port veya ECR modunu kontrol edin.");
                return;
            }

            _posIpAddressTextBox.Text = discoveredIp;
            _statusLabel.Text =
                $"Son baglanti: POS bulundu\r\nPOS IP: {discoveredIp}\r\nPort: {_posPortNumeric.Value}\r\nYanit suresi: -\r\nSon hata: Yok";
            ShowInfo($"Acilik gorunen POS IP adresi bulundu: {discoveredIp}");
        });
    }

    private async Task SaveSettingsSilentlyAsync()
    {
        var saved = await _apiClient.SavePosSettingsAsync(BuildSettingsFromForm(), CancellationToken.None);
        ApplySettingsToForm(saved);
        UpdateStatusFromSettings(saved, "POS ayarlari guncel.");
    }

    private PosSettings BuildSettingsFromForm()
    {
        return new PosSettings
        {
            IsEnabled = _enabledCheckBox.Checked,
            Mode = "IngenicoEcrTcp",
            PcIpAddress = _pcIpAddressTextBox.Text.Trim(),
            PosIpAddress = _posIpAddressTextBox.Text.Trim(),
            PosPort = Decimal.ToInt32(_posPortNumeric.Value),
            TimeoutSeconds = Decimal.ToInt32(_timeoutNumeric.Value),
            CurrencyCode = _currencyCodeTextBox.Text.Trim(),
            CurrencyNumericCode = Decimal.ToInt32(_currencyNumericNumeric.Value),
            DefaultPaymentMethod = _defaultPaymentComboBox.Text,
            MinimumTestAmount = _minimumTestAmountNumeric.Value,
            TerminalId = _terminalIdTextBox.Text.Trim(),
            MerchantId = _merchantIdTextBox.Text.Trim(),
            EcrSecret = _ecrSecretTextBox.Text,
            Protocol = _protocolComboBox.Text
        };
    }

    private void ApplySettingsToForm(PosSettings settings)
    {
        _enabledCheckBox.Checked = settings.IsEnabled;
        _ingenicoRadioButton.Checked = true;
        _pcIpAddressTextBox.Text = settings.PcIpAddress;
        _posIpAddressTextBox.Text = settings.PosIpAddress;
        _posPortNumeric.Value = ClampNumeric(_posPortNumeric, settings.PosPort);
        _timeoutNumeric.Value = ClampNumeric(_timeoutNumeric, settings.TimeoutSeconds);
        _currencyCodeTextBox.Text = settings.CurrencyCode;
        _currencyNumericNumeric.Value = ClampNumeric(_currencyNumericNumeric, settings.CurrencyNumericCode);
        _defaultPaymentComboBox.Text = string.IsNullOrWhiteSpace(settings.DefaultPaymentMethod)
            ? "Kart / Temassiz + Chip"
            : settings.DefaultPaymentMethod;
        _minimumTestAmountNumeric.Value = ClampNumeric(_minimumTestAmountNumeric, settings.MinimumTestAmount);
        _terminalIdTextBox.Text = settings.TerminalId;
        _merchantIdTextBox.Text = settings.MerchantId;
        _ecrSecretTextBox.Text = settings.EcrSecret;
        _protocolComboBox.Text = string.IsNullOrWhiteSpace(settings.Protocol) ? "RawTcp" : settings.Protocol;
    }

    private void UpdateStatusFromSettings(PosSettings settings, string prefix)
    {
        _statusLabel.Text =
            $"{prefix}\r\nPOS IP: {Normalize(settings.PosIpAddress)}\r\nPort: {settings.PosPort}\r\nSon baglanti: {(settings.LastConnectionAtUtc?.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") ?? "Henuz yapilmadi")}\r\nYanit suresi: {(settings.LastRoundTripMilliseconds?.ToString() ?? "-")} ms\r\nSon hata: {Normalize(settings.LastError)}";
    }

    private void UpdateStatusFromConnectionResult(SamerHub.Pos.PosStatusResult result)
    {
        _statusLabel.Text =
            $"Son baglanti: {(result.IsReachable ? "Basarili" : "Basarisiz")}\r\nPOS IP: {_posIpAddressTextBox.Text}\r\nPort: {_posPortNumeric.Value}\r\nYanit suresi: {(result.RoundTripMilliseconds?.ToString() ?? "-")} ms\r\nSon hata: {Normalize(result.ErrorMessage)}\r\n\r\nRaw: {Normalize(result.RawResponse)}";
    }

    private static decimal ClampNumeric(NumericUpDown control, decimal value)
    {
        if (value < control.Minimum)
        {
            return control.Minimum;
        }

        if (value > control.Maximum)
        {
            return control.Maximum;
        }

        return value;
    }

    private async Task RunBusyActionAsync(string busyMessage, Func<CancellationToken, Task> action)
    {
        SetBusy(true, busyMessage);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await action(cts.Token);
        }
        catch (Exception ex)
        {
            _statusLabel.Text =
                $"Son baglanti: Hata\r\nPOS IP: {_posIpAddressTextBox.Text}\r\nPort: {_posPortNumeric.Value}\r\nYanit suresi: -\r\nSon hata: {Normalize(ex.Message)}";
            MessageBox.Show(ex.Message, "POS Entegrasyonu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, string.Empty);
        }
    }

    private void SetBusy(bool isBusy, string busyMessage)
    {
        UseWaitCursor = isBusy;
        foreach (var button in new[] { _detectPcIpButton, _discoverPosButton, _testConnectionButton, _testSaleButton, _saveSettingsButton })
        {
            button.Enabled = !isBusy;
        }

        if (isBusy)
        {
            _statusLabel.Text = busyMessage;
        }
    }

    private static string? DetectLocalIpv4Address()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x =>
                x.OperationalStatus == OperationalStatus.Up &&
                x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(x => x.Address.ToString())
            .Where(x => !x.StartsWith("169.254.", StringComparison.Ordinal))
            .ToList();

        return candidates.FirstOrDefault();
    }

    private static async Task<string?> ScanSubnetForOpenPortAsync(string ipAddress, int port, CancellationToken cancellationToken)
    {
        var parts = ipAddress.Split('.');
        if (parts.Length != 4)
        {
            throw new InvalidOperationException("PC IP adresi gecersiz.");
        }

        var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}.";

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = Enumerable.Range(1, 254).Select(async host =>
        {
            var candidate = prefix + host;
            using var client = new TcpClient();

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(300));
                await client.ConnectAsync(candidate, port, timeoutCts.Token).AsTask();
                linkedCts.Cancel();
                return candidate;
            }
            catch
            {
                return null;
            }
        }).ToList();

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);
            var result = await completed;
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }

        return null;
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "Yok" : value;

    private static void ShowInfo(string message)
    {
        MessageBox.Show(message, "POS Entegrasyonu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
