using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Collections.ObjectModel;
using HtmlAgilityPack;

namespace WPF_a_counter
{
    public class Service
    {
        public static async Task<int> CountAInUrl(string url)
        {
            try
            {
                var web = new HtmlWeb();
                var doc = web.Load(url);

                var links = doc.DocumentNode.SelectNodes("//a");
                return links.Count;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
