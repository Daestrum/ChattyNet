ChattyNet – C# Tool Framework for AI (Updated Architecture)

ChattyNet is a lightweight, high‑performance C# tool framework that allows AI models to discover, understand, and execute tools at runtime.
Tools are self‑describing, dynamically loaded, hot‑swappable, and stored entirely in memory for maximum speed.

Self‑Describing Tools
Each tool DLL declares its own:

schema

parameters

description

usage rules (free, restricted, etc.)

type (output, action, chain, etc.)

The host has no hard‑coded knowledge of any tool.
The AI reads the metadata directly from the DLL at runtime.

Dynamic Loading (No File Locks)
Tools are never loaded from disk directly.

Instead:

The host reads the DLL bytes

Loads them into a collectible AssemblyLoadContext (ALC)

Builds a full “DLL chain”:
bytes → ALC → assembly → instance → metadata

This avoids file locks and enables true hot‑swap.

Database‑Backed Tool Store
All tool DLLs are cached in a SQLite database:

DLL bytes

timestamps

metadata

On startup, tools load from the DB, not from disk.
Disk is only used when a tool is new or updated.

This makes startup extremely fast.

Live & Reserve Stores (Memory‑Resident Tools)
All tools live entirely in memory.

Live Store
Contains active tools with:

DLL bytes

ALC

assembly

instance

metadata

These are the tools the AI can call.

Reserve Store
Contains inactive tools that have been demoted or swapped out.
They remain fully loaded in memory and can be promoted instantly without disk or DB access.

Hot‑Swap Logic
The tool folder is continuously monitored.

New
A new DLL appears → load bytes → cache in DB → build chain → add to Live.

Modified
Timestamp changes → unload old ALC → reload bytes → update DB → rebuild chain.

Removed
DLL disappears → unload ALC → remove from Live/Reserve → delete from DB.

Auto‑Demotion
If Live is full, the least‑used or oldest tool is demoted to Reserve.

Manual Operations (not implemented yet)
Demote(name) — Move tool from Live → Reserve

Promote(name) — Move tool from Reserve → Live

Swap(A, B) — Exchange tools between Live and Reserve

All operations are instant because tools are already in memory.

Why This Matters
This architecture creates a fully memory‑resident, hot‑swappable tool ecosystem where:

tools can be added, updated, or removed at runtime

no DLL is ever locked

no restart is required

the AI always sees the latest tool metadata

updates are safe and leak‑free (ALCs are unloaded properly)

startup is fast due to DB caching

ChattyNet behaves like a miniature plugin engine designed specifically for AI tool use.

Typical start up log:

[2026-05-24 10:00:46] 
Built DLL chain for DateTool
[2026-05-24 10:00:46] 
Built DLL chain for GetDateAndTime
[2026-05-24 10:00:46] 
Built DLL chain for TimeTool
[2026-05-24 10:00:46] 
Built DLL chain for ToolChain
[2026-05-24 10:00:46] 
Built DLL chain for ToolRefernceGuide
[2026-05-24 10:00:46] 
Built DLL chain for WebToolRead
[2026-05-24 10:00:46] 
Built DLL chain for WindowsUpdateStatus
[2026-05-24 10:00:46] Map2: DLL 'DateTool' → Tool 'get_date'
[2026-05-24 10:00:46] Map2: DLL 'GetDateAndTime' → Tool 'get_date_and_time'
[2026-05-24 10:00:46] Map2: DLL 'TimeTool' → Tool 'get_time'
[2026-05-24 10:00:46] Map2: DLL 'ToolChain' → Tool 'chain_tools'
[2026-05-24 10:00:46] Map2: DLL 'ToolRefernceGuide' → Tool 'tool_reference_guide'
[2026-05-24 10:00:46] Map2: DLL 'WebToolRead' → Tool 'web_tool_read'
[2026-05-24 10:00:46] Map2: DLL 'WindowsUpdateStatus' → Tool 'windows_update_check'
[2026-05-24 10:10:32] [Refresher] UPDATED tool detected: DateTool
[2026-05-24 10:10:32] 
Reading real bytes for DateTool from disk at C:\chatty_tools
[2026-05-24 10:10:32] 
Built DLL chain for DateTool
[2026-05-24 10:10:32] Map2: DLL 'DateTool' → Tool 'get_date'
[2026-05-24 10:10:32] Map2: DLL 'GetDateAndTime' → Tool 'get_date_and_time'
[2026-05-24 10:10:32] Map2: DLL 'TimeTool' → Tool 'get_time'
[2026-05-24 10:10:32] Map2: DLL 'ToolChain' → Tool 'chain_tools'
[2026-05-24 10:10:32] Map2: DLL 'ToolRefernceGuide' → Tool 'tool_reference_guide'
[2026-05-24 10:10:32] Map2: DLL 'WebToolRead' → Tool 'web_tool_read'
[2026-05-24 10:10:32] Map2: DLL 'WindowsUpdateStatus' → Tool 'windows_update_check'
[2026-05-24 10:12:36] [10:12:36] ToolCall  ChatID=940034277  Tool=get_date  Args={}
[2026-05-24 10:12:36] [10:12:36] ToolReply ChatID=940034277  Reply={"current_date":"24-05-2026"}
