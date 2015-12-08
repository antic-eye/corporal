﻿using Appccelerate.CommandLineParser;
using Excel;
using SimpleLogger;
using SimpleLogger.Logging.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static SimpleLogger.Logger;

namespace Corporal
{
    class Program
    {
        private static Queue<Task> queue = new Queue<Task>();
        private static string inputFile = null;
        private static string directoryInput = null;
        private static string directoryOut = null;
        private static string speechActs = null;
        private static bool verbose = false;
        private static bool tag = false;
        private static bool singleCorpus = false;
        private static Corpus corpus;

        public static void Main(string[] args)
        {
            // Adding handler - to show log messages (ILoggerHandler)
            Logger.LoggerHandlerManager
                .AddHandler(new ConsoleLoggerHandler())
#if DEBUG
                .AddHandler(new DebugConsoleLoggerHandler());
#endif
            Logger.DefaultLevel = Logger.Level.Debug;

            LoadBanner();

            Logger.Log("Parsing arguments");
            var configuration = CommandLineParserConfigurator
                .Create()
                    .WithNamed("f", v => inputFile = v)
                        .HavingLongAlias("file")
                        .DescribedBy("Input File", "specifies the input file to parse.")
                    .WithSwitch("t", () => tag = true)
                        .HavingLongAlias("tag")
                        .DescribedBy("Tag your texts using TreeTagger")
                    .WithSwitch("c", () => singleCorpus = true)
                        .HavingLongAlias("single-corpus")
                        .DescribedBy("Create a single corpus file in recursive mode.")
                    .WithSwitch("v", () => verbose = true)
                        .HavingLongAlias("verbose")
                        .DescribedBy("enables verbose output")
                    .WithNamed("d", d => directoryInput = d)
                        .HavingLongAlias("directory")
                        .DescribedBy("Input path", "specifies the path to a collection of xslx files to process.")
                    .WithNamed("o", o => directoryOut = o)
                        .HavingLongAlias("out")
                        .DescribedBy("Ouput path", "specifies the path where to save resulting xml files to.")
                    .WithNamed("s", s => speechActs = s)
                        .HavingLongAlias("speech-acts")
                        .DescribedBy("Speech Act Dir", "specifies the path to speech-act databases (*.dat).")
                .BuildConfiguration();
            var parser = new CommandLineParser(configuration);

            var parseResult = parser.Parse(args);

            if (!parseResult.Succeeded)
            {
                ShowUsage(configuration, parseResult);
                Logger.Log(Level.Error, parseResult.Message);
            }
            else if (null != inputFile && !File.Exists(inputFile))
            {
                Logger.Log(Logger.Level.Error,
                    string.Format("The file {0} does not exist, I'm out.", inputFile));
                ShowUsage(configuration, parseResult);
                Environment.ExitCode = -2;
            }
            else if (null != directoryInput && !Directory.Exists(directoryInput))
            {
                Logger.Log(Logger.Level.Error,
                    string.Format("The directory {0} does not exist, I'm out.", directoryInput));
                ShowUsage(configuration, parseResult);
                Environment.ExitCode = -2;
            }
            else
            {
                if (!verbose)
                    Logger.DebugOff();
                else
                    Logger.DebugOn();

                if (!string.IsNullOrEmpty(directoryInput))
                {
                    Logger.Log(string.Format("Reading directory {0}", directoryInput));

                    if (singleCorpus)
                    {
                        Logger.Log("Generating single corpus");
                        List<Corpus> corpses = new List<Corpus>();
                        foreach (string sFile in Directory.GetFiles(directoryInput, "*.xlsx"))
                        {
                            Logger.Log(string.Format("Reading document {0}", sFile));
                            Corpus corp = new Corpus(Path.GetFileName(sFile));
                            corp = FillCorpus(sFile, corp);
                            if (null == corp)
                                Environment.ExitCode = 403;
                            else
                                corpses.Add(corp);
                        }

                        string outputFile = string.Format("{0}\\{1}.xml",
                            (!string.IsNullOrEmpty(directoryOut)) ? directoryOut : Path.GetDirectoryName(inputFile),
                            Path.GetFileNameWithoutExtension(inputFile));

                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        settings.NewLineChars = Environment.NewLine;
                        settings.NewLineHandling = NewLineHandling.Replace;
                        settings.NewLineOnAttributes = true;
                        settings.WriteEndDocumentOnClose = true;
                        settings.Encoding = Encoding.UTF8;

                        int iActCount = 0;
                        int iCurrentText = 0;
                        DateTime loop = DateTime.Now;
                        using (XmlWriter writer = XmlWriter.Create(outputFile, settings))
                        {
                            writer.WriteStartDocument();
                            writer.WriteStartElement("bodies");
                            writer.WriteAttributeString("tokenCount", corpses.Sum(x => x.TokenCount).ToString());
                            foreach (Corpus corp in corpses)
                            {
                                Corpus.Corpus2Xml(corp, ref loop, ref iActCount, ref iCurrentText, writer, corp.SpeechActs);
                            }
                            writer.WriteEndElement();
                        }
                    }
                    else
                    {
                        foreach (string sFile in Directory.GetFiles(directoryInput, "*.xlsx"))
                        {
                            Logger.Log(string.Format("Reading document {0}", sFile));
                            corpus = FillCorpus(sFile, corpus);
                            if (null == corpus)
                                Environment.ExitCode = 403;
                            corpus.ToXml(sFile, (string.IsNullOrEmpty(directoryOut)) ? null : directoryOut);
                        }
                    }
                }
                else
                {
                    Logger.Log(string.Format("Reading document {0}", inputFile));
                    corpus = FillCorpus(inputFile, corpus);
                    if (null == corpus)
                        Environment.ExitCode = 403;
                    else
                        corpus.ToXml(inputFile);
                }
            }

            Logger.Log(string.Format("Exiting with: " + Environment.ExitCode));
#if DEBUG
            Console.WriteLine("Done.");
            Console.ReadLine();
#endif
        }

        private static void LoadBanner()
        {
            Console.Write(@"
                               ``                   
                              ```                   
                             `````                  
                            ```-.``                 
                           ```:dh-```               
                          ```/dmmh:```              
                         ``.odmmmmd/```             
                       ```-ymmmdmmmms.```           
                      ``.+dmmmh-/dmmmh/```          
                    ```:ymmmdo...-smmmds-```        
                  ```-sdmmmy:`/hh:`/hmmmdo.```      
                ```-sdmmmd/.-ymmmds..+dmmmdo.```    
              ```-sdmmmd+..odmmmmmmd+..odmmmdo-```  
            ``./ydmmmd+..+dmmmh/+dmmmh/.-odmmmdy:.``
            ``smmmmh/..odmmmh+.``.+dmmmh+..+dmmmm+``
            ``ymds:`:sdmmmh+.``````.odmmmdo-./ydm+``
            ``+/../ymmmmh/.```    ```.+hmmmdy/.-+/``
            ``./sdmmmds:````        ```./ydmmmdo:.``
            ``smmmmh+.````            ````-ohmmmm+``
            ``ymho-````                  ````:odm+``
            ``/-````                       ````.::``
            ``````                            ``````
            ``                                    ``


 .o88b.  .d88b.  d8888b. d8888b.  .d88b.  d8888b.  .d8b.  db     
d8P  Y8 .8P  Y8. 88  `8D 88  `8D .8P  Y8. 88  `8D d8' `8b 88     
8P      88    88 88oobY' 88oodD' 88    88 88oobY' 88ooo88 88     
8b      88    88 88`8b   88~~~   88    88 88`8b   88~~~88 88     
Y8b  d8 `8b  d8' 88 `88. 88      `8b  d8' 88 `88. 88   88 88booo.
 `Y88P'  `Y88P'  88   YD 88       `Y88P'  88   YD YP   YP Y88888P

                         Stand Still!!!


");
        }

        private static Corpus FillCorpus(string inputFile, Corpus corpus)
        {
            corpus = new Corpus(Path.GetFileName(inputFile));
            if (null != speechActs)
                corpus.SpeechActDir = speechActs;
            corpus.Attributes.Add("created", DateTime.Now);
            corpus.Attributes.Add("author", Environment.UserName);

            Dictionary<int, string> attributes = new Dictionary<int, string>();
            int iRow = 0;
            int iCell = 0;
            int iTextCell = -1;

            try
            {
                foreach (var worksheet in Workbook.Worksheets(inputFile))
                {
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
                                    Logger.Log(Level.Info, string.Format("Found attribute {0}. ", cell.Text));
                                    attributes.Add(iCell, cell.Text.Replace(" ", string.Empty));
                                    if (cell.Text.ToLowerInvariant() == "text")
                                    {
                                        Logger.Log(Level.Info, string.Format("Found text cell @ column {0}. ", iCell));
                                        iTextCell = iCell;
                                    }
                                }
                                else if (iCell == iTextCell && !String.IsNullOrEmpty(cell.Text))
                                    text.Content = Regex.Replace(cell.Text, @"[ ]{2,}", " "); //Replace multiple spaces with one
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
            }
            catch (IOException ex)
            {
                Logger.Log(ex);
                return null;
            }
            Logger.Log("Finished reading excel file.");
            return corpus;
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
