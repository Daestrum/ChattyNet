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
        public string Run(string args)
        {
            return $"{{ \"dummy_result\": \"dummy_value\" }}";
        }
    }
}
