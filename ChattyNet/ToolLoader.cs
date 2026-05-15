using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ChattyNet
{
    public static class ToolLoader
    {
        public static List<object> LoadTools(string folder)
        {
            var tools = new List<object>();

            if (!Directory.Exists(folder))
                return tools;

            foreach (var dll in Directory.GetFiles(folder, "*.dll"))
            {
                var asm = Assembly.LoadFrom(dll);

                foreach (var type in asm.GetTypes())
                {
                    // Must have a public bool Tool property
                    var toolProp = type.GetProperty("Tool");

                    if (toolProp != null && toolProp.PropertyType == typeof(bool))
                    {
                        var instance = Activator.CreateInstance(type);

                        if (instance != null && (bool)toolProp.GetValue(instance) == true)
                            tools.Add(instance);
                    }
                }

            }

            return tools;
        }
    }
}
