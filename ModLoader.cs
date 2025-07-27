using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;
using ModAPI.Abstractions;
using ModAPI.Core;
using ModAPI.Core.Logging;
using SingleFileExtractor.Core;

namespace ModLoader
{
    public class ModMain 
    {
        public static void Init() 
        {
            ConsoleLogger logger = new ConsoleLogger("ModMeria");
            Console.SetOut(new LoggerTextWriter());
            try
            {
                logger.Info("Initializing...");

/*                if (!File.Exists("PocketBlocks.dll"))
                {
                    var reader = new ExecutableReader("PocketBlocks");

                    if (reader.IsSingleFile)
                    {
                        var bundle = reader.Bundle;

                        var entry = bundle.Files
                            .FirstOrDefault(e => Path.GetFileName(e.RelativePath).EndsWith("PocketBlocks.dll", StringComparison.OrdinalIgnoreCase));

                        if (entry != null)
                        {
                            entry.ExtractToFile("PocketBlocks.dll");
                            Console.WriteLine("Extracted PocketBlocks.dll successfully.");
                        }
                        else
                        {
                            Console.WriteLine("PocketBlocks.dll not found in manifest entries.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Not a single-file executable or unsupported format.");
                    }
                }*/
                
                if (!Directory.Exists("mods"))
                {
                    logger.Info("Creating mods directory");
                    Directory.CreateDirectory("mods");
                }

                if (!File.Exists("mods/mods.xml"))
                {
                    XElement root = new XElement("mods");
                    
                    var modDirectories = Directory.GetDirectories("mods");

                    foreach (var modDirectory in modDirectories)
                    {
                        XElement modElement = new XElement("Mod", new XElement("Name", Path.GetFileName(modDirectory)), new XElement("Enabled", true));
                        
                        root.Add(modElement);
                    }
                    
                    XDocument modDocument = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
                    modDocument.Save("mods/mods.xml");
                }
                else 
                {
                    XDocument modDocument = XDocument.Load("mods/mods.xml");
                    if (modDocument.Root == null)
                    {
                        logger.Warn("mods/mods.xml does not have root element. New mods could not be added. HINT: Delete mods/mods.xml. This WILL remove mod order!!!");
                        return;
                    }

                    XElement root = modDocument.Root;
                    var modDirectories = Directory.GetDirectories("mods");

                    foreach (var modDirectory in modDirectories)
                    {
                        var modName = Path.GetFileName(modDirectory);
                        var modElement = root.Elements("Mod").FirstOrDefault(mod => mod.Element("Name")?.Value == modName);

                        if (modElement == null)
                        {
                            logger.Debug($"New mod {modName} found. Adding to mod list");
                            XElement newModElement = new XElement("Mod", new XElement("Name", modName), new XElement("Enabled", true));
                            root.Add(newModElement);
                        }
                    }

                    foreach (var modElement in root.Elements("Mod").ToList())
                    {
                        var modName = modElement.Element("Name")?.Value;
                        if (modName == null || !modDirectories.Contains(Path.Combine("mods/", modName)))
                        {
                            logger.Warn("There was a removed mod");
                            modElement.Remove();
                        }
                    }
                    
                    modDocument.Save("mods/mods.xml");

                }
                
                var harmony = new Harmony("io.github.ggkkaa.modmeria");
                XDocument modDoc = XDocument.Load("mods/mods.xml");
                if (modDoc.Root == null)
                {
                    logger.Warn("mods/mods.xml does not have root element. mods could not be added. HINT: Delete mods/mods.xml. This WILL remove mod order!!!");
                    return;
                }
                
                logger.Info("Loading mods...");
                
                XElement mods = modDoc.Root;

                foreach (var modElement in mods.Elements("Mod"))
                {
                    var modName = modElement.Element("Name")?.Value;
                    var isEnabled = bool.Parse(modElement.Element("Enabled")?.Value ?? "false");

                    if (modName == null || !isEnabled)
                    {
                        logger.Warn($"Skipping mod {modName}");
                        continue;
                    }
                    
                    string modAssemblyPath = Path.Combine("mods", modName, $"{modName}.dll");

                    if (File.Exists(modAssemblyPath))
                    {
                        try
                        {
                            var modAssembly = Assembly.LoadFrom(modAssemblyPath);
                            harmony.PatchAll(modAssembly);
                            logger.Info($"Mod {modName} loaded successfully");

                            var modType = modAssembly.GetType($"{modName}.Mod");
                            if (modType != null && typeof(IMod).IsAssignableFrom(modType))
                            {
                                
                                var modInstance = Activator.CreateInstance(modType) as IMod;
                                IModApi api = new ModApi();
                                modInstance?.Init(api);
                            } else {
                                logger.Warn($"Mod {modName} could not be Init");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Mod {modName} couldn't be loaded: {ex.Message}");
                            if (ex.StackTrace != null)
                            {
                                logger.Debug(ex.StackTrace);
                            }
                        }
                    }
                    else
                    {
                        logger.Warn($"Mod {modName} has no .dll!");
                    }
                }
                
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                logger.Info("Loaded!");
            }
            catch (Exception ex)
            {
                logger.Error("Error during Harmony initialization: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
    
}

