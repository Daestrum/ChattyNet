namespace DummyTool
{
    public class DummyTool
    {
        public bool Tool => true;
        public string Name => "dummy_tool";
        public string Description => "A test tool.";
        public string Schema => "{}";
        public string CanUse => "free";
        public string Type => "output";
        public string Run(string args)
        {
            return "{\"result\":\"dummy ok\"}";
        }
    }
}
