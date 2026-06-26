using SamerHub.Desktop.Controls.Wizard;
using SamerHub.Desktop.Services;

namespace SamerHub.Desktop.Forms;

public sealed partial class FirstRunWizardForm : Form
{
    private readonly List<(string Title, UserControl Control)> _steps;
    private readonly ListBox _stepListBox;
    private readonly Panel _contentHostPanel;
    private readonly Label _stepTitleLabel;
    private readonly Label _stepSubtitleLabel;
    private readonly Button _backButton;
    private readonly Button _nextButton;
    private readonly Button _finishButton;

    private int _currentIndex;

    public FirstRunWizardForm()
    {
        Text = "SAMER Hub Ilk Kurulum Sihirbazi";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 760);
        ClientSize = new Size(1100, 760);
        BackColor = Color.WhiteSmoke;

        _steps =
        [
            ("Isletme Bilgileri", new BusinessInfoStepControl()),
            ("Ag ve PC IP", new NetworkSetupStepControl()),
            ("POS Ayarlari", new PosSetupStepControl()),
            ("POS Testi", new PosTestStepControl()),
            ("Yazici Ayarlari", new PrinterSetupStepControl()),
            ("Platform Baglantilari", new PlatformSetupStepControl()),
            ("Tamamlama", new CompletionStepControl())
        ];

        var headerPanel = new Panel
        {
            BackColor = Color.White,
            Dock = DockStyle.Top,
            Height = 86
        };
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point, 162),
            Location = new Point(24, 14),
            Text = "SAMER Hub Ilk Kurulum"
        });
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Location = new Point(28, 56),
            Text = "Programi kullanima hazir hale getirmek icin temel ayarlari adim adim tamamlayin."
        });

        var leftPanel = new Panel
        {
            BackColor = Color.White,
            Dock = DockStyle.Left,
            Padding = new Padding(16),
            Width = 260
        };

        leftPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 162),
            Location = new Point(8, 10),
            Text = "Kurulum Adimlari"
        });

        _stepListBox = new ListBox
        {
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 162),
            ItemHeight = 38,
            Location = new Point(8, 44),
            Size = new Size(228, 570)
        };
        _stepListBox.DrawItem += StepListBox_DrawItem;
        foreach (var step in _steps)
        {
            _stepListBox.Items.Add(step.Title);
        }
        leftPanel.Controls.Add(_stepListBox);

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        _stepTitleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 162),
            Location = new Point(20, 16)
        };

        _stepSubtitleLabel = new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Location = new Point(24, 52),
            MaximumSize = new Size(760, 0)
        };

        _contentHostPanel = new Panel
        {
            Location = new Point(20, 88),
            Size = new Size(780, 500),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            BackColor = Color.White
        };

        var skipCheckBox = new CheckBox
        {
            Location = new Point(20, 28),
            Size = new Size(300, 20),
            Text = "Bu sihirbazı bir daha gösterme",
            AutoSize = true
        };
        footerPanel.Controls.Add(skipCheckBox);

        _backButton = new Button
        {
            Location = new Point(620, 18),
            Size = new Size(100, 38),
            Text = "Geri"
        };
        _backButton.Click += (_, _) => NavigateTo(_currentIndex - 1);

        _nextButton = new Button
        {
            Location = new Point(730, 18),
            Size = new Size(100, 38),
            Text = "Ileri"
        };
        _nextButton.Click += (_, _) => NavigateTo(_currentIndex + 1);

        _finishButton = new Button
        {
            Location = new Point(840, 18),
            Size = new Size(130, 38),
            Text = "Kurulumu Bitir"
        };
        _finishButton.Click += (_, _) =>
        {
            AppSettings.SetSkipWizardOnStartup(skipCheckBox.Checked);
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            Location = new Point(980, 18),
            Size = new Size(90, 38),
            Text = "Kapat"
        };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footerPanel.Controls.Add(_backButton);
        footerPanel.Controls.Add(_nextButton);
        footerPanel.Controls.Add(_finishButton);
        footerPanel.Controls.Add(cancelButton);

        mainPanel.Controls.Add(_contentHostPanel);
        mainPanel.Controls.Add(_stepSubtitleLabel);
        mainPanel.Controls.Add(_stepTitleLabel);

        Controls.Add(mainPanel);
        Controls.Add(leftPanel);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);

        NavigateTo(0);
    }

    private void NavigateTo(int index)
    {
        if (index < 0 || index >= _steps.Count)
        {
            return;
        }

        _currentIndex = index;
        _stepListBox.SelectedIndex = index;
        _stepListBox.Invalidate();

        var (title, control) = _steps[index];
        _stepTitleLabel.Text = title;
        _stepSubtitleLabel.Text = $"Adim {index + 1} / {_steps.Count}";

        _contentHostPanel.Controls.Clear();
        control.Dock = DockStyle.Fill;
        _contentHostPanel.Controls.Add(control);

        if (control is Controls.Wizard.IWizardStep wizardStep)
        {
            _ = wizardStep.OnStepActivatedAsync();
        }

        _backButton.Enabled = index > 0;
        _nextButton.Enabled = index < _steps.Count - 1;
        _finishButton.Enabled = index == _steps.Count - 1;
    }

    private void StepListBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        var isSelected = e.Index == _currentIndex;
        using var backgroundBrush = new SolidBrush(isSelected ? Color.FromArgb(33, 37, 41) : Color.White);
        using var textBrush = new SolidBrush(isSelected ? Color.White : Color.Black);

        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
        e.Graphics.DrawString(
            $"{e.Index + 1}. {_steps[e.Index].Title}",
            e.Font ?? Font,
            textBrush,
            new RectangleF(e.Bounds.Left + 10, e.Bounds.Top + 9, e.Bounds.Width - 20, e.Bounds.Height - 18));
    }
}
