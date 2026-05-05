namespace SamerHub.Desktop.Controls;

internal static class UiFactory
{
    public static Label CreateTitle(string text, Point location)
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point, 162),
            Location = location,
            Text = text
        };
    }

    public static Label CreateSubtitle(string text, Point location)
    {
        return new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Location = location,
            MaximumSize = new Size(1100, 0),
            Text = text
        };
    }

    public static GroupBox CreateSection(string title, Point location, Size size)
    {
        return new GroupBox
        {
            Text = title,
            Location = location,
            Size = size
        };
    }

    public static Label CreateEmptyState(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ForeColor = SystemColors.GrayText,
            Text = text
        };
    }

    public static Button CreateMenuButton(string text, bool active = false)
    {
        return new Button
        {
            BackColor = active ? Color.FromArgb(33, 37, 41) : Color.White,
            FlatStyle = FlatStyle.Flat,
            ForeColor = active ? Color.White : Color.Black,
            Margin = new Padding(0, 0, 10, 0),
            Size = new Size(text.Length > 12 ? 140 : 110, 34),
            Text = text
        };
    }
}
