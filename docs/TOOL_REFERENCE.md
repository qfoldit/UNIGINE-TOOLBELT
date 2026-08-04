# Tool Reference — qFoldIT Toolbelt for UNIGINE 2 / UNIGINE 2 Sim

Auto-summarized from `registry.json` (2026-08-03). 80 tools across 22 categories.

Companion to UNIGINE's official MCPBridge Plugin (27 base tools) — see README.md for the relationship.

## Assets — Asset Tools

List, instantiate, and find project .node/.mesh/.mat assets by extension and name.

Source: `editor_plugin/Tools/AssetTools.cs`

- `asset_list`
- `asset_instantiate_node`
- `asset_find_by_extension`

## Audio — Audio Tools

ObjectSound source setup, one-shot playback, listener node, volume control.

Source: `editor_plugin/Tools/AudioTools.cs`

- `audio_add_source`
- `audio_play_one_shot`
- `audio_set_listener_node`
- `audio_set_source_volume`

## Camera — Camera Tools

Player/camera creation, dependency-free follow, clipping, FOV, screenshots.

Source: `editor_plugin/Tools/CameraTools.cs`

- `camera_create`
- `camera_set_follow`
- `camera_set_clipping`
- `camera_set_fov`
- `camera_screenshot`

## Components — Component Tools

Reflection-based generic add/remove/get/set/list for C# Components.

Source: `editor_plugin/Tools/ComponentTools.cs`

- `component_add`
- `component_remove`
- `component_set_property`
- `component_get_property`
- `component_list`

## BuildConsole — Console & Build Tools

Run console commands, read console variables, save the world.

Source: `editor_plugin/Tools/ConsoleTools.cs`

- `console_run_command`
- `console_get_variable`
- `world_save`

## Lighting — Lighting Tools

Create lights, set environment/fog, trigger GI reload, apply full lighting presets.

Source: `editor_plugin/Tools/LightingTools.cs`

- `light_create`
- `light_set_environment`
- `light_set_fog`
- `light_bake_gi`
- `light_apply_preset`

## Materials — Material Tools

12 material presets, bulk swap by name match, team-color split, preset listing.

Source: `editor_plugin/Tools/MaterialTools.cs`

- `material_apply_preset`
- `material_bulk_swap`
- `material_team_color_split`
- `material_list_presets`

## Measurement — Measurement Tools

Distance between nodes, per-node bounds, full-world bounds.

Source: `editor_plugin/Tools/MeasurementTools.cs`

- `measure_distance`
- `measure_bounds`
- `measure_world_bounds`

## Navigation — Navigation Tools

NavigationMesh bake, agents, obstacles, runtime destinations (Navigation add-on).

Source: `editor_plugin/Tools/NavigationTools.cs`

- `nav_bake_navmesh`
- `nav_add_agent`
- `nav_add_obstacle`
- `nav_set_destination`

## CodeGen — CodeGen Tools

Generates a WorldLogic component with real, bindable node references for named world nodes.

Source: `editor_plugin/Tools/NodeCodeGenTools.cs`

- `codegen_node_component`

## NodeWorkflow — Node Workflow Tools

Save/reload/variant/XML-export workflow for .node assets — UNIGINE's prefab-equivalent.

Source: `editor_plugin/Tools/NodeWorkflowTools.cs`

- `node_save_as_asset`
- `node_reload_from_asset`
- `node_instantiate_variant`
- `node_export_xml`

## Particles — Particle Tools

ObjectParticles spawning from assets, emission rate, color, stop control.

Source: `editor_plugin/Tools/ParticleTools.cs`

- `particles_spawn_from_asset`
- `particles_set_emission_rate`
- `particles_set_color`
- `particles_stop`

## Physics — Physics Tools

Rigid bodies, collision shapes, physics materials, raycasts, global gravity.

Source: `editor_plugin/Tools/PhysicsTools.cs`

- `physics_add_body`
- `physics_add_shape`
- `physics_set_material`
- `physics_raycast_query`
- `physics_set_gravity`

## Procedural — Procedural Placement & Arena

8 geometric placement patterns (grid, circle, arc, spiral, line, wave, helix, radial) plus a symmetrical arena generator.

Source: `editor_plugin/Tools/ProceduralPlacementTools.cs`

- `procedural_place`
- `arena_generate`

## Project — Project Setup

Standard folder scaffold plus a boilerplate GameManager WorldLogic class.

Source: `editor_plugin/Tools/ProjectSetupTools.cs`

- `project_setup`

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

## Stamps — Stamp Tools

Save a selection as a reusable stamp; place it anywhere with rotation; list saved stamps.

Source: `editor_plugin/Tools/StampTools.cs`

- `stamp_save`
- `stamp_place`
- `stamp_list`

## TagsLayers — Tags & Layers Tools

Free-text tag registry plus real IntersectionMask-backed named layers.

Source: `editor_plugin/Tools/TagsLayersTools.cs`

- `tag_assign`
- `tag_find_nodes`
- `layer_create`
- `layer_assign`

## UI — UI Tools

Widget-based UI: buttons, labels, panels, sliders, tracked by a lightweight name registry.

Source: `editor_plugin/Tools/UITools.cs`

- `ui_create_button`
- `ui_create_text`
- `ui_create_panel`
- `ui_create_slider`
- `ui_set_position`

## Utility — Editor Utility Tools

Batch rename and basic Engine/world info reporting.

Source: `editor_plugin/Tools/UtilityTools.cs`

- `batch_rename`
- `editor_get_engine_info`

## WorldManagement — World Management Tools

New/load/save-as/reload/info for the single active world.

Source: `editor_plugin/Tools/WorldManagementTools.cs`

- `world_new`
- `world_load`
- `world_save_as`
- `world_reload`
- `world_get_info`

## WorldState — World State Export

Exports the full world node graph (names, types, transforms, parents) to JSON for AI context.

Source: `editor_plugin/Tools/WorldStateExportTools.cs`

- `world_state_export`
