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
            MyFollowers = new HashSet<string>();
        }

        public IEnumerable<object> Cookies { get; set; }
        public Nullable<DateTime> CookiesInitDate { get; set; }
        public IDictionary<string, string> LocalStorage { get; set; }
        public HashSet<string> MyFollowers { get; set; }
        public DateTime? MyFollowersUpdate { get; set; }
        public IDictionary<string, string> SessionStorage { get; set; }
        public string UserContactUrl { get; set; }
    }
}