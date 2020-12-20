using System;
using System.Collections.Generic;

namespace IFB
{
    internal class PersistenceData
    {
        public PersistenceData()
        {
            Cookies = new List<object>();
            SessionStorage = new Dictionary<string, string>();
            LocalStorage = new Dictionary<string, string>();
        }

        public IDictionary<string, string> LocalStorage { get; set; }
        public IDictionary<string, string> SessionStorage { get; set; }
        public IEnumerable<object> Cookies { get; set; }
        public Nullable<DateTime> CookiesInitDate { get; set; }
        public string UserContactUrl { get; set; }
    }
}