using System.Collections.Generic;

using Newtonsoft.Json;

public class Shared
{
    public static string filePreferences = "preferences.json";
    public static string fileResources = "resources.json";
    public static string tempDirectory = "temp";
    public static Preferences preferences = new Preferences();
    [JsonProperty]
    public static List<OnlineResource> completeResourceList;
}
