using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public static List<SpeechAct> Regexify(string Directory)
        {
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
                    if (!string.IsNullOrEmpty(s))
                        act.Sentences.Add(Prepare(s));
                }
                acts.Add(act);
            }
            return acts;
        }
        private static string Prepare(string sentence)
        {
            // convert matching syntax to regex
            sentence = sentence.Replace(" . ", @" .* ");          // Find commas with or without spaces
            sentence = sentence.Replace("...", @" .* ");          // Match everything here
            sentence = sentence.Replace(" ? ", @"[ ]?\?[ ]?");    // Mask Questions
            sentence = sentence.Replace(" ! ", @"[ ]?\![ ]?");    // Mask !

            // make common mistakes more fuzzy
            sentence = Regex.Replace(sentence, @"\,[ ]?dass[ ]?", @"\,[ ]?da[s]?[ß]?[ss]?[ ]?");
            //sentence = sentence.Replace(",dass ", );    // match dass, das and daß when comma before
            //sentence = sentence.Replace(", dass ", @"\,\w?da[s]?[ß]?[ss]?\w?");    // match dass, das and daß when comma before
            sentence = sentence.Replace(" , ", @"[ ]?\,[ ]?");    // Find commas with or without spaces

            return sentence;
        }
    }
}
