# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build
dotnet build

# Publish (linux-x64, used by build.sh for deployment)
dotnet publish -c Release -r linux-x64 --no-self-contained -o ./bin/Build

# Run (uses appsettings.{Environment}.json)
dotnet run

# EF Core migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

The project targets `net10.0`, uses C# 13 with implicit usings and nullable enabled.

## Architecture

This is a multiplayer online game server combining an ASP.NET Core Web API with a custom TCP game server. Both run in the same process. The solution is split across two repositories: `MTS-Server` (this one) and `Library/NetworkServer` (custom TCP networking library, referenced as a project reference).

### Dual Server

- **HTTP** (REST, default port 9481): Swagger UI at `/` in development. `GameController` at `/game` is the single API controller (currently empty shell).
- **TCP** (binary protocol, default port 6785): The real game server. `TcpHostedService` starts `TcpServer` as an `IHostedService`. All game logic flows through TCP.

### Connection Lifecycle (State Machine)

TCP clients progress through a state machine defined in [Network/ClientState/](Network/ClientState/):

1. **ConnectState** → **CheckVersionState** (handled by `UnauthenticatedClient`, pre-auth)
2. **AuthState** → `AuthService.CreateOrLogin()` authenticates via `DeveloperAccounts` table
3. **SelectCharacter** → choose existing char or enter **CreateCharacter** flow
4. **AllMaps** → main gameplay state (map movement, other players online). After character select, `CoreMessageHandler` modules are registered for mail/inventory/admin/scene management.
5. Scene switching via `SceneManager` transitions to **LuyenDan** (alchemy refinement) or **LuyenKhi** (cultivation/equipment crafting)

States implement `IClientState` (which extends `IMessageState<MessageTag, MessageState>`). The `MessageState` enum in [MessageState.cs](Network/ClientState/MessageState.cs) lists all states.

### Message Handling: Two Layers

After character selection (in `SelectCharacter.Handle`), two parallel message-handling layers are active:

- **State handlers** (`IClientState`): Handle messages tied to the current scene/game mode (map movement, alchemy, cultivation).
- **Core handlers** (`IClientHandler`): Registered on `CoreMessageHandler` and handle cross-cutting concerns regardless of current state — `MailHandler`, `InventoryHandler`, `AdminHandler`, `SceneManager`.

All message tags are defined in the `MessageTag` enum in [MessageTag.cs](Network/MessageTag.cs).

### Custom Binary Protocol

The project uses a custom binary serialization protocol (not JSON, Protobuf, or MessagePack). All wire types implement `IMemorySerializable<T>` and use `IReader`/`IWriter` from the `Network` namespace. The protocol library lives in the external `NetworkServer` project. `ClientConnection` extends `BaseClient<MessageTag>` from that library. `TcpServer` extends `BaseTcpServer` from `NetworkServer`.

### Database

- MySQL via **Pomelo.EntityFrameworkCore.MySql** 9.0
- Uses `IDbContextFactory<AppDbContext>` via the static `Program.Db` property — TCP connections are long-lived and cannot use scoped DI, so DbContexts are manually created with `await Program.CreateDbAsync()` and disposed with `await using`
- `AppDbContext.OnModelCreating` configures all relationships (mostly 1:1 via shared PK `CharId`). JSON columns use `Newtonsoft.Json` serialization with custom `ValueComparer` classes for change tracking.

### Entity Model

`CharacterData` is the central aggregate root. It has 1:1 owned entities: `CharacterStats`, `Progression`, `Appearance`, `EquipmentManager`, `InventoryManager`, `Social`. `InventoryManager` owns collections: `FurnaceData`, `CamNgo`, `CongPhap`, `NgoDao`, `ThanThong`, and has lazy-loaded `ItemRef` children.

`UserData` owns multiple `CharacterData`. `DeveloperAccount` links a username to a `UserData`.

### Game Config & Binary Data

`GameConfig.Init()` (called in `Program.Main`) loads game configuration from files in [Data/](Data/):

- `character-template.json` — JSON template for new characters
- `step-config.bin` — character creation step definitions
- `skin-resources.bin` — appearance/skin options
- `maps.bin` — game map graph (nodes + edges for BFS pathfinding)
- `LuyenDanResources.bin` — alchemy recipes, herbs, tiers
- `LuyenKhiResources.bin` — cultivation effects, equipment grades, materials

The `.bin` files use the same custom `IReader`/`IWriter` serialization. The `Json` helper in [Converter/Json.cs](Converter/Json.cs) wraps Newtonsoft.Json with custom converters for `Color`, `ItemRef`, and `FurnaceData`.

### Key Design Patterns

- **Change tracking**: Models have `IsDefault()` checks and a dirty-flag pattern via an internal `_isDefault` bool. `SaveChanges(AppDbContext)` on each model only writes to DB if the model was mutated.
- **Save loop**: Each `Client` runs a background 10-second save loop (`await SaveChanges()`) while connected. On server stop (`TcpHostedService.StopAsync`), `TcpServer.SaveClients()` saves all connected clients with a 10-concurrency semaphore.
- **IdAllocator**: Custom ID allocator with free-range tracking, used for managing self-allocated IDs within character-owned collections (e.g., furnace slots, equipment slots).

## Key Files

| File | Purpose |
|------|---------|
| [Program.cs](Program.cs) | App startup, DI, JWT config, GameConfig.Init() |
| [Data/AppDbContext.cs](Data/AppDbContext.cs) | All EF Core entity/relationship configuration |
| [Network/TcpServer.cs](Network/TcpServer.cs) | TCP accept loop, client tracking, save orchestration |
| [Network/Client.cs](Network/Client.cs) | Authenticated client: state, save loop, mail/inventory access |
| [Network/ClientState/SelectCharacter.cs](Network/ClientState/SelectCharacter.cs) | Character selection + CoreHandler registration |
| [Network/Core/SceneManager.cs](Network/Core/SceneManager.cs) | Scene transition dispatch (AllMaps, LuyenDan, LuyenKhi) |
| [Game/GameConfig.cs](Game/GameConfig.cs) | Static game data loaded from Data/*.bin files |
| [Models/CharacterData.cs](Models/CharacterData.cs) | Central entity, serialization, dirty-tracking save |
| [Models/InventoryManager.cs](Models/InventoryManager.cs) | Largest model (715 lines), item management logic |
| [Utils/IdAllocator.cs](Utils/IdAllocator.cs) | Reusable ID allocator with defragmentation |

## Frontend

The frontend is a separate Vue 3 + Vite project at `frontend/mts-frontend/`. It communicates with the server via the TCP binary protocol (not REST).
