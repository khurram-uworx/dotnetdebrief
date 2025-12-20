using Microsoft.Extensions.Logging;
using System.Text;

static class Utils
{
    private static readonly ILoggerFactory _loggerFactory;

    static Utils()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    static async Task showSpinner(string msg, CancellationToken token)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var sequence = new[] { '◴', '◷', '◶', '◵' };

        int counter = 0;

        while (!token.IsCancellationRequested)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"{msg}... {sequence[counter % sequence.Length]}\t");
            counter++;
            await Task.Delay(200, token).ContinueWith(_ => { });
        }

        Console.WriteLine($"Done.\n");
    }

    public static ILogger GetAppLogger() =>
        _loggerFactory.CreateLogger("FoundryLocalSamples");

    public static async Task RunWithSpinner<T>(string msg, T workTask, bool warnOnException = true) where T : Task
    {
        // Start the spinner
        using var cts = new CancellationTokenSource();
        var spinnerTask = showSpinner(msg, cts.Token);

        try
        {
            await workTask;     // wait for the real work to finish
        }
        catch (Exception fex)
        {
            // we're only using this for EP registration currently an exception here is non-fatal as we have built-in
            // execution providers that can be used. in a production app you may want to handle this differently.
            if (warnOnException)
            {
                cts.Cancel();
                Console.WriteLine($"\nWarning: {fex.Message}");
                return;
            }

            throw;  // rethrow otherwise
        }

        cts.Cancel();       // stop the spinner
        await spinnerTask;  // wait for spinner to exit
    }
}
