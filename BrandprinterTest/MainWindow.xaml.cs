using System.Windows;
using System.Windows.Controls;

namespace BrandprinterTest;

public partial class MainWindow : Window
{
    private readonly BrandPrinterHub _printerHub = new();
    
    private readonly BrandPrinter _printer1 = new()
    {
        Name = "P1",

        Settings =
        {
            ConnectionString = "TCP://192.168.42.14:22171",
            JobFile = "vicampo_front.job",
            Group = "1",
            Object = "Bild_Standard",
            Content = "Choice_1"
        }
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void btnPrint3_Click(object sender, RoutedEventArgs e)
    {
        ((Button)sender).IsEnabled = false;

        try
        {
            await _printerHub.Print(_printer1, "3");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ((Button)sender).IsEnabled = true;
        }
    }

    private async void btnPrint5_Click(object sender, RoutedEventArgs e)
    {
        ((Button)sender).IsEnabled = false;

        try
        {
            await _printerHub.Print(_printer1, "5");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ((Button)sender).IsEnabled = true;
        }
    }
}