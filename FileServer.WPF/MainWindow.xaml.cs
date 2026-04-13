using FileServer.WPF.ViewModels;
using System.Windows;

namespace FileServer.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}