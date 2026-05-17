using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChattyNet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<(object Instance, ToolLoadContext Context)> _tools;
        private LlmClient _llm;
        private List<Dictionary<string, object>> _messages = new();
        private const int MaxMessages = 10;
        private bool _isToolInUse = false;   // ⭐ SAFETY GUARD
        private string toolFolder = @"C:\chatty_tools";
        private List<object> _toolSpecs;
        public DLLStore dllStore;

        public MainWindow()
        {
            InitializeComponent();
            
            OutputBox.FontSize = 18;
            InputBox.FontSize = 18;

            _llm = new LlmClient("http://192.168.0.44:1234");
            

            ToolRefresher.Initialize(toolFolder);
            ToolRefresher.Start();   // ← leave commented for now

            _tools = ToolLoader.LoadTools(toolFolder);

            DebugToolList("Startup");
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var input = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            AppendOutput($"You: {input}");

            var response = await ProcessMessageAsync(input);

            AppendOutput($"ChattyNET: {response}");

            InputBox.Clear();
        }
        private void InputBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (InputBox.ContextMenu == null)
                return;

            // Only add once
            if (!InputBox.ContextMenu.Items.OfType<MenuItem>().Any(i => (string)i.Header == "Send"))
            {
                var sendItem = new MenuItem
                {
                    Header = "Send"
                };

                sendItem.Click += (s, ev) =>
                {
                    // Call your existing button click handler
                    SendButton_Click(null, null);
                };

                // Insert at top of menu
                InputBox.ContextMenu.Items.Insert(0, sendItem);
                InputBox.ContextMenu.Items.Insert(1, new Separator());
            }
        }

        private void AppendOutput(string text)
        {
            OutputBox.AppendText(text + "\n");
            OutputBox.ScrollToEnd();
        }
        private async Task<string> ProcessMessageAsync(string userInput)
        {
            // Add user message to rolling buffer
            AddMessage("user", userInput);

            // Build context + visible tools
            var context = BuildContext();
            var visibleTools = GetVisibleTools(userInput);
            _toolSpecs = BuildToolSpecs(visibleTools);

            // First LLM call
            var payload = new
            {
                model = "nvidia/nemotron-3-nano-omni",
                messages = context,
                tools = _toolSpecs
            };

            var doc = await _llm.ChatAsync(payload);

            var root = default(JsonElement);
            var msg = default(JsonElement);
            try
            {
                root = doc.RootElement;
                msg = root.GetProperty("choices")[0].GetProperty("message");
            }
            catch (Exception ex)
            {
                OutputBox.AppendText("\n=== JSON PARSE ERROR ===\n");
                OutputBox.AppendText(ex.ToString() + "\n");
                OutputBox.AppendText("\n=== RAW RESPONSE ===\n");
                OutputBox.AppendText(doc.RootElement.ToString() + "\n");
                return "JSON parse error — see output.";
            }

            // -------------------------
            // TOOL CALL?
            // -------------------------
            if (msg.TryGetProperty("tool_calls", out var toolCalls) &&
                toolCalls.ValueKind == JsonValueKind.Array &&
                toolCalls.GetArrayLength() > 0)
            {
                var call = toolCalls[0];
                var func = call.GetProperty("function");
                var toolName = func.GetProperty("name").GetString();
                var argsJson = func.GetProperty("arguments").GetString() ?? "{}";
                var callId = call.GetProperty("id").GetString();

                // 🔵 LOG TOOL CALL
                LogToolCall(callId, toolName, argsJson);

                // Find tool
                var tool = FindToolByName(toolName);
                if (tool == null)
                    return $"AI requested tool '{toolName}' but it was not found.";

                object result;

                // ⭐ SAFETY GUARD — prevent unload while tool is running
                _isToolInUse = true;
                try
                {
                    var runMethod = tool.GetType().GetMethod("Run");
                    result = runMethod.Invoke(tool, new object[] { argsJson })?.ToString();
                }
                finally
                {
                    _isToolInUse = false;
                }

                // 🔵 LOG TOOL REPLY
                LogToolReply(callId, result.ToString() ?? "{}");

                // Add tool result to buffer WITH ID
                AddMessage("tool", result.ToString() ?? "{}", callId);

                // Second LLM call (NO model, NO tools)
                var payload2 = new
                {
                    messages = BuildContext()
                };

                var doc2 = await _llm.ChatAsync(payload2);

                var final = doc2.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

                AddMessage("assistant", final);

                _isToolInUse = false;   // just in case

                ApplyRefresherChanges();
                return final;
            }

            // -------------------------
            // NORMAL ASSISTANT REPLY
            // -------------------------
            var text = msg.GetProperty("content").GetString();
                AddMessage("assistant", text);
                ApplyRefresherChanges();
            return text;
        }
        private void ApplyRefresherChanges()
        {
            if (_isToolInUse)
                return;

            // 1. Handle removed tools
            foreach (var removed in ToolRefresher.RemovedTools)
            {
                var entry = _tools.FirstOrDefault(t => t.Instance.GetType().Name == removed);
                if (entry.Instance != null)
                {
                    entry.Context.Unload();
                    _tools.Remove(entry);
                }

            }
            // 1b. Handle NEW tools
            foreach (var added in ToolRefresher.NewTools)
            {
                var newTools = ToolLoader.LoadTools(toolFolder);
                foreach (var t in newTools)
                {
                    if (t.Instance.GetType().Name == added)
                    {
                        _tools.Add(t);
                        Logger.Write($"[DEBUG] Added NEW tool: {added}");
                    }
                }
            }


            // 2. Handle updated tools (remove + reload)
            foreach (var changed in ToolRefresher.UpdatedTools)
            {
                var entry = _tools.FirstOrDefault(t => t.Instance.GetType().Name == changed);
                if (entry.Instance != null)
                {
                    entry.Context.Unload();
                    _tools.Remove(entry);
                }

                // Reload using ToolLoader
                var newTools = ToolLoader.LoadTools(toolFolder);
                foreach (var t in newTools)
                {
                    if (t.Instance.GetType().Name == changed)
                        _tools.Add(t);
                }
            }

            // 3. Rebuild schema
            _toolSpecs = BuildToolSpecs(GetVisibleTools(""));

            OutputBox.AppendText("tool count = " + _toolSpecs.Count);

            DebugToolList("AfterRefresh");

            _tools = ToolLoader.LoadTools(toolFolder);
            Logger.Write($"[DEBUG] Reloaded tools, count = {_tools.Count}");

            ToolRefresher.NewTools.Clear();
            ToolRefresher.UpdatedTools.Clear();
            ToolRefresher.RemovedTools.Clear();
        }


        private void AddMessage(string role, string content, string toolCallId = null)
        {
            if (toolCallId == null)
            {
                _messages.Add(new Dictionary<string, object>
                {
                    ["role"] = role,
                    ["content"] = content
                });
            }
            else
            {
                _messages.Add(new Dictionary<string, object>
                {
                    ["role"] = role,
                    ["tool_call_id"] = toolCallId,
                    ["content"] = content
                });
            }
            while (_messages.Count > 10)
                _messages.RemoveAt(0);
        }


        private List<object> BuildContext()
        {
            var list = new List<object>
    {
        new { role = "system", content = "You may call (free) tools when needed." }
    };

            foreach (var msg in _messages)
                list.Add(msg);

            return list;
        }

        private object? FindToolByName(string name)
        {
            return _tools
                    .Select(t => t.Instance)
                    .FirstOrDefault(inst =>
        inst.GetType().GetProperty("Name")?.GetValue(inst)?.ToString() == name);

        }

        private List<object> BuildToolSpecs(List<object> visibleTools)
        {
            var list = new List<object>();

            foreach (var tool in visibleTools)
            {
                var name = tool.GetType().GetProperty("Name")?.GetValue(tool)?.ToString();
                var desc = tool.GetType().GetProperty("Description")?.GetValue(tool)?.ToString();
                var schemaStr = tool.GetType().GetProperty("Schema")?.GetValue(tool)?.ToString();

                Dictionary<string, object> schemaObj;

                // If schema is empty or "{}", replace with valid OpenAI schema
                if (string.IsNullOrWhiteSpace(schemaStr) || schemaStr.Trim() == "{}")
                {
                    schemaObj = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>()
                    };
                }
                else
                {
                    schemaObj = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaStr);
                }

                list.Add(new
                {
                    type = "function",
                    function = new
                    {
                        name = name,
                        description = desc,
                        parameters = schemaObj
                    }
                });
            }

            return list;
        }

        private void LogToolCall(string chatId, string toolName, string argsJson)
        {
            var truncated = argsJson.Length > 100
                ? argsJson.Substring(0, 100) + "..."
                : argsJson;

            Logger.Write(
                $"[{DateTime.Now:HH:mm:ss}] ToolCall  ChatID={chatId}  Tool={toolName}  Args={truncated}"
            );
        }

        private void LogToolReply(string chatId, string replyJson)
        {
            var truncated = replyJson.Length > 100
                ? replyJson.Substring(0, 100) + "..."
                : replyJson;

            Logger.Write(
                $"[{DateTime.Now:HH:mm:ss}] ToolReply ChatID={chatId}  Reply={truncated}"
            );
        }

        private void DebugToolList(string label)
        {
            Logger.Write($"[DEBUG] Tool list at: {label}");

            foreach (var (Instance, _) in _tools)
            {
                var name = Instance.GetType().GetProperty("Name")?.GetValue(Instance)?.ToString();
                var type = Instance.GetType().Name;
                Logger.Write($"[DEBUG]   - {type} (Name={name})");
            }
        }

        private List<object> GetVisibleTools(string userInput)
        {
            var visible = new List<object>();

            foreach (var (Instance,_) in _tools)
            {
                var tool = Instance;

                var name = tool.GetType().GetProperty("Name")?.GetValue(tool)?.ToString();
                var canUse = tool.GetType().GetProperty("CanUse")?.GetValue(tool)?.ToString();

                // Restricted tool → only include if trigger word is present
                if (canUse != null && canUse.Equals("restricted", StringComparison.OrdinalIgnoreCase))
                {
                    var triggersProp = tool.GetType().GetProperty("HiddenTriggers");
                    var triggers = triggersProp?.GetValue(tool) as IEnumerable<string>;

                    if (triggers != null && triggers.Any(t => userInput.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        visible.Add(tool);
                }
                else
                {
                    visible.Add(tool);
                }

            }

            return visible;
        }

    }
}