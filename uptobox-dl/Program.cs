using System;
using System.IO;
using CommandLine;

namespace UptoboxDl
{
    class Program
    {
        private class Options
        {
            [Option('a', "batch-file", Required = false,
                HelpText =
                    "File containing URLs to download ('-' for stdin), one URL per line.  Lines starting with '#', ';' or ']' are considered as comments and ignored.")]
            public string BatchFilename { get; set; }

            [Option("restrict-filenames", Required = false,
                HelpText = "Restrict filenames to only ASCII characters, and avoid \"&\" and spaces in filenames.")]
            public bool RestrictFilenames { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            public void Dump(TextWriter tw)
            {
                foreach (var prop in typeof(Options).GetProperties())
                {
                    var val = prop.GetValue(this)?.ToString() ?? "null";
                    tw.WriteLine($"{prop.Name}: {val}");
                }
            }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions);
        }

        static void RunOptions(Options opts)
        {
            if (opts.Verbose)
            {
                Console.WriteLine("Called with the following options:");
                opts.Dump(Console.Out);
            }
        }
    }
}