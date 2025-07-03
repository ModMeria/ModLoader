using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;
using ModAPI.Abstractions;
using ModAPI.Core;
using ModAPI.Core.Logging;

namespace ModLoader
{
    public class ModMain 
    {
        public void Init() 
        {
            ConsoleLogger logger = new ConsoleLogger("ModMeria");
            Console.SetOut(new LoggerTextWriter());
            try
            {
                logger.Info("Initializing...");

                if (!Directory.Exists("Mods"))
                {
                    logger.Info("Creating Mods directory");
                    Directory.CreateDirectory("Mods");
                }

                if (!File.Exists("Mods/Mods.xml"))
                {
                    XElement root = new XElement("Mods");
                    
                    var modDirectories = Directory.GetDirectories("Mods");

                    foreach (var modDirectory in modDirectories)
                    {
                        XElement modElement = new XElement("Mod", new XElement("Name", Path.GetFileName(modDirectory)), new XElement("Enabled", true));
                        
                        root.Add(modElement);
                    }
                    
                    XDocument modDocument = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
                    modDocument.Save("Mods/Mods.xml");
                }
                else 
                {
                    XDocument modDocument = XDocument.Load("Mods/Mods.xml");
                    if (modDocument.Root == null)
                    {
                        logger.Warn("Mods/Mods.xml does not have root element. New mods could not be added. HINT: Delete Mods/Mods.xml. This WILL remove mod order!!!");
                        return;
                    }

                    XElement root = modDocument.Root;
                    var modDirectories = Directory.GetDirectories("Mods");

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
                        if (modName == null || !modDirectories.Contains(Path.Combine("Mods/", modName)))
                        {
                            logger.Warn("There was a removed mod");
                            modElement.Remove();
                        }
                    }
                    
                    modDocument.Save("Mods/Mods.xml");

                }
                
                var harmony = new Harmony("io.github.ggkkaa.modmeria");
                XDocument modDoc = XDocument.Load("Mods/Mods.xml");
                if (modDoc.Root == null)
                {
                    logger.Warn("Mods/Mods.xml does not have root element. Mods could not be added. HINT: Delete Mods/Mods.xml. This WILL remove mod order!!!");
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
                    
                    string modAssemblyPath = Path.Combine("Mods", modName, $"{modName}.dll");

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
                                modInstance?.Init(new ModApi());
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

