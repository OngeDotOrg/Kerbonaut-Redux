using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KerbonautRedux
{

    public class HairPiece
    {
        public string MeshName;
        public string TexturePath;
        public string NormalMapPath;
        public string BumpTexturePath;
        public Color Color = Color.white;
        public Vector3 PositionOffset = Vector3.zero;
        public Vector3 RotationOffset = Vector3.zero;
        public float Scale = 1.0f;
        public string BoneName = "bn_upperJaw01";
        public string Shader = "KSP/Specular";
    }

    public class HairConfig
    {
        public string KerbalName;

        public List<HairPiece> HairPieces = new List<HairPiece>();

        public string Trait;
        public float Courage = 0.5f;
        public float Stupidity = 0.5f;
        public bool IsBadass = false;

        public string KerbalTexture;
        public string KerbalNormalMap;

        public string bodyTexHead;
        public string bodyTexHeadNormal;
        public string bodyTexBody;
        public string bodyTexBodyNormal;
        public string bodyTexArms;
        public string bodyTexArmsNormal;
        public string bodyTexLegs;
        public string bodyTexLegsNormal;
        public string bodyTexHelmet;
        public string bodyTexHelmetNormal;
        public string bodyTexEyes;
        public string bodyTexEyesNormal;

        public bool HideHead = false;
        public bool HidePonytail = false;
        public bool HideEyes = false;
        public bool HideTeeth = false;
        public bool HideTongue = false;

        public string HairMeshName;
        public string HairTexturePath;
        public Color HairColor = Color.white;
        public Vector3 PositionOffset = Vector3.zero;
        public Vector3 RotationOffset = Vector3.zero;
        public float Scale = 1.0f;
        public string BoneName = "bn_upperJaw01";
        public string Shader = "KSP/Specular";
    }

    public class HairConfigs
    {
        public static HairConfigs Instance;

        private Dictionary<string, HairConfig> configs = new Dictionary<string, HairConfig>();
        private Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        private static readonly string DIR = "KerbonautRedux/";
        private static readonly string TEX_DIR = DIR + "Textures/";
        private static readonly string MODELS_DIR = "GameData/" + DIR + "Models/";

        public static readonly string[] AvailableShaders = new string[]
        {
            "KSP/Specular",
            "KSP/Bumped",
            "KSP/Bumped Specular",
            "KSP/Bumped Specular (Mapped)",
            "KSP/Alpha/Cutoff",
            "KSP/Alpha/Cutoff Bumped",
            "KSP/Alpha/Translucent",
            "KSP/Alpha/Translucent Specular",
            "KSP/Alpha/Unlit Transparent"
        };

        public IEnumerable<string> ConfigNames => configs.Keys;

        public void Load()
        {
            LoadMeshes();
            LoadTextures();

            string configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                try
                {
                    LoadConfigsFromJson(configPath);
                    Debug.Log($"[KerbonautRedux] Loaded {configs.Count} configs from JSON");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[KerbonautRedux] Failed to load JSON config: {ex.Message}");
                    DefineConfigs();
                }
            }
            else
            {
                DefineConfigs();
                Debug.Log($"[KerbonautRedux] Loaded {configs.Count} built-in configs");
            }
        }

        private string GetConfigPath()
        {
            try
            {
                string dllPath = typeof(HairConfigs).Assembly.Location;
                if (!string.IsNullOrEmpty(dllPath))
                {
                    string dllDir = Path.GetDirectoryName(dllPath);
                    return Path.Combine(dllDir, "KerbonautRedux.json");
                }
            }
            catch { }

            return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "KerbonautRedux", "KerbonautRedux.json");
        }

        private void LoadConfigsFromJson(string path)
        {
            string json = File.ReadAllText(path);

            int configsIdx = json.IndexOf("\"configs\"");
            if (configsIdx < 0) throw new Exception("No 'configs' array found");

            int arrayStart = json.IndexOf('[', configsIdx);
            int arrayEnd = FindMatchingBracket(json, arrayStart);
            if (arrayStart < 0 || arrayEnd <= arrayStart) throw new Exception("Invalid configs array");

            string arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);

            int braceDepth = 0;
            int objStart = -1;

            for (int i = 0; i < arrayContent.Length; i++)
            {
                char c = arrayContent[i];
                if (c == '{')
                {
                    if (braceDepth == 0) objStart = i;
                    braceDepth++;
                }
                else if (c == '}')
                {
                    braceDepth--;
                    if (braceDepth == 0 && objStart >= 0)
                    {
                        string obj = arrayContent.Substring(objStart + 1, i - objStart - 1);
                        ParseConfigObject(obj);
                        objStart = -1;
                    }
                }
            }
        }

        private int FindMatchingBracket(string text, int openPos)
        {
            int depth = 1;
            for (int i = openPos + 1; i < text.Length; i++)
            {
                if (text[i] == '[') depth++;
                else if (text[i] == ']') depth--;
                if (depth == 0) return i;
            }
            return -1;
        }

        private void ParseConfigObject(string content)
        {
            string kerbalName = GetJsonString(content, "kerbalName");
            if (string.IsNullOrEmpty(kerbalName)) return;

            var config = new HairConfig
            {
                KerbalName = kerbalName
            };

            config.HideHead = GetJsonBool(content, "hideHead", false);
            config.HidePonytail = GetJsonBool(content, "hidePonytail", false);
            config.HideEyes = GetJsonBool(content, "hideEyes", false);
            config.HideTeeth = GetJsonBool(content, "hideTeeth", false);
            config.HideTongue = GetJsonBool(content, "hideTongue", false);

            config.KerbalTexture = GetJsonString(content, "kerbalTexture");
            config.KerbalNormalMap = GetJsonString(content, "kerbalNormalMap");

            ParseBodyTextures(content, config);

            config.HairPieces = ParseHairPieces(content);
            Debug.Log($"[KerbonautRedux] ParseConfigObject: HairPieces.Count = {config.HairPieces.Count} for {kerbalName}");

            if (config.HairPieces.Count > 0)
            {

                Debug.Log($"[KerbonautRedux] Config: {kerbalName} with {config.HairPieces.Count} hair pieces");
            }
            else
            {

                config.HairMeshName = GetJsonString(content, "meshName") ?? "ValentinaHair";
                config.HairTexturePath = TEX_DIR + (GetJsonString(content, "meshTexture") ?? "valentina_hair.png");
                config.BoneName = GetJsonString(content, "boneName") ?? "bn_upperJaw01";
                config.PositionOffset = new Vector3(
                    GetJsonFloat(content, "posX", 0f),
                    GetJsonFloat(content, "posY", 0.05f),
                    GetJsonFloat(content, "posZ", 0.02f)
                );
                config.RotationOffset = new Vector3(
                    GetJsonFloat(content, "rotX", 0f),
                    GetJsonFloat(content, "rotY", 0f),
                    GetJsonFloat(content, "rotZ", 0f)
                );
                config.Scale = GetJsonFloat(content, "scale", 1f);
                config.Shader = GetJsonString(content, "shader") ?? "KSP/Specular";

                float r = GetJsonFloat(content, "hairColorR", 0.2f);
                float g = GetJsonFloat(content, "hairColorG", 0.15f);
                float b = GetJsonFloat(content, "hairColorB", 0.1f);
                config.HairColor = new Color(r, g, b, 1f);

                Debug.Log($"[KerbonautRedux] Config: {kerbalName} -> {config.HairMeshName} (single hair, backwards compatible)");
            }

            configs[kerbalName] = config;
        }

        private List<HairPiece> ParseHairPieces(string content)
        {
            var pieces = new List<HairPiece>();

            Debug.Log($"[KerbonautRedux] ParseHairPieces: Content preview (first 200 chars): {content.Substring(0, Math.Min(200, content.Length))}");

            string pattern = "\"hairPieces\"";
            int idx = content.IndexOf(pattern);
            if (idx < 0)
            {
                Debug.Log("[KerbonautRedux] ParseHairPieces: 'hairPieces' not found in content");
                return pieces;
            }

            Debug.Log($"[KerbonautRedux] ParseHairPieces: Found 'hairPieces' at index {idx}");

            int bracketStart = content.IndexOf('[', idx);
            if (bracketStart < 0)
            {
                Debug.Log("[KerbonautRedux] ParseHairPieces: No opening bracket found");
                return pieces;
            }

            int bracketEnd = FindMatchingBracket(content, bracketStart);
            if (bracketEnd <= bracketStart)
            {
                Debug.Log("[KerbonautRedux] ParseHairPieces: No matching closing bracket found");
                return pieces;
            }

            string arrayContent = content.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            Debug.Log($"[KerbonautRedux] ParseHairPieces: Array content length: {arrayContent.Length}");
            Debug.Log($"[KerbonautRedux] ParseHairPieces: Array content: {arrayContent}");

            int braceDepth = 0;
            int objStart = -1;
            int objectCount = 0;

            for (int i = 0; i < arrayContent.Length; i++)
            {
                char c = arrayContent[i];
                if (c == '{')
                {
                    if (braceDepth == 0) objStart = i;
                    braceDepth++;
                }
                else if (c == '}')
                {
                    braceDepth--;
                    if (braceDepth == 0 && objStart >= 0)
                    {
                        string obj = arrayContent.Substring(objStart + 1, i - objStart - 1);
                        Debug.Log($"[KerbonautRedux] ParseHairPieces: Found object {objectCount}: {obj.Substring(0, Math.Min(50, obj.Length))}...");
                        HairPiece piece = ParseHairPieceObject(obj);
                        if (piece != null)
                        {
                            pieces.Add(piece);
                            Debug.Log($"[KerbonautRedux] ParseHairPieces: Added piece '{piece.MeshName}' with shader '{piece.Shader}'");
                        }
                        objStart = -1;
                        objectCount++;
                    }
                }
            }

            Debug.Log($"[KerbonautRedux] ParseHairPieces: Total pieces parsed: {pieces.Count}");
            return pieces;
        }

        private HairPiece ParseHairPieceObject(string content)
        {
            var piece = new HairPiece
            {
                MeshName = GetJsonString(content, "meshName") ?? "ValentinaHair",
                TexturePath = TEX_DIR + (GetJsonString(content, "meshTexture") ?? "valentina_hair.png"),
                BoneName = GetJsonString(content, "boneName") ?? "bn_upperJaw01",
                PositionOffset = new Vector3(
                    GetJsonFloat(content, "posX", 0f),
                    GetJsonFloat(content, "posY", 0.05f),
                    GetJsonFloat(content, "posZ", 0.02f)
                ),
                RotationOffset = new Vector3(
                    GetJsonFloat(content, "rotX", 0f),
                    GetJsonFloat(content, "rotY", 0f),
                    GetJsonFloat(content, "rotZ", 0f)
                ),
                Scale = GetJsonFloat(content, "scale", 1f),
                Shader = GetJsonString(content, "shader") ?? "KSP/Specular"
            };

            float r = GetJsonFloat(content, "hairColorR", 0.2f);
            float g = GetJsonFloat(content, "hairColorG", 0.15f);
            float b = GetJsonFloat(content, "hairColorB", 0.1f);
            piece.Color = new Color(r, g, b, 1f);

            string normalMap = GetJsonString(content, "normalMap");
            if (string.IsNullOrEmpty(normalMap))
            {
                normalMap = GetJsonString(content, "bumpTexture");
            }
            if (!string.IsNullOrEmpty(normalMap))
            {
                piece.NormalMapPath = TEX_DIR + normalMap;
                piece.BumpTexturePath = piece.NormalMapPath;
                Debug.Log($"[KerbonautRedux] ParseHairPieceObject: Normal map set to '{piece.NormalMapPath}'");
            }

            return piece;
        }

        private void ParseBodyTextures(string content, HairConfig config)
        {

            string pattern = "\"bodyTextures\"";
            int idx = content.IndexOf(pattern);
            if (idx < 0) return;

            int braceStart = content.IndexOf('{', idx);
            if (braceStart < 0) return;

            int braceEnd = FindMatchingBrace(content, braceStart);
            if (braceEnd <= braceStart) return;

            string objContent = content.Substring(braceStart + 1, braceEnd - braceStart - 1);

            config.bodyTexHead = GetJsonString(objContent, "HeadDiffuse") ?? "";
            config.bodyTexHeadNormal = GetJsonString(objContent, "HeadNormal") ?? "";
            config.bodyTexBody = GetJsonString(objContent, "BodyDiffuse") ?? "";
            config.bodyTexBodyNormal = GetJsonString(objContent, "BodyNormal") ?? "";
            config.bodyTexArms = GetJsonString(objContent, "ArmsDiffuse") ?? "";
            config.bodyTexArmsNormal = GetJsonString(objContent, "ArmsNormal") ?? "";
            config.bodyTexLegs = GetJsonString(objContent, "LegsDiffuse") ?? "";
            config.bodyTexLegsNormal = GetJsonString(objContent, "LegsNormal") ?? "";
            config.bodyTexHelmet = GetJsonString(objContent, "HelmetDiffuse") ?? "";
            config.bodyTexHelmetNormal = GetJsonString(objContent, "HelmetNormal") ?? "";
            config.bodyTexEyes = GetJsonString(objContent, "EyesDiffuse") ?? "";
            config.bodyTexEyesNormal = GetJsonString(objContent, "EyesNormal") ?? "";

            Debug.Log($"[KerbonautRedux] Parsed body textures for {config.KerbalName}: Head={config.bodyTexHead}, Body={config.bodyTexBody}, Arms={config.bodyTexArms}, Legs={config.bodyTexLegs}, Helmet={config.bodyTexHelmet}, Eyes={config.bodyTexEyes}");
        }

        private int FindMatchingBrace(string text, int openPos)
        {
            int depth = 1;
            for (int i = openPos + 1; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}') depth--;
                if (depth == 0) return i;
            }
            return -1;
        }

        private string GetJsonString(string content, string key)
        {
            string pattern = "\"" + key + "\"";
            int idx = content.IndexOf(pattern);
            if (idx < 0) return null;

            int colonIdx = content.IndexOf(':', idx);
            if (colonIdx < 0) return null;

            int valueStart = -1, valueEnd = -1;
            bool inString = false;

            for (int i = colonIdx + 1; i < content.Length; i++)
            {
                char c = content[i];
                if (c == '"')
                {
                    if (!inString)
                    {
                        valueStart = i + 1;
                        inString = true;
                    }
                    else
                    {
                        valueEnd = i;
                        break;
                    }
                }
            }

            if (valueStart >= 0 && valueEnd > valueStart)
                return content.Substring(valueStart, valueEnd - valueStart);

            return null;
        }

        private float GetJsonFloat(string content, string key, float defaultValue)
        {
            string pattern = "\"" + key + "\"";
            int idx = content.IndexOf(pattern);
            if (idx < 0) return defaultValue;

            int colonIdx = content.IndexOf(':', idx);
            if (colonIdx < 0) return defaultValue;

            int numStart = -1;
            for (int i = colonIdx + 1; i < content.Length; i++)
            {
                char c = content[i];
                if (char.IsDigit(c) || c == '-' || c == '.')
                {
                    if (numStart < 0) numStart = i;
                }
                else if (numStart >= 0)
                {
                    string numStr = content.Substring(numStart, i - numStart);
                    if (float.TryParse(numStr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float val))
                    {
                        return val;
                    }
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        private bool GetJsonBool(string content, string key, bool defaultValue)
        {
            string pattern = "\"" + key + "\"";
            int idx = content.IndexOf(pattern);
            if (idx < 0) return defaultValue;

            int colonIdx = content.IndexOf(':', idx);
            if (colonIdx < 0) return defaultValue;

            for (int i = colonIdx + 1; i < content.Length; i++)
            {
                char c = content[i];
                if (char.IsWhiteSpace(c)) continue;

                if (c == 't' && i + 3 < content.Length && content.Substring(i, 4) == "true")
                    return true;
                if (c == 'f' && i + 4 < content.Length && content.Substring(i, 5) == "false")
                    return false;

                if (c == ',' || c == '}') break;
            }

            return defaultValue;
        }

        private List<string> GetJsonStringArray(string content, string key)
        {
            var result = new List<string>();
            string pattern = "\"" + key + "\"";
            int idx = content.IndexOf(pattern);
            if (idx < 0) return result;

            int bracketStart = content.IndexOf('[', idx);
            if (bracketStart < 0) return result;

            int bracketEnd = FindMatchingBracket(content, bracketStart);
            if (bracketEnd <= bracketStart) return result;

            string arrayContent = content.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);

            bool inString = false;
            int valueStart = -1;

            for (int i = 0; i < arrayContent.Length; i++)
            {
                char c = arrayContent[i];
                if (c == '"')
                {
                    if (!inString)
                    {
                        inString = true;
                        valueStart = i + 1;
                    }
                    else
                    {
                        if (valueStart >= 0 && i > valueStart)
                        {
                            result.Add(arrayContent.Substring(valueStart, i - valueStart));
                        }
                        inString = false;
                        valueStart = -1;
                    }
                }
            }

            return result;
        }

        private void LoadMeshes()
        {

            if (!Directory.Exists(MODELS_DIR))
            {
                Debug.LogWarning($"[KerbonautRedux] Models directory not found: {MODELS_DIR}");
                return;
            }

            string[] vtxFiles = Directory.GetFiles(MODELS_DIR, "*.vtx");
            Debug.Log($"[KerbonautRedux] Found {vtxFiles.Length} .vtx files in {MODELS_DIR}");

            foreach (string vtxPath in vtxFiles)
            {
                string name = Path.GetFileNameWithoutExtension(vtxPath);
                try
                {
                    Mesh mesh = LoadMesh(name);
                    if (mesh != null)
                    {
                        meshes[name] = mesh;
                        Debug.Log($"[KerbonautRedux] Loaded mesh: {name}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[KerbonautRedux] Failed to load mesh {name}: {ex.Message}");
                }
            }
        }

        private void LoadTextures()
        {

            Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
            foreach (Texture2D tex in allTextures)
            {
                textures[tex.name] = tex;
            }

            Debug.Log($"[KerbonautRedux] Loaded {textures.Count} textures from Resources");
        }

        private void DefineConfigs()
        {

            var valentinaConfig = new HairConfig
            {
                KerbalName = "Valentina",
                HairMeshName = "ValentinaHair",
                HairTexturePath = TEX_DIR + "valentina_hair.png",
                HairColor = new Color(0.2f, 0.15f, 0.1f),
                PositionOffset = new Vector3(0f, 0.05f, 0.02f),
                RotationOffset = Vector3.zero,
                Scale = 1.0f,
                Shader = "KSP/Specular",
                Trait = "Pilot",
                Courage = 0.5f,
                Stupidity = 0.5f,
                IsBadass = true
            };

            configs["Valentina"] = valentinaConfig;
            configs["Valentina Kerman"] = valentinaConfig;

        }

        private Mesh LoadMesh(string name)
        {
            string basePath = MODELS_DIR + name;

            Vector3[] vertices = LoadVector3Array(basePath + ".vtx");
            Vector2[] uvs = LoadVector2Array(basePath + ".tex");
            Vector3[] normals = LoadVector3ArrayIfExists(basePath + ".nml");
            int[] indices = LoadIntArray(basePath + ".idx");

            if (vertices == null || indices == null)
            {
                Debug.LogWarning($"[KerbonautRedux] Could not load mesh: {name}");
                return null;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = indices;

            if (normals != null)
                mesh.normals = normals;
            else
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();

            return mesh;
        }

        private Vector3[] LoadVector3Array(string path)
        {
            if (!File.Exists(path))
                return null;

            using (FileStream fs = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                uint count = reader.ReadUInt32();
                Vector3[] array = new Vector3[count];

                for (int i = 0; i < count; i++)
                {
                    array[i] = new Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle()
                    );
                }

                return array;
            }
        }

        private Vector3[] LoadVector3ArrayIfExists(string path)
        {
            return File.Exists(path) ? LoadVector3Array(path) : null;
        }

        private Vector2[] LoadVector2Array(string path)
        {
            if (!File.Exists(path))
                return null;

            using (FileStream fs = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                uint count = reader.ReadUInt32();
                Vector2[] array = new Vector2[count];

                for (int i = 0; i < count; i++)
                {
                    array[i] = new Vector2(
                        reader.ReadSingle(),
                        reader.ReadSingle()
                    );
                }

                return array;
            }
        }

        private int[] LoadIntArray(string path)
        {
            if (!File.Exists(path))
                return null;

            using (FileStream fs = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                uint count = reader.ReadUInt32();
                int[] array = new int[count];

                for (int i = 0; i < count; i++)
                {
                    array[i] = reader.ReadInt32();
                }

                return array;
            }
        }

        public HairConfig GetConfig(string kerbalName)
        {
            if (string.IsNullOrEmpty(kerbalName))
                return null;

            Debug.Log($"[KerbonautRedux] Looking up config for: '{kerbalName}'");

            if (configs.TryGetValue(kerbalName, out HairConfig config))
            {
                Debug.Log($"[KerbonautRedux] Found exact match for: {kerbalName}");
                return config;
            }

            string withoutKerman = kerbalName.Replace("Kerman", "").Trim();
            if (!string.IsNullOrEmpty(withoutKerman) && configs.TryGetValue(withoutKerman, out config))
            {
                Debug.Log($"[KerbonautRedux] Found match for '{withoutKerman}' (from '{kerbalName}')");
                return config;
            }

            string firstName = kerbalName.Split(' ')[0].Trim();
            if (!string.IsNullOrEmpty(firstName) && configs.TryGetValue(firstName, out config))
            {
                Debug.Log($"[KerbonautRedux] Found match for first name: {firstName}");
                return config;
            }

            Debug.Log($"[KerbonautRedux] No config found for '{kerbalName}' (tried: '{withoutKerman}', '{firstName}')");
            return null;
        }

        public Mesh GetMesh(string meshName)
        {
            meshes.TryGetValue(meshName, out Mesh mesh);
            return mesh;
        }

        public Texture2D GetTexture(string textureName)
        {

            string fullPath = TEX_DIR + textureName.Replace(".png", "").Replace(".dds", "");
            if (textures.TryGetValue(fullPath, out Texture2D tex))
            {
                Debug.Log($"[KerbonautRedux] Texture found with full path: '{fullPath}'");
                return tex;
            }

            string nameOnly = textureName.Replace(".png", "").Replace(".dds", "");
            if (textures.TryGetValue(nameOnly, out tex))
            {
                Debug.Log($"[KerbonautRedux] Texture found with name only: '{nameOnly}'");
                return tex;
            }

            if (textures.TryGetValue(textureName, out tex))
            {
                Debug.Log($"[KerbonautRedux] Texture found exact match: '{textureName}'");
                return tex;
            }

            Debug.LogWarning($"[KerbonautRedux] Texture NOT found. Tried: '{fullPath}', '{nameOnly}', '{textureName}'");
            return null;
        }

        public Material CreateMaterial(Texture2D diffuseTexture, Texture2D normalMap, Color color, string shaderName)
        {
            Debug.Log($"[KerbonautRedux] Creating material... shader: {shaderName}, diffuse is null: {diffuseTexture == null}, normalMap is null: {normalMap == null}");

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[KerbonautRedux] Shader '{shaderName}' not found, falling back to KSP/Specular");
                shader = Shader.Find("KSP/Specular");
            }

            Material mat = new Material(shader);

            if (diffuseTexture != null)
            {
                mat.mainTexture = diffuseTexture;

                mat.color = Color.white;
                Debug.Log($"[KerbonautRedux] *** MATERIAL CREATED WITH DIFFUSE TEXTURE ***");
                Debug.Log($"[KerbonautRedux] Diffuse: {diffuseTexture.name} ({diffuseTexture.width}x{diffuseTexture.height})");
            }
            else
            {

                mat.color = color;
                Debug.Log($"[KerbonautRedux] *** MATERIAL CREATED WITH SOLID COLOR (NO DIFFUSE) ***");
                Debug.Log($"[KerbonautRedux] Color used: {color}");
            }

            if (normalMap != null)
            {
                mat.SetTexture("_BumpMap", normalMap);
                mat.SetFloat("_BumpScale", 1.0f);
                Debug.Log($"[KerbonautRedux] Normal map: {normalMap.name} ({normalMap.width}x{normalMap.height})");
            }

            return mat;
        }

        public Material CreateMaterial(Texture2D texture, Color color, string shaderName)
        {
            return CreateMaterial(texture, null, color, shaderName);
        }

        public Material CreateMaterial(string texturePath, Color color, string shaderName)
        {
            Debug.Log($"[KerbonautRedux] CreateMaterial called with texturePath: '{texturePath}', shader: {shaderName}");

            Texture2D tex = GetTexture(texturePath);
            return CreateMaterial(tex, null, color, shaderName);
        }

        public Material CreateMaterial(string diffusePath, string normalMapPath, Color color, string shaderName)
        {
            Debug.Log($"[KerbonautRedux] CreateMaterial called with diffuse: '{diffusePath}', normal: '{normalMapPath}', shader: {shaderName}");

            Texture2D diffuse = GetTexture(diffusePath);
            Texture2D normalMap = null;

            if (!string.IsNullOrEmpty(normalMapPath))
            {
                normalMap = GetTexture(normalMapPath);
                if (normalMap == null)
                {
                    Debug.LogWarning($"[KerbonautRedux] Normal map not found: '{normalMapPath}'");
                }
            }

            return CreateMaterial(diffuse, normalMap, color, shaderName);
        }

        public void ReloadConfigs()
        {
            Debug.Log("[KerbonautRedux] Reloading configs, meshes, and textures...");

            textures.Clear();
            LoadTextures();

            meshes.Clear();
            LoadMeshes();

            configs.Clear();
            string configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                try
                {
                    LoadConfigsFromJson(configPath);
                    Debug.Log($"[KerbonautRedux] Reloaded {configs.Count} configs from JSON");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[KerbonautRedux] Failed to reload JSON config: {ex.Message}");
                    DefineConfigs();
                }
            }
            else
            {
                DefineConfigs();
                Debug.Log($"[KerbonautRedux] Reloaded {configs.Count} built-in configs");
            }
        }
    }
}
