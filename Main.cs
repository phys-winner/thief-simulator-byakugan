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
        private float _lastWallTransparency = -1.0f; // Force first update
        
        private Rect _windowRect = new Rect(20, 20, 350, 440);
        private float _lastUpdateTime = 0;
        
        // Cached game objects
        private List<ItemData> _cachedItems = new List<ItemData>();
        private List<AIData> _cachedAI = new List<AIData>();
        private List<CameraData> _cachedCameras = new List<CameraData>();
        private List<VehicleData> _cachedVehicles = new List<VehicleData>();
        
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

        // Reflection cache
        private System.Type _itemObjectType;
        private System.Type _humanAIObjectType;
        private System.Type _cctvCameraObjectType;
        private System.Type _vehicleType;

        private FieldInfo _itemAssetField;
        private FieldInfo _isCashField;
        private FieldInfo _itemNameField;
        private FieldInfo _isBigItemField;
        private FieldInfo _itemTypeField;
        private FieldInfo _renderersOnField;
        private PropertyInfo _thiefDetectorProperty;
        private FieldInfo _thiefsVehicleField;

        // Optimized updates
        private List<Material> _instantiatedTransparentMaterials = new List<Material>();

        private void Start()
        {
            // Cache types and fields
            _itemObjectType = System.Type.GetType("ItemObject, Assembly-CSharp");
            _humanAIObjectType = System.Type.GetType("HumanAIObject, Assembly-CSharp");
            _cctvCameraObjectType = System.Type.GetType("CCTVCameraObject, Assembly-CSharp");
            _vehicleType = System.Type.GetType("ControllableVehicle, Assembly-CSharp");

            if (_itemObjectType != null)
            {
                _itemAssetField = _itemObjectType.GetField("itemAsset", BindingFlags.Public | BindingFlags.Instance);
                _isCashField = _itemObjectType.GetField("isCash", BindingFlags.Public | BindingFlags.Instance);

                if (_itemAssetField != null)
                {
                    System.Type itemAssetType = _itemAssetField.FieldType;
                    _itemNameField = itemAssetType.GetField("itemName", BindingFlags.Public | BindingFlags.Instance);
                    _isBigItemField = itemAssetType.GetField("isBigItem", BindingFlags.Public | BindingFlags.Instance);
                    _itemTypeField = itemAssetType.GetField("itemType", BindingFlags.Public | BindingFlags.Instance);
                }
            }

            if (_humanAIObjectType != null)
            {
                _renderersOnField = _humanAIObjectType.GetField("renderersOn", BindingFlags.NonPublic | BindingFlags.Instance);
                _thiefDetectorProperty = _humanAIObjectType.GetProperty("thiefDetector", BindingFlags.Public | BindingFlags.Instance);
            }

            if (_vehicleType != null)
            {
                _thiefsVehicleField = _vehicleType.GetField("thiefsVehicle", BindingFlags.Public | BindingFlags.Instance);
            }

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
                // Optimization: Only update material colors if the transparency setting has changed
                if (_wallTransparency != _lastWallTransparency)
                {
                    UpdateWallTransparency();
                    _lastWallTransparency = _wallTransparency;
                }
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
            if (_itemObjectType != null)
            {
                Component[] itemObjects = GameObject.FindObjectsOfType(_itemObjectType) as Component[];
                if (itemObjects != null)
                {
                    foreach (var item in itemObjects)
                    {
                        if (item != null && item.gameObject.activeInHierarchy)
                        {
                            _cachedItems.Add(new ItemData
                            {
                                Component = item,
                                Transform = item.transform,
                                Info = GetItemInfo(item),
                                Color = GetItemColor(item),
                                ShouldDraw = ShouldDrawItem(item)
                            });
                        }
                    }
                }
            }

            // Find HumanAIObject components
            if (_humanAIObjectType != null)
            {
                Component[] aiObjects = GameObject.FindObjectsOfType(_humanAIObjectType) as Component[];
                if (aiObjects != null)
                {
                    foreach (var ai in aiObjects)
                    {
                        if (ai != null && ai.gameObject.activeInHierarchy)
                        {
                            Transform eyeTrans = null;
                            try
                            {
                                if (_thiefDetectorProperty != null)
                                {
                                    object thiefDetector = _thiefDetectorProperty.GetValue(ai, null);
                                    if (thiefDetector != null)
                                    {
                                        Component detectorComp = thiefDetector as Component;
                                        if (detectorComp != null)
                                            eyeTrans = detectorComp.transform;
                                    }
                                }
                            }
                            catch { }

                            _cachedAI.Add(new AIData
                            {
                                Component = ai,
                                Transform = ai.transform,
                                EyeTransform = eyeTrans,
                                IsVisible = IsNPCVisible(ai)
                            });
                        }
                    }
                }
            }

            // Find CCTVCameraObject components
            if (_cctvCameraObjectType != null)
            {
                Component[] cctvCameras = GameObject.FindObjectsOfType(_cctvCameraObjectType) as Component[];
                if (cctvCameras != null)
                {
                    foreach (var cam in cctvCameras)
                    {
                        if (cam != null && cam.gameObject.activeInHierarchy)
                        {
                            _cachedCameras.Add(new CameraData
                            {
                                Component = cam,
                                Transform = cam.transform
                            });
                        }
                    }
                }
            }
            
            // Find ALL vehicles
            if (_vehicleType != null)
            {
                Component[] vehicles = GameObject.FindObjectsOfType(_vehicleType) as Component[];
                if (vehicles != null)
                {
                    foreach (var car in vehicles)
                    {
                        if (car != null && car.gameObject.activeInHierarchy)
                        {
                            bool isPlayerCar = IsPlayerCar(car);
                            _cachedVehicles.Add(new VehicleData
                            {
                                Component = car,
                                Transform = car.transform,
                                IsPlayerCar = isPlayerCar,
                                Color = isPlayerCar ? Color.green : new Color(1f, 0.5f, 0f),
                                Label = isPlayerCar ? "🚗 MY CAR" : "🚗 CAR"
                            });
                        }
                    }
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
            _instantiatedTransparentMaterials.Clear();

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
                        _instantiatedTransparentMaterials.Add(transMats[i]);
                    }
                    
                    renderer.materials = transMats;
                    _transparentWallRenderers.Add(renderer);
                }
            }
        }

        private void UpdateWallTransparency()
        {
            // Optimization: Iterate over cached materials directly
            // This avoids accessing renderer.materials which creates a new array every time
            for (int i = 0; i < _instantiatedTransparentMaterials.Count; i++)
            {
                Material mat = _instantiatedTransparentMaterials[i];
                if (mat != null)
                {
                    Color col = mat.color;
                    col.a = _wallTransparency;
                    mat.color = col;
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
            _instantiatedTransparentMaterials.Clear();
        }

        private void ApplyAIChams()
        {
            _aiRenderers.Clear();
            _originalAIMaterials.Clear();

            for (int i = 0; i < _cachedAI.Count; i++)
            {
                AIData ai = _cachedAI[i];
                if (ai.Component == null) continue;
                if (!ai.Component.gameObject.activeInHierarchy) continue;
                if (!ai.IsVisible) continue;
                
                Renderer[] renderers = ai.Component.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled) continue;
                    if (renderer.gameObject.layer == 5) continue;
                    
                    _originalAIMaterials[renderer] = renderer.materials;
                    
                    Material[] chamMats = new Material[renderer.materials.Length];
                    for (int j = 0; j < chamMats.Length; j++)
                    {
                        chamMats[j] = _aiChamMaterial;
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

            for (int i = 0; i < _cachedVehicles.Count; i++)
            {
                VehicleData car = _cachedVehicles[i];
                if (car.Component == null) continue;
                if (!car.Component.gameObject.activeInHierarchy) continue;
                
                Material chamMat = car.IsPlayerCar ? _playerCarChamMaterial : _otherCarChamMaterial;
                
                Renderer[] renderers = car.Component.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled) continue;
                    if (renderer.gameObject.layer == 5) continue;
                    
                    _originalCarMaterials[renderer] = renderer.materials;
                    
                    Material[] chamMats = new Material[renderer.materials.Length];
                    for (int j = 0; j < chamMats.Length; j++)
                    {
                        chamMats[j] = chamMat;
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
                if (_renderersOnField != null)
                {
                    return (bool)_renderersOnField.GetValue(aiComp);
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

            if (IsGamePaused() && !_showMenu) return;

            // Cache camera once per frame
            Camera cam = Camera.main;
            if (cam == null) cam = Camera.current;
            if (cam == null) return;

            // Cache frame-wide constants for ESP
            Vector3 camPos = cam.transform.position;
            float screenHeight = Screen.height;
            float sqrMaxDist = _espDistance * _espDistance;

            if (_itemEsp)
            {
                DrawItemESP(cam, camPos, screenHeight, sqrMaxDist);
            }

            if (_aiEsp)
            {
                DrawAIESP(cam, camPos, screenHeight, sqrMaxDist);
            }
            
            if (_carEsp)
            {
                DrawCarESP(cam, camPos, screenHeight, sqrMaxDist);
            }
        }

        private void WindowFunction(int windowID)
        {
            GUI.contentColor = Color.cyan;
            GUILayout.Label("--- FEATURES ---");
            GUI.contentColor = Color.white;

            _itemEsp = GUILayout.Toggle(_itemEsp, " Item ESP");
            GUI.enabled = _itemEsp;
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            _showBrickItems = GUILayout.Toggle(_showBrickItems, " Show Bricks");
            if (!_itemEsp)
            {
                GUI.contentColor = Color.gray;
                GUILayout.Label("(Requires Item ESP)");
                GUI.contentColor = Color.white;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            _aiEsp = GUILayout.Toggle(_aiEsp, " AI ESP + Chams");
            _carEsp = GUILayout.Toggle(_carEsp, " Car ESP + Chams");
            _transparentWalls = GUILayout.Toggle(_transparentWalls, " Transparent Walls");

            GUI.enabled = !_transparentWalls;
            _whiteWalls = GUILayout.Toggle(_whiteWalls, " White Walls");
            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUI.contentColor = Color.gray;
            string whiteWallsHint = _transparentWalls ? "(N/A: Transparent Walls active)" : "High-contrast textures";
            GUILayout.Label(whiteWallsHint);
            GUI.contentColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.contentColor = Color.yellow;
            GUILayout.Label("--- SETTINGS ---");
            GUI.contentColor = Color.white;

            if (GUILayout.Button("Reset All to Defaults"))
            {
                _itemEsp = true;
                _aiEsp = true;
                _carEsp = true;
                _transparentWalls = false;
                _whiteWalls = false;
                _showBrickItems = false;
                _confirmUnload = false;
                _espDistance = 100f;
                _wallTransparency = 0.3f;
                _lastWallTransparency = -1.0f;
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"ESP Distance: {_espDistance:F0}m");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", GUILayout.Width(50)))
            {
                _espDistance = 100f;
            }
            GUILayout.EndHorizontal();
            _espDistance = GUILayout.HorizontalSlider(_espDistance, 10f, 500f);
            
            if (_transparentWalls)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Wall Alpha: {_wallTransparency:F2}");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset", GUILayout.Width(50)))
                {
                    _wallTransparency = 0.3f;
                }
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
                GUILayout.BeginHorizontal();
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
                GUILayout.EndHorizontal();
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
                if (_itemAssetField != null)
                {
                    object itemAsset = _itemAssetField.GetValue(itemComp);
                    if (itemAsset != null)
                    {
                        string itemName = _itemNameField != null ? (string)_itemNameField.GetValue(itemAsset) : "Unknown";
                        bool isBig = _isBigItemField != null && (bool)_isBigItemField.GetValue(itemAsset);
                        bool isCash = _isCashField != null && (bool)_isCashField.GetValue(itemComp);
                        
                        string typeIcon = "💎";
                        
                        if (isCash)
                        {
                            typeIcon = "💵";
                        }
                        else if (_itemTypeField != null)
                        {
                            int typeValue = (int)_itemTypeField.GetValue(itemAsset);
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
                if (_itemAssetField != null)
                {
                    object itemAsset = _itemAssetField.GetValue(itemComp);
                    if (itemAsset != null)
                    {
                        string itemName = _itemNameField != null ? ((string)_itemNameField.GetValue(itemAsset)).ToLower() : "";
                        
                        if (itemName.Contains("key"))
                            return Color.yellow;
                        if (itemName.Contains("note"))
                            return Color.cyan;
                        if (itemName.Contains("brick"))
                            return new Color(1f, 0.5f, 0f);
                        
                        if (_itemTypeField != null)
                        {
                            int typeValue = (int)_itemTypeField.GetValue(itemAsset);
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
                if (_itemAssetField != null)
                {
                    object itemAsset = _itemAssetField.GetValue(itemComp);
                    if (itemAsset != null)
                    {
                        string itemName = _itemNameField != null ? ((string)_itemNameField.GetValue(itemAsset)).ToLower() : "";
                        
                        if (itemName.Contains("brick") && !_showBrickItems)
                            return false;
                    }
                }
            }
            catch { }
            
            return true;
        }

        private void DrawItemESP(Camera cam, Vector3 camPos, float screenHeight, float sqrMaxDist)
        {
            for (int i = 0; i < _cachedItems.Count; i++)
            {
                ItemData item = _cachedItems[i];
                if (item.Component == null) continue;
                if (!item.Component.gameObject.activeInHierarchy) continue;
                if (!item.ShouldDraw) continue;

                Vector3 worldPos = item.Transform.position;

                // Performance: Check distance before expensive WorldToScreenPoint
                // Performance: Use sqrMagnitude to avoid square root
                float sqrDist = (worldPos - camPos).sqrMagnitude;
                if (sqrDist > sqrMaxDist) continue;

                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z > 0)
                {
                    float x = screenPos.x;
                    float y = screenHeight - screenPos.y;

                    GUI.color = item.Color;
                    GUI.Box(new Rect(x - 2, y - 2, 4, 4), "");
                    GUI.Label(new Rect(x + 5, y - 10, 250, 20), $"{item.Info} [{Mathf.RoundToInt(Mathf.Sqrt(sqrDist))}m]");
                }
            }
        }

        private void DrawAIESP(Camera cam, Vector3 camPos, float screenHeight, float sqrMaxDist)
        {
            for (int i = 0; i < _cachedAI.Count; i++)
            {
                AIData ai = _cachedAI[i];
                if (ai.Component == null) continue;
                if (!ai.Component.gameObject.activeInHierarchy) continue;
                if (!ai.IsVisible) continue;

                try
                {
                    Vector3 worldPos = ai.Transform.position;

                    // Performance: Check distance before expensive WorldToScreenPoint
                    // Performance: Use sqrMagnitude to avoid square root
                    float sqrDist = (worldPos - camPos).sqrMagnitude;
                    if (sqrDist > sqrMaxDist) continue;

                    Vector3 eyePos = ai.EyeTransform != null ? ai.EyeTransform.position : worldPos + Vector3.up * 1.6f;
                    Vector3 screenPos = cam.WorldToScreenPoint(eyePos);

                    if (screenPos.z > 0)
                    {
                        float x = screenPos.x;
                        float y = screenHeight - screenPos.y;

                        GUI.color = Color.red;
                        GUI.Box(new Rect(x - 2, y - 2, 4, 4), "");
                        GUI.Label(new Rect(x + 5, y - 10, 150, 20), $"NPC [{Mathf.RoundToInt(Mathf.Sqrt(sqrDist))}m]");
                    }
                }
                catch { }
            }

            // Draw Cameras
            for (int i = 0; i < _cachedCameras.Count; i++)
            {
                CameraData camData = _cachedCameras[i];
                if (camData.Component == null) continue;
                if (!camData.Component.gameObject.activeInHierarchy) continue;

                Vector3 worldPos = camData.Transform.position;

                // Performance: Check distance before expensive WorldToScreenPoint
                // Performance: Use sqrMagnitude to avoid square root
                float sqrDist = (worldPos - camPos).sqrMagnitude;
                if (sqrDist > sqrMaxDist) continue;

                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z > 0)
                {
                    float x = screenPos.x;
                    float y = screenHeight - screenPos.y;

                    GUI.color = Color.magenta;
                    GUI.Box(new Rect(x - 2, y - 2, 4, 4), "");
                    GUI.Label(new Rect(x + 5, y - 10, 150, 20), $"CAM [{Mathf.RoundToInt(Mathf.Sqrt(sqrDist))}m]");
                }
            }
        }

        private bool IsPlayerCar(Component carComp)
        {
            try
            {
                if (_thiefsVehicleField != null)
                {
                    return (bool)_thiefsVehicleField.GetValue(carComp);
                }
            }
            catch { }
            
            return false;
        }

        private void DrawCarESP(Camera cam, Vector3 camPos, float screenHeight, float sqrMaxDist)
        {
            for (int i = 0; i < _cachedVehicles.Count; i++)
            {
                VehicleData car = _cachedVehicles[i];
                if (car.Component == null) continue;
                if (!car.Component.gameObject.activeInHierarchy) continue;

                Vector3 worldPos = car.Transform.position;

                // Performance: Check distance before expensive WorldToScreenPoint
                // Performance: Use sqrMagnitude to avoid square root
                float sqrDist = (worldPos - camPos).sqrMagnitude;
                if (sqrDist > sqrMaxDist) continue;

                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z > 0)
                {
                    float x = screenPos.x;
                    float y = screenHeight - screenPos.y;

                    GUI.color = car.Color;
                    GUI.Box(new Rect(x - 2, y - 2, 4, 4), "");
                    GUI.Label(new Rect(x + 5, y - 10, 150, 20), $"{car.Label} [{Mathf.RoundToInt(Mathf.Sqrt(sqrDist))}m]");
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

        #region Cache Structs
        private struct ItemData
        {
            public Component Component;
            public Transform Transform;
            public string Info;
            public Color Color;
            public bool ShouldDraw;
        }

        private struct AIData
        {
            public Component Component;
            public Transform Transform;
            public Transform EyeTransform;
            public bool IsVisible;
        }

        private struct CameraData
        {
            public Component Component;
            public Transform Transform;
        }

        private struct VehicleData
        {
            public Component Component;
            public Transform Transform;
            public bool IsPlayerCar;
            public Color Color;
            public string Label;
        }
        #endregion
    }
}
