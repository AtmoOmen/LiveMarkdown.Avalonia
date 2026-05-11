using Avalonia.Controls;
using ClassicDiagnostics.Avalonia;

namespace LiveMarkdown.Avalonia.Demo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.AttachDevTools();
    }
}