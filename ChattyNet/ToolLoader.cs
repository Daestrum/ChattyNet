using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace ChattyNet
{
    // Each tool DLL gets its own unloadable context
    public class ToolLoadContext : AssemblyLoadContext
    {
        public ToolLoadContext() : base(isCollectible: true) { }
    }

    public static class ToolLoader
    {
        public static List<(object Instance, ToolLoadContext Context)> LoadTools(string folder)
        {
            var tools = new List<(object, ToolLoadContext)>();

            if (!Directory.Exists(folder))
                return tools;

            foreach (var dll in Directory.GetFiles(folder, "*.dll"))
            {
                try
                {
                    // Create a new unloadable context for this DLL
                    var alc = new ToolLoadContext();

                    // Load the assembly into this context
                    var asm = alc.LoadFromAssemblyPath(Path.GetFullPath(dll));

                    foreach (var type in asm.GetTypes())
                    {
                        // Must have a public bool Tool property
                        var toolProp = type.GetProperty("Tool");

                        if (toolProp != null && toolProp.PropertyType == typeof(bool))
                        {
                            var instance = Activator.CreateInstance(type);

                            if (instance != null && (bool)toolProp.GetValue(instance) == true)
                            {
                                tools.Add((instance, alc));
                            }
                            else
                            {
                                // If not used, unload the context immediately
                                alc.Unload();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading tool from {dll}: {ex.Message}");
                }
            }

            return tools;
        }
    }
}
