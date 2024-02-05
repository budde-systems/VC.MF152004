using System.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using ReaPiSharp;

namespace BrandprinterTest;

public partial class MainWindow : Window
{
    private readonly BrandPrinterHub _printerHub = new(new NullLogger<BrandPrinterHub>());
    
    private readonly BrandPrinter _printer1 = new()
    {
        Settings =
        {
            ConnectionString = "TCP://192.168.42.15:22171"
        }
    };

    private readonly BrandPrinter _printer2 = new()
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
            await Task.WhenAll(_printerHub.ConnectAsync(_printer1), _printerHub.ConnectAsync(_printer2));
        }
        catch (Exception exception)
        {
        }
    }
}