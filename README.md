C# tool framework for AI. Tools self‑describe their capabilities, inputs, and usage limits so an AI can reason about them.

Tools declare their own schemas, descriptions, and usage rules (free, restricted, etc.)

Tools are loaded dynamically as DLLs

Each tool enforces its own limits and behaviour

The host doesn’t need to know anything about a tool in advance

This creates a plug‑and‑play tool ecosystem where the AI can discover, validate, and call tools at runtime.

Expanding the tool store so tools are all read in to memory and are loaded and swapped there.
There will be a Live and Reserve set of tools, a simple Swap( ToolA, ToolB) will unload the DLL for A, and swap in ToolB DLL.
No tools are 'lost' if they go from live to reserve, they dont need to be read from disk again.
DLLs removed from tool folder are deleted from store (Live or Reserve).
Other commands:
     Demote  Live -> Reserve. 
     Promote Reserve -> live, with oldest tool/least used auto demoted if Live is full.
