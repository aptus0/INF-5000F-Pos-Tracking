using SamerHub.Core.Entities;
using SamerHub.Desktop.Services;
using DrawingPrinterSettings = System.Drawing.Printing.PrinterSettings;
using PrintDocument = System.Drawing.Printing.PrintDocument;
using StandardPrintController = System.Drawing.Printing.StandardPrintController;

namespace SamerHub.Desktop.Controls.Wizard;

public sealed class PrinterSetupStepControl : UserControl, IWizardStep
{
    private readonly SamerHubApiClient _apiClient = new();
    private ComboBox _kitchenPrinterComboBox = null!;
    private ComboBox _cashPrinterComboBox = null!;
    private ComboBox _deliveryPrinterComboBox = null!;
    private CheckBox _kitchenEscPosCheckBox = null!;
    private CheckBox _cashEscPosCheckBox = null!;
    private CheckBox _deliveryEscPosCheckBox = null!;
    private Label _statusLabel = null!;

    public PrinterSetupStepControl()
    {
        BackColor = Color.White;

        Controls.Add(UiFactory.CreateSubtitle(
            "Windows yazicilari secilir, kaydedilir ve ornek mutfak fisi ile dogrulanir.",
            new Point(0, 34)));
        Controls.Add(UiFactory.CreateTitle("5. Yazici Ayarlari", new Point(0, 0)));

        BuildLayout();
    }

    public async Task OnStepActivatedAsync()
    {
        await RefreshInstalledPrintersAsync();
        await LoadSettingsAsync();
    }

    private void BuildLayout()
    {
        var section = UiFactory.CreateSection("Yazici Kurulumu", new Point(0, 72), new Size(760, 420));

        var table = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 3,
            Location = new Point(20, 28)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));

        _kitchenPrinterComboBox = CreatePrinterComboBox();
        _cashPrinterComboBox = CreatePrinterComboBox();
        _deliveryPrinterComboBox = CreatePrinterComboBox();
        _kitchenEscPosCheckBox = CreateEscPosCheckBox();
        _cashEscPosCheckBox = CreateEscPosCheckBox();
        _deliveryEscPosCheckBox = CreateEscPosCheckBox();

        AddPrinterRow(table, 0, "Mutfak Yazicisi:", _kitchenPrinterComboBox, _kitchenEscPosCheckBox);
        AddPrinterRow(table, 1, "Kasa Yazicisi:", _cashPrinterComboBox, _cashEscPosCheckBox);
        AddPrinterRow(table, 2, "Paket Yazicisi:", _deliveryPrinterComboBox, _deliveryEscPosCheckBox);

        section.Controls.Add(table);

        var actionsPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 180),
            Size = new Size(700, 46)
        };

        var refreshButton = CreateActionButton("Yazicilari Yenile");
        var saveButton = CreateActionButton("Ayarlari Kaydet");
        var testButton = CreateActionButton("Test Fisi Bas");

        refreshButton.Click += async (_, _) => await RefreshInstalledPrintersAsync();
        saveButton.Click += async (_, _) => await SaveSettingsAsync();
        testButton.Click += async (_, _) => await TestPrintAsync();

        actionsPanel.Controls.Add(refreshButton);
        actionsPanel.Controls.Add(saveButton);
        actionsPanel.Controls.Add(testButton);
        section.Controls.Add(actionsPanel);

        _statusLabel = UiFactory.CreateEmptyState("Yazici ayarlari henuz yuklenmedi.");
        _statusLabel.Location = new Point(20, 244);
        _statusLabel.Size = new Size(700, 120);
        section.Controls.Add(_statusLabel);

        Controls.Add(section);
    }

    private async Task RefreshInstalledPrintersAsync()
    {
        await RunBusyActionAsync("Windows yazicilari listeleniyor...", async cancellationToken =>
        {
            var printers = DrawingPrinterSettings.InstalledPrinters.Cast<string>().OrderBy(x => x).ToArray();
            BindPrinterCombo(_kitchenPrinterComboBox, printers);
            BindPrinterCombo(_cashPrinterComboBox, printers);
            BindPrinterCombo(_deliveryPrinterComboBox, printers);
            UpdateStatus(printers.Length == 0
                ? "Windows tarafinda hic yazici bulunamadi."
                : $"{printers.Length} yazici bulundu ve liste guncellendi.");
            await Task.CompletedTask;
        });
    }

    private async Task LoadSettingsAsync()
    {
        await RunBusyActionAsync("Yazici ayarlari yukleniyor...", async cancellationToken =>
        {
            var settings = await _apiClient.GetPrinterSettingsAsync(cancellationToken);
            SelectPrinter(_kitchenPrinterComboBox, settings.KitchenPrinterName);
            SelectPrinter(_cashPrinterComboBox, settings.CashPrinterName);
            SelectPrinter(_deliveryPrinterComboBox, settings.DeliveryPrinterName);
            _kitchenEscPosCheckBox.Checked = settings.UseEscPosForKitchen;
            _cashEscPosCheckBox.Checked = settings.UseEscPosForCash;
            _deliveryEscPosCheckBox.Checked = settings.UseEscPosForDelivery;
            UpdateStatus(string.IsNullOrWhiteSpace(settings.LastTestResult)
                ? "Yazici ayarlari yuklendi."
                : $"Son test: {settings.LastTestResult}");
        });
    }

    private async Task SaveSettingsAsync()
    {
        await RunBusyActionAsync("Yazici ayarlari kaydediliyor...", async cancellationToken =>
        {
            await _apiClient.SavePrinterSettingsAsync(BuildSettings(), cancellationToken);
            UpdateStatus("Yazici ayarlari kaydedildi.");
            MessageBox.Show("Yazici ayarlari kaydedildi.", "Yazici Ayarlari", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async Task TestPrintAsync()
    {
        await RunBusyActionAsync("Test yazdirma hazirlaniyor...", async cancellationToken =>
        {
            await _apiClient.SavePrinterSettingsAsync(BuildSettings(), cancellationToken);
            var apiResult = await _apiClient.TestPrinterAsync(cancellationToken);

            if (!apiResult.IsSuccessful)
            {
                UpdateStatus(apiResult.Message);
                MessageBox.Show(apiResult.Message, "Yazici Testi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var printerName = SelectedPrinterName(_kitchenPrinterComboBox);
            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new InvalidOperationException("Test fisi icin mutfak yazicisi secilmelidir.");
            }

            await Task.Run(() => PrintKitchenTestSlip(printerName), cancellationToken);
            UpdateStatus($"Test fisi gonderildi: {printerName}");
            MessageBox.Show($"Test fisi secili yaziciya gonderildi:\n{printerName}", "Yazici Testi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private PrinterSettings BuildSettings()
        => new()
        {
            KitchenPrinterName = SelectedPrinterName(_kitchenPrinterComboBox),
            CashPrinterName = SelectedPrinterName(_cashPrinterComboBox),
            DeliveryPrinterName = SelectedPrinterName(_deliveryPrinterComboBox),
            UseEscPosForKitchen = _kitchenEscPosCheckBox.Checked,
            UseEscPosForCash = _cashEscPosCheckBox.Checked,
            UseEscPosForDelivery = _deliveryEscPosCheckBox.Checked
        };

    private void PrintKitchenTestSlip(string printerName)
    {
        using var document = new PrintDocument
        {
            PrinterSettings = { PrinterName = printerName },
            PrintController = new StandardPrintController()
        };

        document.PrintPage += (_, args) =>
        {
            using var titleFont = new Font("Consolas", 11F, FontStyle.Bold);
            using var bodyFont = new Font("Consolas", 9F, FontStyle.Regular);
            var graphics = args.Graphics;
            if (graphics is null)
            {
                return;
            }

            float top = 12F;
            graphics.DrawString("SAMER HUB TEST FISI", titleFont, Brushes.Black, 8F, top);
            top += 28F;
            graphics.DrawString($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}", bodyFont, Brushes.Black, 8F, top);
            top += 22F;
            graphics.DrawString("Mutfak yazici baglantisi dogrulandi.", bodyFont, Brushes.Black, 8F, top);
            top += 22F;
            graphics.DrawString("1 x Test Urunu", bodyFont, Brushes.Black, 8F, top);
            top += 22F;
            graphics.DrawString("Toplam: 0,00 TL", bodyFont, Brushes.Black, 8F, top);
            top += 28F;
            graphics.DrawString("Bu fis ilk kurulum sihirbazindan gonderildi.", bodyFont, Brushes.Black, 8F, top);
        };

        document.Print();
    }

    private async Task RunBusyActionAsync(string message, Func<CancellationToken, Task> action)
    {
        UseWaitCursor = true;
        UpdateStatus(message);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await action(cts.Token);
        }
        catch (Exception ex)
        {
            UpdateStatus(ex.Message);
            MessageBox.Show(ex.Message, "Yazici Ayarlari", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private static ComboBox CreatePrinterComboBox()
        => new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 260
        };

    private static CheckBox CreateEscPosCheckBox()
        => new()
        {
            AutoSize = true,
            Text = "ESC/POS"
        };

    private static Button CreateActionButton(string text)
        => new() { Text = text, Size = new Size(180, 36), Margin = new Padding(0, 0, 10, 0) };

    private static void AddPrinterRow(TableLayoutPanel table, int row, string label, Control combo, Control option)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 8, 12, 8),
            Text = label
        }, 0, row);
        combo.Margin = new Padding(0, 4, 0, 4);
        option.Margin = new Padding(8, 8, 0, 4);
        table.Controls.Add(combo, 1, row);
        table.Controls.Add(option, 2, row);
    }

    private static void BindPrinterCombo(ComboBox comboBox, IReadOnlyList<string> printers)
    {
        var current = SelectedPrinterName(comboBox);
        comboBox.Items.Clear();
        comboBox.Items.AddRange(printers.Cast<object>().ToArray());

        if (!string.IsNullOrWhiteSpace(current) && printers.Contains(current))
        {
            comboBox.SelectedItem = current;
        }
        else if (comboBox.Items.Count > 0 && comboBox.SelectedIndex < 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private static void SelectPrinter(ComboBox comboBox, string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return;
        }

        if (!comboBox.Items.Contains(printerName))
        {
            comboBox.Items.Add(printerName);
        }

        comboBox.SelectedItem = printerName;
    }

    private static string SelectedPrinterName(ComboBox comboBox)
        => comboBox.SelectedItem?.ToString()?.Trim() ?? string.Empty;
}
