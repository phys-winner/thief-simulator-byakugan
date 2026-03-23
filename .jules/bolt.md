
## 2026-03-23 - [Unity Performance: Caching Reflection and Materials]
**Learning:** In Unity mods, string-based reflection (e.g., `System.Type.GetType`) and repeated member lookups (`GetField`) are extremely slow when done every frame. Additionally, accessing `renderer.materials` inside `Update()` or `OnGUI()` allocates a new array each time, creating significant GC pressure.
**Action:** Cache `Type`, `FieldInfo`, and `PropertyInfo` objects in `Start()` or lazily. For material-heavy updates like transparency, store instantiated materials in a dedicated `List<Material>` to iterate over them directly without re-querying the renderer.
