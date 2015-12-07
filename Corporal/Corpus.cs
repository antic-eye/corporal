﻿using SimpleLogger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using static SimpleLogger.Logger;

namespace Corporal
{
    /// <summary>
    /// This class contains the definition of a corpus, it's metadata and provides the ability to save it to a xml file.
    /// </summary>
    public class Corpus
    {
        private Hashtable attributes = new Hashtable();
        private List<Text> texts = new List<Text>();
        /// <summary>
        /// COntains metadata of the corpus
        /// </summary>
        public Hashtable Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }
        /// <summary>
        /// Contains a list of texts that are part of the corpus.
        /// </summary>
        public List<Text> Texts
        {
            get { return texts; }
            set { texts = value; }
        }
        /// <summary>
        /// Save texts to a xml file
        /// </summary>
        /// <param name="path">Path to save the xml file to.</param>
        /// <returns>false on error</returns>
        public bool ToXml(string inputFile)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            DateTime loop = DateTime.Now;

            string outputFile = string.Format("{0}\\{1}.xml", 
                Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile));

            List<SpeechAct> acts = null;
            try {
                if (Directory.Exists(Path.GetDirectoryName(inputFile) + @"\speech-acts"))
                {
                    Logger.Log(Level.Info, "Found speech-acts directory, parsing acts.");
                    acts = SpeechAct.Regexify(Path.GetDirectoryName(inputFile) + @"\speech-acts");
                    if (acts.Count()>0)
                        Logger.Log(Level.Info, string.Format("Parsed {0} speech acts.", acts.Count()));
                }
            }
            catch(Exception ex) { Logger.Log(ex); }

            Logger.Log(Level.Info, string.Format("Writing document {0}", outputFile));

            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineChars = Environment.NewLine;
                settings.NewLineHandling = NewLineHandling.Replace;
                settings.NewLineOnAttributes = true;
                settings.WriteEndDocumentOnClose = true;
                settings.Encoding = Encoding.UTF8;

                int iActCount = 0;
                int iCurrentText = 0;
                int iTokenCount = 0;
                using (XmlWriter writer = XmlWriter.Create(outputFile, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("corpus");
                    Logger.Log(Level.Info, string.Format("Converting {0} texts", this.texts.Count));

                    foreach (Text text in this.texts)
                    {
                        iCurrentText++;

                        if ((DateTime.Now - loop) > new TimeSpan(0, 0, 3))
                        {
                            Logger.Log(Level.Info, string.Format("Processing text {0}, {1} texts left.", 
                                iCurrentText, this.texts.Count - iCurrentText));
                            loop = DateTime.Now;
                        }

                        if (string.IsNullOrEmpty(text.Content))
                            continue;

                        Logger.Log(string.Format("Adding text  {0}", text.Attributes["id"]));

                        writer.WriteStartElement("text");
                        foreach (DictionaryEntry attr in text.Attributes)
                        {
                            writer.WriteAttributeString(attr.Key.ToString(), attr.Value.ToString());
                        }
                        //writer.WriteString(Environment.NewLine);

                        if (null != acts)
                        {
                            bool bAct = false;
                            foreach (SpeechAct act in acts)
                            {
                                foreach (string pattern in act.Sentences)
                                {
                                    Regex reg = new Regex(pattern);
                                    Match m = reg.Match(text.Content);
                                    if (m.Success)
                                    {
                                        writer.WriteStartElement("speechact");
                                        writer.WriteAttributeString("name", act.Act);
                                        writer.WriteString(m.Value);
                                        writer.WriteEndElement();
                                        bAct = true;
                                    }
                                }
                            }
                            if(bAct)
                                iActCount++;
                        }
                        writer.WriteString(text.Content.Replace(" ", Environment.NewLine));
                        writer.WriteEndElement();
                    }
                }
                watch.Stop();
                Logger.Log(Level.Info, string.Format("XML file has been written to {0}", outputFile));
                if (null != acts)
                    Logger.Log(Level.Info, string.Format("{0} texts contained speech acts", iActCount));
                Logger.Log(Level.Info, string.Format("Conversion took {0}s", watch.Elapsed.TotalSeconds));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.Level.Error, string.Format("Exception during xml generation: {0}", ex.Message));
                return false;
            }
        }
    }
    /// <summary>
    /// The Text element contains the rows of an Excel file as texts with attributes.
    /// </summary>
    public class Text
    {
        private Hashtable attributes = new Hashtable();
        private string content = string.Empty;
        /// <summary>
        /// Controls if the content of a Text element should e tagged or not.
        /// </summary>
        public bool TagTheText { get; set; }
        /// <summary>
        /// Contains metadata of the text.
        /// </summary>
        public Hashtable Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }
        /// <summary>
        /// This conatins the content of the text element.
        /// </summary>
        /// The content will be tagged if TagTheText is set
        /// <see cref="TagTheText"/>
        public string Content
        {
            get
            {
                return content;
            }
            set
            {
                if (this.TagTheText)
                    TagText(value);
                else
                {
                    FindSpeechAct();
                    content = value;
                }
            }
        }

        public void FindSpeechAct()
        {

        }
        /// <summary>
        /// This function calls the TreeTagger.
        /// </summary>
        /// The content of the given content object is saved to a temporary file, calledwith the TreeTagger and saved back to the content.
        /// <param name="content">Text to be tagged</param>
        public void TagText(string content)
        {
            Logger.Log(string.Format("Tagging text {0}", this.attributes["id"]));

            string sTempFile = Path.GetTempFileName();
            using (TextWriter writer = new StreamWriter(sTempFile))
            {
                Logger.Log(string.Format("Writing text to temp file {0}", sTempFile));
                writer.Write(content);
                writer.Close();
            }
            using (Process p = new Process())
            {
                p.StartInfo = new ProcessStartInfo();
                p.StartInfo.FileName = "tag-german.bat";
                p.StartInfo.Arguments = "\"" + sTempFile + "\"";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.StandardOutputEncoding = Encoding.UTF8;

                Logger.Log(string.Format("Starting TreeTagger from {0}", p.StartInfo.FileName));
                Logger.Log(string.Format("Arguments are: {0}", p.StartInfo.Arguments));
                if (p.Start())
                {
                    while (!p.HasExited)
                    {
                        Thread.Sleep(500);
                    }

                    string stdOut = p.StandardOutput.ReadToEnd();
                    string stdErr = p.StandardError.ReadToEnd();

                    if (p.ExitCode == 0)
                    {
                        Logger.Log("Yeay, TreeTagger exited clean.");
                        this.content = stdOut;
                    }
                    else
                    {
                        Logger.Log(Level.Error, 
                            string.Format("Eieiei, TreeTagger made a booboo, Exit Code was {0}", p.ExitCode));
                    }
                }
            }
        }
    }
}