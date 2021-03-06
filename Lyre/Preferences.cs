﻿using System.Drawing;

using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Preferences // Preferences / Settings / Defaults
{
    [JsonProperty]
    public Color colorBackground = Color.FromArgb(30, 30, 30); // Darker gray // Color.FromArgb(37, 37, 38); 
    [JsonProperty]
    public Color colorForeground = Color.FromArgb(45, 45, 48); // Dark gray
    [JsonProperty]
    public Color colorFontDefault = Color.FromArgb(255, 255, 255); // White
    [JsonProperty]
    public Color colorAccent1 = Color.FromArgb(0, 84, 166); // Download blue
    [JsonProperty]
    public Color colorAccent2 = Color.FromArgb(0, 255, 144); // Util green
    [JsonProperty]
    public Color colorAccent3 = Color.FromArgb(237, 20, 91); // Radish red
    [JsonProperty]
    public Color colorAccent4 = Color.FromArgb(247, 148, 29); // Finished orange
    [JsonProperty]
    public Color colorAccent5 = Color.FromArgb(0, 255, 180); // Util green-ish // Gradient1_1
    [JsonProperty]
    public Color colorAccent6 = Color.FromArgb(78, 0, 255); // Berry purple // Gradient1_2
    [JsonProperty]
    public Color colorAccent7 = Color.FromArgb(255, 199, 0); // Golden-ish
    [JsonProperty]
    public Font fontDefault = new Font(new FontFamily("Segoe UI"), 14, GraphicsUnit.Pixel); // Default font
    [JsonProperty]
    public int formWidth = 1100;
    [JsonProperty]
    public int formHeight = 660;
    [JsonProperty]
    public int formTop = 100;
    [JsonProperty]
    public int formLeft = 100;
    [JsonProperty]
    public string tempDirectoy = "temp";
    [JsonProperty]
    public string downloadsDirectory = "downloads";
    [JsonProperty]
    public int maxActiveProcesses = 2; // 3
    [JsonProperty]
    public int maxDownloadContainerControls = 5; // 10
    [JsonProperty]
    public int maxVideoQualitySelector = 5; // 0 = 144p, 240p, 360p, 480p, 720p, 1080p, 1440p, 2160p, 4320p
    [JsonProperty]
    public int maxVideoFrameRateSelector = 2; // 24, 25, 30, 48, 50, 60
    [JsonProperty]
    public int maxAudioQualitySelector = 0; // AUTO, 128k, 192k, 256k, 320k
    [JsonProperty]
    public bool enableThumbnailAnimations = false;
    [JsonProperty]
    public bool canConvert = false;
    [JsonProperty]
    public int secondsCooldown = 10; // how long before downloading next video in queue
}
