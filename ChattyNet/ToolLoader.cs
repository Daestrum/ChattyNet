using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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
                    if (type.GetInterface("ITool") != null)
                    {
                        var instance = Activator.CreateInstance(type);
                        if (instance != null)
                            tools.Add(instance);
                    }
                }
            }

            return tools;
        }
    }
}

