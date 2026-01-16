using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;

namespace MauiApp1;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<JournalDatabase>(s =>
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var journalFolder = Path.Combine(documentsPath, "JournalApp");

            if (!Directory.Exists(journalFolder))
            {
                Directory.CreateDirectory(journalFolder);
            }

            var dbPath = Path.Combine(journalFolder, "journal.db3");
            return new JournalDatabase(dbPath);
        });

        builder.Services.AddScoped<AuthenticationService>(s =>
        {
            var journalDb = s.GetRequiredService<JournalDatabase>();
            return new AuthenticationService(journalDb);
        });

        builder.Services.AddScoped<PdfExportService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
