using System.Collections.Generic;
using UnityEngine;

namespace KerbonautRedux
{
    public class HairModule : MonoBehaviour
    {
        private bool isApplied;
        private List<GameObject> hairObjects = new List<GameObject>();
        private List<SkinnedMeshRenderer> hairRenderers = new List<SkinnedMeshRenderer>();

        public void Start()
        {
            ApplyHair();
        }

        private void ApplyHair()
        {
            if (isApplied) return;


            Kerbal kerbal = GetComponent<Kerbal>();
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
            

            HairConfig config = HairConfigs.Instance.GetConfig(kerbalName);
            if (config == null)
            {
                Debug.Log($"[KerbonautRedux] No hair config found for '{kerbalName}'");
                return;
            }
            
            int pieceCount = config.HairPieces.Count > 0 ? config.HairPieces.Count : 1;
            Debug.Log($"[KerbonautRedux] Found config for {kerbalName} with {pieceCount} hair piece(s)");


            HideOriginalMeshesPonyStyle(config);


            int layer = GetKerbalLayer();


            if (config.HairPieces.Count > 0)
            {

                for (int i = 0; i < config.HairPieces.Count; i++)
                {
                    ApplyHairPiece(kerbal, config.HairPieces[i], layer, i);
                }
            }
            else
            {

                var piece = new HairPiece
                {
                    MeshName = config.HairMeshName,
                    TexturePath = config.HairTexturePath,
                    Color = config.HairColor,
                    PositionOffset = config.PositionOffset,
                    RotationOffset = config.RotationOffset,
                    Scale = config.Scale,
                    BoneName = config.BoneName
                };
                ApplyHairPiece(kerbal, piece, layer, 0);
            }

            isApplied = true;
            Debug.Log($"[KerbonautRedux] SUCCESS: Applied {hairObjects.Count} hair piece(s) to {kerbalName}");
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


            Material mat = HairConfigs.Instance.CreateMaterial(piece.TexturePath, piece.NormalMapPath, piece.Color);
            renderer.material = mat;


            SetupBoneWeights(hairMesh);


            hairObj.transform.localPosition = piece.PositionOffset;
            hairObj.transform.localEulerAngles = piece.RotationOffset;
            hairObj.transform.localScale = Vector3.one * piece.Scale;


            HairVisibilityChecker checker = hairObj.AddComponent<HairVisibilityChecker>();
            checker.Initialize(kerbal, renderer);


            hairObjects.Add(hairObj);
            hairRenderers.Add(renderer);
            
            Debug.Log($"[KerbonautRedux] Applied hair piece {index}: {piece.MeshName} at {piece.PositionOffset}");
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

            foreach (var hairObj in hairObjects)
            {
                if (hairObj != null)
                    Destroy(hairObj);
            }
            hairObjects.Clear();
            hairRenderers.Clear();
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
                isApplied = false;
                

                ApplyHair();
                Debug.Log("[KerbonautRedux] Hair reapplied after hot-reload");
            }
        }
    }

    public class EvaHairModule : PartModule
    {
        private bool isApplied;
        private List<GameObject> hairObjects = new List<GameObject>();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            
            if (!isApplied)
            {
                ApplyHair();
            }
        }

        private void ApplyHair()
        {
            if (part == null || part.protoModuleCrew == null || part.protoModuleCrew.Count == 0)
                return;

            ProtoCrewMember crew = part.protoModuleCrew[0];
            string kerbalName = crew.name;

            HairConfig config = HairConfigs.Instance?.GetConfig(kerbalName);
            if (config == null)
            {
                Debug.Log($"[KerbonautRedux] EVA: No config found for {kerbalName}");
                return;
            }
            
            Debug.Log($"[KerbonautRedux] EVA: Found config for {kerbalName}, HairPieces.Count = {config.HairPieces.Count}");


            HideOriginalMeshesEvaPonyStyle(config);


            if (config.HairPieces.Count > 0)
            {

                for (int i = 0; i < config.HairPieces.Count; i++)
                {
                    ApplyEvaHairPiece(crew, config.HairPieces[i], i);
                }
            }
            else
            {

                var piece = new HairPiece
                {
                    MeshName = config.HairMeshName,
                    TexturePath = config.HairTexturePath,
                    Color = config.HairColor,
                    PositionOffset = config.PositionOffset,
                    RotationOffset = config.RotationOffset,
                    Scale = config.Scale,
                    BoneName = config.BoneName
                };
                ApplyEvaHairPiece(crew, piece, 0);
            }

            isApplied = true;
            Debug.Log($"[KerbonautRedux] Applied {hairObjects.Count} EVA hair piece(s) to {kerbalName}");
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
            
            Material mat = HairConfigs.Instance.CreateMaterial(piece.TexturePath, piece.Color);
            renderer.material = mat;

            SetupBoneWeights(hairMesh);

            hairObj.transform.localPosition = piece.PositionOffset;
            hairObj.transform.localEulerAngles = piece.RotationOffset;
            hairObj.transform.localScale = Vector3.one * piece.Scale;

            HairVisibilityChecker checker = hairObj.AddComponent<HairVisibilityChecker>();
            Kerbal evaKerbal = part.GetComponent<Kerbal>();
            if (evaKerbal != null)
                checker.Initialize(evaKerbal, renderer);

            hairObjects.Add(hairObj);
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
                isApplied = false;
                

                ApplyHair();
                Debug.Log("[KerbonautRedux] EVA hair reapplied after hot-reload");
            }
        }
    }
}
