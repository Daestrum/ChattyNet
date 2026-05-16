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

        public MainWindow()
        {
            InitializeComponent();
            
            OutputBox.FontSize = 18;
            InputBox.FontSize = 18;

            _llm = new LlmClient("http://192.168.0.44:1234");

            var toolFolder = @"C:\chatty_tools";

            _tools = ToolLoader.LoadTools(toolFolder);
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
            var toolSpecs = BuildToolSpecs(visibleTools);

            // First LLM call
            var payload = new
            {
                model = "nvidia/nemotron-3-nano-omni",
                messages = context,
                tools = toolSpecs
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

                // Run tool
                var runMethod = tool.GetType().GetMethod("Run");
                var result = runMethod.Invoke(tool, new object[] { argsJson })?.ToString();

                // 🔵 LOG TOOL REPLY
                LogToolReply(callId, result ?? "{}");

                // Add tool result to buffer WITH ID
                AddMessage("tool", result ?? "{}", callId);

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
                return final;
            }

    
            // -------------------------
            // NORMAL ASSISTANT REPLY
            // -------------------------
            var text = msg.GetProperty("content").GetString();
                AddMessage("assistant", text);
                return text;
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