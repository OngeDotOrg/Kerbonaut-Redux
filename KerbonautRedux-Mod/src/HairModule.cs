using System.Collections.Generic;
using UnityEngine;

namespace KerbonautRedux
{

    public class HairModule : MonoBehaviour
    {
        private bool isApplied;
        private List<GameObject> hairObjects = new List<GameObject>();
        private List<SkinnedMeshRenderer> hairRenderers = new List<SkinnedMeshRenderer>();
        private HairConfig currentConfig;
        private Kerbal kerbal;

        private List<Vector3> currentPositions = new List<Vector3>();
        private List<Vector3> currentRotations = new List<Vector3>();
        private List<float> currentScales = new List<float>();

        public void Start()
        {
            ApplyHair();

            KerbonautReduxAddon.RegisterHairModule(this);
        }

        private void ApplyHair()
        {
            if (isApplied) return;

            kerbal = GetComponent<Kerbal>();
            if (kerbal == null || kerbal.protoCrewMember == null)
            {
                Debug.LogWarning("[KerbonautRedux] No kerbal found on this GameObject");
                return;
            }

            string kerbalName = kerbal.protoCrewMember.name;
            Debug.Log($"[KerbonautRedux] ApplyHair called for: {kerbalName}");

            if (HairConfigs.Instance == null)
            {
                Debug.LogError("[KerbonautRedux] HairConfigs.Instance is null!");
                return;
            }

            currentConfig = HairConfigs.Instance.GetConfig(kerbalName);
            if (currentConfig == null)
            {
                Debug.Log($"[KerbonautRedux] No hair config found for '{kerbalName}'");
                return;
            }

            int pieceCount = currentConfig.HairPieces.Count > 0 ? currentConfig.HairPieces.Count : 1;
            Debug.Log($"[KerbonautRedux] Found config for {kerbalName} with {pieceCount} hair piece(s)");

            ApplyKerbalTexture(currentConfig);

            HideOriginalMeshesPonyStyle(currentConfig);

            int layer = GetKerbalLayer();

            if (currentConfig.HairPieces.Count > 0)
            {

                for (int i = 0; i < currentConfig.HairPieces.Count; i++)
                {
                    ApplyHairPiece(kerbal, currentConfig.HairPieces[i], layer, i);
                }
            }
            else
            {

                var piece = new HairPiece
                {
                    MeshName = currentConfig.HairMeshName,
                    TexturePath = currentConfig.HairTexturePath,
                    Color = currentConfig.HairColor,
                    PositionOffset = currentConfig.PositionOffset,
                    RotationOffset = currentConfig.RotationOffset,
                    Scale = currentConfig.Scale,
                    BoneName = currentConfig.BoneName,
                    Shader = currentConfig.Shader
                };
                ApplyHairPiece(kerbal, piece, layer, 0);
            }

            isApplied = true;
            Debug.Log($"[KerbonautRedux] SUCCESS: Applied {hairObjects.Count} hair piece(s) to {kerbalName}");
        }

        private void ApplyKerbalTexture(HairConfig config)
        {
            if (kerbal == null || kerbal.protoCrewMember == null) return;

            bool hasPerPartTextures = !string.IsNullOrEmpty(config.bodyTexHead) ||
                                      !string.IsNullOrEmpty(config.bodyTexBody) ||
                                      !string.IsNullOrEmpty(config.bodyTexArms) ||
                                      !string.IsNullOrEmpty(config.bodyTexLegs) ||
                                      !string.IsNullOrEmpty(config.bodyTexHelmet);

            if (hasPerPartTextures)
            {
                ApplyPerPartBodyTextures(config);
            }
            else
            {

                ApplyLegacyBodyTexture(config);
            }
        }

        private void ApplyPerPartBodyTextures(HairConfig config)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {

                if (renderer.name.StartsWith("CustomHair_")) continue;
                if (!(renderer is SkinnedMeshRenderer smr)) continue;

                string nameLower = renderer.name.ToLower();
                BodyPartType partType = ClassifyBodyPart(nameLower);

                switch (partType)
                {
                    case BodyPartType.Head:
                        ApplyTextureToRenderer(renderer, config.bodyTexHead, config.bodyTexHeadNormal);
                        break;
                    case BodyPartType.Body:
                        ApplyTextureToRenderer(renderer, config.bodyTexBody, config.bodyTexBodyNormal);
                        break;
                    case BodyPartType.Arms:
                        ApplyTextureToRenderer(renderer, config.bodyTexArms, config.bodyTexArmsNormal);
                        break;
                    case BodyPartType.Legs:
                        ApplyTextureToRenderer(renderer, config.bodyTexLegs, config.bodyTexLegsNormal);
                        break;
                    case BodyPartType.Helmet:
                        ApplyTextureToRenderer(renderer, config.bodyTexHelmet, config.bodyTexHelmetNormal);
                        break;
                    case BodyPartType.Eyes:
                        ApplyTextureToRenderer(renderer, config.bodyTexEyes, config.bodyTexEyesNormal);
                        break;
                }
            }
        }

        private BodyPartType ClassifyBodyPart(string nameLower)
        {

            if (nameLower.Contains("helmet"))
                return BodyPartType.Helmet;

            if (nameLower == "headmesh01" ||
                nameLower == "headmesh" ||
                nameLower == "mesh_female_kerbalastronaut01_kerbalgirl_mesh_polysurface51" ||
                nameLower.Contains("headpivot") ||
                nameLower.Contains("_head_") ||
                (nameLower.Contains("head") && !nameLower.Contains("eye") &&
                 !nameLower.Contains("teeth") && !nameLower.Contains("tongue")))
                return BodyPartType.Head;

            if (nameLower.Contains("eyeball") || nameLower.Contains("pupil") ||
                nameLower.Contains("tear") || nameLower.Contains("brow") ||
                nameLower.Contains("lash") || nameLower.Contains("lid"))
                return BodyPartType.Eyes;

            if (nameLower.Contains("teeth") || nameLower.Contains("tongue") ||
                nameLower.Contains("mouth") || nameLower.Contains("tooth"))
                return BodyPartType.Unknown;

            if (nameLower.Contains("arm") || nameLower.Contains("hand") ||
                nameLower.Contains("wrist") || nameLower.Contains("elbow") ||
                nameLower.Contains("shoulder") || nameLower.Contains("clavicle") ||
                nameLower.Contains("thumb") || nameLower.Contains("finger") ||
                nameLower.Contains("index") || nameLower.Contains("pinky") ||
                nameLower.Contains("palm"))
                return BodyPartType.Arms;

            if (nameLower.Contains("leg") || nameLower.Contains("foot") ||
                nameLower.Contains("toe") || nameLower.Contains("knee") ||
                nameLower.Contains("hip") || nameLower.Contains("ankle") ||
                nameLower.Contains("thigh") || nameLower.Contains("calf") ||
                nameLower.Contains("boot"))
                return BodyPartType.Legs;

            if (nameLower.Contains("body") || nameLower.Contains("torso") ||
                nameLower.Contains("chest") || nameLower.Contains("back") ||
                nameLower.Contains("stomach") || nameLower.Contains("abdomen") ||
                nameLower.Contains("pelvis") || nameLower.Contains("spine") ||
                nameLower.Contains("suit") || nameLower.Contains("mesh"))
                return BodyPartType.Body;

            return BodyPartType.Unknown;
        }

        private void ApplyTextureToRenderer(Renderer renderer, string diffusePath, string normalPath)
        {
            if (!string.IsNullOrEmpty(diffusePath))
            {
                Texture2D diffuse = HairConfigs.Instance.GetTexture(diffusePath);
                if (diffuse != null)
                {
                    renderer.material.mainTexture = diffuse;
                    Debug.Log($"[KerbonautRedux] Applied diffuse texture to {renderer.name}: {diffusePath}");
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] Could not find diffuse texture: {diffusePath}");
                }
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                Texture2D normal = HairConfigs.Instance.GetTexture(normalPath);
                if (normal != null)
                {
                    renderer.material.SetTexture("_BumpMap", normal);
                    Debug.Log($"[KerbonautRedux] Applied normal map to {renderer.name}: {normalPath}");
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] Could not find normal map: {normalPath}");
                }
            }
        }

        private void ApplyLegacyBodyTexture(HairConfig config)
        {

            if (!string.IsNullOrEmpty(config.KerbalTexture))
            {
                Texture2D bodyTexture = HairConfigs.Instance.GetTexture(config.KerbalTexture);
                if (bodyTexture != null)
                {
                    Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.name.StartsWith("CustomHair_")) continue;

                        if (renderer is SkinnedMeshRenderer smr)
                        {
                            string name = smr.name.ToLower();
                            if (name.Contains("body") || name.Contains("suit") ||
                                name.Contains("mesh") || name.Contains("helmet"))
                            {
                                renderer.material.mainTexture = bodyTexture;
                                Debug.Log($"[KerbonautRedux] Applied legacy body texture to {smr.name}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] Could not find kerbal texture: {config.KerbalTexture}");
                }
            }

            if (!string.IsNullOrEmpty(config.KerbalNormalMap))
            {
                Texture2D normalMap = HairConfigs.Instance.GetTexture(config.KerbalNormalMap);
                if (normalMap != null)
                {
                    Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.name.StartsWith("CustomHair_")) continue;

                        if (renderer is SkinnedMeshRenderer smr)
                        {
                            string name = smr.name.ToLower();
                            if (name.Contains("body") || name.Contains("suit") ||
                                name.Contains("mesh") || name.Contains("helmet"))
                            {
                                renderer.material.SetTexture("_BumpMap", normalMap);
                                Debug.Log($"[KerbonautRedux] Applied legacy body normal map to {smr.name}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] Could not find kerbal normal map: {config.KerbalNormalMap}");
                }
            }
        }

        private enum BodyPartType
        {
            Unknown,
            Head,
            Body,
            Arms,
            Legs,
            Helmet,
            Eyes
        }

        private void ApplyHairPiece(Kerbal kerbal, HairPiece piece, int layer, int index)
        {
            string kerbalName = kerbal.protoCrewMember.name;
            Debug.Log($"[KerbonautRedux] ApplyHairPiece {index}: Loading mesh '{piece.MeshName}' for {kerbalName}");

            Mesh hairMesh = HairConfigs.Instance.GetMesh(piece.MeshName);
            if (hairMesh == null)
            {
                Debug.LogError($"[KerbonautRedux] ApplyHairPiece {index}: MESH NOT FOUND - '{piece.MeshName}'");
                return;
            }

            Transform attachBone = FindBone(piece.BoneName);
            if (attachBone == null)
            {
                Debug.LogError($"[KerbonautRedux] Bone not found: {piece.BoneName}");
                return;
            }

            GameObject hairObj = new GameObject($"CustomHair_{kerbalName}_{index}");
            hairObj.transform.parent = transform;
            hairObj.layer = layer;

            SkinnedMeshRenderer renderer = hairObj.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = hairMesh;
            renderer.bones = new Transform[] { attachBone };

            string normalMapPath = piece.NormalMapPath ?? piece.BumpTexturePath;

            Material mat = HairConfigs.Instance.CreateMaterial(
                piece.TexturePath,
                normalMapPath,
                piece.Color,
                piece.Shader);
            renderer.material = mat;

            SetupBoneWeights(hairMesh);

            hairObj.transform.localPosition = piece.PositionOffset;
            hairObj.transform.localEulerAngles = piece.RotationOffset;
            hairObj.transform.localScale = Vector3.one * piece.Scale;

            currentPositions.Add(piece.PositionOffset);
            currentRotations.Add(piece.RotationOffset);
            currentScales.Add(piece.Scale);

            HairVisibilityChecker checker = hairObj.AddComponent<HairVisibilityChecker>();
            checker.Initialize(kerbal, renderer);

            hairObjects.Add(hairObj);
            hairRenderers.Add(renderer);

            Debug.Log($"[KerbonautRedux] Applied hair piece {index}: {piece.MeshName} at {piece.PositionOffset} with shader {piece.Shader}");
        }

        private Transform FindBone(string boneName)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (t.name == boneName)
                    return t;
            }
            return null;
        }

        private void HideOriginalMeshesPonyStyle(HairConfig config)
        {

            if (!config.HideHead && !config.HidePonytail && !config.HideEyes && !config.HideTeeth && !config.HideTongue)
                return;

            Debug.Log($"[KerbonautRedux] === HIDE MESHES (PONY STYLE) ===");
            Debug.Log($"[KerbonautRedux] HideHead={config.HideHead}, HidePonytail={config.HidePonytail}, HideEyes={config.HideEyes}, HideTeeth={config.HideTeeth}, HideTongue={config.HideTongue}");

            Mesh emptyMesh = new Mesh();

            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Debug.Log($"[KerbonautRedux] Found {renderers.Length} SkinnedMeshRenderers");

            int hiddenCount = 0;

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                string name = renderer.name;
                bool shouldHide = false;

                switch (name)
                {
                    case "headMesh01":
                    case "headMesh":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51":
                        shouldHide = config.HideHead;
                        break;

                    case "ponytail":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
                        shouldHide = config.HidePonytail;
                        break;

                    case "eyeballLeft":
                    case "eyeballRight":
                    case "pupilLeft":
                    case "pupilRight":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight":
                        shouldHide = config.HideEyes;
                        break;

                    case "upTeeth01":
                    case "upTeeth02":
                    case "downTeeth01":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01":
                        shouldHide = config.HideTeeth;
                        break;

                    case "tongue":
                        shouldHide = config.HideTongue;
                        break;
                }

                if (shouldHide)
                {
                    Debug.Log($"[KerbonautRedux] HIDING mesh: '{name}'");
                    renderer.sharedMesh = emptyMesh;
                    hiddenCount++;
                }
            }

            Debug.Log($"[KerbonautRedux] Hidden {hiddenCount} meshes");
            Debug.Log($"[KerbonautRedux] === END HIDE MESHES ===");
        }

        private int GetKerbalLayer()
        {

            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer r in renderers)
            {
                return r.gameObject.layer;
            }
            return gameObject.layer;
        }

        private void SetupBoneWeights(Mesh mesh)
        {

            BoneWeight[] weights = new BoneWeight[mesh.vertexCount];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i].boneIndex0 = 0;
                weights[i].weight0 = 1f;
            }
            mesh.boneWeights = weights;

            mesh.bindposes = new Matrix4x4[] { Matrix4x4.identity };
        }

        private void OnDestroy()
        {

            KerbonautReduxAddon.UnregisterHairModule(this);

            foreach (var hairObj in hairObjects)
            {
                if (hairObj != null)
                    Destroy(hairObj);
            }
            hairObjects.Clear();
            hairRenderers.Clear();
            currentPositions.Clear();
            currentRotations.Clear();
            currentScales.Clear();
        }

        public void ReapplyHair()
        {
            if (isApplied && hairObjects.Count > 0)
            {

                foreach (var hairObj in hairObjects)
                {
                    if (hairObj != null)
                        Destroy(hairObj);
                }
                hairObjects.Clear();
                hairRenderers.Clear();
                currentPositions.Clear();
                currentRotations.Clear();
                currentScales.Clear();
                isApplied = false;
                currentConfig = null;

                ApplyHair();
                Debug.Log("[KerbonautRedux] Hair reapplied after hot-reload");
            }
        }

        #region Runtime Modification Methods

        public int GetHairPieceCount()
        {
            return hairObjects.Count;
        }

        public void SetHairPiecePosition(int index, Vector3 newPosition)
        {
            if (index < 0 || index >= hairObjects.Count) return;
            if (hairObjects[index] == null) return;

            hairObjects[index].transform.localPosition = newPosition;
            currentPositions[index] = newPosition;
            Debug.Log($"[KerbonautRedux] Hair piece {index} position updated to {newPosition}");
        }

        public void SetHairPieceRotation(int index, Vector3 newRotation)
        {
            if (index < 0 || index >= hairObjects.Count) return;
            if (hairObjects[index] == null) return;

            hairObjects[index].transform.localEulerAngles = newRotation;
            currentRotations[index] = newRotation;
            Debug.Log($"[KerbonautRedux] Hair piece {index} rotation updated to {newRotation}");
        }

        public void SetHairPieceScale(int index, float newScale)
        {
            if (index < 0 || index >= hairObjects.Count) return;
            if (hairObjects[index] == null) return;

            hairObjects[index].transform.localScale = Vector3.one * newScale;
            currentScales[index] = newScale;
            Debug.Log($"[KerbonautRedux] Hair piece {index} scale updated to {newScale}");
        }

        public bool GetHairPieceTransform(int index, out Vector3 position, out Vector3 rotation, out float scale)
        {
            if (index < 0 || index >= hairObjects.Count || hairObjects[index] == null)
            {
                position = Vector3.zero;
                rotation = Vector3.zero;
                scale = 1f;
                return false;
            }

            Transform t = hairObjects[index].transform;
            position = t.localPosition;
            rotation = t.localEulerAngles;
            scale = t.localScale.x;
            return true;
        }

        public void SetHairPieceColor(int index, Color newColor)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            hairRenderers[index].material.color = newColor;
            Debug.Log($"[KerbonautRedux] Hair piece {index} color updated to {newColor}");
        }

        public void SetHairPieceTexture(int index, Texture2D newTexture)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            if (newTexture != null)
            {
                hairRenderers[index].material.mainTexture = newTexture;
                Debug.Log($"[KerbonautRedux] Hair piece {index} texture updated to {newTexture.name}");
            }
        }

        public void SetHairPieceNormalMap(int index, Texture2D newNormalMap)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            if (newNormalMap != null)
            {
                hairRenderers[index].material.SetTexture("_BumpMap", newNormalMap);
                Debug.Log($"[KerbonautRedux] Hair piece {index} normal map updated to {newNormalMap.name}");
            }
        }

        public void SetHairPieceShader(int index, string shaderName)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {

                Material oldMat = hairRenderers[index].material;
                Texture mainTex = oldMat.mainTexture;
                Texture bumpMap = oldMat.GetTexture("_BumpMap");
                Color color = oldMat.color;

                Material newMat = new Material(shader);
                if (mainTex != null) newMat.mainTexture = mainTex;
                if (bumpMap != null) newMat.SetTexture("_BumpMap", bumpMap);
                newMat.color = color;

                hairRenderers[index].material = newMat;
                Debug.Log($"[KerbonautRedux] Hair piece {index} shader changed to {shaderName}");
            }
            else
            {
                Debug.LogWarning($"[KerbonautRedux] Shader '{shaderName}' not found");
            }
        }

        public void SetHairPieceVisible(int index, bool visible)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            hairRenderers[index].enabled = visible;
        }

        public string GetKerbalName()
        {
            return kerbal?.protoCrewMember?.name;
        }

        #endregion
    }

    public class EvaHairModule : PartModule
    {
        private bool isApplied;
        private List<GameObject> hairObjects = new List<GameObject>();
        private List<SkinnedMeshRenderer> hairRenderers = new List<SkinnedMeshRenderer>();
        private HairConfig currentConfig;
        private object lastSuitType = null;
        private int suitCheckFrameCounter = 0;
        private const int SUIT_CHECK_INTERVAL = 30;

        private List<Vector3> currentPositions = new List<Vector3>();
        private List<Vector3> currentRotations = new List<Vector3>();
        private List<float> currentScales = new List<float>();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!isApplied)
            {
                ApplyHair();
            }

            KerbonautReduxAddon.RegisterEvaHairModule(this);
        }

        public void FixedUpdate()
        {

            suitCheckFrameCounter++;
            if (suitCheckFrameCounter >= SUIT_CHECK_INTERVAL)
            {
                suitCheckFrameCounter = 0;
                CheckSuitChange();
            }
        }

        private object GetSuitType(ProtoCrewMember crew)
        {
            if (crew == null) return null;

            System.Type type = crew.GetType();

            System.Reflection.PropertyInfo prop = type.GetProperty("suitType");
            if (prop != null) return prop.GetValue(crew, null);

            prop = type.GetProperty("SuitType");
            if (prop != null) return prop.GetValue(crew, null);

            prop = type.GetProperty("suit");
            if (prop != null) return prop.GetValue(crew, null);

            prop = type.GetProperty("Suit");
            if (prop != null) return prop.GetValue(crew, null);

            prop = type.GetProperty("KerbalSuit");
            if (prop != null) return prop.GetValue(crew, null);

            prop = type.GetProperty("kerbalSuit");
            if (prop != null) return prop.GetValue(crew, null);

            return null;
        }

        private void CheckSuitChange()
        {
            if (part == null || part.protoModuleCrew == null || part.protoModuleCrew.Count == 0)
                return;

            ProtoCrewMember crew = part.protoModuleCrew[0];
            object currentSuitType = GetSuitType(crew);

            if (!isApplied)
            {
                lastSuitType = currentSuitType;
                return;
            }

            if (currentSuitType != null && !currentSuitType.Equals(lastSuitType))
            {
                Debug.Log($"[KerbonautRedux] Suit change detected for {crew.name}: {lastSuitType} -> {currentSuitType}");

                lastSuitType = currentSuitType;

                foreach (var hairObj in hairObjects)
                {
                    if (hairObj != null)
                        Destroy(hairObj);
                }
                hairObjects.Clear();
                hairRenderers.Clear();
                currentPositions.Clear();
                currentRotations.Clear();
                currentScales.Clear();
                isApplied = false;

                ApplyHair();
            }
        }

        private void ApplyHair()
        {
            if (part == null || part.protoModuleCrew == null || part.protoModuleCrew.Count == 0)
                return;

            ProtoCrewMember crew = part.protoModuleCrew[0];
            string kerbalName = crew.name;

            lastSuitType = GetSuitType(crew);

            currentConfig = HairConfigs.Instance?.GetConfig(kerbalName);
            if (currentConfig == null)
            {
                Debug.Log($"[KerbonautRedux] EVA: No config found for {kerbalName}");
                return;
            }

            Debug.Log($"[KerbonautRedux] EVA: Found config for {kerbalName}, HairPieces.Count = {currentConfig.HairPieces.Count}");

            ApplyKerbalTexture(currentConfig);

            HideOriginalMeshesEvaPonyStyle(currentConfig);

            if (currentConfig.HairPieces.Count > 0)
            {

                for (int i = 0; i < currentConfig.HairPieces.Count; i++)
                {
                    ApplyEvaHairPiece(crew, currentConfig.HairPieces[i], i);
                }
            }
            else
            {

                var piece = new HairPiece
                {
                    MeshName = currentConfig.HairMeshName,
                    TexturePath = currentConfig.HairTexturePath,
                    Color = currentConfig.HairColor,
                    PositionOffset = currentConfig.PositionOffset,
                    RotationOffset = currentConfig.RotationOffset,
                    Scale = currentConfig.Scale,
                    BoneName = currentConfig.BoneName,
                    Shader = currentConfig.Shader
                };
                ApplyEvaHairPiece(crew, piece, 0);
            }

            isApplied = true;
            Debug.Log($"[KerbonautRedux] Applied {hairObjects.Count} EVA hair piece(s) to {kerbalName}");
        }

        private void ApplyKerbalTexture(HairConfig config)
        {
            if (part == null) return;

            bool hasPerPartTextures = !string.IsNullOrEmpty(config.bodyTexHead) ||
                                      !string.IsNullOrEmpty(config.bodyTexBody) ||
                                      !string.IsNullOrEmpty(config.bodyTexArms) ||
                                      !string.IsNullOrEmpty(config.bodyTexLegs) ||
                                      !string.IsNullOrEmpty(config.bodyTexHelmet);

            if (hasPerPartTextures)
            {
                ApplyPerPartBodyTextures(config);
            }
            else
            {

                ApplyLegacyBodyTexture(config);
            }
        }

        private void ApplyPerPartBodyTextures(HairConfig config)
        {
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {

                if (renderer.name.StartsWith("CustomHair_")) continue;
                if (!(renderer is SkinnedMeshRenderer smr)) continue;

                string nameLower = renderer.name.ToLower();
                EvaBodyPartType partType = ClassifyEvaBodyPart(nameLower);

                switch (partType)
                {
                    case EvaBodyPartType.Head:
                        ApplyTextureToRenderer(renderer, config.bodyTexHead, config.bodyTexHeadNormal);
                        break;
                    case EvaBodyPartType.Body:
                        ApplyTextureToRenderer(renderer, config.bodyTexBody, config.bodyTexBodyNormal);
                        break;
                    case EvaBodyPartType.Arms:
                        ApplyTextureToRenderer(renderer, config.bodyTexArms, config.bodyTexArmsNormal);
                        break;
                    case EvaBodyPartType.Legs:
                        ApplyTextureToRenderer(renderer, config.bodyTexLegs, config.bodyTexLegsNormal);
                        break;
                    case EvaBodyPartType.Helmet:
                        ApplyTextureToRenderer(renderer, config.bodyTexHelmet, config.bodyTexHelmetNormal);
                        break;
                    case EvaBodyPartType.Eyes:
                        ApplyTextureToRenderer(renderer, config.bodyTexEyes, config.bodyTexEyesNormal);
                        break;
                }
            }
        }

        private EvaBodyPartType ClassifyEvaBodyPart(string nameLower)
        {

            if (nameLower.Contains("helmet"))
                return EvaBodyPartType.Helmet;

            if (nameLower == "headmesh01" ||
                nameLower == "headmesh" ||
                nameLower == "mesh_female_kerbalastronaut01_kerbalgirl_mesh_polysurface51" ||
                nameLower.Contains("headpivot") ||
                nameLower.Contains("_head_") ||
                (nameLower.Contains("head") && !nameLower.Contains("eye") &&
                 !nameLower.Contains("teeth") && !nameLower.Contains("tongue")))
                return EvaBodyPartType.Head;

            if (nameLower.Contains("eyeball") || nameLower.Contains("pupil") ||
                nameLower.Contains("tear") || nameLower.Contains("brow") ||
                nameLower.Contains("lash") || nameLower.Contains("lid"))
                return EvaBodyPartType.Eyes;

            if (nameLower.Contains("teeth") || nameLower.Contains("tongue") ||
                nameLower.Contains("mouth") || nameLower.Contains("tooth"))
                return EvaBodyPartType.Unknown;

            if (nameLower.Contains("arm") || nameLower.Contains("hand") ||
                nameLower.Contains("wrist") || nameLower.Contains("elbow") ||
                nameLower.Contains("shoulder") || nameLower.Contains("clavicle") ||
                nameLower.Contains("thumb") || nameLower.Contains("finger") ||
                nameLower.Contains("index") || nameLower.Contains("pinky") ||
                nameLower.Contains("palm"))
                return EvaBodyPartType.Arms;

            if (nameLower.Contains("leg") || nameLower.Contains("foot") ||
                nameLower.Contains("toe") || nameLower.Contains("knee") ||
                nameLower.Contains("hip") || nameLower.Contains("ankle") ||
                nameLower.Contains("thigh") || nameLower.Contains("calf") ||
                nameLower.Contains("boot"))
                return EvaBodyPartType.Legs;

            if (nameLower.Contains("body") || nameLower.Contains("torso") ||
                nameLower.Contains("chest") || nameLower.Contains("back") ||
                nameLower.Contains("stomach") || nameLower.Contains("abdomen") ||
                nameLower.Contains("pelvis") || nameLower.Contains("spine") ||
                nameLower.Contains("suit") || nameLower.Contains("mesh"))
                return EvaBodyPartType.Body;

            return EvaBodyPartType.Unknown;
        }

        private void ApplyTextureToRenderer(Renderer renderer, string diffusePath, string normalPath)
        {
            if (!string.IsNullOrEmpty(diffusePath))
            {
                Texture2D diffuse = HairConfigs.Instance.GetTexture(diffusePath);
                if (diffuse != null)
                {
                    renderer.material.mainTexture = diffuse;
                    Debug.Log($"[KerbonautRedux] EVA: Applied diffuse texture to {renderer.name}: {diffusePath}");
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] EVA: Could not find diffuse texture: {diffusePath}");
                }
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                Texture2D normal = HairConfigs.Instance.GetTexture(normalPath);
                if (normal != null)
                {
                    renderer.material.SetTexture("_BumpMap", normal);
                    Debug.Log($"[KerbonautRedux] EVA: Applied normal map to {renderer.name}: {normalPath}");
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] EVA: Could not find normal map: {normalPath}");
                }
            }
        }

        private void ApplyLegacyBodyTexture(HairConfig config)
        {

            if (!string.IsNullOrEmpty(config.KerbalTexture))
            {
                Texture2D bodyTexture = HairConfigs.Instance.GetTexture(config.KerbalTexture);
                if (bodyTexture != null)
                {
                    Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.name.StartsWith("CustomHair_")) continue;

                        if (renderer is SkinnedMeshRenderer smr)
                        {
                            string name = smr.name.ToLower();
                            if (name.Contains("body") || name.Contains("suit") ||
                                name.Contains("mesh") || name.Contains("helmet"))
                            {
                                renderer.material.mainTexture = bodyTexture;
                                Debug.Log($"[KerbonautRedux] EVA: Applied legacy body texture to {smr.name}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] EVA: Could not find kerbal texture: {config.KerbalTexture}");
                }
            }

            if (!string.IsNullOrEmpty(config.KerbalNormalMap))
            {
                Texture2D normalMap = HairConfigs.Instance.GetTexture(config.KerbalNormalMap);
                if (normalMap != null)
                {
                    Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.name.StartsWith("CustomHair_")) continue;

                        if (renderer is SkinnedMeshRenderer smr)
                        {
                            string name = smr.name.ToLower();
                            if (name.Contains("body") || name.Contains("suit") ||
                                name.Contains("mesh") || name.Contains("helmet"))
                            {
                                renderer.material.SetTexture("_BumpMap", normalMap);
                                Debug.Log($"[KerbonautRedux] EVA: Applied legacy body normal map to {smr.name}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] EVA: Could not find kerbal normal map: {config.KerbalNormalMap}");
                }
            }
        }

        private enum EvaBodyPartType
        {
            Unknown,
            Head,
            Body,
            Arms,
            Legs,
            Helmet,
            Eyes
        }

        private void ApplyEvaHairPiece(ProtoCrewMember crew, HairPiece piece, int index)
        {
            Debug.Log($"[KerbonautRedux] ApplyEvaHairPiece {index}: Attempting to load mesh '{piece.MeshName}'");

            Mesh hairMesh = HairConfigs.Instance.GetMesh(piece.MeshName);
            if (hairMesh == null)
            {
                Debug.LogError($"[KerbonautRedux] ApplyEvaHairPiece {index}: MESH NOT FOUND - '{piece.MeshName}'");
                return;
            }
            Debug.Log($"[KerbonautRedux] ApplyEvaHairPiece {index}: Mesh '{piece.MeshName}' loaded successfully");

            Transform attachBone = FindBone(piece.BoneName);
            if (attachBone == null)
                return;

            GameObject hairObj = new GameObject($"CustomHair_{crew.name}_{index}");
            hairObj.transform.parent = part.transform;

            SkinnedMeshRenderer renderer = hairObj.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = hairMesh;
            renderer.bones = new Transform[] { attachBone };

            string normalMapPath = piece.NormalMapPath ?? piece.BumpTexturePath;

            Material mat = HairConfigs.Instance.CreateMaterial(
                piece.TexturePath,
                normalMapPath,
                piece.Color,
                piece.Shader);
            renderer.material = mat;

            SetupBoneWeights(hairMesh);

            hairObj.transform.localPosition = piece.PositionOffset;
            hairObj.transform.localEulerAngles = piece.RotationOffset;
            hairObj.transform.localScale = Vector3.one * piece.Scale;

            currentPositions.Add(piece.PositionOffset);
            currentRotations.Add(piece.RotationOffset);
            currentScales.Add(piece.Scale);

            HairVisibilityChecker checker = hairObj.AddComponent<HairVisibilityChecker>();
            Kerbal evaKerbal = part.GetComponent<Kerbal>();
            if (evaKerbal != null)
                checker.Initialize(evaKerbal, renderer);

            hairObjects.Add(hairObj);
            hairRenderers.Add(renderer);

            Debug.Log($"[KerbonautRedux] EVA: Applied hair piece {index} with shader {piece.Shader}");
        }

        private Transform FindBone(string boneName)
        {
            Transform[] allTransforms = part.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (t.name == boneName)
                    return t;
            }
            return null;
        }

        private void HideOriginalMeshesEvaPonyStyle(HairConfig config)
        {
            if (!config.HideHead && !config.HidePonytail && !config.HideEyes && !config.HideTeeth && !config.HideTongue)
                return;

            Debug.Log($"[KerbonautRedux] === EVA HIDE MESHES ===");

            Mesh emptyMesh = new Mesh();
            SkinnedMeshRenderer[] renderers = part.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int hiddenCount = 0;

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                string name = renderer.name;
                bool shouldHide = false;

                switch (name)
                {
                    case "headMesh01":
                    case "headMesh":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51":
                        shouldHide = config.HideHead;
                        break;
                    case "ponytail":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
                        shouldHide = config.HidePonytail;
                        break;
                    case "eyeballLeft":
                    case "eyeballRight":
                    case "pupilLeft":
                    case "pupilRight":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight":
                        shouldHide = config.HideEyes;
                        break;
                    case "upTeeth01":
                    case "upTeeth02":
                    case "downTeeth01":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01":
                        shouldHide = config.HideTeeth;
                        break;
                    case "tongue":
                        shouldHide = config.HideTongue;
                        break;
                }

                if (shouldHide)
                {
                    Debug.Log($"[KerbonautRedux] HIDING EVA mesh: '{name}'");
                    renderer.sharedMesh = emptyMesh;
                    hiddenCount++;
                }
            }

            Debug.Log($"[KerbonautRedux] Hidden {hiddenCount} EVA meshes");
            Debug.Log($"[KerbonautRedux] === END EVA HIDE MESHES ===");
        }

        private void SetupBoneWeights(Mesh mesh)
        {
            BoneWeight[] weights = new BoneWeight[mesh.vertexCount];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i].boneIndex0 = 0;
                weights[i].weight0 = 1f;
            }
            mesh.boneWeights = weights;
            mesh.bindposes = new Matrix4x4[] { Matrix4x4.identity };
        }

        public void ReapplyHair()
        {
            if (isApplied && hairObjects.Count > 0)
            {

                foreach (var hairObj in hairObjects)
                {
                    if (hairObj != null)
                        Destroy(hairObj);
                }
                hairObjects.Clear();
                hairRenderers.Clear();
                currentPositions.Clear();
                currentRotations.Clear();
                currentScales.Clear();
                isApplied = false;
                currentConfig = null;

                ApplyHair();
                Debug.Log("[KerbonautRedux] EVA hair reapplied after hot-reload");
            }
        }

        private void OnDestroy()
        {

            KerbonautReduxAddon.UnregisterEvaHairModule(this);

            foreach (var hairObj in hairObjects)
            {
                if (hairObj != null)
                    Destroy(hairObj);
            }
            hairObjects.Clear();
            hairRenderers.Clear();
            currentPositions.Clear();
            currentRotations.Clear();
            currentScales.Clear();
        }

        #region Runtime Modification Methods

        public int GetHairPieceCount()
        {
            return hairObjects.Count;
        }

        public void SetHairPiecePosition(int index, Vector3 newPosition)
        {
            if (index < 0 || index >= hairObjects.Count) return;
            if (hairObjects[index] == null) return;

            hairObjects[index].transform.localPosition = newPosition;
            currentPositions[index] = newPosition;
            Debug.Log($"[KerbonautRedux] EVA: Hair piece {index} position updated to {newPosition}");
        }

        public void SetHairPieceRotation(int index, Vector3 newRotation)
        {
            if (index < 0 || index >= hairObjects.Count) return;
            if (hairObjects[index] == null) return;

            hairObjects[index].transform.localEulerAngles = newRotation;
            currentRotations[index] = newRotation;
            Debug.Log($"[KerbonautRedux] EVA: Hair piece {index} rotation updated to {newRotation}");
        }

        public void SetHairPieceScale(int index, float newScale)
        {
            if (index < 0 || index >= hairObjects.Count) return;
            if (hairObjects[index] == null) return;

            hairObjects[index].transform.localScale = Vector3.one * newScale;
            currentScales[index] = newScale;
            Debug.Log($"[KerbonautRedux] EVA: Hair piece {index} scale updated to {newScale}");
        }

        public bool GetHairPieceTransform(int index, out Vector3 position, out Vector3 rotation, out float scale)
        {
            if (index < 0 || index >= hairObjects.Count || hairObjects[index] == null)
            {
                position = Vector3.zero;
                rotation = Vector3.zero;
                scale = 1f;
                return false;
            }

            Transform t = hairObjects[index].transform;
            position = t.localPosition;
            rotation = t.localEulerAngles;
            scale = t.localScale.x;
            return true;
        }

        public void SetHairPieceColor(int index, Color newColor)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            hairRenderers[index].material.color = newColor;
            Debug.Log($"[KerbonautRedux] EVA: Hair piece {index} color updated to {newColor}");
        }

        public void SetHairPieceTexture(int index, Texture2D newTexture)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            if (newTexture != null)
            {
                hairRenderers[index].material.mainTexture = newTexture;
                Debug.Log($"[KerbonautRedux] EVA: Hair piece {index} texture updated to {newTexture.name}");
            }
        }

        public void SetHairPieceNormalMap(int index, Texture2D newNormalMap)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            if (newNormalMap != null)
            {
                hairRenderers[index].material.SetTexture("_BumpMap", newNormalMap);
                Debug.Log($"[KerbonautRedux] EVA: Hair piece {index} normal map updated to {newNormalMap.name}");
            }
        }

        public void SetHairPieceShader(int index, string shaderName)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {

                Material oldMat = hairRenderers[index].material;
                Texture mainTex = oldMat.mainTexture;
                Texture bumpMap = oldMat.GetTexture("_BumpMap");
                Color color = oldMat.color;

                Material newMat = new Material(shader);
                if (mainTex != null) newMat.mainTexture = mainTex;
                if (bumpMap != null) newMat.SetTexture("_BumpMap", bumpMap);
                newMat.color = color;

                hairRenderers[index].material = newMat;
                Debug.Log($"[KerbonautRedux] EVA: Hair piece {index} shader changed to {shaderName}");
            }
            else
            {
                Debug.LogWarning($"[KerbonautRedux] Shader '{shaderName}' not found");
            }
        }

        public void SetHairPieceVisible(int index, bool visible)
        {
            if (index < 0 || index >= hairRenderers.Count) return;
            if (hairRenderers[index] == null) return;

            hairRenderers[index].enabled = visible;
        }

        public string GetKerbalName()
        {
            return part?.protoModuleCrew?[0]?.name;
        }

        #endregion
    }
}
