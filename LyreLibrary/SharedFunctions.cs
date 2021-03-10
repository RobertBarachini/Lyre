using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Drawing;

using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Diagnostics;

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

    public static bool loadJSON<T>(ref T obj, string contents)
    {
        try
        {
            obj = JsonConvert.DeserializeObject<T>(contents);
            if(obj == null)
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            return false;
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

    public static Bitmap resizeImage(Image source)
    {
        double height = 144; // 144p
        double multi = source.Height / height;
        double width = source.Width / multi;
        return new Bitmap(source, new Size((int)width, (int)height));
    }

    public static bool saveJpeg(Image source, string path, long quality)
    {
        try
        {
            quality = limitLong(quality, 0, 100);

            // https://docs.microsoft.com/en-us/dotnet/framework/winforms/advanced/how-to-set-jpeg-compression-level

            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

            Encoder myEncoder = Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality); // quality 0 = most compression
            myEncoderParameters.Param[0] = myEncoderParameter;
            source.Save(path, jpgEncoder, myEncoderParameters);

            return true;
        }
        catch(Exception ex)
        {
            return false;
        }
    }

    public static void convertToJpg(string thumbnailPath)
    {
        try
        {
            string arguments = "";

            Process jpgEncoder = new Process();
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string thumbnailPathJpg = Path.ChangeExtension(thumbnailPath, ".jpg");
                arguments = "-y -i \"" + thumbnailPath + "\" \"" + thumbnailPathJpg + "\"";
                jpgEncoder.StartInfo.FileName = @"ffmpeg.exe";
                jpgEncoder.StartInfo.Arguments = arguments;
                jpgEncoder.StartInfo.CreateNoWindow = true;
                jpgEncoder.StartInfo.UseShellExecute = false;
                jpgEncoder.StartInfo.RedirectStandardOutput = false;
                jpgEncoder.StartInfo.RedirectStandardError = false;
                jpgEncoder.EnableRaisingEvents = false;
                jpgEncoder.Start();
                jpgEncoder.WaitForExit();
            }
        }
        catch (Exception ex) { }
    }

 

    public static Image getThumbnail(string thumbnailPath)
    {
        try
        {
            string pathSmall = Path.GetFileNameWithoutExtension(thumbnailPath) + "_144" + ".jpg"/*Path.GetExtension(historyItem.path_thumbnail)*/;
            pathSmall = Path.Combine(Path.GetDirectoryName(thumbnailPath), pathSmall);
            Image img;
            if (File.Exists(pathSmall) == false)
            {
                img = resizeImage(SharedFunctions.getImage(thumbnailPath));
                saveJpeg(img, pathSmall, 75);
            }
            else
            {
                img = getImage(pathSmall);
            }

            return img;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public static long limitLong(long input, long lowerBound, long upperBound)
    {
        if(input < lowerBound)
        {
            input = lowerBound;
        }
        if(input > upperBound)
        {
            input = upperBound;
        }
        return input;
    }

    public static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null;
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

    public static bool isLegitID(string id)
    {
        return new Regex("[0-9A-Za-z_-]{10}[048AEIMQUYcgkosw]").IsMatch(id);
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
            .Replace(";", "")
            .Replace("-", "")
            .Replace("_", "")
            .Replace("'", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("&", "")
            .Replace("/", "")
            .Replace("\\", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("|", "");
    }

    public static string getDateTimeStamp(DateTime dt)
    {
        string stamp = "";
        stamp = dt.Year.ToString().PadLeft(4, '0') + "_" 
            + dt.Month.ToString().PadLeft(2, '0') + "_" 
            + dt.Day.ToString().PadLeft(2, '0') + "_" 
            + dt.Hour.ToString().PadLeft(2, '0') + "_" 
            + dt.Minute.ToString().PadLeft(2, '0') + "_"
            + dt.Second.ToString().PadLeft(2, '0');
        return stamp;
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