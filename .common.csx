#r "nuget: BookStackApiClient, 25.5.0-lib.1"
#r "nuget: SkiaSharp, 3.119.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly, 0.83.0"
#nullable enable
using System.Threading;
using BookStackApiClient;
using Kokuban;
using Lestaly;
using SkiaSharp;

/// <summary>API Access Information</summary>
/// <param name="ApiEntry">API base address</param>
/// <param name="Token">API Token ID</param>
/// <param name="Secret">API Token Secret</param>
public record ApiKeyInfo(Uri ApiEntry, string Token, string Secret);

/// <summary>
/// API Key Storage Management
/// </summary>
public class ApiKeyStore
{
    /// <summary>Default stored file name</summary>
    public static string DefaultStoreFileName { get; } = ".api-key.sav";

    /// <summary>API Key Information</summary>
    public ApiKeyInfo Key { get; }

    /// <summary>API Entry address</summary>
    public Uri ApiEntry => this.Key.ApiEntry;

    /// <summary>API key scramble save file.</summary>
    public FileInfo ScrambleFile { get; }

    /// <summary>Attempt to recover saved API key information.</summary>
    /// <param name="apiEntry">API base address</param>
    /// <param name="cancelToken">cancel token</param>
    /// <param name="storeFile">API key scramble save file</param>
    /// <returns>API Key Management Instance</returns>
    public static async ValueTask<ApiKeyStore> RestoreAsync(Uri apiEntry, CancellationToken cancelToken = default, FileInfo? storeFile = default)
    {
        // Determine the scrambled save file.
        var scrambleFile = storeFile ?? ThisSource.RelativeFile(DefaultStoreFileName);

        // Attempt to read the stored API key information.
        var scrambler = new RoughScrambler(context: scrambleFile.FullName);
        var keyInfo = await scrambler.DescrambleObjectFromFileAsync<ApiKeyInfo>(scrambleFile, cancelToken);
        if (keyInfo != null && keyInfo.ApiEntry.AbsoluteUri == apiEntry.AbsoluteUri)
        {
            return new(keyInfo, scrambleFile, scrambler, stored: true);
        }

        // If there is no restoration information, it asks for input.
        var token = ConsoleWig.Write("API Token\n>").ReadLine();
        if (token.IsWhite()) throw new OperationCanceledException();
        var secret = ConsoleWig.Write("API Secret\n>").ReadLine();
        if (secret.IsWhite()) throw new OperationCanceledException();
        keyInfo = new(apiEntry, token, secret);
        return new(keyInfo, scrambleFile, scrambler, stored: false);
    }

    /// <summary>Save API key information</summary>
    /// <param name="cancelToken"></param>
    /// <returns>Success or failure</returns>
    public async ValueTask<bool> SaveAsync(CancellationToken cancelToken = default)
    {
        var result = true;
        if (!this.stored)
        {
            try
            {
                await this.scrambler.ScrambleObjectToFileAsync(ScrambleFile, this.Key, cancelToken: cancelToken);
                this.stored = true;
                return true;
            }
            catch { result = false; }
        }
        return result;
    }

    /// <summary>constructor</summary>
    /// <param name="key">API Key Information</param>
    /// <param name="storeFile">API key scramble save file</param>
    /// <param name="scrambler">Key scrambler</param>
    /// <param name="stored">Is the information stored</param>
    private ApiKeyStore(ApiKeyInfo key, FileInfo storeFile, RoughScrambler scrambler, bool stored)
    {
        this.Key = key;
        this.ScrambleFile = storeFile;
        this.scrambler = scrambler;
        this.stored = stored;
    }

    /// <summary>Key scrambler</summary>
    private RoughScrambler scrambler;

    /// <summary>Is the information stored</summary>
    private bool stored;
}

/// <summary>
/// Auxiliary class for BookStackClient
/// </summary>
public class BookStackClientHelper
{
    /// <summary>Constructor that ties the client instance.</summary>
    /// <param name="client">BookStackClient instance</param>
    /// <param name="cancelToken">Cancel token.</param>
    public BookStackClientHelper(BookStackClient client, CancellationToken cancelToken)
    {
        this.Client = client;
    }

    /// <summary>client instance</summary>
    public BookStackClient Client { get; }

    /// <summary>cancel token</summary>
    public CancellationToken CancelToken { get; }

    /// <summary>Helper method to retry at API request limit</summary>
    /// <param name="accessor">API request processing</param>
    /// <typeparam name="TResult">API return type</typeparam>
    /// <returns>API return value</returns>
    public async ValueTask<TResult> Try<TResult>(Func<BookStackClient, Task<TResult>> accessor)
    {
        while (true)
        {
            try
            {
                return await accessor(this.Client).ConfigureAwait(true);
            }
            catch (ApiLimitResponseException ex)
            {
                Console.WriteLine(Chalk.Yellow[$"Caught in API call rate limitation. Rate limit: {ex.RequestsPerMin} [per minute], {ex.RetryAfter} seconds to lift the limit."]);
                Console.WriteLine(Chalk.Yellow[$"It will automatically retry after a period of time has elapsed."]);
                Console.WriteLine(Chalk.Yellow[$"[Waiting...]"]);
                await Task.Delay(500 + (int)(ex.RetryAfter * 1000), this.CancelToken);
                Console.WriteLine();
            }
        }
    }

    /// <summary>Helper method to retry at API request limit</summary>
    /// <param name="accessor">API request processing</param>
    public async ValueTask Try(Func<BookStackClient, Task> accessor)
    {
        await Try<int>(async c => { await accessor(c); return 0; });
    }
}

/// <summary>
/// Content data generator
/// </summary>
public static class ContentGenerator
{
    public static byte[] CreateRectImage(float x, float y, float width, float height, int imgWidth = 200, int imgHeight = 150, uint fgcolor = 0xFF0000FF, uint bgcolor = 0xFFFFFFFF, string format = "png")
        => CreateImage((canvas, font, painter) => canvas.DrawRect(x, y, width, height, painter), imgWidth, imgHeight, fgcolor, bgcolor, format);

    public static byte[] CreateTextImage(string text, int imgWidth = 200, int imgHeight = 150, uint fgcolor = 0xFF000000, uint bgcolor = 0xFF808080, string format = "png")
        => CreateImage((canvas, font, painter) => canvas.DrawText(text, 5f, font.Size / 2 + imgHeight / 2, new(), painter), imgWidth, imgHeight, fgcolor, bgcolor, format);

    public static byte[] CreateCircleImage(float x, float y, float radius, int imgWidth = 200, int imgHeight = 150, uint fgcolor = 0xFF0000FF, uint bgcolor = 0xFFFFFFFF, string format = "png")
        => CreateImage((canvas, font, painter) => canvas.DrawCircle(x, y, radius, painter), imgWidth, imgHeight, fgcolor, bgcolor, format);

    public static byte[] CreateImage(Action<SKCanvas, SKFont, SKPaint> drawer, int width, int height, uint fgcolor, uint bgcolor, string format)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888));
        var font = new SKFont();
        var paint = new SKPaint()
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(fgcolor),
        };
        surface.Canvas.Clear(new SKColor(bgcolor));
        drawer(surface.Canvas, font, paint);
        using var image = surface.Snapshot();
        using var data = image.Encode(Enum.Parse<SKEncodedImageFormat>(format, ignoreCase: true), 100);

        return data.ToArray();
    }

}
