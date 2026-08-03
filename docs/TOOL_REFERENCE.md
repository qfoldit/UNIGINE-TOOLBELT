# Tool Reference — qFoldIT Toolbelt for UNIGINE 2 / UNIGINE 2 Sim

Auto-summarized from `registry.json` (2026-08-02). 25 tools across 9 categories.

Companion to UNIGINE's official MCPBridge Plugin (27 base tools) — see README.md for the relationship.

## Scene — Scene Tools

Spawn, transform, clone, delete, parent, list, and find nodes in the loaded world.

Source: `editor_plugin/Tools/SceneTools.cs`

- `spawn_primitive`
- `transform_node`
- `clone_node`
- `delete_node`
- `parent_node`
- `world_list_nodes`
- `world_find_by_name`

## Materials — Material Tools

12 material presets, bulk swap by name match, team-color split, preset listing.

Source: `editor_plugin/Tools/MaterialTools.cs`

- `material_apply_preset`
- `material_bulk_swap`
- `material_team_color_split`
- `material_list_presets`

## Procedural — Procedural Placement & Arena

8 geometric placement patterns (grid, circle, arc, spiral, line, wave, helix, radial) plus a symmetrical arena generator.

Source: `editor_plugin/Tools/ProceduralPlacementTools.cs`

- `procedural_place`
- `arena_generate`

## Stamps — Stamp Tools

Save a selection as a reusable stamp; place it anywhere with rotation; list saved stamps.

Source: `editor_plugin/Tools/StampTools.cs`

- `stamp_save`
- `stamp_place`
- `stamp_list`

## Project — Project Setup

Standard folder scaffold plus a boilerplate GameManager WorldLogic class.

Source: `editor_plugin/Tools/ProjectSetupTools.cs`

- `project_setup`

## WorldState — World State Export

Exports the full world node graph (names, types, transforms, parents) to JSON for AI context.

Source: `editor_plugin/Tools/WorldStateExportTools.cs`

- `world_state_export`

## CodeGen — CodeGen Tools

Generates a WorldLogic component with real, bindable node references for named world nodes.

Source: `editor_plugin/Tools/NodeCodeGenTools.cs`

- `codegen_node_component`

## Assets — Asset Tools

List, instantiate, and find project .node/.mesh/.mat assets by extension and name.

Source: `editor_plugin/Tools/AssetTools.cs`

- `asset_list`
- `asset_instantiate_node`
- `asset_find_by_extension`

## BuildConsole — Console & Build Tools

Run console commands, read console variables, save the world.

Source: `editor_plugin/Tools/ConsoleTools.cs`

- `console_run_command`
- `console_get_variable`
- `world_save`
