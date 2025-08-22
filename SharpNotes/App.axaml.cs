using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SharpNotes.Database;
using SharpNotes.Services;
using SharpNotes.ViewModels;
using SharpNotes.Views;
using System;
using System.Linq;

namespace SharpNotes;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        string connectionString = "Data Source=localhost;Initial Catalog=JiPP4_SharpNotes;User Id=sa;Password=JiPP4@Passw0rd;Encrypt=False;";
        DatabaseSetup.ConfigureServices(services, connectionString);

        services.AddSingleton<NotesService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TagsViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        try
        {
            DatabaseSetup.InitializeDatabase(_serviceProvider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas inicjalizacji bazy danych: {ex.Message}");
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}