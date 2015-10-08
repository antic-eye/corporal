using SimpleLogger;
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

namespace Corporal
{
    public class Corpus
    {

        private Hashtable attributes = new Hashtable();
        private List<Text> texts = new List<Text>();
        public Hashtable Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }
        public List<Text> Texts
        {
            get { return texts; }
            set { texts = value; }
        }
        public bool ToXml(string path)
        {
            Logger.Log(string.Format("Writing document {0}", path));

            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineChars = Environment.NewLine;
                settings.NewLineHandling = NewLineHandling.None;
                settings.WriteEndDocumentOnClose = true;
                settings.Encoding = Encoding.UTF8;

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("corpus");
                    Logger.Log(string.Format("Converting {0} texts", this.texts.Count));

                    foreach (Text text in this.texts)
                    {
                        writer.WriteStartElement("text");
                        foreach (DictionaryEntry attr in text.Attributes)
                            writer.WriteAttributeString(attr.Key.ToString(), attr.Value.ToString());
                        writer.WriteString(Environment.NewLine);
                        writer.WriteString(text.Content);
                        writer.WriteEndElement();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.Level.Error, string.Format("Exception during xml generation: {0}", ex.Message));
                return false;
            }
        }
    }
    public class Text
    {
        private Hashtable attributes = new Hashtable();
        private string content = string.Empty;

        public bool TagTheText { get; set; }
        public Hashtable Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }
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
                    content = value;
            }
        }
        public void TagText(string content)
        {
            string sTempFile = Path.GetTempFileName();
            using (TextWriter writer = new StreamWriter(sTempFile))
            {
                writer.Write(content);
                writer.Close();
            }
            using (Process p = new Process())
            {
                p.StartInfo = new ProcessStartInfo();
                p.StartInfo.FileName = @"E:\ITI-Projekte_NoBackup\Corporal\TreeTagger\bin\tag-german.bat";
                p.StartInfo.Arguments = "\"" + sTempFile + "\"";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.StandardOutputEncoding = Encoding.UTF8;

                if (p.Start())
                {
                    while (!p.HasExited)
                    {
                        Thread.Sleep(500);
                    }

                    string stdOut = p.StandardOutput.ReadToEnd();
                    string stdErr = p.StandardError.ReadToEnd();

                    if (p.ExitCode == 0)
                        this.content = stdOut;
                    else
                        throw new Exception(stdErr);
                }
            }
        }
    }
}