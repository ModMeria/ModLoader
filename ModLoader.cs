using System.Reflection;
using Allumeria;
using ModAPI.Abstractions;
using ModAPI.Core;

namespace Loader;

public class ModMeriaLoader : IExternalLoader
{
    public static Version Version { get; } = new(0, 1, 0);
    
    private readonly string _modsDirectory = "mods";
    private readonly ModApi _api = new();
    
    public void Init()
    {
        Logger.Init("Initializing ModMeria...");

        string fullVersion = Game.VERSION;
        Logger.Init($"Game version: {fullVersion}");

        Game.VERSION = fullVersion + $" with ModMeria {Version}";
        
        LoadMods();
    }

    private void LoadMods()
    {
        if (!Directory.Exists(_modsDirectory)) // This is dead code since modloader is ALWAYS in modsDirectory, but I included it just in case
        {
            Directory.CreateDirectory(_modsDirectory);
        }
        
        var modFolders = Directory.GetDirectories(_modsDirectory);

        var modNames = modFolders.Select(Path.GetFileName).ToList();

        if (modNames.Count == 0)
        {
            Console.WriteLine("No mods found.");
            return;
        }

        Logger.Info($"Found {modNames.Count} mod{(modNames.Count == 1 ? "" : "s")}:");
        for (var i = 0; i < modNames.Count; i++)
        {
            Logger.Info($"{i + 1}) {modNames[i]}");
        }
        Logger.Info("");
        
        foreach (var folder in modFolders)
        {
            var modName = Path.GetFileName(folder);
            var modDllPath = Path.Combine(folder, "mod.dll");

            if (!File.Exists(modDllPath))
            {
                Logger.Error($"Mod {modName} is invalid! No Mod.dll found!");
                continue;
            }

            try
            {
                Logger.Info($"Loading {modName}...");
                var assembly = Assembly.LoadFrom(modDllPath);

                var modTypes = assembly.GetTypes()
                    .Where(t => typeof(IMod).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToList();
                ;

                if (modTypes.Count == 0)
                {
                    Logger.Error($"Mod {modName} is invalid! No IMod implementation found!");
                    continue;
                }

                foreach (var type in modTypes)
                {
                    var mod = (IMod)Activator.CreateInstance(type)!;
                    mod.Init(_api);
                    Logger.Info($"Finished loading mod {modName} ({type.FullName})");
                }
            }
            catch (Exception exception)
            {
                Logger.Error($"Failed to load mod {modName}: {exception.Message}");
            }
        }
    }
}