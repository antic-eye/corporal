using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Corporal
{
    public class SpeechAct
    {
        private string act;
        private string tax;
        private List<string> sentences=new List<string>();

        public string Act { get { return this.act; } set { this.act = value; } }
        public string Tax{ get { return this.tax; } set { this.tax= value; } }
        public List<string> Sentences{ get { return this.sentences; } set { this.sentences = value; } }

        public SpeechAct(string Act)
        {
            this.act = Act;
        }
        public static StringBuilder Regexify(string Directory)
        {
            StringBuilder sb = new StringBuilder();
            List<SpeechAct> acts = new List<SpeechAct>();
            foreach (FileInfo file in new DirectoryInfo(Directory).GetFiles("*.dat"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file.FullName);
                XmlNode node = doc.DocumentElement.SelectSingleNode("/list/header/speechact_type");
                SpeechAct act = new SpeechAct(node.InnerText);
                act.Tax = doc.DocumentElement.SelectSingleNode("/list/header/taxonomie").InnerText;
                foreach (string s in doc.DocumentElement.SelectSingleNode("/list/body").InnerText.Split(Environment.NewLine.ToCharArray()))
                {
                    act.Sentences.Add(s);
                }
                acts.Add(act);
            }

            return sb;
        }
    }
}
