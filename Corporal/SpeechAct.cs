using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Corporal
{
    [Serializable]
    public class SpeechAct
    {
        private string act;
        private string tax;
        private List<string> sentences = new List<string>();
        private bool emotional;
        private bool comment;
        public string Act { get { return this.act; } set { this.act = value; } }
        public string Tax
        {
            get { return this.tax; }
            set
            {
                this.tax = value;
                if (null != this.tax && this.tax.ToLowerInvariant() == "gefühlsausdruck")
                    this.emotional = true;
                if (null != this.tax && (this.tax.ToLowerInvariant().Contains("bewertung") || this.tax.ToLowerInvariant().Contains("kommentar")))
                    this.comment = true;
            }
        }
        public List<string> Sentences { get { return this.sentences; } set { this.sentences = value; } }
        public bool IsEmotional
        {
            get { return this.emotional; }
            set { this.emotional = value; }
        }
        public bool IsComment
        {
            get { return this.comment; }
            set { this.comment = value; }
        }
        public SpeechAct()
        {
        }
        public SpeechAct(string Act)
        {
            this.act = Act;
        }
    }
    public class IsSingleWordOnlyException : Exception
    {
        public string Word { get; set; }
        public IsSingleWordOnlyException(string word)
        {
            this.Word = word;
        }
    }
}
