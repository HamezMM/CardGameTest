using System.Windows;
using Gamma931.App.Services;
using Gamma931.Core.Data;

namespace Gamma931.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CardDatabase database;
        try
        {
            database = new CsvCardLoader().LoadFromDirectory(AppPaths.CardDataDirectory);
        }
        catch (CardDataException ex)
        {
            MessageBox.Show(
                $"Could not load card data:\n\n{ex.Message}\n\nExpected CSV files in:\n{AppPaths.CardDataDirectory}",
                "Gamma-931 — Card Data Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var mainWindow = new MainWindow(database);
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
