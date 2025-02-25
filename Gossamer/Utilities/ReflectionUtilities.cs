using System.Reflection;

namespace Gossamer.Utilities;

/// <summary>
/// Collection of reflection utilities.
/// </summary>
public static class ReflectionUtilities
{
    /// <summary>
    /// Loads an embedded resource from the executing assembly.
    /// </summary>
    /// <param name="name"></param>
    public static byte[] LoadEmbeddedResource(string name)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        using Stream? resourceStream = assembly.GetManifestResourceStream(name);

        byte[] resourceBytes = [];

        if (resourceStream != null)
        {
            resourceBytes = new byte[resourceStream.Length];
            resourceStream.ReadExactly(resourceBytes);
        }

        return resourceBytes;
    }
}