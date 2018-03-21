using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

class Shared
{
    public static object lockHistory = new object();
    [JsonProperty]
    public static LinkedList<HistoryItem> history = new LinkedList<HistoryItem>();
}