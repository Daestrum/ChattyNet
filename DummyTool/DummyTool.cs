using Chatty.Shared;

namespace DummyTool
{
    public class DummyTool
    {
        public string Name => "dummy_tool";
        public string Description => "Just a dummy tool for testing";
        public string Schema => "{}";
        public string Type => "output";
        public string CanUse => "free";
        public bool Tool => true;
        public int return_count => 1;
        public string return_layout => "dummy_result";

        public string Run(string args)
        {
            return ToolUtils.WrapResult(return_count, return_layout, "dummy_value");
        }
    }
}
