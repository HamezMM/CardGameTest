using System.Windows;
using Gamma931.App.ViewModels;
using Gamma931.Core.Data;

namespace Gamma931.App;

public partial class MainWindow : Window
{
    public MainWindow(CardDatabase database)
    {
        InitializeComponent();
        DataContext = new MainViewModel(database);
    }
}
