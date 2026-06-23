using SamerHub.Core.Entities;
using SamerHub.Desktop.Services;

namespace SamerHub.Desktop.Controls.Wizard;

public sealed class PlatformSetupStepControl : UserControl, IWizardStep
{
    private readonly SamerHubApiClient _apiClient = new();
    private PlatformEditor _trendyolEditor = null!;
    private PlatformEditor _getirEditor = null!;
    private PlatformEditor _yemeksepetiEditor = null!;
    private Label _statusLabel = null!;

    public PlatformSetupStepControl()
    {
        BackColor = Color.White;

        Controls.Add(UiFactory.CreateSubtitle(
            "Trendyol Go, Getir ve Yemeksepeti API bilgileri kaydedilir ve baglanti dogrulanir.",
            new Point(0, 34)));
        Controls.Add(UiFactory.CreateTitle("6. Platform Baglantilari", new Point(0, 0)));

        BuildLayout();
    }

    public async Task OnStepActivatedAsync()
    {
        await LoadSettingsAsync();
    }

    private void BuildLayout()
    {
        var tabControl = new TabControl
        {
            Location = new Point(0, 72),
            Size = new Size(760, 380)
        };

        _trendyolEditor = CreatePlatformTab(tabControl, "Trendyol Go");
        _getirEditor = CreatePlatformTab(tabControl, "Getir");
        _yemeksepetiEditor = CreatePlatformTab(tabControl, "Yemeksepeti");

        var actionPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 466),
            Size = new Size(760, 42)
        };

        var saveButton = CreateActionButton("Platformlari Kaydet");
        var trendyolTestButton = CreateActionButton("Trendyol Test");
        var getirTestButton = CreateActionButton("Getir Test");
        var yemeksepetiTestButton = CreateActionButton("Yemeksepeti Test");

        saveButton.Click += async (_, _) => await SaveSettingsAsync();
        trendyolTestButton.Click += async (_, _) => await TestPlatformAsync("trendyolgo");
        getirTestButton.Click += async (_, _) => await TestPlatformAsync("getir");
        yemeksepetiTestButton.Click += async (_, _) => await TestPlatformAsync("yemeksepeti");

        actionPanel.Controls.Add(saveButton);
        actionPanel.Controls.Add(trendyolTestButton);
        actionPanel.Controls.Add(getirTestButton);
        actionPanel.Controls.Add(yemeksepetiTestButton);

        _statusLabel = UiFactory.CreateEmptyState("Platform ayarlari henuz yuklenmedi.");
        _statusLabel.Location = new Point(0, 514);
        _statusLabel.Size = new Size(760, 70);

        Controls.Add(tabControl);
        Controls.Add(actionPanel);
        Controls.Add(_statusLabel);
    }

    private PlatformEditor CreatePlatformTab(TabControl tabControl, string title)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White
        };

        var editor = new PlatformEditor();
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            Location = new Point(18, 18)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));

        editor.EnabledCheckBox = new CheckBox { AutoSize = true, Text = "Bu platform aktif" };
        editor.BaseUrlTextBox = CreateTextBox();
        editor.MerchantIdTextBox = CreateTextBox();
        editor.StoreIdTextBox = CreateTextBox();
        editor.ApiKeyTextBox = CreateTextBox();
        editor.ApiSecretTextBox = CreateTextBox();
        editor.ApiSecretTextBox.UseSystemPasswordChar = true;

        AddEditorRow(table, 0, "Durum:", editor.EnabledCheckBox);
        AddEditorRow(table, 1, "Base URL:", editor.BaseUrlTextBox);
        AddEditorRow(table, 2, "Merchant ID:", editor.MerchantIdTextBox);
        AddEditorRow(table, 3, "Store ID:", editor.StoreIdTextBox);
        AddEditorRow(table, 4, "API Key:", editor.ApiKeyTextBox);
        AddEditorRow(table, 5, "API Secret:", editor.ApiSecretTextBox);

        page.Controls.Add(table);
        tabControl.TabPages.Add(page);
        return editor;
    }

    private async Task LoadSettingsAsync()
    {
        await RunBusyActionAsync("Platform ayarlari yukleniyor...", async cancellationToken =>
        {
            var settings = await _apiClient.GetPlatformSettingsAsync(cancellationToken);
            ApplyEditor(_trendyolEditor, settings.TrendyolGo);
            ApplyEditor(_getirEditor, settings.Getir);
            ApplyEditor(_yemeksepetiEditor, settings.Yemeksepeti);
            UpdateStatus("Platform ayarlari yuklendi.");
        });
    }

    private async Task SaveSettingsAsync()
    {
        await RunBusyActionAsync("Platform ayarlari kaydediliyor...", async cancellationToken =>
        {
            await _apiClient.SavePlatformSettingsAsync(BuildSettings(), cancellationToken);
            UpdateStatus("Platform ayarlari kaydedildi.");
            MessageBox.Show("Platform ayarlari kaydedildi.", "Platform Baglantilari", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async Task TestPlatformAsync(string platformName)
    {
        await RunBusyActionAsync($"{platformName} baglantisi test ediliyor...", async cancellationToken =>
        {
            await _apiClient.SavePlatformSettingsAsync(BuildSettings(), cancellationToken);
            var result = await _apiClient.TestPlatformAsync(platformName, cancellationToken);
            UpdateStatus($"{DisplayName(platformName)}: {result.Message}");
            MessageBox.Show(
                $"{DisplayName(platformName)}\nSonuc: {(result.IsSuccessful ? "Basarili" : "Basarisiz")}\nHTTP: {(result.HttpStatusCode?.ToString() ?? "-")}\nMesaj: {result.Message}",
                "Platform Testi",
                MessageBoxButtons.OK,
                result.IsSuccessful ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        });
    }

    private PlatformApiSettings BuildSettings()
        => new()
        {
            TrendyolGo = BuildConnection(_trendyolEditor),
            Getir = BuildConnection(_getirEditor),
            Yemeksepeti = BuildConnection(_yemeksepetiEditor)
        };

    private static PlatformConnectionSettings BuildConnection(PlatformEditor editor)
        => new()
        {
            IsEnabled = editor.EnabledCheckBox.Checked,
            BaseUrl = editor.BaseUrlTextBox.Text.Trim(),
            MerchantId = editor.MerchantIdTextBox.Text.Trim(),
            StoreId = editor.StoreIdTextBox.Text.Trim(),
            ApiKey = editor.ApiKeyTextBox.Text.Trim(),
            ApiSecret = editor.ApiSecretTextBox.Text
        };

    private static void ApplyEditor(PlatformEditor editor, PlatformConnectionSettings settings)
    {
        editor.EnabledCheckBox.Checked = settings.IsEnabled;
        editor.BaseUrlTextBox.Text = settings.BaseUrl;
        editor.MerchantIdTextBox.Text = settings.MerchantId;
        editor.StoreIdTextBox.Text = settings.StoreId;
        editor.ApiKeyTextBox.Text = settings.ApiKey;
        editor.ApiSecretTextBox.Text = settings.ApiSecret;
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
            MessageBox.Show(ex.Message, "Platform Baglantilari", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private static TextBox CreateTextBox()
        => new() { Width = 360 };

    private static Button CreateActionButton(string text)
        => new() { Text = text, Size = new Size(160, 36), Margin = new Padding(0, 0, 10, 0) };

    private static void AddEditorRow(TableLayoutPanel table, int row, string label, Control control)
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

    private static string DisplayName(string platformName)
        => platformName.ToLowerInvariant() switch
        {
            "trendyolgo" => "Trendyol Go",
            "getir" => "Getir",
            "yemeksepeti" => "Yemeksepeti",
            _ => platformName
        };

    private sealed class PlatformEditor
    {
        public CheckBox EnabledCheckBox { get; set; } = null!;
        public TextBox BaseUrlTextBox { get; set; } = null!;
        public TextBox MerchantIdTextBox { get; set; } = null!;
        public TextBox StoreIdTextBox { get; set; } = null!;
        public TextBox ApiKeyTextBox { get; set; } = null!;
        public TextBox ApiSecretTextBox { get; set; } = null!;
    }
}
