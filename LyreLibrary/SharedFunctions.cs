using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Drawing;

using Newtonsoft.Json;

public class SharedFunctions // Common functions for entire Lyre solution
{
    public static void loadJSON<T>(string path, ref T obj)
    {
        if (File.Exists(path))
        {
            try
            {
                string fileString = File.ReadAllText(path);
                obj = JsonConvert.DeserializeObject<T>(fileString);
            }
            catch (Exception ex)
            {
                saveJSON(path, obj);
            }
        }
        else
        {
            saveJSON(path, obj);
        }
    }

    public static void saveJSON<T>(string path, T obj)
    {
        string jsonString = JsonConvert.SerializeObject(obj, Formatting.Indented);//, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });
        File.WriteAllText(path, jsonString);
    }

    public static int getResourcesMissingCount(List<OnlineResource> resourceList)
    {
        int count = 0;
        foreach (OnlineResource onR in resourceList)
        {
            foreach (string path in onR.paths)
            {
                if (File.Exists(path) == false)
                {
                    count++;
                    break;
                }
            }
        }
        return count;
    }

    public static Image getImage(string path)
    {
        int counter = 0;
        while (File.Exists(path) == false)
        {
            Application.DoEvents();
            Thread.Sleep(50);
            counter += 50;
            if (counter > 1000)
            {
                break;
            }
        }
        try
        {
            Image img = Image.FromStream(new MemoryStream(File.ReadAllBytes(path)));
            return img;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public static string getVideoID(string url)
    {
        int index = url.IndexOf("watch?v=");
        if (index < 0)
        {
            return null;
        }
        else
        {
            url = url.Substring(index + "watch?v=".Length);
            return url;
        }
    }

    public static string getValidFileName(string filename)
    {
        // Illegal chars : \/:*?"<>|
        return filename.
            Replace("\\", "＼").
            Replace("/", "⁄").
            Replace(":", "˸").
            Replace("*", "⁎").
            Replace("?", "？").
            Replace("\"", "ʺ").
            Replace("<", "˂").
            Replace(">", "˃").
            Replace("|", "ǀ");
    }

    public static string getSearchString(string match)
    {
        return match
            .ToLower()
            .Replace(" ", "")
            .Replace(":", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("-", "")
            .Replace("'", "");
    }

    public static string getExtension(string input)
    {
        int index = input.LastIndexOf(".");
        if (index < 0)
        {
            return null;
        }
        else
        {
            return input.Substring(index);
        }
    }
}