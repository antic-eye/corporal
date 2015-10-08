using SimpleLogger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            set{
                content = value.Replace(" ", Environment.NewLine);
                foreach (string s in new string[] { ".", "," })
                    content = content.Replace(s, Environment.NewLine + s + Environment.NewLine);
            }
        }
    }
}
