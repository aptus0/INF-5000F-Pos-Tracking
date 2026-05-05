namespace SamerHub.Desktop.Controls.Wizard;

public sealed class BusinessInfoStepControl : UserControl
{
    public BusinessInfoStepControl()
    {
        BackColor = Color.White;

        Controls.Add(UiFactory.CreateSubtitle(
            "Isletme adi, sube adi, yetkili kisi ve temel kurulum bilgileri bu adimda toplanacak.",
            new Point(0, 34)));
        Controls.Add(UiFactory.CreateTitle("1. Isletme Bilgileri", new Point(0, 0)));

        var section = UiFactory.CreateSection("Temel Bilgiler", new Point(0, 72), new Size(760, 300));
        var table = CreateTable();
        AddRow(table, 0, "Isletme Adi:");
        AddRow(table, 1, "Sube Adi:");
        AddRow(table, 2, "Yetkili Kisi:");
        AddRow(table, 3, "Telefon:");
        AddRow(table, 4, "Adres:");
        section.Controls.Add(table);

        Controls.Add(section);
    }

    private static TableLayoutPanel CreateTable()
    {
        return new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            Location = new Point(20, 30)
        };
    }

    private static void AddRow(TableLayoutPanel table, int rowIndex, string label)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 8, 14, 8),
            Text = label
        }, 0, rowIndex);
        table.Controls.Add(new TextBox
        {
            Width = 420,
            Margin = new Padding(0, 4, 0, 4)
        }, 1, rowIndex);
    }
}
