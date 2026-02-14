using System.Collections.Generic;
using System.Reflection;
using KSP.UI.Screens.Flight;
using UnityEngine;

namespace CrewPortraitCamera;

[KSPAddon()]
public class CrewPortraitCameraMod : MonoBehaviour
{
    public static Vector3 CameraOffset = new Vector3(2.858399E-09f, 0.2f, 0.2f);
    public static float CameraFOV = 105.6918f;
    public static float NearClipPlane = 0.01f;
    public static float CameraDistance = 1.139623f;
    public static bool ShowOverlayKerbals = true;

    private void Start()
    {
        LoadConfig();
        Debug.Log((object)"[CrewPortraitCamera] Mod loaded");
    }

    private void Update()
    {
        ApplySettings();
    }

    private void LoadConfig()
    {
        ConfigNode[] configNodes = GameDatabase.Instance.GetConfigNodes("CREW_PORTRAIT_CAMERA");
        foreach (ConfigNode val in configNodes)
        {
            string[] array = val.GetValue("cameraOffset")?.Split(new char[1] { ',' }) ?? new string[3] { "2.858399E-09", "0.2", "0.2" };
            if (array.Length == 3)
            {
                CameraOffset = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));
            }
            if (float.TryParse(val.GetValue("cameraFOV"), out var result))
            {
                CameraFOV = result;
            }
            if (float.TryParse(val.GetValue("nearClipPlane"), out var result2))
            {
                NearClipPlane = result2;
            }
            if (float.TryParse(val.GetValue("cameraDistance"), out var result3))
            {
                CameraDistance = result3;
            }
            if (bool.TryParse(val.GetValue("showOverlayKerbals"), out var result4))
            {
                ShowOverlayKerbals = result4;
            }
        }
    }

    private void ApplySettings()
    {
        if ((Object)(object)KerbalPortraitGallery.Instance == (Object)null)
        {
            return;
        }
        List<KerbalPortrait> portraits = KerbalPortraitGallery.Instance.Portraits;
        if (portraits == null || portraits.Count == 0)
        {
            return;
        }
        FieldInfo field = typeof(KerbalPortrait).GetField("<crewEVAMember>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            return;
        }
        foreach (KerbalPortrait item in portraits)
        {
            if ((Object)(object)item == (Object)null)
            {
                continue;
            }
            object? value = field.GetValue(item);
            KerbalEVA val = (KerbalEVA)((value is KerbalEVA) ? value : null);
            if ((Object)(object)val == (Object)null)
            {
                continue;
            }
            Camera kerbalPortraitCamera = val.kerbalPortraitCamera;
            if ((Object)(object)kerbalPortraitCamera != (Object)null)
            {
                kerbalPortraitCamera.fieldOfView = CameraFOV;
                kerbalPortraitCamera.nearClipPlane = NearClipPlane;
                ((Component)kerbalPortraitCamera).transform.localPosition = CameraOffset;
            }
            string[] array = new string[4] { "standardCameraDistance", "swimmingCameraDistance", "runningCameraDistance", "ragdollCameraDistance" };
            foreach (string name in array)
            {
                FieldInfo field2 = typeof(KerbalEVA).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field2 != null)
                {
                    field2.SetValue(val, CameraDistance);
                }
            }
            HandleOverlayKerbals(val);
        }
    }

    private void HandleOverlayKerbals(KerbalEVA eva)
    {
        if ((Object)(object)eva == (Object)null)
        {
            return;
        }
        FieldInfo field = typeof(KerbalEVA).GetField("kerbalObjects", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null && field.GetValue(eva) is List<GameObject> { Count: >1 } list)
        {
            bool flag = false;
            foreach (GameObject item in list)
            {
                if (!((Object)(object)item == (Object)null))
                {
                    bool flag2 = (Object)(object)item.GetComponentInChildren<Camera>() != (Object)null;
                    if (!flag && flag2)
                    {
                        flag = true;
                        item.SetActive(true);
                    }
                    else
                    {
                        item.SetActive(ShowOverlayKerbals);
                    }
                }
            }
        }
        Renderer[] componentsInChildren = ((Component)eva).GetComponentsInChildren<Renderer>(true);
        foreach (Renderer val in componentsInChildren)
        {
            if (!((Object)(object)val == (Object)null) && (((Component)val).gameObject.layer == LayerMask.NameToLayer("UI") || ((Component)val).gameObject.layer == LayerMask.NameToLayer("InternalUI") || ((Object)((Component)val).gameObject).name.Contains("Overlay") || ((Object)((Component)val).gameObject).name.Contains("Billboard")))
            {
                val.enabled = ShowOverlayKerbals;
            }
        }
        Transform[] componentsInChildren2 = ((Component)eva).GetComponentsInChildren<Transform>(true);
        foreach (Transform val2 in componentsInChildren2)
        {
            if (!((Object)(object)val2 == (Object)null))
            {
                string text = ((Object)((Component)val2).gameObject).name.ToLower();
                if (text.Contains("portrait_overlay") || text.Contains("kerbal_overlay") || text.Contains("avatar_overlay") || text.Contains("billboard"))
                {
                    ((Component)val2).gameObject.SetActive(ShowOverlayKerbals);
                }
            }
        }
        Camera[] componentsInChildren3 = ((Component)eva).GetComponentsInChildren<Camera>(true);
        foreach (Camera val3 in componentsInChildren3)
        {
            if ((Object)(object)val3 != (Object)(object)eva.kerbalPortraitCamera)
            {
                ((Behaviour)val3).enabled = ShowOverlayKerbals;
            }
        }
    }
}
