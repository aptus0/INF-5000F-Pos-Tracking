using Avalonia.Controls;
using Avalonia.Interactivity;
using SamerHub.Desktop.Avalonia.ViewModels;

namespace SamerHub.Desktop.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel Vm => (MainWindowViewModel)DataContext!;

    private async void Reload_Click(object? sender, RoutedEventArgs e) => await Vm.LoadAsync();

    private async void SaveSettings_Click(object? sender, RoutedEventArgs e) => await Vm.SaveSettingsAsync();

    private async void TestPosConnection_Click(object? sender, RoutedEventArgs e) => await Vm.TestPosAsync();

    private async void TestPosSale_Click(object? sender, RoutedEventArgs e) => await Vm.TestPosSaleAsync();

    private async void TestFiscal_Click(object? sender, RoutedEventArgs e) => await Vm.TestFiscalAsync();

    private async void TestDatabase_Click(object? sender, RoutedEventArgs e) => await Vm.TestDatabaseAsync();

    private async void CreateTable_Click(object? sender, RoutedEventArgs e) => await Vm.CreateTableAsync();

    private async void OpenTable_Click(object? sender, RoutedEventArgs e) => await Vm.OpenSelectedTableAsync();

    private async void AddProductToSession_Click(object? sender, RoutedEventArgs e) => await Vm.AddSelectedProductToSessionAsync();

    private async void SendKitchen_Click(object? sender, RoutedEventArgs e) => await Vm.SendKitchenAsync();

    private async void PayCash_Click(object? sender, RoutedEventArgs e) => await Vm.PayCashAsync();

    private async void PayCard_Click(object? sender, RoutedEventArgs e) => await Vm.PayCardAsync();

    private async void SaveProduct_Click(object? sender, RoutedEventArgs e) => await Vm.SaveProductAsync();

    private async void DetectLocalIp_Click(object? sender, RoutedEventArgs e) => await Vm.AutofillLocalIpAsync();

    private void OpenWizard_Click(object? sender, RoutedEventArgs e) => Vm.OpenWizard();
}
