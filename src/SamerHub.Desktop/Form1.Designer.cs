namespace SamerHub.Desktop;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private Panel headerPanel;
    private PictureBox logoPictureBox;
    private Label titleLabel;
    private Label subtitleLabel;
    private Panel menuPanel;
    private FlowLayoutPanel menuFlowPanel;
    private Button dashboardButton;
    private Button tablesButton;
    private Button productsButton;
    private Button settingsButton;
    private Panel contentPanel;
    private Panel contentHostPanel;
    private StatusStrip footerStatusStrip;
    private ToolStripStatusLabel posStatusLabel;
    private ToolStripStatusLabel internetStatusLabel;
    private ToolStripStatusLabel printerStatusLabel;
    private ToolStripStatusLabel serviceStatusLabel;
    private ToolStripStatusLabel userStatusLabel;
    private ToolStripStatusLabel branchStatusLabel;
    private ToolStripStatusLabel springStatusLabel;
    private ToolStripStatusLabel clockStatusLabel;
    private System.Windows.Forms.Timer footerTimer;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        headerPanel = new Panel();
        logoPictureBox = new PictureBox();
        subtitleLabel = new Label();
        titleLabel = new Label();
        menuPanel = new Panel();
        menuFlowPanel = new FlowLayoutPanel();
        dashboardButton = new Button();
        tablesButton = new Button();
        productsButton = new Button();
        settingsButton = new Button();
        contentPanel = new Panel();
        contentHostPanel = new Panel();
        footerStatusStrip = new StatusStrip();
        posStatusLabel = new ToolStripStatusLabel();
        internetStatusLabel = new ToolStripStatusLabel();
        printerStatusLabel = new ToolStripStatusLabel();
        serviceStatusLabel = new ToolStripStatusLabel();
        userStatusLabel = new ToolStripStatusLabel();
        branchStatusLabel = new ToolStripStatusLabel();
        springStatusLabel = new ToolStripStatusLabel();
        clockStatusLabel = new ToolStripStatusLabel();
        footerTimer = new System.Windows.Forms.Timer(components);
        ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
        headerPanel.SuspendLayout();
        menuPanel.SuspendLayout();
        menuFlowPanel.SuspendLayout();
        contentPanel.SuspendLayout();
        footerStatusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.White;
        headerPanel.Controls.Add(logoPictureBox);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Name = "headerPanel";
        headerPanel.Padding = new Padding(24, 16, 24, 12);
        headerPanel.Size = new Size(1262, 86);
        headerPanel.TabIndex = 0;
        // 
        // logoPictureBox
        // 
        logoPictureBox.Location = new Point(24, 16);
        logoPictureBox.Name = "logoPictureBox";
        logoPictureBox.Size = new Size(46, 46);
        logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        logoPictureBox.TabIndex = 2;
        logoPictureBox.TabStop = false;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.ForeColor = SystemColors.GrayText;
        subtitleLabel.Location = new Point(78, 50);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(311, 20);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "Kasa, masa, paket servis ve POS yonetimi";
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point, 162);
        titleLabel.Location = new Point(76, 7);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(248, 46);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "SAMER Hub";
        // 
        // menuPanel
        // 
        menuPanel.BackColor = Color.FromArgb(245, 247, 250);
        menuPanel.Controls.Add(menuFlowPanel);
        menuPanel.Dock = DockStyle.Top;
        menuPanel.Location = new Point(0, 86);
        menuPanel.Name = "menuPanel";
        menuPanel.Padding = new Padding(20, 8, 20, 8);
        menuPanel.Size = new Size(1262, 58);
        menuPanel.TabIndex = 1;
        // 
        // menuFlowPanel
        // 
        menuFlowPanel.Controls.Add(dashboardButton);
        menuFlowPanel.Controls.Add(tablesButton);
        menuFlowPanel.Controls.Add(productsButton);
        menuFlowPanel.Controls.Add(settingsButton);
        menuFlowPanel.Dock = DockStyle.Fill;
        menuFlowPanel.Location = new Point(20, 10);
        menuFlowPanel.Name = "menuFlowPanel";
        menuFlowPanel.Size = new Size(1222, 40);
        menuFlowPanel.TabIndex = 0;
        // 
        // dashboardButton
        // 
        dashboardButton.BackColor = Color.FromArgb(33, 37, 41);
        dashboardButton.FlatAppearance.BorderSize = 0;
        dashboardButton.FlatStyle = FlatStyle.Flat;
        dashboardButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 162);
        dashboardButton.ForeColor = Color.White;
        dashboardButton.Location = new Point(3, 3);
        dashboardButton.Name = "dashboardButton";
        dashboardButton.Size = new Size(110, 34);
        dashboardButton.TabIndex = 0;
        dashboardButton.Text = "Dashboard";
        dashboardButton.UseVisualStyleBackColor = false;
        // 
        // tablesButton
        // 
        tablesButton.BackColor = Color.White;
        tablesButton.FlatStyle = FlatStyle.Flat;
        tablesButton.Location = new Point(119, 3);
        tablesButton.Name = "tablesButton";
        tablesButton.Size = new Size(110, 34);
        tablesButton.TabIndex = 1;
        tablesButton.Text = "Masalar";
        tablesButton.UseVisualStyleBackColor = false;
        // 
        // productsButton
        // 
        productsButton.BackColor = Color.White;
        productsButton.FlatStyle = FlatStyle.Flat;
        productsButton.Location = new Point(235, 3);
        productsButton.Name = "productsButton";
        productsButton.Size = new Size(110, 34);
        productsButton.TabIndex = 2;
        productsButton.Text = "Urunler";
        productsButton.UseVisualStyleBackColor = false;
        // 
        // settingsButton
        // 
        settingsButton.BackColor = Color.White;
        settingsButton.FlatStyle = FlatStyle.Flat;
        settingsButton.Location = new Point(351, 3);
        settingsButton.Name = "settingsButton";
        settingsButton.Size = new Size(110, 34);
        settingsButton.TabIndex = 3;
        settingsButton.Text = "Ayarlar";
        settingsButton.UseVisualStyleBackColor = false;
        // 
        // contentPanel
        // 
        contentPanel.BackColor = Color.WhiteSmoke;
        contentPanel.Controls.Add(contentHostPanel);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(0, 144);
        contentPanel.Name = "contentPanel";
        contentPanel.Padding = new Padding(24);
        contentPanel.Size = new Size(1262, 567);
        contentPanel.TabIndex = 2;
        // 
        // contentHostPanel
        // 
        contentHostPanel.Dock = DockStyle.Fill;
        contentHostPanel.Location = new Point(24, 24);
        contentHostPanel.Name = "contentHostPanel";
        contentHostPanel.Size = new Size(1214, 515);
        contentHostPanel.TabIndex = 0;
        // 
        // footerStatusStrip
        // 
        footerStatusStrip.ImageScalingSize = new Size(20, 20);
        footerStatusStrip.Items.AddRange(new ToolStripItem[] { posStatusLabel, internetStatusLabel, printerStatusLabel, serviceStatusLabel, userStatusLabel, branchStatusLabel, springStatusLabel, clockStatusLabel });
        footerStatusStrip.Location = new Point(0, 711);
        footerStatusStrip.Name = "footerStatusStrip";
        footerStatusStrip.Size = new Size(1262, 26);
        footerStatusStrip.TabIndex = 3;
        // 
        // posStatusLabel
        // 
        posStatusLabel.ForeColor = Color.ForestGreen;
        posStatusLabel.Name = "posStatusLabel";
        posStatusLabel.Size = new Size(82, 20);
        posStatusLabel.Text = "POS: Bekleniyor";
        // 
        // internetStatusLabel
        // 
        internetStatusLabel.ForeColor = Color.ForestGreen;
        internetStatusLabel.Name = "internetStatusLabel";
        internetStatusLabel.Size = new Size(100, 20);
        internetStatusLabel.Text = "Internet: Kontrol";
        // 
        // printerStatusLabel
        // 
        printerStatusLabel.ForeColor = Color.ForestGreen;
        printerStatusLabel.Name = "printerStatusLabel";
        printerStatusLabel.Size = new Size(97, 20);
        printerStatusLabel.Text = "Yazici: Bekliyor";
        // 
        // serviceStatusLabel
        // 
        serviceStatusLabel.ForeColor = Color.Goldenrod;
        serviceStatusLabel.Name = "serviceStatusLabel";
        serviceStatusLabel.Size = new Size(92, 20);
        serviceStatusLabel.Text = "Servis: Bagli";
        // 
        // userStatusLabel
        // 
        userStatusLabel.Name = "userStatusLabel";
        userStatusLabel.Size = new Size(112, 20);
        userStatusLabel.Text = "Kullanici: -";
        // 
        // branchStatusLabel
        // 
        branchStatusLabel.Name = "branchStatusLabel";
        branchStatusLabel.Size = new Size(100, 20);
        branchStatusLabel.Text = "Sube: -";
        // 
        // springStatusLabel
        // 
        springStatusLabel.Name = "springStatusLabel";
        springStatusLabel.Size = new Size(664, 20);
        springStatusLabel.Spring = true;
        // 
        // clockStatusLabel
        // 
        clockStatusLabel.Name = "clockStatusLabel";
        clockStatusLabel.Size = new Size(0, 20);
        // 
        // footerTimer
        // 
        footerTimer.Interval = 1000;
        footerTimer.Tick += footerTimer_Tick;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(1262, 737);
        Controls.Add(contentPanel);
        Controls.Add(menuPanel);
        Controls.Add(headerPanel);
        Controls.Add(footerStatusStrip);
        MinimumSize = new Size(1280, 784);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SAMER Hub Desktop";
        ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        menuPanel.ResumeLayout(false);
        menuFlowPanel.ResumeLayout(false);
        contentPanel.ResumeLayout(false);
        footerStatusStrip.ResumeLayout(false);
        footerStatusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
