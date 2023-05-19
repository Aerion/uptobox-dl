using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;
using CommandLine;
using UptoboxDl.UptoboxClient;

namespace UptoboxDl;

class Program
{
    private class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('d', "debug", Required = false, HelpText = "Print debug data.")]
        public bool Debug { get; set; }

        [Option("output-directory", Required = false, HelpText = "Output directory (defaults to the current working directory if unset)")]
        public string OutputDirectory { get; set; }

        [Option('t', "token", Required = true,
            HelpText = "Uptobox user token. See https://docs.uptobox.com/?javascript#how-to-find-my-api-token")]
        public string UserToken { get; set; }

        [Value(0, Required = false, HelpText = "Uptobox links to download")]
        public IReadOnlyList<string> Links { get; set; }

        public void Dump(TextWriter tw)
        {
            tw.WriteLine($"{nameof(Verbose)}: {Verbose}");
            tw.WriteLine($"{nameof(UserToken)}: {UserToken}");
            tw.WriteLine($"{nameof(Links)}: {string.Join(" ", Links)}");
        }
    }

    private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };
    private static Client Client;

    static async Task Main(string[] args)
    {
        Options opts = default;
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(parsed => opts = parsed)
            .WithNotParsed(_ => Environment.Exit(1));

        if (opts.Verbose)
        {
            Console.WriteLine("Called with the following options:");
            opts.Dump(Console.Out);
            Console.WriteLine();
        }

        var debugWriter = opts.Debug ? Console.Out : null;
        Client = new Client(opts.UserToken, customHttpClient: HttpClient,
            debugWriter: debugWriter);

        var (fileCodes, directFilesCount, folderCount) = await GetFileCodesFromUrls(opts.Links, opts.UserToken);
        Console.WriteLine($"{fileCodes.Count} files to download (from {directFilesCount} direct links and {folderCount} folders)");

        foreach (var fileCode in fileCodes)
        {
            await RetryOnFailure(() => ProcessLink(fileCode, opts));
            Console.WriteLine();
        }
    }

    private static async Task<(IReadOnlyList<string> fileCodes, int directFilesCount, int folderCount)> GetFileCodesFromUrls(IReadOnlyList<string> links, string userToken)
    {
        var fileCodes = new List<string>();
        var directFilesCount = 0;
        var folderCount = 0;

        foreach (var link in links)
        {
            var linkType = LinkParser.GetLinkType(link);
            if (linkType == LinkParser.LinkType.DirectLink)
            {
                fileCodes.Add(LinkParser.ParseFileCodeFromDirectLink(link));
                directFilesCount += 1;
            }
            else if (linkType == LinkParser.LinkType.Folder)
            {
                var (folder, hash) = LinkParser.ParseFolderHashFromLink(link);
                var folderFileCodes = await Client.GetFolderFileCodes(folder, hash);
                fileCodes.AddRange(folderFileCodes);
                folderCount += 1;
            }
            else
            {
                throw new Exception("Unknown link format: " + link);
            }
        }

        return (fileCodes, directFilesCount, folderCount);
    }

    private static async Task ProcessLink(string fileCode, Options opts)
    {
        Console.WriteLine($"Start processing {fileCode}");

        var waitingToken = await GetValidWaitingTokenAsync(fileCode);
        var downloadLink =
            await RetryOnFailure(() => Client.GetDownloadLinkAsync(fileCode, waitingToken)).ConfigureAwait(false);
        var outputFilename = Path.GetFileName(downloadLink.ToString());
        if (!string.IsNullOrWhiteSpace(opts.OutputDirectory))
        {
            outputFilename = Path.Combine(opts.OutputDirectory, outputFilename);
        }

        if (opts.Verbose)
        {
            Console.WriteLine($"Got download link: downloading {outputFilename} from {downloadLink}");
        }

        await RetryOnFailure(() => DownloadFile(downloadLink, outputFilename));

        Console.WriteLine();
        Console.WriteLine($"Downloaded {outputFilename}");
    }

    private static async Task<WaitingToken> GetValidWaitingTokenAsync(string fileCode)
    {
        var waitingToken = await RetryOnFailure(() => Client.GetWaitingTokenAsync(fileCode)).ConfigureAwait(false);
        if (waitingToken.Delay == 0)
        {
            return waitingToken;
        }
        var waitingTokenDelay = TimeSpan.FromSeconds(waitingToken.Delay + 1);

        Console.WriteLine(
            $"Got waiting token, awaiting for {waitingTokenDelay} - until {DateTime.Now.Add(waitingTokenDelay).ToLongTimeString()}");

        await Task.Delay(waitingTokenDelay).ConfigureAwait(false);

        if (waitingToken.Token == null)
        {
            return await GetValidWaitingTokenAsync(fileCode);
        }

        return waitingToken;
    }

    private static async Task<T> RetryOnFailure<T>(Func<Task<T>> task)
    {
        try
        {
            return await task();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Error.WriteLine($"Caught exception - retrying once: {ex}");
            Console.ResetColor();
            return await task();
        }
    }

    private static async Task RetryOnFailure(Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Error.WriteLine($"Caught exception - retrying once: {ex}");
            Console.ResetColor();
            await Task.Delay(TimeSpan.FromMinutes(1));
            await task();
        }
    }

    private static async Task DownloadFile(Uri link, string filename)
    {
        var handler = new HttpClientHandler() { AllowAutoRedirect = true };
        var ph = new ProgressMessageHandler(handler);

        ph.HttpReceiveProgress += (_, args) =>
        {
            var transferredBytesString = ByteSizeLib.ByteSize.FromBytes(args.BytesTransferred).ToString("0.00", CultureInfo.CurrentCulture, true);
            var totalBytesString = ByteSizeLib.ByteSize.FromBytes(args.TotalBytes.Value).ToString("0.00", CultureInfo.CurrentCulture, true);
            var text = $"\r{transferredBytesString} / {totalBytesString}: {Math.Floor((decimal)(args.BytesTransferred * 100 / args.TotalBytes))}%";
            Console.Write(text);
            var padding = Console.WindowWidth - text.Length;
            if (padding > 0)
            {
                Console.Write(new string(' ', padding));
            }
        };

        var client = new HttpClient(ph);
        using var responseStream = await client.GetStreamAsync(link);
        const int bufferSize = 8192;
        using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);
        await responseStream.CopyToAsync(fs);
    }
}