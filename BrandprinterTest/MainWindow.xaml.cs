using System.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using ReaPiSharp;

namespace BrandprinterTest;

public partial class MainWindow : Window
{
    private readonly BrandPrinterHub _printerHub = new(new NullLogger<BrandPrinterHub>());
    private readonly BrandPrinter _printer = new()
    {
        Settings =
        {
            ConnectionString = "TCP://192.168.42.15:22171"
        }
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void btnConnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _printerHub.ConnectAsync(_printer);
        }
        catch (Exception exception)
        {
        }
    }
}