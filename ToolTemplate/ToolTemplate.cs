using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTemplate
{
    public class ToolTemplate
    {
        public string Name => "tool_name";  // unique name for the tool, e.g. "get_date"
        public string Description => "Returns ??";  // one line description of what the tool does  
        public string Schema => "{}";  // JSON schema for input parameters, e.g. { "type": "object", "properties": { "param1": { "type": "string" } }, "required": ["param1"] }
        public string Type => "output";  // input, output, action 
        public string CanUse => "free";  // restricted, paid, free
        public bool Tool => false; // is this a tool that can be called by the AI, or just a function for internal use by other tools
        public string Run(string jsonInput)  // jsonInput will be a JSON string that matches the Schema defined above
        {
            // what tool does goes here
            // return response to AI (JSON format)
            return "{}";
        }
    }    
}
