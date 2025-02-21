using System.Text.Json.Serialization;

using Gossamer.Utilities;

namespace Gossamer.Frontend;

public class GuiParameters
{
    /// <summary>
    /// Name and title of the <see cref="Window"/>.
    /// </summary>
    [JsonIgnore]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Determines whether or not the graphics debugging layer is enabled. Defaults to true in debug builds.
    /// </summary>
    [JsonIgnore]
    public bool EnableDebug { get; set; } = false;

    /// <summary>
    /// Path to the icon to be set the for the <see cref="Window"/>.
    /// </summary>
    [JsonIgnore]
    public string IconPath { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based index of the monitor to initially display the window in.
    /// Zero is always the system main monitor.
    /// </summary>
    public int StartupMonitor { get; set; }
    /// <summary>
    /// Whether or not to initially fullscreen the window.
    /// </summary>
    public bool StartupFullscreen { get; set; }

    /// <summary>
    /// Initial position of the window.
    /// </summary>
    [JsonConverter(typeof(JsonVector2Converter))]
    public Vector2? Position { get; set; }
    /// <summary>
    /// Initial size of the window.
    /// </summary>
    [JsonConverter(typeof(JsonVector2Converter))]
    public Vector2? Size { get; set; }

    /// <summary>
    /// Size of the edges of the <see cref="Window"/>.
    /// </summary>
    [JsonIgnore]
    public Vector4 SizeOfFrame { get; set; } = new Vector4(3.0f, 32.0f, 3.0f, 3.0f);

    /// <summary>
    /// Color of the edges of the <see cref="Window"/>.
    /// </summary>
    [JsonConverter(typeof(JsonColorConverter))]
    public Color ColorOfFrame { get; set; } = Color.UnpackRGB(0x2B2A33);
    /// <summary>
    /// Color of the client area of the <see cref="Window"/>.
    /// </summary>
    [JsonConverter(typeof(JsonColorConverter))]
    public Color ColorOfBackground { get; set; } = Color.UnpackRGB(0x1f1e25);
}
