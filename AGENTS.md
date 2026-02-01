# Mate Engine Linux Port - Agent Guidelines

## Build Commands
```bash
./build.sh                    # Full build (plugin + Unity project)
make -C Plugins/StandaloneFileBrowser  # Build native plugin only
# Unity: 6000.3.6f1, set UNITY_PATH env if non-default location
```

## Project Structure
- `Assets/MATE ENGINE - Scripts/` - Main C# scripts (AvatarHandlers, APIs, Settings, VRMLoader)
- `Assets/MATE ENGINE - Mod SDK/` - MEManipulator for runtime component injection
- `Assets/DiscordRPC/` - Discord RPC with cross-platform NamedPipe implementation
- `Plugins/` - Native plugins: StandaloneFileBrowser (C), kdotool (Rust/D-Bus)
- `Assets/LLMUnity/` - AI chat integration (requires Meta-Llama-3.1-8B model)

## Code Style
- C# without namespaces (most scripts), `public class Foo : MonoBehaviour` pattern
- Use `[SerializeField]` for inspector fields, `[Header("Section")]` for grouping
- Data classes: `[Serializable] public class FooEntry { ... }`
- Platform-specific code via `#if UNITY_STANDALONE_LINUX` / `Environment.OSVersion.Platform`
- P/Invoke for native libs: `[DllImport("libname")]` with explicit CallingConvention
- Coroutines for async UI: `IEnumerator FooRoutine()` + `StartCoroutine()`

## Key APIs
- `AvatarRandomMessages.ShowSpecificMessage(AvatarMessage)` - Display pet bubble messages
- `SaveLoadHandler.Instance.data` - Persistent settings access
- `WindowManager` / `KWinManager` - X11/KWin window control
- `TrayIndicator` - System tray via libayatana-appindicator
