using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace ThiefSimulatorHack
{
    public class Main : MonoBehaviour
    {
        // Menu state
        private bool _showMenu = true;
        
        // Feature toggles
        private bool _itemEsp = true;
        private bool _aiEsp = true;
        private bool _carEsp = true;
        private bool _transparentWalls = false;
        private bool _whiteWalls = false;
        private bool _showBrickItems = false;
        private bool _confirmUnload = false;
        
        // Settings
        private float _espDistance = 100f;
        private float _wallTransparency = 0.3f;
        
        private Rect _windowRect = new Rect(20, 20, 350, 440);
        private float _lastUpdateTime = 0;
        
        // Cached game objects
        private List<Component> _cachedItems = new List<Component>();
        private List<Component> _cachedAI = new List<Component>();
        private List<Component> _cachedCameras = new List<Component>();
        private List<Component> _cachedVehicles = new List<Component>();
        
        // White walls
        private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private List<Renderer> _wallRenderers = new List<Renderer>();
        private Material _whiteMaterial;
        
        // Transparent walls
        private Dictionary<Renderer, Material[]> _originalWallMaterials = new Dictionary<Renderer, Material[]>();
        private List<Renderer> _transparentWallRenderers = new List<Renderer>();
        private Material _transparentMaterial;
        
        // Chams for AI
        private Dictionary<Renderer, Material[]> _originalAIMaterials = new Dictionary<Renderer, Material[]>();
        private List<Renderer> _aiRenderers = new List<Renderer>();
        private Material _aiChamMaterial;
        
        // Chams for Cars
        private Dictionary<Renderer, Material[]> _originalCarMaterials = new Dictionary<Renderer, Material[]>();
        private List<Renderer> _carRenderers = new List<Renderer>();
        private Material _playerCarChamMaterial;
        private Material _otherCarChamMaterial;

        private void Start()
        {
            // Create white material for walls
            _whiteMaterial = new Material(Shader.Find("Standard"));
            _whiteMaterial.color = Color.white;
            
            // Create transparent material for walls
            Shader transparentShader = Shader.Find("Transparent/Diffuse");
            if (transparentShader != null)
            {
                _transparentMaterial = new Material(transparentShader);
                _transparentMaterial.color = new Color(1f, 1f, 1f, _wallTransparency);
            }
            
            // Create cham material for AI (visible through walls)
            _aiChamMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            _aiChamMaterial.hideFlags = HideFlags.HideAndDontSave;
            _aiChamMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _aiChamMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _aiChamMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _aiChamMaterial.SetInt("_ZWrite", 0);
            _aiChamMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            _aiChamMaterial.color = new Color(1f, 0f, 0f, 0.8f); // Bright red
            
            // Create cham material for player car (green)
            _playerCarChamMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            _playerCarChamMaterial.hideFlags = HideFlags.HideAndDontSave;
            _playerCarChamMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _playerCarChamMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _playerCarChamMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _playerCarChamMaterial.SetInt("_ZWrite", 0);
            _playerCarChamMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            _playerCarChamMaterial.color = new Color(0f, 1f, 0f, 0.8f); // Bright green
            
            // Create cham material for other cars (orange)
            _otherCarChamMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            _otherCarChamMaterial.hideFlags = HideFlags.HideAndDontSave;
            _otherCarChamMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _otherCarChamMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _otherCarChamMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _otherCarChamMaterial.SetInt("_ZWrite", 0);
            _otherCarChamMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            _otherCarChamMaterial.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                _showMenu = !_showMenu;
            }

            // Periodically refresh cache
            if (Time.time - _lastUpdateTime > 3.0f)
            {
                RefreshCache();
                _lastUpdateTime = Time.time;
            }

            // Handle white walls (Check transparent first to avoid override)
            if (_whiteWalls && !_transparentWalls && _wallRenderers.Count == 0)
            {
                ApplyWhiteWalls();
            }
            else if ((!_whiteWalls || _transparentWalls) && _wallRenderers.Count > 0)
            {
                DisableWhiteWalls();
            }
            
            // Handle transparent walls
            if (_transparentWalls && _transparentWallRenderers.Count == 0)
            {
                ApplyTransparentWalls();
            }
            else if (!_transparentWalls && _transparentWallRenderers.Count > 0)
            {
                DisableTransparentWalls();
            }
            else if (_transparentWalls && _transparentWallRenderers.Count > 0)
            {
                UpdateWallTransparency();
            }
            
            // Handle AI chams
            if (_aiEsp && _aiRenderers.Count == 0)
            {
                ApplyAIChams();
            }
            else if (!_aiEsp && _aiRenderers.Count > 0)
            {
                DisableAIChams();
            }
            
            // Handle Car chams
            if (_carEsp && _carRenderers.Count == 0)
            {
                ApplyCarChams();
            }
            else if (!_carEsp && _carRenderers.Count > 0)
            {
                DisableCarChams();
            }
        }

        private void RefreshCache()
        {
            _cachedItems.Clear();
            _cachedAI.Clear();
            _cachedCameras.Clear();
            _cachedVehicles.Clear();

            // Find ItemObject components
            Component[] itemObjects = GameObject.FindObjectsOfType(System.Type.GetType("ItemObject, Assembly-CSharp")) as Component[];
            if (itemObjects != null)
            {
                foreach (var item in itemObjects)
                {
                    if (item != null && item.gameObject.activeInHierarchy)
                        _cachedItems.Add(item);
                }
            }

            // Find HumanAIObject components
            Component[] aiObjects = GameObject.FindObjectsOfType(System.Type.GetType("HumanAIObject, Assembly-CSharp")) as Component[];
            if (aiObjects != null)
            {
                foreach (var ai in aiObjects)
                {
                    if (ai != null && ai.gameObject.activeInHierarchy)
                        _cachedAI.Add(ai);
                }
            }

            // Find CCTVCameraObject components
            Component[] cctvCameras = GameObject.FindObjectsOfType(System.Type.GetType("CCTVCameraObject, Assembly-CSharp")) as Component[];
            if (cctvCameras != null)
            {
                foreach (var cam in cctvCameras)
                {
                    if (cam != null && cam.gameObject.activeInHierarchy)
                        _cachedCameras.Add(cam);
                }
            }
            
            // Find ALL vehicles
            Component[] vehicles = GameObject.FindObjectsOfType(System.Type.GetType("ControllableVehicle, Assembly-CSharp")) as Component[];
            if (vehicles != null)
            {
                foreach (var car in vehicles)
                {
                    if (car != null && car.gameObject.activeInHierarchy)
                        _cachedVehicles.Add(car);
                }
            }
        }

        private void ApplyWhiteWalls()
        {
            _wallRenderers.Clear();
            _originalMaterials.Clear();

            MeshRenderer[] allRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
            foreach (var renderer in allRenderers)
            {
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                
                if (renderer.gameObject.layer == 5) continue; // Skip UI
                
                string objName = renderer.gameObject.name.ToLower();
                if (objName.Contains("wall") || objName.Contains("house") || objName.Contains("floor") || 
                    objName.Contains("building") || objName.Contains("ceiling") || objName.Contains("roof"))
                {
                    _originalMaterials[renderer] = renderer.materials;
                    
                    Material[] whiteMats = new Material[renderer.materials.Length];
                    for (int i = 0; i < whiteMats.Length; i++)
                    {
                        whiteMats[i] = _whiteMaterial;
                    }
                    
                    renderer.materials = whiteMats;
                    _wallRenderers.Add(renderer);
                }
            }
        }

        private void DisableWhiteWalls()
        {
            foreach (var renderer in _wallRenderers)
            {
                if (renderer != null && _originalMaterials.ContainsKey(renderer))
                {
                    renderer.materials = _originalMaterials[renderer];
                }
            }
            _wallRenderers.Clear();
            _originalMaterials.Clear();
        }

        private void ApplyTransparentWalls()
        {
            _transparentWallRenderers.Clear();
            _originalWallMaterials.Clear();

            MeshRenderer[] allRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
            foreach (var renderer in allRenderers)
            {
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                
                if (renderer.gameObject.layer == 5) continue; // Skip UI
                
                string objName = renderer.gameObject.name.ToLower();
                if (objName.Contains("wall") || objName.Contains("house") || objName.Contains("floor") || 
                    objName.Contains("building") || objName.Contains("ceiling") || objName.Contains("roof"))
                {
                    _originalWallMaterials[renderer] = renderer.materials;
                    
                    Material[] transMats = new Material[renderer.materials.Length];
                    for (int i = 0; i < transMats.Length; i++)
                    {
                        transMats[i] = new Material(_transparentMaterial);
                        if (renderer.materials[i].mainTexture != null)
                            transMats[i].mainTexture = renderer.materials[i].mainTexture;
                        
                        Color col = Color.white;
                        col.a = _wallTransparency;
                        transMats[i].color = col;
                    }
                    
                    renderer.materials = transMats;
                    _transparentWallRenderers.Add(renderer);
                }
            }
        }

        private void UpdateWallTransparency()
        {
            foreach (var renderer in _transparentWallRenderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    foreach (var mat in renderer.materials)
                    {
                        if (mat != null)
                        {
                            Color col = mat.color;
                            col.a = _wallTransparency;
                            mat.color = col;
                        }
                    }
                }
            }
        }

        private void DisableTransparentWalls()
        {
            foreach (var renderer in _transparentWallRenderers)
            {
                if (renderer != null && _originalWallMaterials.ContainsKey(renderer))
                {
                    renderer.materials = _originalWallMaterials[renderer];
                }
            }
            _transparentWallRenderers.Clear();
            _originalWallMaterials.Clear();
        }

        private void ApplyAIChams()
        {
            _aiRenderers.Clear();
            _originalAIMaterials.Clear();

            foreach (var aiComp in _cachedAI)
            {
                if (aiComp == null || !aiComp.gameObject.activeInHierarchy) continue;
                if (!IsNPCVisible(aiComp)) continue;
                
                Renderer[] renderers = aiComp.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled) continue;
                    if (renderer.gameObject.layer == 5) continue;
                    
                    _originalAIMaterials[renderer] = renderer.materials;
                    
                    Material[] chamMats = new Material[renderer.materials.Length];
                    for (int i = 0; i < chamMats.Length; i++)
                    {
                        chamMats[i] = _aiChamMaterial;
                    }
                    
                    renderer.materials = chamMats;
                    _aiRenderers.Add(renderer);
                }
            }
        }

        private void DisableAIChams()
        {
            foreach (var renderer in _aiRenderers)
            {
                if (renderer != null && _originalAIMaterials.ContainsKey(renderer))
                {
                    renderer.materials = _originalAIMaterials[renderer];
                }
            }
            _aiRenderers.Clear();
            _originalAIMaterials.Clear();
        }

        private void ApplyCarChams()
        {
            _carRenderers.Clear();
            _originalCarMaterials.Clear();

            foreach (var carComp in _cachedVehicles)
            {
                if (carComp == null || !carComp.gameObject.activeInHierarchy) continue;
                
                bool isPlayerCar = IsPlayerCar(carComp);
                Material chamMat = isPlayerCar ? _playerCarChamMaterial : _otherCarChamMaterial;
                
                Renderer[] renderers = carComp.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled) continue;
                    if (renderer.gameObject.layer == 5) continue;
                    
                    _originalCarMaterials[renderer] = renderer.materials;
                    
                    Material[] chamMats = new Material[renderer.materials.Length];
                    for (int i = 0; i < chamMats.Length; i++)
                    {
                        chamMats[i] = chamMat;
                    }
                    
                    renderer.materials = chamMats;
                    _carRenderers.Add(renderer);
                }
            }
        }

        private void DisableCarChams()
        {
            foreach (var renderer in _carRenderers)
            {
                if (renderer != null && _originalCarMaterials.ContainsKey(renderer))
                {
                    renderer.materials = _originalCarMaterials[renderer];
                }
            }
            _carRenderers.Clear();
            _originalCarMaterials.Clear();
        }

        private bool IsNPCVisible(Component aiComp)
        {
            try
            {
                FieldInfo renderersOnField = aiComp.GetType().GetField("renderersOn", BindingFlags.NonPublic | BindingFlags.Instance);
                if (renderersOnField != null)
                {
                    return (bool)renderersOnField.GetValue(aiComp);
                }
                
                return true;
            }
            catch
            {
                return true;
            }
        }

        private bool IsGamePaused()
        {
            return Time.timeScale == 0f || Cursor.visible;
        }

        private void OnGUI()
        {
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            GUI.color = Color.white;

            if (_showMenu)
            {
                _windowRect = GUI.Window(0, _windowRect, WindowFunction, "BYAKUGAN");
            }

            if (IsGamePaused()) return;

            if (_itemEsp)
            {
                DrawItemESP();
            }

            if (_aiEsp)
            {
                DrawAIESP();
            }
            
            if (_carEsp)
            {
                DrawCarESP();
            }
        }

        private void WindowFunction(int windowID)
        {
            GUI.contentColor = Color.cyan;
            GUILayout.Label("--- FEATURES ---");
            GUI.contentColor = Color.white;

            _itemEsp = GUILayout.Toggle(_itemEsp, " [X] Item ESP");
            _aiEsp = GUILayout.Toggle(_aiEsp, " [X] AI ESP + Chams");
            _carEsp = GUILayout.Toggle(_carEsp, " [X] Car ESP + Chams");
            _transparentWalls = GUILayout.Toggle(_transparentWalls, " [X] Transparent Walls");
            _whiteWalls = GUILayout.Toggle(_whiteWalls, " [X] White Walls");
            _showBrickItems = GUILayout.Toggle(_showBrickItems, " [X] Show Bricks");

            GUILayout.Space(10);
            GUI.contentColor = Color.yellow;
            GUILayout.Label("--- SETTINGS ---");
            GUI.contentColor = Color.white;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"ESP Distance: {_espDistance:F0}m");
            GUILayout.EndHorizontal();
            _espDistance = GUILayout.HorizontalSlider(_espDistance, 10f, 500f);
            
            if (_transparentWalls)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Wall Alpha: {_wallTransparency:F2}");
                GUILayout.EndHorizontal();
                _wallTransparency = GUILayout.HorizontalSlider(_wallTransparency, 0.0f, 1.0f);
            }

            GUILayout.Space(10);
            GUI.contentColor = Color.yellow;
            GUILayout.Label("--- UTILS ---");
            GUI.contentColor = Color.white;

            if (!_confirmUnload)
            {
                if (GUILayout.Button("Unload & Cleanup"))
                {
                    _confirmUnload = true;
                }
            }
            else
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("CONFIRM UNLOAD"))
                {
                    DisableWhiteWalls();
                    DisableTransparentWalls();
                    DisableAIChams();
                    DisableCarChams();
                    Loader.Unload();
                }
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Restore default
                if (GUILayout.Button("Cancel"))
                {
                    _confirmUnload = false;
                }
            }

            GUILayout.FlexibleSpace();
            GUI.contentColor = Color.gray;
            GUILayout.Label("Press INSERT to toggle menu");

            GUI.DragWindow();
        }

        private string GetItemInfo(Component itemComp)
        {
            try
            {
                FieldInfo itemAssetField = itemComp.GetType().GetField("itemAsset", BindingFlags.Public | BindingFlags.Instance);
                if (itemAssetField != null)
                {
                    object itemAsset = itemAssetField.GetValue(itemComp);
                    if (itemAsset != null)
                    {
                        FieldInfo nameField = itemAsset.GetType().GetField("itemName", BindingFlags.Public | BindingFlags.Instance);
                        string itemName = nameField != null ? (string)nameField.GetValue(itemAsset) : "Unknown";
                        
                        FieldInfo isBigField = itemAsset.GetType().GetField("isBigItem", BindingFlags.Public | BindingFlags.Instance);
                        bool isBig = isBigField != null && (bool)isBigField.GetValue(itemAsset);
                        
                        FieldInfo cashField = itemComp.GetType().GetField("isCash", BindingFlags.Public | BindingFlags.Instance);
                        bool isCash = cashField != null && (bool)cashField.GetValue(itemComp);
                        
                        FieldInfo itemTypeField = itemAsset.GetType().GetField("itemType", BindingFlags.Public | BindingFlags.Instance);
                        string typeIcon = "💎";
                        
                        if (isCash)
                        {
                            typeIcon = "💵";
                        }
                        else if (itemTypeField != null)
                        {
                            int typeValue = (int)itemTypeField.GetValue(itemAsset);
                            if (typeValue == 1)
                                typeIcon = "🔧";
                            else if (typeValue == 2)
                                typeIcon = "🔑";
                        }
                        
                        if (isBig)
                            typeIcon = "📦";
                        
                        return $"{typeIcon} {itemName}";
                    }
                }
            }
            catch { }
            
            return "Item";
        }

        private Color GetItemColor(Component itemComp)
        {
            try
            {
                FieldInfo itemAssetField = itemComp.GetType().GetField("itemAsset", BindingFlags.Public | BindingFlags.Instance);
                if (itemAssetField != null)
                {
                    object itemAsset = itemAssetField.GetValue(itemComp);
                    if (itemAsset != null)
                    {
                        FieldInfo nameField = itemAsset.GetType().GetField("itemName", BindingFlags.Public | BindingFlags.Instance);
                        string itemName = nameField != null ? ((string)nameField.GetValue(itemAsset)).ToLower() : "";
                        
                        if (itemName.Contains("key"))
                            return Color.yellow;
                        if (itemName.Contains("note"))
                            return Color.cyan;
                        if (itemName.Contains("brick"))
                            return new Color(1f, 0.5f, 0f);
                        
                        FieldInfo itemTypeField = itemAsset.GetType().GetField("itemType", BindingFlags.Public | BindingFlags.Instance);
                        if (itemTypeField != null)
                        {
                            int typeValue = (int)itemTypeField.GetValue(itemAsset);
                            if (typeValue == 1)
                                return new Color(0.5f, 0.8f, 1f);
                        }
                    }
                }
            }
            catch { }
            
            return Color.green;
        }

        private bool ShouldDrawItem(Component itemComp)
        {
            try
            {
                FieldInfo itemAssetField = itemComp.GetType().GetField("itemAsset", BindingFlags.Public | BindingFlags.Instance);
                if (itemAssetField != null)
                {
                    object itemAsset = itemAssetField.GetValue(itemComp);
                    if (itemAsset != null)
                    {
                        FieldInfo nameField = itemAsset.GetType().GetField("itemName", BindingFlags.Public | BindingFlags.Instance);
                        string itemName = nameField != null ? ((string)nameField.GetValue(itemAsset)).ToLower() : "";
                        
                        if (itemName.Contains("brick") && !_showBrickItems)
                            return false;
                    }
                }
            }
            catch { }
            
            return true;
        }

        private void DrawItemESP()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Camera.current;
            if (cam == null) return;

            foreach (var itemComp in _cachedItems)
            {
                if (itemComp == null || !itemComp.gameObject.activeInHierarchy) continue;
                if (!ShouldDrawItem(itemComp)) continue;

                Vector3 worldPos = itemComp.transform.position;
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                if (screenPos.z > 0)
                {
                    float dist = Vector3.Distance(cam.transform.position, worldPos);
                    if (dist > _espDistance) continue;

                    float x = screenPos.x;
                    float y = Screen.height - screenPos.y;

                    string itemInfo = GetItemInfo(itemComp);
                    Color itemColor = GetItemColor(itemComp);
                    
                    GUI.color = itemColor;
                    GUI.Box(new Rect(x - 2, y - 2, 4, 4), "");
                    GUI.Label(new Rect(x + 5, y - 10, 250, 20), $"{itemInfo} [{Mathf.RoundToInt(dist)}m]");
                }
            }
        }

        private Vector3 GetEyePosition(Component aiComp)
        {
            try
            {
                PropertyInfo property = aiComp.GetType().GetProperty("thiefDetector", BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    object thiefDetector = property.GetValue(aiComp, null);
                    if (thiefDetector != null)
                    {
                        Component detectorComp = thiefDetector as Component;
                        if (detectorComp != null)
                            return detectorComp.transform.position;
                    }
                }
            }
            catch { }
            
            return aiComp.transform.position + Vector3.up * 1.6f;
        }

        private void DrawAIESP()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Camera.current;
            if (cam == null) return;

            foreach (var aiComp in _cachedAI)
            {
                if (aiComp == null || !aiComp.gameObject.activeInHierarchy) continue;
                if (!IsNPCVisible(aiComp)) continue;

                try
                {
                    Vector3 worldPos = aiComp.transform.position;
                    Vector3 eyePos = GetEyePosition(aiComp);
                    Vector3 screenPos = cam.WorldToScreenPoint(eyePos);

                    if (screenPos.z > 0)
                    {
                        float dist = Vector3.Distance(cam.transform.position, worldPos);
                        if (dist > _espDistance) continue;

                        float x = screenPos.x;
                        float y = Screen.height - screenPos.y;

                        GUI.color = Color.red;
                        GUI.Box(new Rect(x - 2, y - 2, 4, 4), "");
                        GUI.Label(new Rect(x + 5, y - 10, 150, 20), $"NPC [{Mathf.RoundToInt(dist)}m]");
                    }
                }
                catch
                {
                    continue;
                }
            }

            // Draw Cameras
            foreach (var camComp in _cachedCameras)
            {
                if (camComp == null || !camComp.gameObject.activeInHierarchy) continue;

                Vector3 worldPos = camComp.transform.position;
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                if (screenPos.z > 0)
                {
                    float dist = Vector3.Distance(cam.transform.position, worldPos);
                    if (dist > _espDistance) continue;

                    float x = screenPos.x;
                    float y = Screen.height - screenPos.y;

                    GUI.color = Color.magenta;
                    GUI.Box(new Rect(x - 2, y - 2, 4, 4), "");
                    GUI.Label(new Rect(x + 5, y - 10, 150, 20), $"CAM [{Mathf.RoundToInt(dist)}m]");
                }
            }
        }

        private bool IsPlayerCar(Component carComp)
        {
            try
            {
                FieldInfo thiefsVehicleField = carComp.GetType().GetField("thiefsVehicle", BindingFlags.Public | BindingFlags.Instance);
                if (thiefsVehicleField != null)
                {
                    return (bool)thiefsVehicleField.GetValue(carComp);
                }
            }
            catch { }
            
            return false;
        }

        private void DrawCarESP()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Camera.current;
            if (cam == null) return;

            foreach (var carComp in _cachedVehicles)
            {
                if (carComp == null || !carComp.gameObject.activeInHierarchy) continue;

                Vector3 worldPos = carComp.transform.position;
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                if (screenPos.z > 0)
                {
                    float dist = Vector3.Distance(cam.transform.position, worldPos);
                    if (dist > _espDistance) continue;

                    float x = screenPos.x;
                    float y = Screen.height - screenPos.y;

                    bool isPlayerCar = IsPlayerCar(carComp);
                    Color carColor = isPlayerCar ? Color.green : new Color(1f, 0.5f, 0f);
                    string label = isPlayerCar ? "🚗 MY CAR" : "🚗 CAR";

                    GUI.color = carColor;
                    GUI.Box(new Rect(x - 2, y - 2, 4, 4), "");
                    GUI.Label(new Rect(x + 5, y - 10, 150, 20), $"{label} [{Mathf.RoundToInt(dist)}m]");
                }
            }
        }

        private void OnDisable()
        {
            DisableWhiteWalls();
            DisableTransparentWalls();
            DisableAIChams();
            DisableCarChams();
        }
    }
}
