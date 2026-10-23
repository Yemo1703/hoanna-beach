using System.Windows;
using XR18BarControl.Services;
namespace XR18BarControl;

public partial class UpdateWindow : Window
{
    private readonly Func<IProgress<UpdateProgress>, Task<bool>> _install;

    public UpdateWindow(Version version, string? notes, Func<IProgress<UpdateProgress>, Task<bool>> install)
    {
        InitializeComponent();
        _install = install;
        var message = $"Hay una versión nueva disponible (v{version}).\n¿Deseas instalarla ahora?";
        if (!string.IsNullOrWhiteSpace(notes)) message += $"\n\n{notes}";
        MessageText.Text = message;
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        ButtonsPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;
        TitleText.Text = "DESCARGANDO ACTUALIZACIÓN";
        MessageText.Text = "Puedes esperar aquí. La aplicación se cerrará únicamente cuando el instalador esté preparado.";
        var progress = new Progress<UpdateProgress>(value =>
        {
            DownloadProgress.IsIndeterminate = value.Percent is null;
            if (value.Percent is double percent) DownloadProgress.Value = percent;
            ProgressText.Text = value.Message;
        });
        if (await _install(progress))
        {
            TitleText.Text = "INSTALANDO ACTUALIZACIÓN";
            MessageText.Text = "La aplicación se cerrará y volverá a abrir automáticamente con la versión nueva.";
            DownloadProgress.IsIndeterminate = true;
            ProgressText.Text = "Iniciando instalador…";
            await Task.Delay(900);
            DialogResult = true;
            return;
        }
        TitleText.Text = "NO SE PUDO ACTUALIZAR";
        MessageText.Text = "La descarga o el instalador fallaron. La aplicación seguirá abierta; puedes intentarlo de nuevo más tarde.";
        DownloadProgress.IsIndeterminate = false;
        ProgressText.Text = "Actualización cancelada";
        ButtonsPanel.Visibility = Visibility.Visible;
    }
    private void Later_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
