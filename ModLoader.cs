using Allumeria;
using ModAPI.Core;

namespace Loader;

public class ModMeriaLoader : IExternalLoader
{
    public static Version Version { get; } = new(0, 1, 0);
    public void Init()
    {
        Logger.Init("Initializing ModMeria...");

        string fullVersion = Game.VERSION;
        Logger.Init($"Game version: {fullVersion}");

        Game.VERSION = fullVersion + $" with ModMeria {Version}";

        ModApi api = new();

    }
}