using Chatty.Shared;

namespace $safeprojectname$
{
    public class $safeprojectname$
{
        public string Name => "tool_name"; //tool name, should be unique across all tools
        public string Description => "What it does."; //description of the tool, should be concise and informative
        public string Schema => "{}"; //json schema for the input, should be a valid json schema, if no input needed, use empty json object
        public string Type => "output"; //type of the tool, can be "input" or "output", input means the tool will receive input from user, output means the tool will return output to user
        public string CanUse => "free"; //can use condition, can be "free", "paid", "restricted", free means anyone can use, paid means only paid users can use, restricted means only specific users can use
        public bool Tool => true; //whether this is a tool, should be true for all tools
        public int return_count => 1; //number of return values, should be a positive integer
        public string return_layout => "return_name"; //layout of the return values, should be a string that describes the return values, can be used to format the output, for example, if return_count is 2 and return_layout is "name, age", then the output will be "name: value1, age: value2"

        public string Run(string jsonInput)
        {
            // code for the tool
            // example of tool return
            // return ToolUtils.WrapResult(return_count, return_layout, data);
            return "";  // remove this and usel line above in use, this is just a placeholder to make the code compile
        }

    }
}
