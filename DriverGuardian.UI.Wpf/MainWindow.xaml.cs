using DriverGuardian.UI.Wpf.ViewModels;

namespace DriverGuardian.UI.Wpf;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
