using System;
using System.Linq;
using UnityEngine;

namespace KerbonautRedux
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class KerbonautReduxAddon : MonoBehaviour
    {
        private bool isInitialized;
        private static Game game;

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


            AttachToExpansionPack("makinghistory_assets", 
                "assets/expansions/missions/kerbals/iva/kerbalmalevintage.prefab",
                "assets/expansions/missions/kerbals/iva/kerbalfemalevintage.prefab",
                "kerbalEVAVintage", "kerbalEVAfemaleVintage");


            AttachToExpansionPack("serenity_assets",
                "assets/expansions/serenity/kerbals/iva/kerbalmalefuture.prefab",
                "assets/expansions/serenity/kerbals/iva/kerbalfemalefuture.prefab",
                "kerbalEVAFuture", "kerbalEVAfemaleFuture");

            Debug.Log("[KerbonautRedux] Prefabs initialized");
        }

        private void AttachToEVA(string partName)
        {
            try
            {
                Part partPrefab = PartLoader.getPartInfoByName(partName)?.partPrefab;
                if (partPrefab != null)
                {
                    AttachEvaHairModule(partPrefab.gameObject);
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
            try
            {
                AssetBundle bundle = AssetBundle.GetAllLoadedAssetBundles()
                    .FirstOrDefault(b => b.name == bundleName);
                
                if (bundle != null)
                {
                    GameObject malePrefab = bundle.LoadAsset<GameObject>(maleAsset);
                    GameObject femalePrefab = bundle.LoadAsset<GameObject>(femaleAsset);
                    
                    if (malePrefab != null) AttachHairModule(malePrefab);
                    if (femalePrefab != null) AttachHairModule(femalePrefab);

                    AttachToEVA(malePart);
                    AttachToEVA(femalePart);
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
            
            Debug.Log("[KerbonautRedux] 🔄 Hot-reload triggered (quickload detected)");
            

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
            
            Debug.Log($"[KerbonautRedux] ✅ Hot-reload complete: {hairModules.Length} IVA + {evaModules.Length} EVA modules updated");
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
    }
}
