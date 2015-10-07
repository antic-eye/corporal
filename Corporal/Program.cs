using Appccelerate.CommandLineParser;
using SimpleLogger;
using SimpleLogger.Logging.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Corporal
{
    class Program
    {
        static Queue<Task> queue = new Queue<Task>();
        static string inputFile = null;
        static bool verbose = false;

        static void Main(string[] args)
        {
            // Adding handler - to show log messages (ILoggerHandler)
            Logger.LoggerHandlerManager
                .AddHandler(new ConsoleLoggerHandler())
#if DEBUG
                .AddHandler(new DebugConsoleLoggerHandler());
#endif
            Logger.Log("Parsing arguments");
            var configuration = CommandLineParserConfigurator
                .Create()
                    .WithNamed("f", v => inputFile = v)
                        .HavingLongAlias("file")
                        .Required()
                        //.RestrictedTo(ShortOutput, LongOutput)
                        .DescribedBy("Input File", "specifies the input file to parse.")
                    //.WithNamed("t", (int v) => threshold = v)
                    //    .HavingLongAlias("threshold")
                    //    .DescribedBy("value", "specifies the threshold used in output.")
                    .WithSwitch("v", () => verbose = true)
                        .HavingLongAlias("verbose")
                        .DescribedBy("enables verbose output")
                .BuildConfiguration();
            var parser = new CommandLineParser(configuration);

            var parseResult = parser.Parse(args);

            if (!parseResult.Succeeded)
                ShowUsage(configuration, parseResult);
            else if(!File.Exists(inputFile))
            {
                Logger.Log(Logger.Level.Error, 
                    string.Format("The file {0} does not exist, I'm out.", inputFile));
                ShowUsage(configuration, parseResult);
            }
            else
            {
                Logger.Log(string.Format("Reading document {0}", inputFile));

            }

#if DEBUG
            Logger.Log(Logger.Level.Error,
                string.Format("[DEBUG] Exiting with: " + Environment.ExitCode));
            Console.ReadLine();
#endif
        }

        private static void ShowUsage(CommandLineConfiguration configuration, ParseResult parseResult)
        {
            Usage usage = new UsageComposer(configuration).Compose();
            Console.WriteLine(parseResult.Message);
            Console.WriteLine("usage:" + usage.Arguments);
            Console.WriteLine("options");
            Console.WriteLine(usage.Options.IndentBy(4));
            Console.WriteLine();

#if !DEBUG
                Environment.Exit(-1);
#else
            Environment.ExitCode = -1;
#endif
        }
    }
}
