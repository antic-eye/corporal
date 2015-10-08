using Appccelerate.CommandLineParser;
using Excel;
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
        private static Queue<Task> queue = new Queue<Task>();
        private static string inputFile = null;
        private static bool verbose = false;
        private static bool tag = false;
        private static Corpus corpus = new Corpus();

        public static void Main(string[] args)
        {
            // Adding handler - to show log messages (ILoggerHandler)
            Logger.LoggerHandlerManager
                .AddHandler(new ConsoleLoggerHandler())
#if DEBUG
                .AddHandler(new DebugConsoleLoggerHandler());
#endif
            Logger.DefaultLevel = Logger.Level.Debug;
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
                    .WithSwitch("t", () => tag = true)
                        .HavingLongAlias("tag")
                        .DescribedBy("Tag your texts using TreeTagger")
                    .WithSwitch("v", () => verbose = true)
                        .HavingLongAlias("verbose")
                        .DescribedBy("enables verbose output")
                .BuildConfiguration();
            var parser = new CommandLineParser(configuration);

            var parseResult = parser.Parse(args);

            if (!parseResult.Succeeded)
                ShowUsage(configuration, parseResult);
            else if (!File.Exists(inputFile))
            {
                Logger.Log(Logger.Level.Error,
                    string.Format("The file {0} does not exist, I'm out.", inputFile));
                ShowUsage(configuration, parseResult);
            }
            else
            {
                if (!verbose)
                    Logger.DebugOff();
                else
                    Logger.DebugOn();

                Logger.Log(string.Format("Reading document {0}", inputFile));
                FillCorpus();

                corpus.ToXml(inputFile + ".xml");
            }

            Logger.Log(string.Format("Exiting with: " + Environment.ExitCode));
#if DEBUG
            Console.ReadLine();
#endif
        }

        private static void FillCorpus()
        {
            corpus.Attributes.Add("created", DateTime.Now);
            corpus.Attributes.Add("author", Environment.UserName);

            Dictionary<int, string> attributes = new Dictionary<int, string>();
            int iRow = 0;
            int iCell = 0;
            int iTextCell = -1;
            foreach (var worksheet in Workbook.Worksheets(inputFile))
                foreach (var row in worksheet.Rows)
                {
                    Text text = new Text();
                    text.TagTheText = tag;
                    foreach (var cell in row.Cells)
                    {
                        if (cell != null)
                        {
                            if (iRow == 0)//GetHeaders
                            {
                                attributes.Add(iCell, cell.Text);
                                if (cell.Text == "text")
                                    iTextCell = iCell;
                            }
                            else
                                if (iCell == iTextCell && !String.IsNullOrEmpty(cell.Text))
                                text.Content = string.Format("{0}{1}{0}", Environment.NewLine, cell.Text);
                            else
                                text.Attributes.Add(attributes[iCell], cell.Text);
                        }
                        iCell++;
                    }
                    if (iRow > 0)
                        corpus.Texts.Add(text);
                    iRow++;
                    iCell = 0;
                }
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
