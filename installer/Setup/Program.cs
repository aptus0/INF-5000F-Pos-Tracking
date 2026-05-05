using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SamerHubSetup
{
    static class Program
    {
        private static string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SAMER Hub", "Logs", $"Setup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        [STAThread]
        static void Main()
        {
            try
            {
                // Klasör oluştur
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath));

                // Check admin
                if (!IsAdministrator())
                {
                    MessageBox.Show(
                        "Bu kurulum yönetici ayrıcalıkları gerektirir.\n\nLütfen Setup.exe'ye sağ tıkla → 'Yönetici Olarak Çalıştır'",
                        "Yönetici Gerekli",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new SetupForm());
            }
            catch (Exception ex)
            {
                LogError("Main", ex);
                MessageBox.Show(
                    $"Kurulum başlatılamadı: {ex.Message}",
                    "Kritik Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        static bool IsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void LogError(string method, Exception ex)
        {
            try
            {
                File.AppendAllText(_logPath, 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR in {method}: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
            catch { }
        }

        private static void LogInfo(string message)
        {
            try
            {
                File.AppendAllText(_logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\r\n");
            }
            catch { }
        }
    }

    public class SetupForm : Form
    {
        private Label titleLabel;
        private Label statusLabel;
        private ProgressBar progressBar;
        private Button nextButton;
        private Button cancelButton;
        private Button showLogsButton;
        private Panel mainPanel;
        private TextBox installPathTextBox;
        private ListBox fileListBox;

        private const string ServiceName = "SamerHubService";
        private const string ServiceDisplayName = "SAMER Hub Service";

        private string _installPath = @"C:\Program Files\SAMER Hub";
        private string _sourceServiceDir;
        private string _sourceDesktopDir;
        private string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SAMER Hub", "Logs", $"Setup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        private int currentStep = 0;

        public SetupForm()
        {
            InitializeComponent();
            CenterToScreen();
            StartPosition = FormStartPosition.CenterScreen;

            // Klasör oluştur
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath));
            LogInfo("Kurulum başlatıldı");
        }

        private void InitializeComponent()
        {
            Text = "SAMER Hub Kurulumu";
            Width = 650;
            Height = 500;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // Logo yükleme
            try
            {
                string logoPath = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath),
                    @"..\..\publish\desktop\logo.ico");
                logoPath = Path.GetFullPath(logoPath);
                
                if (File.Exists(logoPath))
                {
                    Icon = new Icon(logoPath);
                }
            }
            catch { }

            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };

            titleLabel = new Label
            {
                Text = "SAMER Hub'ı Yüklemek İçin Hoş Geldiniz",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = false,
                Height = 40,
                Dock = DockStyle.Top,
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            statusLabel = new Label
            {
                Text = "Kurulum başlamak için 'İleri' tuşuna tıklayın.\n\nKurulum Klasörü:",
                AutoSize = false,
                Height = 60,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 10, 0, 10)
            };

            installPathTextBox = new TextBox
            {
                Text = _installPath,
                Dock = DockStyle.Top,
                Height = 30,
                Margin = new Padding(0, 5, 0, 10)
            };

            fileListBox = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 100,
                Visible = false,
                SelectionMode = SelectionMode.None
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 30,
                Margin = new Padding(0, 10, 0, 0),
                Visible = false
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10)
            };

            nextButton = new Button
            {
                Text = "İleri",
                Width = 100,
                Height = 35,
                Location = new Point(415, 15),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            nextButton.Click += NextButton_Click;

            cancelButton = new Button
            {
                Text = "İptal",
                Width = 100,
                Height = 35,
                Location = new Point(515, 15),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            cancelButton.Click += (s, e) =>
            {
                if (MessageBox.Show("Kurmayı iptal etmek istediğinize emin misiniz?", "Onayla",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    LogInfo("Kurulum kullanıcı tarafından iptal edildi");
                    Close();
                }
            };

            showLogsButton = new Button
            {
                Text = "Logları Göster",
                Width = 100,
                Height = 35,
                Location = new Point(10, 15),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10),
                Visible = false
            };
            showLogsButton.Click += (s, e) => Process.Start("notepad.exe", _logPath);

            mainPanel.Controls.Add(progressBar);
            mainPanel.Controls.Add(fileListBox);
            mainPanel.Controls.Add(installPathTextBox);
            mainPanel.Controls.Add(statusLabel);
            mainPanel.Controls.Add(titleLabel);

            buttonPanel.Controls.Add(showLogsButton);
            buttonPanel.Controls.Add(nextButton);
            buttonPanel.Controls.Add(cancelButton);

            Controls.Add(mainPanel);
            Controls.Add(buttonPanel);
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            nextButton.Enabled = false;
            cancelButton.Enabled = false;

            try
            {
                switch (currentStep)
                {
                    case 0: // Klasör seçimi
                        _installPath = installPathTextBox.Text;
                        installPathTextBox.ReadOnly = true;
                        UpdateUI("Sistem Kontrol Ediliyor...", "", 0);
                        if (CheckSystem())
                        {
                            currentStep++;
                            UpdateUI("Kontrol Tamam", "Yerel veritabani hazirlanacak.", 10);
                            nextButton.Text = "Kuruluma Başla";
                            nextButton.Enabled = true;
                        }
                        break;

                    case 1: // Yerel veritabani hazirlama
                        UpdateUI("Yerel Veritabani Hazirlaniyor...", "", 30);
                        if (PrepareLocalDatabase())
                        {
                            currentStep++;
                            UpdateUI("Yerel Veritabani Hazir", "Dosyalar kuruluma baslanacak.", 35);
                            nextButton.Enabled = true;
                        }
                        break;

                    case 2: // Dosyalar
                        UpdateUI("Dosyalar Kuruluyor...", "", 30);
                        fileListBox.Visible = true;
                        progressBar.Visible = true;
                        if (InstallFiles())
                        {
                            currentStep++;
                            UpdateUI("Kısayollar Oluşturuluyor...", "", 80);
                            nextButton.Enabled = true;
                        }
                        break;

                    case 3: // Kısayollar
                        UpdateUI("Kısayollar Oluşturuluyor...", "", 85);
                        if (CreateShortcuts())
                        {
                            currentStep++;
                            UpdateUI("Servis Kurulması...", "", 90);
                            nextButton.Enabled = true;
                        }
                        break;

                    case 4: // Servis kurulumu
                        UpdateUI("Servis Kurulması...", "", 95);
                        if (InstallService())
                        {
                            currentStep++;
                            UpdateUI("✅ Kurulum Tamamlandı!",
                                $"SAMER Hub başarıyla kuruldu.\n\n" +
                                $"✓ Dosyalar: {_installPath}\n" +
                                $"✓ Runtime: Uygulama icinde hazir (self-contained)\n" +
                                $"✓ Veritabani: Yerel SQLite (arka planda hazir)\n" +
                                $"✓ Harici MySQL: Istenirse sonradan baglanabilir\n" +
                                $"✓ Masaüstü kısayolu\n" +
                                $"✓ Start Menu kısayolu\n" +
                                $"✓ Windows Servisi\n\nHazırdasınız!",
                                100);
                            nextButton.Text = "Bitir";
                            nextButton.Enabled = true;
                            cancelButton.Enabled = true;
                            showLogsButton.Visible = true;
                        }
                        break;

                    case 5: // Bitir
                        LogInfo("Kurulum başarıyla tamamlandı");
                        Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError("NextButton_Click", ex);
                MessageBox.Show(
                    $"Hata oluştu: {ex.Message}",
                    "Kurulum Hatalı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
            }
        }

        private void UpdateUI(string title, string status, int progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateUI(title, status, progress)));
                return;
            }

            titleLabel.Text = title;
            if (!string.IsNullOrEmpty(status))
            {
                statusLabel.Text = status;
            }
            progressBar.Value = Math.Min(progress, 100);
            Application.DoEvents();
        }

        private void LogInfo(string message)
        {
            try
            {
                File.AppendAllText(_logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\r\n");
            }
            catch { }
        }

        private void LogError(string method, Exception ex)
        {
            try
            {
                File.AppendAllText(_logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR in {method}: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
            catch { }
        }

        private bool CheckSystem()
        {
            try
            {
                LogInfo("Sistem kontrol başlatıldı");

                // Kaynak klasörleri bul
                string baseDir = Path.GetDirectoryName(Application.ExecutablePath) ?? AppContext.BaseDirectory;
                _sourceServiceDir = Path.Combine(baseDir, @"..\service");
                _sourceDesktopDir = Path.Combine(baseDir, @"..\desktop");

                _sourceServiceDir = Path.GetFullPath(_sourceServiceDir);
                _sourceDesktopDir = Path.GetFullPath(_sourceDesktopDir);

                if (!Directory.Exists(_sourceServiceDir) || !Directory.Exists(_sourceDesktopDir))
                {
                    LogError("CheckSystem", new Exception("Publish klasörleri bulunamadı"));
                    MessageBox.Show(
                        $"Publish dosyaları bulunamadı.\n\nBeklenen:\n{_sourceServiceDir}\n{_sourceDesktopDir}",
                        "Dosya Bulunamadı",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                if (!File.Exists(Path.Combine(_sourceServiceDir, "SamerHub.Service.exe")))
                {
                    LogError("CheckSystem", new Exception("Service exe bulunamadı"));
                    MessageBox.Show(
                        "SamerHub.Service.exe bulunamadı!",
                        "Hata",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                if (!File.Exists(Path.Combine(_sourceDesktopDir, "SamerHub.Desktop.Avalonia.exe")))
                {
                    LogError("CheckSystem", new Exception("Desktop exe bulunamadı"));
                    MessageBox.Show(
                        "SamerHub.Desktop.Avalonia.exe bulunamadı!",
                        "Hata",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                LogInfo("Sistem kontrol başarıyla tamamlandı");
                return true;
            }
            catch (Exception ex)
            {
                LogError("CheckSystem", ex);
                MessageBox.Show(
                    $"Kontrol başarısız: {ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private bool PrepareLocalDatabase()
        {
            try
            {
                LogInfo("Yerel SQLite veritabani hazirlaniyor");

                string dataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SAMER Hub");
                Directory.CreateDirectory(dataPath);
                Directory.CreateDirectory(Path.Combine(dataPath, "Logs"));
                Directory.CreateDirectory(Path.Combine(dataPath, "Backups"));
                Directory.CreateDirectory(Path.Combine(dataPath, "Receipts"));

                string sqlitePath = Path.Combine(dataPath, "samerhub.db");
                if (!File.Exists(sqlitePath))
                {
                    using (File.Create(sqlitePath))
                    {
                    }
                }

                LogInfo("Yerel SQLite veritabani hazir");
                return true;
            }
            catch (Exception ex)
            {
                LogError("PrepareLocalDatabase", ex);
                MessageBox.Show(
                    $"Yerel veritabani hazirlanirken hata: {ex.Message}\n\nLoglari kontrol edin.",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private bool InstallFiles()
        {
            try
            {
                LogInfo($"Dosyalar {_installPath} konumuna kurulmaya başlandı");

                // Kurulum klasörü oluştur
                Directory.CreateDirectory(_installPath);

                // ProgramData klasörleri oluştur
                string dataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SAMER Hub");
                Directory.CreateDirectory(Path.Combine(dataPath, "Logs"));
                Directory.CreateDirectory(Path.Combine(dataPath, "Backups"));
                Directory.CreateDirectory(Path.Combine(dataPath, "Receipts"));

                // Service dosyalarını kopyala
                fileListBox.Items.Add("→ Service dosyaları kopyalanıyor...");
                Application.DoEvents();

                string serviceDest = Path.Combine(_installPath, "service");
                CopyDirectory(_sourceServiceDir, serviceDest);
                progressBar.Value = 50;
                Application.DoEvents();

                LogInfo("Service dosyaları kopyalandı");

                // Desktop dosyalarını kopyala
                fileListBox.Items.Add("→ Desktop dosyaları kopyalanıyor...");
                Application.DoEvents();

                string desktopDest = Path.Combine(_installPath, "desktop");
                CopyDirectory(_sourceDesktopDir, desktopDest);
                progressBar.Value = 70;
                Application.DoEvents();

                LogInfo("Desktop dosyaları kopyalandı");

                fileListBox.Items.Add("✓ Tüm dosyalar kopyalandı");
                progressBar.Value = 75;
                Application.DoEvents();

                return true;
            }
            catch (Exception ex)
            {
                LogError("InstallFiles", ex);
                MessageBox.Show(
                    $"Dosyalar kurulamadı: {ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private void CopyDirectory(string source, string destination)
        {
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }

            var dir = new DirectoryInfo(source);
            Directory.CreateDirectory(destination);

            foreach (var file in dir.GetFiles())
            {
                file.CopyTo(Path.Combine(destination, file.Name), true);
            }

            foreach (var subdir in dir.GetDirectories())
            {
                CopyDirectory(subdir.FullName, Path.Combine(destination, subdir.Name));
            }
        }

        private bool CreateShortcuts()
        {
            try
            {
                LogInfo("Kısayollar oluşturulmaya başlandı");

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string desktopExePath = Path.Combine(_installPath, "desktop", "SamerHub.Desktop.Avalonia.exe");

                // Desktop kısayolu
                string desktopShortcut = Path.Combine(desktopPath, "SAMER Hub.lnk");
                CreateShortcut(desktopShortcut, desktopExePath, _installPath);

                // Start Menu kısayolu
                string startMenuPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    "SAMER Hub");
                Directory.CreateDirectory(startMenuPath);

                string startMenuShortcut = Path.Combine(startMenuPath, "SAMER Hub.lnk");
                CreateShortcut(startMenuShortcut, desktopExePath, _installPath);

                LogInfo("Kısayollar başarıyla oluşturuldu");
                progressBar.Value = 85;
                Application.DoEvents();

                return true;
            }
            catch (Exception ex)
            {
                LogError("CreateShortcuts", ex);
                MessageBox.Show(
                    $"Kısayollar oluşturulamadı: {ex.Message}",
                    "Uyarı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return true; // Yine de devam et
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
        {
            try
            {
                var escapedShortcutPath = shortcutPath.Replace("'", "''");
                var escapedTargetPath = targetPath.Replace("'", "''");
                var escapedWorkingDirectory = workingDirectory.Replace("'", "''");

                var command =
                    "$WshShell = New-Object -ComObject WScript.Shell; " +
                    $"$Shortcut = $WshShell.CreateShortcut('{escapedShortcutPath}'); " +
                    $"$Shortcut.TargetPath = '{escapedTargetPath}'; " +
                    $"$Shortcut.WorkingDirectory = '{escapedWorkingDirectory}'; " +
                    $"$Shortcut.IconLocation = '{escapedTargetPath},0'; " +
                    "$Shortcut.Save();";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                LogError("CreateShortcut", ex);
            }
        }

        private bool InstallService()
        {
            try
            {
                LogInfo("Windows Servisi kurulumu başlatıldı");

                string servicePath = Path.Combine(_installPath, "service", "SamerHub.Service.exe");

                // Servisi kur
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"create {ServiceName} binPath= \"{servicePath}\" DisplayName= \"{ServiceDisplayName}\" start= auto",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                }

                // Servisi başlat
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = $"start {ServiceName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }

                LogInfo("Windows Servisi başarıyla kuruldu ve başlatıldı");
                progressBar.Value = 95;
                Application.DoEvents();

                return true;
            }
            catch (Exception ex)
            {
                LogError("InstallService", ex);
                MessageBox.Show(
                    $"Servis kurulamadı: {ex.Message}\n\nSizin kendiniz 'services.msc' üzerinden yapabilirsiniz.",
                    "Uyarı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return true; // Yine de devam et
            }
        }
    }
}
