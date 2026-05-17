using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Windows.Media.Control;
using Windows.Storage.Streams;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-?" || args[0] == "--help" || args[0] == "-h")
        {
            ShowHelp();
            return;
        }

        string command = args[0].ToLower();

        var sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        var currentSession = sessionManager.GetCurrentSession();

        if (currentSession == null)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { error = "No active media session found." }));
            return;
        }

        var mediaProps = await currentSession.TryGetMediaPropertiesAsync();
        var playbackInfo = currentSession.GetPlaybackInfo();
        var timelineProps = currentSession.GetTimelineProperties();

        string title = mediaProps.Title ?? "Unknown Title";
        string artist = mediaProps.Artist ?? "Unknown Artist";
        string description = $"{title} - {artist}";
        string status = playbackInfo.PlaybackStatus.ToString();

        switch (command)
        {
            case "-title":
            case "-name":
                Console.WriteLine(title);
                break;

            case "-artist":
                Console.WriteLine(artist);
                break;

            case "-desc":
            case "-description":
                Console.WriteLine(description);
                break;

            case "-status":
                Console.WriteLine(status);
                break;

            case "-time":
                var timeline = new
                {
                    Start = timelineProps.StartTime.TotalSeconds,
                    End = timelineProps.EndTime.TotalSeconds,
                    Elapsed = timelineProps.Position.TotalSeconds
                };
                Console.WriteLine(JsonSerializer.Serialize(timeline));
                break;

            case "-art":
            case "-icon":
                string? base64Img = await ExtractBase64ThumbnailAsync(mediaProps.Thumbnail);
                Console.WriteLine(base64Img ?? "No artwork found.");
                break;

            case "-all":
                string? fullBase64Img = await ExtractBase64ThumbnailAsync(mediaProps.Thumbnail);
                var bundle = new
                {
                    Title = title,
                    Artist = artist,
                    Description = description,
                    Status = status,
                    Timeline = new
                    {
                        Start = timelineProps.StartTime.TotalSeconds,
                        End = timelineProps.EndTime.TotalSeconds,
                        Elapsed = timelineProps.Position.TotalSeconds
                    },
                    ArtworkBase64 = fullBase64Img
                };
                Console.WriteLine(JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true }));
                break;

            default:
                Console.WriteLine($"Error: Unknown flag '{args[0]}'. Use --help for options.");
                break;
        }
    }

    private static async Task<string?> ExtractBase64ThumbnailAsync(IRandomAccessStreamReference thumbnailRef)
    {
        if (thumbnailRef == null) return null;

        try
        {
            using var stream = await thumbnailRef.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memoryStream);
            return Convert.ToBase64String(memoryStream.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage: SongThingy [option]\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  -title               Get current song title");
        Console.WriteLine("  -artist              Get artist name");
        Console.WriteLine("  -desc                Get title and artist");
        Console.WriteLine("  -status              Get playback status");
        Console.WriteLine("  -time                Get timeline data as json");
        Console.WriteLine("  -art                 Get album art");
        Console.WriteLine("  -all                 Get all data as json");
    }
}