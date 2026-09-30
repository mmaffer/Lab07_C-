using System.Windows;
using Biblioteca.WPF.Services;
using Biblioteca.WPF.Views;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF;

// Ventana principal: solo arma el menú, las notificaciones y el tema. El trabajo lo hacen las páginas.
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        UiServices.Initialize(SnackbarPresenter, RootDialogHost);

        Loaded += (s, e) =>
        {
            RootNavigation.Navigate(typeof(LibrosPage));
            ActualizarIconoTema();
        };
    }

    private void btnTema_Click(object sender, RoutedEventArgs e)
    {
        var siguiente = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        App.SetTheme(siguiente);
        ActualizarIconoTema();
    }

    private void ActualizarIconoTema()
    {
        bool oscuro = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
        btnTema.Icon = new SymbolIcon(oscuro ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24);
    }
}
