C# tool framework for AI.  
Tools self‑describe their capabilities, inputs, and usage limits so an AI can reason about them without any hard‑coded knowledge in the host.

Key Features
Self‑describing tools  
Each tool declares its own schema, description, usage rules (free, restricted, etc.), and type.
The AI reads this metadata directly from the DLL at runtime.

Dynamic loading  
Tools are loaded dynamically as DLLs.
The host never loads DLLs from disk directly — it reads the bytes and loads them into a collectible AssemblyLoadContext, avoiding file locks and enabling hot‑swap.

Tool autonomy  
Each tool enforces its own limits and behaviour.
The host doesn’t need to know anything about a tool in advance.

Plug‑and‑play ecosystem  
Tools can be added, updated, or removed simply by changing the DLLs in the tools folder.
The AI discovers, validates, and calls tools at runtime.

Live & Reserve Stores
Tools are stored entirely in memory:

Live store  
Contains active tools with fully built DLL chains (bytes → ALC → assembly → instance).

Reserve store  
Holds inactive tools that have been demoted or swapped out.
Tools in Reserve remain in memory and can be promoted instantly without re‑reading from disk.

Hot‑Swap Logic
Tools added to the folder are detected as New and loaded immediately.

Tools updated on disk are detected as Modified and reloaded.

Tools removed from the folder are detected as Removed and deleted from Live/Reserve.

If the Live store is full, the oldest or least‑used tool is automatically demoted to Reserve.

Manual Operations
Demote(name) — Move a tool from Live → Reserve

Promote(name) — Move a tool from Reserve → Live

Swap(A, B) — Swap two tools between Live and Reserve

This creates a flexible, memory‑resident tool ecosystem where tools can be swapped, updated, or removed at runtime without restarting the host or locking DLLs.
