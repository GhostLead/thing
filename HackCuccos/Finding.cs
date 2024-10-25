using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackCuccos
{
    public class Finding
    {
        public string Title { get; set; }
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> Detailsek { get; set; } = new Dictionary<string, string>();
        public string RiskFactor { get; set; } // New property to hold risk factor

        public void AddDetail(string key, string value)
        {
            Details[key] = value;
            Detailsek[key] = value;
        }

        public string Kiirat()
        {
            string cucc = $"\nName: {Title}";
            foreach (var item in Details)
            {

                cucc += $"\n{item.Key} : {item.Value}";

            }
            cucc += new string('-', 50);
            return cucc;
        }



        public void AppendToDetail(string key, string text)
        {
            if (Details.ContainsKey(key))
            {
                Details[key] += Details[key].Length > 0 ? " " + text : text;
            }
            else
            {
                Details[key] = text;
            }

            if (Detailsek.ContainsKey(key))
            {
                Detailsek[key] += Detailsek[key].Length > 0 ? " " + text : text;
            }
            else
            {
                Detailsek[key] = text;
            }
        }
    }
}
