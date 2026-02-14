using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KerbonautRedux
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class KerbonautReduxAddon : MonoBehaviour
    {
        private bool isInitialized;
        private static Game game;

        private static List<HairModule> activeHairModules = new List<HairModule>();
        private static List<EvaHairModule> activeEvaHairModules = new List<EvaHairModule>();

        public void Start()
        {

            HairConfigs.Instance = new HairConfigs();

            GameEvents.onKerbalAddComplete.Add(OnKerbalAdd);
            GameEvents.onGameStateCreated.Add(OnGameStateCreated);

            DontDestroyOnLoad(this);

            Debug.Log("[KerbonautRedux] Addon started");
        }

        public void LateUpdate()
        {

            if (!isInitialized && PartLoader.Instance != null && PartLoader.Instance.IsReady())
            {
                isInitialized = true;
                InitializePrefabs();
                HairConfigs.Instance.Load();
                Debug.Log("[KerbonautRedux] Hair configs loaded");
            }
        }

        private void InitializePrefabs()
        {

            Kerbal[] kerbals = Resources.FindObjectsOfTypeAll<Kerbal>();
            foreach (Kerbal kerbal in kerbals)
            {
                AttachHairModule(kerbal.gameObject);
            }

            AttachToEVA("kerbalEVAfemale");
            AttachToEVA("kerbalEVA");

            AttachToEVA("kerbalEVAVintage");
            AttachToEVA("kerbalEVAfemaleVintage");

            AttachToEVA("kerbalEVAfemaleVintage");
            AttachToEVA("kerbalEVAVintage");
            AttachToEVA("kerbalEVAFuture");
            AttachToEVA("kerbalEVAfemaleFuture");

            AttachToAllEVAParts();

            Debug.Log("[KerbonautRedux] Prefabs initialized");
        }

        private void AttachToAllEVAParts()
        {
            if (PartLoader.Instance == null || PartLoader.LoadedPartsList == null)
                return;

            int attachedCount = 0;

            foreach (AvailablePart avPart in PartLoader.LoadedPartsList)
            {
                if (avPart?.partPrefab == null) continue;

                string partName = avPart.name;

                bool isEvaPart = partName.StartsWith("kerbalEVA", StringComparison.OrdinalIgnoreCase) ||
                                 partName.IndexOf("eva", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isEvaPart)
                {
                    try
                    {
                        if (avPart.partPrefab.gameObject.GetComponent<EvaHairModule>() == null)
                        {
                            avPart.partPrefab.gameObject.AddComponent<EvaHairModule>();
                            attachedCount++;
                            Debug.Log($"[KerbonautRedux] Attached EvaHairModule to EVA part: {partName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[KerbonautRedux] Could not attach to {partName}: {ex.Message}");
                    }
                }
            }

            if (attachedCount > 0)
            {
                Debug.Log($"[KerbonautRedux] Attached EvaHairModule to {attachedCount} EVA part(s)");
            }
        }

        private void AttachToEVA(string partName)
        {
            try
            {
                AvailablePart avPart = PartLoader.getPartInfoByName(partName);
                if (avPart == null)
                {

                    Debug.Log($"[KerbonautRedux] EVA part '{partName}' not found (may not be installed)");
                    return;
                }

                Part partPrefab = avPart.partPrefab;
                if (partPrefab != null)
                {
                    if (partPrefab.gameObject.GetComponent<EvaHairModule>() == null)
                    {
                        partPrefab.gameObject.AddComponent<EvaHairModule>();
                        Debug.Log($"[KerbonautRedux] Successfully attached EvaHairModule to '{partName}'");
                    }
                    else
                    {
                        Debug.Log($"[KerbonautRedux] EvaHairModule already attached to '{partName}'");
                    }
                }
                else
                {
                    Debug.LogWarning($"[KerbonautRedux] EVA part '{partName}' has no partPrefab");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KerbonautRedux] Could not attach to {partName}: {ex.Message}");
            }
        }

        private void AttachToExpansionPack(string bundleName, string maleAsset, string femaleAsset,
            string malePart, string femalePart)
        {

            AttachToEVA(malePart);
            AttachToEVA(femalePart);

            try
            {
                AssetBundle bundle = AssetBundle.GetAllLoadedAssetBundles()
                    .FirstOrDefault(b => b.name == bundleName);

                if (bundle != null)
                {
                    GameObject malePrefab = bundle.LoadAsset<GameObject>(maleAsset);
                    GameObject femalePrefab = bundle.LoadAsset<GameObject>(femaleAsset);

                    if (malePrefab != null)
                    {
                        AttachHairModule(malePrefab);
                        Debug.Log($"[KerbonautRedux] Attached HairModule to {bundleName} male IVA prefab");
                    }
                    if (femalePrefab != null)
                    {
                        AttachHairModule(femalePrefab);
                        Debug.Log($"[KerbonautRedux] Attached HairModule to {bundleName} female IVA prefab");
                    }
                }
                else
                {
                    Debug.Log($"[KerbonautRedux] Asset bundle '{bundleName}' not loaded (DLC may not be installed)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KerbonautRedux] Could not attach to {bundleName}: {ex.Message}");
            }
        }

        private void AttachHairModule(GameObject go)
        {
            if (go.GetComponent<HairModule>() == null)
            {
                go.AddComponent<HairModule>();
            }
        }

        private void AttachEvaHairModule(GameObject go)
        {
            if (go.GetComponent<EvaHairModule>() == null)
            {
                go.AddComponent<EvaHairModule>();
            }
        }

        private void OnKerbalAdd(ProtoCrewMember crew)
        {

            if (crew.type != ProtoCrewMember.KerbalType.Applicant)
                return;

            ApplyHairToCrew(crew);
        }

        private void OnGameStateCreated(Game g)
        {
            game = g;

            foreach (ProtoCrewMember crew in g.CrewRoster.Crew)
            {
                ApplyHairToCrew(crew);
            }

            ReloadAllHair();
        }

        private void ReloadAllHair()
        {
            if (HairConfigs.Instance == null) return;

            Debug.Log("[KerbonautRedux]  Hot-reload triggered (quickload detected)");

            HairConfigs.Instance.ReloadConfigs();

            HairModule[] hairModules = FindObjectsOfType<HairModule>();
            foreach (HairModule module in hairModules)
            {
                if (module != null)
                {
                    module.ReapplyHair();
                }
            }

            EvaHairModule[] evaModules = FindObjectsOfType<EvaHairModule>();
            foreach (EvaHairModule module in evaModules)
            {
                if (module != null)
                {
                    module.ReapplyHair();
                }
            }

            Debug.Log($"[KerbonautRedux]  Hot-reload complete: {hairModules.Length} IVA + {evaModules.Length} EVA modules updated");
        }

        private void ApplyHairToCrew(ProtoCrewMember crew)
        {
            HairConfig config = HairConfigs.Instance.GetConfig(crew.name);
            if (config != null)
            {
                Debug.Log($"[KerbonautRedux] Applying hair config to {crew.name}");

                if (!string.IsNullOrEmpty(config.Trait))
                {
                    KerbalRoster.SetExperienceTrait(crew, config.Trait);
                }

                crew.courage = config.Courage;
                crew.stupidity = config.Stupidity;
                crew.isBadass = config.IsBadass;
            }
        }

        #region Runtime Access Methods

        public static void RegisterHairModule(HairModule module)
        {
            if (!activeHairModules.Contains(module))
            {
                activeHairModules.Add(module);
            }
        }

        public static void UnregisterHairModule(HairModule module)
        {
            activeHairModules.Remove(module);
        }

        public static void RegisterEvaHairModule(EvaHairModule module)
        {
            if (!activeEvaHairModules.Contains(module))
            {
                activeEvaHairModules.Add(module);
            }
        }

        public static void UnregisterEvaHairModule(EvaHairModule module)
        {
            activeEvaHairModules.Remove(module);
        }

        public static IEnumerable<HairModule> GetAllHairModules()
        {

            activeHairModules.RemoveAll(m => m == null);
            return activeHairModules;
        }

        public static IEnumerable<EvaHairModule> GetAllEvaHairModules()
        {

            activeEvaHairModules.RemoveAll(m => m == null);
            return activeEvaHairModules;
        }

        public static HairModule FindHairModule(string kerbalName)
        {
            activeHairModules.RemoveAll(m => m == null);
            return activeHairModules.Find(m => m.GetKerbalName() == kerbalName);
        }

        public static EvaHairModule FindEvaHairModule(string kerbalName)
        {
            activeEvaHairModules.RemoveAll(m => m == null);
            return activeEvaHairModules.Find(m => m.GetKerbalName() == kerbalName);
        }

        public static void SetKerbalHairPosition(string kerbalName, int pieceIndex, Vector3 newPosition)
        {
            var ivaModule = FindHairModule(kerbalName);
            if (ivaModule != null)
            {
                ivaModule.SetHairPiecePosition(pieceIndex, newPosition);
                return;
            }

            var evaModule = FindEvaHairModule(kerbalName);
            if (evaModule != null)
            {
                evaModule.SetHairPiecePosition(pieceIndex, newPosition);
            }
        }

        public static void SetKerbalHairRotation(string kerbalName, int pieceIndex, Vector3 newRotation)
        {
            var ivaModule = FindHairModule(kerbalName);
            if (ivaModule != null)
            {
                ivaModule.SetHairPieceRotation(pieceIndex, newRotation);
                return;
            }

            var evaModule = FindEvaHairModule(kerbalName);
            if (evaModule != null)
            {
                evaModule.SetHairPieceRotation(pieceIndex, newRotation);
            }
        }

        public static void SetKerbalHairScale(string kerbalName, int pieceIndex, float newScale)
        {
            var ivaModule = FindHairModule(kerbalName);
            if (ivaModule != null)
            {
                ivaModule.SetHairPieceScale(pieceIndex, newScale);
                return;
            }

            var evaModule = FindEvaHairModule(kerbalName);
            if (evaModule != null)
            {
                evaModule.SetHairPieceScale(pieceIndex, newScale);
            }
        }

        public static void SetKerbalHairShader(string kerbalName, int pieceIndex, string shaderName)
        {
            var ivaModule = FindHairModule(kerbalName);
            if (ivaModule != null)
            {
                ivaModule.SetHairPieceShader(pieceIndex, shaderName);
                return;
            }

            var evaModule = FindEvaHairModule(kerbalName);
            if (evaModule != null)
            {
                evaModule.SetHairPieceShader(pieceIndex, shaderName);
            }
        }

        public static string[] GetAvailableShaders()
        {
            return HairConfigs.AvailableShaders;
        }

        #endregion
    }
}
