using System;
using System.Collections.Generic;
using System.Text;

namespace TimeTool
{
    public enum ToolType
    {
        Output,
        Action,
        Transform,
        Restricted
    }

    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        string Schema { get; }
        ToolType Type { get; }
        string CanUse { get; }
        string Run(string jsonInput);
    }
}
