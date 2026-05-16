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

            var dllFiles = Directory.GetFiles(folder, "*.dll");
            foreach (var dll in dllFiles)
            {
                Logger.Write($"Found DLL: {dll}\n");
            }

            foreach (var dll in dllFiles)
            {
                var alc = new ToolLoadContext();
                
                var asm = alc.LoadFromAssemblyPath(Path.GetFullPath(dll));

                bool foundTool = false;

                foreach (var type in asm.GetTypes())
                {
                    var toolProp = type.GetProperty("Tool");

                    if (toolProp != null && toolProp.PropertyType == typeof(bool))
                    {
                        var instance = Activator.CreateInstance(type);

                        if (instance != null && (bool)toolProp.GetValue(instance) == true)
                        {
                            tools.Add((instance, alc));
                            foundTool = true;
                        }
                    }
                }

                if (!foundTool)
                    alc.Unload();
            }
            
            return tools;
        }
    }
}
