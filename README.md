C# tool framework for AI. Tools self‑describe their capabilities, inputs, and usage limits so an AI can reason about them.

Tools declare their own schemas, descriptions, and usage rules (free, restricted, etc.)

Tools are loaded dynamically as DLLs

Each tool enforces its own limits and behaviour

The host doesn’t need to know anything about a tool in advance

This creates a plug‑and‑play tool ecosystem where the AI can discover, validate, and call tools at runtime.
