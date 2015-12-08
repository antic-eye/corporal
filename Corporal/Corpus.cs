using SimpleLogger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private TrulyObservableCollection<Text> texts = new TrulyObservableCollection<Text>();
        private int tokenCount = 0;
        private string name;
        private List<SpeechAct> acts = new List<SpeechAct>();
        private string actDir;
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
        public TrulyObservableCollection<Text> Texts
        {
            get { return this.texts; }
            set {
                this.texts = value;
            }
        }
        public string SpeechActDir
        {
            get { return this.actDir; }
            set
            {
                this.actDir = value;
                this.GetSpeechActs();
            }
        }
        public Corpus(string name)
        {
            this.name = name;
            texts.CollectionChanged += Texts_CollectionChanged;
        }

        private void Texts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            tokenCount = this.texts.Sum(x => x.TokenCount);
        }

        public bool ToXml(string inputFile)
        {
            return ToXml(inputFile, Path.GetDirectoryName(inputFile));
        }
        private void GetSpeechActs()
        {
            try
            {
                if (Directory.Exists(Path.GetDirectoryName(this.actDir)))
                {
                    Logger.Log(Level.Info, "Found speech-acts directory, parsing acts.");
                    acts = SpeechAct.Regexify(this.actDir);
                    if (acts.Count() > 0)
                        Logger.Log(Level.Info, string.Format("Parsed {0} speech acts.", acts.Count()));
                }
            }
            catch (Exception ex) { Logger.Log(ex); }
        }
        /// <summary>
        /// Save texts to a xml file
        /// </summary>
        /// The output xml file will be saved next to the input file with a .xml extension
        /// <param name="inputFile">Path to the input file.</param>
        /// <returns>false on error</returns>
        public bool ToXml(string inputFile, string outputFolder)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            DateTime loop = DateTime.Now;

            string outputFile = string.Format("{0}\\{1}.xml",
                outputFolder, Path.GetFileNameWithoutExtension(inputFile));

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
                using (XmlWriter writer = XmlWriter.Create(outputFile, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("corpus");
                    writer.WriteAttributeString("tokenCount", this.tokenCount.ToString());
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
    public class Text : INotifyPropertyChanged
    {
        private Hashtable attributes = new Hashtable();
        private string content = string.Empty;
        private int tokenCount = 0;

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
                //if (this.TagTheText)
                //TagText(value);
                //else
                //{
                //    FindSpeechAct();
                content = value;
                PropertyChanged(this, new PropertyChangedEventArgs("content"));
                try {
                    tokenCount = 0;
                    foreach (char c in this.content.ToCharArray())
                        if (c == ' ')
                            tokenCount++;
                    if (this.attributes.ContainsKey("tokenCount"))
                        this.attributes["tokenCount"] = this.tokenCount;
                    else
                        this.attributes.Add("tokenCount", this.tokenCount);
                }
                catch(Exception ex) { Logger.Log(ex); }
                //}
            }
        }
        public int TokenCount
        {
            get
            {
                return this.tokenCount;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Text()
        {
            this.PropertyChanged += Text_PropertyChanged;
            
        }

        private void Text_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.Log(Level.Debug, "Changed " + e.PropertyName);
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
    public sealed class TrulyObservableCollection<T> : ObservableCollection<T>
    where T : INotifyPropertyChanged
    {
        public TrulyObservableCollection()
        {
            CollectionChanged += FullObservableCollectionCollectionChanged;
        }

        public TrulyObservableCollection(IEnumerable<T> pItems) : this()
        {
            foreach (var item in pItems)
            {
                this.Add(item);
            }
        }

        private void FullObservableCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Object item in e.NewItems)
                {
                    ((INotifyPropertyChanged)item).PropertyChanged += ItemPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (Object item in e.OldItems)
                {
                    ((INotifyPropertyChanged)item).PropertyChanged -= ItemPropertyChanged;
                }
            }
        }

        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T)sender));
            OnCollectionChanged(args);
        }
    }
}