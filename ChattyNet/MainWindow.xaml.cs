using Chatty.Shared;
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
        public static MainWindow Instance { get; private set; }
        private List<(object Instance, ToolLoadContext Context)> _tools;
        private LlmClient _llm;
        private List<Dictionary<string, object>> _messages = new();
        private const int MaxMessages = 10;
        public bool _isToolInUse = false;   // ⭐ SAFETY GUARD
        private string toolFolder = @"C:\chatty_tools";
        private List<object> _toolSpecs;
        public DLLStore dllStore;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            // fire up dll db store
            var dllDbPath = System.IO.Path.Combine("C:/Temp", "dll_store.db");

            DBDllStore.Initialize($"Data Source={dllDbPath};Version=3;");

            OutputBox.FontSize = 18;
            InputBox.FontSize = 18;

            _llm = new LlmClient("http://192.168.0.44:1234");
            

            ToolRefresher.Initialize(toolFolder);
            ToolRefresher.Start();   // ← leave commented for now

            _tools = new List<(object Instance, ToolLoadContext Context)>();
            DebugToolList("Startup");
        }
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedModel == null)
            {
                OutputBox.AppendText("Pick a model first.\n");
                return;
            }

            _llm = new LlmClient("http://192.168.0.44:1234");

            OutputBox.AppendText($"Connected using model: {_selectedModel}\n");

            ToolRefresher.Initialize(toolFolder);
            ToolRefresher.Start();   // ← leave commented for now

         
            _tools = new List<(object Instance, ToolLoadContext Context)>();
        }
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var input = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            AppendOutput($"You: {input}");

            if (input.StartsWith("**"))
            {
                ProcessDirective(input);
                OutputBox.AppendText("Directive processed.\n");
                return;
            }

            var response = await ProcessMessageAsync(input);

            AppendOutput($"ChattyNET: {response}");

            InputBox.Clear();
        }
        void ProcessDirective(string directive)
        {
            var parts = directive.Substring(2).Split(' ', 2);
            var command = parts[0].ToLower();
            var argument = parts.Length > 1 ? parts[1] : "";

            Logger.Write($"[DEBUG] Directive raw: '{directive}'\n");
            Logger.Write($"[DEBUG] parts : '{parts[0]}'\n");
            Logger.Write($"[DEBUG] command : '{command}'");


            switch (command)
            {
                case "demote":
                    if (argument!="")
                    {
                        DLLStore.Instance.Demote(argument.Trim());
                    }
                    break;

                case "promote":
                    if (argument!="")
                    {
                        DLLStore.Instance.Promote(argument.Trim());
                    }
                    break;

                case "swap":
                    var args = argument.Split(',', 2);
                    if (args.Length == 2)
                        DLLStore.Instance.Swap(args[0].Trim(), args[1].Trim());
                    else
                        OutputBox.AppendText("Usage: /swap ToolA,ToolB\n");
                    break;

                case "listtools":
                    {
                        var live = DBDllStore.GetLiveTools();
                        OutputBox.AppendText("Live Tools:\n" + string.Join("\n", live) + "\n");
                        break;
                    }

                case "reservetools":
                    {
                        var reserve = DBDllStore.GetReserveTools();
                        OutputBox.AppendText("Reserve Tools:\n" + string.Join("\n", reserve) + "\n");
                        break;
                    }



                default:
                    OutputBox.AppendText($"Unknown directive: {command}\n");
                    break;
            }
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
        private string _selectedModel;

        private void ModelRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string modelId)
            {
                _selectedModel = modelId;
                OutputBox.AppendText($"Selected model: {_selectedModel}\n");
            }
        }

        private async Task<string> ProcessMessageAsync(string userInput)
        {
            // Add user message to rolling buffer
            AddMessage("user", userInput);

            // Build context + visible tools
            var context = BuildContext();
            //var visibleTools = GetVisibleTools(userInput);
            //_toolSpecs = BuildToolSpecs(visibleTools);
            
            _toolSpecs = DLLStore.Instance.ConvertSchemaToToolList(DLLStore.Instance._lastToolSpecJson);

            // First LLM call
            var payload = new
            {
                model = "nvidia/nemotron-3-nano-omni",
                //model = _selectedModel ?? "nvidia/nemotron-3-nano-omni",
                //model = "google/gemma-4-e4b",   // <===   Will not work with tool calls until we handle tool spec format differences (e.g. "function" vs "tool" wrapper, and "parameters" vs "args_schema")
                //model = _selectedModel ?? "google/gemma-4-e4b",
                //model = "mistralai/devstral-small-2-2512",
                //model = "qwen/qwen3.6-35b-a3b",
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
                    if (toolName == "chain_tools")  
                    {
                        result = GoRunTheTools(argsJson);
                    }
                    else
                    {
                        var runMethod = tool.GetType().GetMethod("Run");
                        result = runMethod.Invoke(tool, new object[] { argsJson })?.ToString();
                    }
                }
                finally
                {
                    _isToolInUse = false;
                }

                // 🔵 LOG TOOL REPLY
                LogToolReply(callId, result.ToString() ?? "{}");
                //Logger.Write(JsonSerializer.Serialize(BuildContext(), new JsonSerializerOptions { WriteIndented = true }));

                // Add tool result to buffer WITH ID
                AddMessage("tool", result.ToString() ?? "{}", callId);

                // Second LLM call (NO model, NO tools)
                var payload2 = new
                {
                    messages = BuildContext()
                };
                //OutputBox.AppendText("\n=== SECOND PASS CONTEXT ===\n");
                //OutputBox.AppendText(JsonSerializer.Serialize(BuildContext(), new JsonSerializerOptions { WriteIndented = true }));

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

            _toolSpecs = DLLStore.Instance.ConvertSchemaToToolList(DLLStore.Instance._lastToolSpecJson);

        }
        private string GoRunTheTools(string argsJson)
        {
            Logger.Write("GoRunTheTools called with args: " + argsJson);

            var results = new List<object>();
            string lastResult = null;

            try
            {
                var doc = JsonDocument.Parse(argsJson);
                var steps = doc.RootElement.GetProperty("steps");

                foreach (var step in steps.EnumerateArray())
                {
                    string toolName = step.GetProperty("tool_name").GetString();
                    bool forward = step.TryGetProperty("forward", out var fwdProp) && fwdProp.GetBoolean();

                    // Parse args into a mutable dictionary
                    var argsElement = step.GetProperty("args");
                    var argsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText())
                                   ?? new Dictionary<string, object>();

                    // Inject previous result if forward = true
                    if (forward && lastResult != null)
                    {
                        argsDict["input"] = lastResult;
                    }

                    string toolArgsJson = JsonSerializer.Serialize(argsDict);

                    // 1. Find the tool instance
                    var toolInstance = FindToolByName(toolName);
                    if (toolInstance == null)
                    {
                        results.Add(new
                        {
                            tool = toolName,
                            error = "Tool not found"
                        });
                        continue;
                    }

                    // 2. Run the tool
                    string toolResult;
                    try
                    {
                        var runMethod = toolInstance.GetType().GetMethod("Run");
                        toolResult = runMethod.Invoke(toolInstance, new object[] { toolArgsJson })?.ToString();
                    }
                    catch (Exception ex)
                    {
                        toolResult = $"ERROR: {ex.Message}";
                    }

                    // Save for forwarding
                    lastResult = toolResult;

                    // 3. Add structured result
                    results.Add(new
                    {
                        tool = toolName,
                        args = argsDict,
                        result = toolResult
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    error = "Failed to run tool chain",
                    details = ex.Message
                });
            }

            // 4. Return structured JSON Nemo can reason with
            return JsonSerializer.Serialize(results);
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

            while (_messages.Count > 20)
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

        private object? FindToolByName(string toolName)
        {
            if (!DLLStore.Instance.ToolNameToDllName.TryGetValue(toolName, out var dllName))
                return null;

            if (!DLLStore.Instance.LiveDllStore.TryGetValue(dllName, out var entry))
                return null;

            return entry.Instance;
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