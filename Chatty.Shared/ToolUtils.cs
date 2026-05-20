using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Chatty.Shared
{
    public static class ToolUtils
    {
            public static string WrapResult(int count, string layout, params string[] data)
            {
                var names = layout.Split(',')
                                  .Select(n => n.Trim())
                                  .ToArray();

                if (names.Length != data.Length || names.Length != count)
                {
                    return JsonSerializer.Serialize(new
                    {
                        error = "Tool metadata mismatch",
                        expected = names.Length,
                        actual = data.Length,
                        message = $"return_layout has {names.Length} fields but tool returned {data.Length} values."
                    });
                }

                var dict = new Dictionary<string, string>();
                for (int i = 0; i < names.Length; i++)
                    dict[names[i]] = data[i];

                return JsonSerializer.Serialize(dict);
            }
        }
    }



