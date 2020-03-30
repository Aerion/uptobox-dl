using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CommandLine;

namespace UptoboxDl
{
    class Program
    {
        private class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('t', "token", Required = true,
                HelpText = "Uptobox user token. See https://docs.uptobox.com/?javascript#how-to-find-my-api-token")]
            public string UserToken { get; set; }

            [Value(0, Required = false, HelpText = "Uptobox links to download")]
            public IEnumerable<string> Links { get; set; }

            public void Dump(TextWriter tw)
            {
                foreach (var prop in typeof(Options).GetProperties())
                {
                    var val = prop.GetValue(this)?.ToString() ?? "null";
                    tw.WriteLine($"{prop.Name}: {val}");
                }
            }
        }

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

            foreach (var link in opts.Links)
            {
                await ProcessLink(link, opts).ConfigureAwait(false);
            }
        }

        private static async Task ProcessLink(string link, Options opts)
        {
            Console.WriteLine($"Start processing {link}");
            var fileCode = GetFileCode(link);
            if (opts.Verbose)
            {
                Console.WriteLine($"Filecode: {fileCode}");
            }

            var client = new UptoboxClient.Client(fileCode, opts.UserToken);

            var waitingToken = await client.GetWaitingTokenAsync().ConfigureAwait(false);
            var waitingTokenDelay = TimeSpan.FromSeconds(waitingToken.Delay + 1);

            Console.WriteLine(
                $"Got waiting token, awaiting for {waitingTokenDelay} - until {DateTime.Now.Add(-waitingTokenDelay).ToLongTimeString()}");

            await Task.Delay(waitingTokenDelay).ConfigureAwait(false);

            var downloadLink = await client.GetDownloadLinkAsync(waitingToken).ConfigureAwait(false);
            var outputFilename = Path.GetFileName(downloadLink.ToString());
            if (opts.Verbose)
            {
                Console.WriteLine($"Got download link: downloading {outputFilename} from {downloadLink}");
            }

            await DownloadFile(downloadLink, outputFilename).ConfigureAwait(false);
        }

        private static async Task DownloadFile(Uri link, string filename)
        {
            var wc = new WebClient();

            wc.DownloadProgressChanged += (sender, args) =>
            {
                Console.Write($"\r{args.BytesReceived}B/{args.TotalBytesToReceive}B: {args.ProgressPercentage}%");
            };

            await wc.DownloadFileTaskAsync(link, filename).ConfigureAwait(false);
            Console.WriteLine();
        }

        private static string GetFileCode(string link)
        {
            // Simple implementation, let's assume that every link ends with the fileCode: https://uptobox.com/m5f0ce9h197j
            return link.TrimEnd('/').Split('/')[^1];
        }
    }
}