using System.Windows;
namespace XR18BarControl;

public partial class UpdateWindow : Window
{
    public UpdateWindow(Version version, string? notes)
    {
        InitializeComponent();
        var message = $"Hay una versión nueva disponible (v{version}).\n¿Deseas instalarla ahora?";
        if (!string.IsNullOrWhiteSpace(notes)) message += $"\n\n{notes}";
        MessageText.Text = message;
    }

    private void Update_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Later_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
