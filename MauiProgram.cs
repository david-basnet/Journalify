using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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
            // Create a "Journal" folder in Documents for easy access
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var journalFolder = Path.Combine(documentsPath, "JournalApp");
            
            // Create folder if it doesn't exist
            if (!Directory.Exists(journalFolder))
            {
                Directory.CreateDirectory(journalFolder);
            }
            
            var dbPath = Path.Combine(journalFolder, "journal.db3");
            return new JournalDatabase(dbPath);
        });

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
