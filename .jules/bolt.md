
## 2026-03-23 - [Unity Performance: Caching Reflection and Materials]
**Learning:** In Unity mods, string-based reflection (e.g., `System.Type.GetType`) and repeated member lookups (`GetField`) are extremely slow when done every frame. Additionally, accessing `renderer.materials` inside `Update()` or `OnGUI()` allocates a new array each time, creating significant GC pressure.
**Action:** Cache `Type`, `FieldInfo`, and `PropertyInfo` objects in `Start()` or lazily. For material-heavy updates like transparency, store instantiated materials in a dedicated `List<Material>` to iterate over them directly without re-querying the renderer.

## 2026-03-24 - [Unity ESP: Culling and Constant Caching]
**Learning:** ESP loops in `OnGUI` are a major performance sink. Calling `Camera.main` and `WorldToScreenPoint` for every object in the scene—regardless of distance—causes massive CPU overhead.
**Action:** Always perform a squared distance check (`sqrMagnitude`) BEFORE `WorldToScreenPoint`. Cache frame-wide constants (`cam.transform.position`, `Screen.height`, `sqrMaxDist`) at the start of `OnGUI` to avoid repeated native transitions.
