## 2025-05-15 - [CRITICAL] Memory Leak and DoS via Unity Material Cloning
**Vulnerability:** Every-frame access to `renderer.materials` in Unity clones the material array and all its contents. In this mod's `UpdateWallTransparency`, this created thousands of material instances per second, leading to a rapid heap overflow and game crash (Denial of Service).
**Learning:** Unity's `.materials` property is a getter that performs deep cloning. Developers often mistake it for a simple reference, but it's a "cloning factory" that must be used sparingly.
**Prevention:** Use `renderer.sharedMaterials` for read-only access or to modify materials that you have already explicitly instantiated. Always `Destroy()` any material instances created via `new Material()` when they are no longer needed.
