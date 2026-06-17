# Project Progress — Untitled Military Strategy Game

**Engine**: Godot 4 .NET (C#) | **As of**: June 2026 | **Phase**: Prototype (Phase 2)

---

## What Exists Right Now

### World & Camera
- `World/GameCamera.cs` — Perspective Camera3D, ~50° downward pitch (Civ 6 style). Fixed-pitch zoom (scales height + pull-back distance together so angle stays constant). WASD + arrow keys pan the focus point.
- **Right-click drag to pan**: Holding RMB and dragging moves the camera focus by projecting both mouse positions onto the Y=0 ground plane and offsetting by the world-space delta. Short RMB click (< 6px movement) still issues move orders.
- `World/Baseplate.cs` — 80×80 tile flat ground plane. Creates PlaneMesh + StaticBody3D collider at runtime. `MapCenter()` returns `(40, 0, 40)`.

### Unit System

#### Architecture
- `Entities/Units/BaseUnit.cs` — `CharacterBody3D`. Wraps four components: `HealthComponent`, `MovementComponent`, `SelectionComponent`, `CombatComponent` (optional — `GetNodeOrNull`). Implements `IDamageable`, `ISelectable`, `IOrderReceiver`.
- Component pattern: each component is a child Node; `BaseUnit._Ready()` grabs them and wires events.
- `Data/UnitData.cs` — `[GlobalClass]` Godot Resource. Fields: `UnitName`, `UnitType` (enum), `MaxHealth`, `MoveSpeed`, `Damage`, `AttackRange`, `AttackCooldown`, `Cost`, `TrainingTime`, `Scene` (PackedScene back-ref), soft counter multipliers (`DamageVsInfantry`, `DamageVsVehicle`, `DamageVsAircraft`, `DamageVsShip`, `DamageVsBuilding`).

#### All 7 Infantry Units (fully implemented as .tscn + .tres resources)

| Unit | HP | Speed | Damage | Range | Cooldown | Cost | Notes |
|---|---|---|---|---|---|---|---|
| Rifleman | 100 | 6.0 | 8 | 6 | 0.8s | 50 | Balanced generalist |
| Machine Gunner | 110 | 5.0 | 6 | 5 | 0.3s | 90 | High vs Infantry (1.4x), weak vs vehicles |
| Sniper | 65 | 5.5 | 35 | 12 | 2.2s | 120 | Longest range, highest single-shot damage (2.2x vs infantry) |
| RPG Soldier | 90 | 5.0 | 25 | 7 | 1.8s | 100 | Anti-vehicle (2.5x), anti-building (1.6x) |
| Engineer | 80 | 6.0 | — | — | — | 60 | No combat component |
| Medic | 75 | 6.0 | — | — | — | 70 | No combat component |
| Builder | 100 | 6.0 | — | — | — | 50 | UnitType=Builder (4), no combat |

Scene files: `Scenes/Units/{UnitName}.tscn`
Data resources: `Data/Units/{UnitName}Data.tres`

#### Unit Visuals
- `Entities/Units/Visual/UnitCubePlaceholder.cs` — BoxMesh (0.5×0.8×0.5). Blue (`#4D8CFF`) for friendly, red (`#F24033`) for hostile. Color set at spawn from `PlayerManager.AreHostile()`.
- **Name labels**: 4× `Label3D` nodes on each face of the cube, billboard disabled, rotated per face (0°, 90°, 180°, -90°), showing the unit's `UnitData.UnitName`.

#### Components
- `HealthComponent` — `MaxHealth`, `CurrentHealth`, `IsDead`. Emits `Died(attackerId)`, `HealthChanged`, `DamageStateChanged` (at 50% and 25% crossings). `InitHealth()` resets current to max (called from `BaseUnit.ApplyData()` after setting `MaxHealth` so child-before-parent _Ready order doesn't corrupt HP ratio).
- `MovementComponent` — `CharacterBody3D`-based movement. `MoveTo(Vector3)`, `Stop()`. Emits `ArrivedAtDestination` signal.
- `SelectionComponent` — `OwnerId` (int). TorusMesh ring that toggles visibility on select/deselect. Implements `ISelectable`.
- `CombatComponent` (optional) — Auto-targeting: scans `SelectionRegistry` every physics frame for the nearest hostile unit within `AttackRange`. Fires via `EventBus.RaiseUnitFired(from, to)` and calls `TakeDamage`. Soft counter multipliers applied per `UnitType`. `LoadFromData(UnitData)` sets all fields from the resource.

### Friend/Foe System
- `OwnerId` (int) on `SelectionComponent` and `BaseBuilding`. Player 1 = friendly (OwnerId=1), enemies = OwnerId=2.
- `PlayerManager.AreHostile(a, b)`: same ID = friendly; no registered `PlayerContext` = hostile for different IDs; registered: compare `TeamId`.
- `NetworkManager.Instance.LocalPlayerId` = `Multiplayer.GetUniqueId()`, defaults to 1 in offline/singleplayer.
- Selection is restricted to friendly units only. Hover range ring works on all units.

### Selection & Orders
- `Systems/SelectionRegistry.cs` — Static list of all live `ISelectable` entities. All combat, selection, and health-bar systems iterate this.
- `Systems/SelectionManager.cs` — Full-screen `Control` overlay (CanvasLayer, MouseFilter=Ignore). Handles all input and all screen-space drawing.
  - **Single click**: selects nearest friendly unit within 40px screen radius.
  - **Box drag**: selects all friendly units whose screen projections fall inside the drag rectangle.
  - **Shift+click**: additive selection toggle.
  - **F**: select all friendly units visible on screen.
  - **C**: deselect all.
  - **Right-click** (short tap): issue `MoveOrder` to ground-plane raycast point. Ctrl+right-click = add waypoint.

### Screen-Space Drawing (all in SelectionManager._Draw())
- **Health bars**: Fixed 36×5px bars above every unit in `SelectionRegistry`. Background is always full width (black, 60% opacity). Fill color scales green→yellow→red via `Color.FromHsv(ratio / 3f, 1, 0.9)`. Ratio clamped 0–1.
- **Unit routes**: Dashed lines from unit position through each waypoint. First segment green, subsequent waypoints yellow.
- **Selection box**: Semi-transparent green rectangle while dragging.
- **Click indicators**: Fading circle+crosshair at the move target position (green = move, yellow = waypoint).
- **Hover range ring**: 48-segment polyline circle projected from 3D world at the hovered unit's `AttackRange` radius. Red (`#F23333`), 1.5px wide. Shows for any unit (friendly or hostile). Hidden if `AttackRange == 0`.
- **Gunfire tracers**: Red lines (`#FF2619`) drawn in screen space between muzzle and hit positions. Fade out over 0.12s. Fired from `CombatComponent` via `EventBus.OnUnitFired`, consumed by `SelectionManager`.

### Scene Layout (Game.tscn)
- 7 **friendly units** (OwnerId=1) clustered near center at X=37–43, Z=40.
- 7 **enemy units** (OwnerId=2) placed in a circle radius ~30 from map center (40,40), out of range of all friendly units at spawn:
  - EnemyRifleman1 (70, 0, 40)
  - EnemyMachineGunner1 (59, 0, 63)
  - EnemySniper1 (33, 0, 69)
  - EnemyRPGSoldier1 (13, 0, 53)
  - EnemyEngineer1 (13, 0, 27)
  - EnemyMedic1 (33, 0, 11)
  - EnemyBuilder1 (59, 0, 17)

### Autoloads / Global Systems
- `Autoloads/EventBus.cs` — Static C# events (not Godot signals) for cross-system communication. Current events: `OnSelectionChanged`, `OnUnitDied`, `OnEconomyUpdated`, `OnUnitFired(Vector3 from, Vector3 to)`.
- `Autoloads/PlayerManager.cs` — Manages `PlayerContext` list. `LocalPlayerId`, `AreHostile(a, b)`, `GetPlayer(id)`.
- `Autoloads/NetworkManager.cs` — Wraps `Multiplayer.GetUniqueId()`. Defaults to ID=1 offline.
- `Autoloads/GameManager.cs` — Game state machine (stub, not yet doing win condition checks).
- `Systems/EconomySystem.cs` — Stub, iterates `IIncomeSource` implementations per tick.

### UI
- `UI/MinimapPanel.cs` — 244×244px minimap (bottom-right). True overhead view with real trapezoid viewport indicator projected from the perspective camera frustum. Click/drag to move camera focus.
- `UI/PlacementTestPanel.cs` — hidden developer tool, toggled by the **comma (`,`)** key. Scans `Data/Units/` and `Data/Buildings/` for `.tres` files and lists every unit/building as sorted buttons in two scrollable columns (auto-populates — no hardcoded list). Clicking an entry records it into public `SelectedScene`/`SelectedName` properties (ready for a future placement system) and logs to console. Only the inner panel captures mouse input (root `MouseFilter=Ignore`, panel `Stop`), so world clicks pass through empty areas. **Placement itself is intentionally not wired up yet.** Note: `DirAccess` scanning of `res://` works in the editor (F5) but not in exported builds where `.tres` files are remapped — fine for a dev-only tool.

### Core Interfaces & Enums
- `IDamageable` — `MaxHealth`, `CurrentHealth`, `IsDead`, `TakeDamage(float, int)`
- `ISelectable` — `OwnerId`, `IsSelected`, `Select()`, `Deselect()`
- `IOrderReceiver` — `IssueOrder(IOrder)`, `QueueOrder(IOrder)`, `ClearOrders()`
- `IIncomeSource`, `IProducer` — stubs for economy/production
- Enums: `UnitType` (Infantry=0, Vehicle=1, Aircraft=2, Ship=3, Builder=4), `BuildingType`, `GameState`, `TerrainType`, `CrystalType`, `ResearchCategory`, `MatchSpeed`

---

### Buildings (all 11 from GDD implemented)

#### Architecture
- `Entities/Buildings/BaseBuilding.cs` — `StaticBody3D`. Implements `IDamageable`, `ISelectable`. Grabs `HealthComponent` + `SelectionComponent` (required) and `IncomeComponent` / `ProductionComponent` / `TurretCombatComponent` (optional via `GetNodeOrNull`). Registers in `SelectionRegistry` (so buildings are selectable, show health bars, and appear on the minimap). `OwnerId` is a root `[Export]` set per-instance in `Game.tscn` (simpler than the unit index-4 override). `ApplyData()` sets MaxHealth + calls `InitHealth()`, pushes OwnerId to selection, configures+activates income, sets production OwnerId, loads turret stats. HQ destruction raises `EventBus.RaiseHqDestroyed`.
- `Data/BuildingData.cs` — added `IncomePerTick`, `Damage`, `AttackRange`, `AttackCooldown` fields.
- `Entities/Buildings/Visual/BuildingBoxPlaceholder.cs` — BoxMesh placeholder, exported `Size`, blue/red owner tint (same scheme as units), single billboarded name label above the building.
- `Entities/Buildings/Components/TurretCombatComponent.cs` — stationary auto-attack for the Turret. Mirrors the unit `CombatComponent` but parents to `BaseBuilding` (the unit version hard-casts its parent to `BaseUnit`). Scans `SelectionRegistry` for nearest hostile `BaseUnit` in range, fires via `EventBus.RaiseUnitFired` + `TakeDamage`.
- `SelectionComponent` — added exported `RingScale` (default 1.0) so buildings get a larger selection ring than units.

#### All 11 buildings (`.tscn` in `Scenes/Buildings/`, `.tres` in `Data/Buildings/`)

| Building | HP | Cost | BuildTime | Extra component | ReqCC | ReqCrystal |
|---|---|---|---|---|---|---|
| Headquarters | 2000 | 1000 | 30s | Production | no | yes |
| Command Center | 1500 | 600 | 25s | — | no | yes |
| Powerplant | 500 | 150 | 10s | Income (10/tick) | no | yes |
| Oil Rig | 500 | 150 | 12s | Income (10/tick) | no | yes |
| Barracks | 800 | 200 | 15s | Production | yes | no |
| Vehicle Depot | 1000 | 350 | 20s | Production | yes | no |
| Airfield | 1000 | 500 | 25s | Production | yes | no |
| Shipyard | 900 | 400 | 22s | Production | no | no |
| Research Lab | 700 | 300 | 18s | — | yes | no |
| Wall | 400 | 20 | 3s | — | no | no |
| Turret | 600 | 120 | 8s | TurretCombat (15 dmg, 10 range, 0.6s cd) | no | no |

**Stats are placeholders — not balanced.** Naval buildings (Oil Rig, Shipyard) included for completeness even though the naval *system* is deferred per GDD.

### Placement System
- `Systems/PlacementController.cs` — `Node3D` under `World`. Subscribes to `EventBus.OnPlacementRequested(PackedScene, isBuilding, BuildingData)`. Shows a translucent box ghost (sized to the scene's `BuildingBoxPlaceholder.Size`, or 0.5×0.8×0.5 for units) that follows the mouse on the Y=0 ground (free placement, no snapping). **Green** ghost = valid, **red** = obstructed. Left-click places (instantiates the real scene, sets `OwnerId`/`SelectionComponent.OwnerId` to `LocalPlayerId` before `AddChild`) and exits placement; Esc or right-click cancels.
- **Validity rules** (all must pass for green): footprint fully inside the 80×80 map; footprint doesn't AABB-overlap any existing building (units don't block); and for `BuildingData.RequiresCrystal` buildings only, footprint center is within some crystal's `BuildRadius` **and that crystal isn't enemy-claimed**. A crystal is "enemy-claimed" (relative to the placing owner) when a hostile building with `BuildingData.ClaimsCrystalZone = true` sits within its `BuildRadius`; powerplants set `ClaimsCrystalZone = false` so they don't claim. Both rules are data-driven (no hardcoded building-type checks): every building's `.tres` sets `RequiresCrystal = true` except the Shipyard, and only the Powerplant sets `ClaimsCrystalZone = false`.
- `UI/PlacementTestPanel.cs` now raises `EventBus.RaisePlacementRequested(scene, PlaceableKind, BuildingData)` on entry click (previously only recorded the selection). Three columns: **UNITS** (`Data/Units/*.tres`), **BUILDINGS** (`Data/Buildings/*.tres`), **CRYSTALS** (`Scenes/Crystals/*.tscn`, listed by file name). `PlaceableKind` enum (Unit/Building/Crystal) drives both the panel loading and `PlacementController` placement/validity branching.
- `SelectionManager.HandleMouseButton` early-returns while `PlacementController.IsPlacing`, so world clicks don't select/box/order during placement.
- `Entities/Crystals/CrystalNode.cs` + `Systems/CrystalRegistry.cs` + `Scenes/Crystals/Crystal.tscn` — new crystal entity. Exported `BuildRadius` (default 15) + `CrystalType`. Cyan box + a flat ground ring showing the build zone. Two crystals pre-placed in `Game.tscn` (`World/Crystals`) at (34,30) and (60,60), and crystals are also placeable from the spawn menu (CRYSTALS column). Placement validity for a crystal = on-map + no building overlap (no owner, no crystal-zone requirement). Footprint size (1.2×2.2×1.2) is a constant in `PlacementController` matching the code-built `CrystalNode` mesh.
- Ghost tint alpha is 0.6 — tweak `ValidColor`/`InvalidColor` in `PlacementController` if a more/less opaque preview is wanted.

All 11 building types are placed in `Game.tscn` under `World/Buildings` (two rows, OwnerId=1) plus one `EnemyHeadquarters1` (OwnerId=2) for testing selection/destruction. They are selectable, show health bars, appear on the minimap, and the Turret auto-fires at hostile units in range.

#### Bugs fixed during this pass
- `IncomeComponent` previously self-registered in `_Ready()` with `OwnerId=0`/`Income=0` *before* `BaseBuilding.ApplyData` configured it. Now it registers only when `BaseBuilding` calls `SetActive(true)` after configuring it (matches the skill-doc design).
- `BaseBuilding.ApplyData` never called `_health.InitHealth()`, so building HP stayed at the component default (100) instead of the data value. Fixed.

## What Is NOT Yet Built

- **Production wiring**: Production buildings (HQ, Barracks, Vehicle Depot, Airfield, Shipyard) have a configured `ProductionComponent` and `BaseBuilding.QueueUnit(UnitData)` forwards to it, but nothing consumes the `ProductionComplete` signal to actually spawn units yet. Spawn-on-complete (with rally points, unit OwnerId-on-spawn, and unit caps) is the next step — it needs the production UI and a way to set a spawned unit's owner before `_Ready`. Vehicle/Aircraft/Ship unit scenes also don't exist yet.
- **Build zone / placement**: ~~no build-zone enforcement~~ — **placement now implemented** (see below). `RequiresCommandCenter` is treated as an elimination condition, *not* a placement rule.
- **Economy income**: `EconomySystem` ticks and `IncomeComponent` now registers correctly, but income only accrues for players with a registered `PlayerContext` (`PlayerManager.AddMoney` no-ops otherwise). No player bootstrap exists yet, so money does not visibly tick in the prototype scene.
- **Win condition**: No HQ destruction or game-over logic.
- **Vehicles, Aircraft, Ships**: Designed in GDD, not started.
- **Research system**: Designed in GDD, not started.
- **Multiplayer**: All networking is stubbed. GodotSteam not yet integrated.
- **AI opponents**: Deferred to post-launch.
- **Fog of war**: Deferred to post-launch.
- **Unit abilities**: Medic heal, Engineer repair — no ability system exists yet.
- **Builder placing structures**: Placement logic exists (via the dev spawn menu), but it isn't yet gated behind a selected Builder unit or tied to costs/build time — any menu entry can be placed for free.

---

## Pending / Interrupted Tasks
- **Health bars uniform size**: User asked "i want every health bar to be the same size. none should be bigger than others for units that have more health." The bars are already drawn at a fixed 36×5px in screen space — the background is always 36px wide regardless of max HP. The fill rect shrinks as HP drops (intended behavior). This may have been resolved by the earlier blue-bar bugfix (Sniper's bar appeared wrong due to HP init ordering), or the user may want further confirmation or a visual tweak. Clarify before implementing anything.

---

## Key Architecture Rules (from user)
- All scripts in C# — no GDScript.
- Data-driven: stats live in `.tres` Godot Resources, not hardcoded.
- EventBus static C# events for cross-system communication (not Godot signals for cross-system).
- Ask before making architectural decisions — surface trade-offs, let user decide.
- No comments unless the WHY is non-obvious. No docstrings.
- No backwards-compat hacks, no error handling for impossible scenarios.
