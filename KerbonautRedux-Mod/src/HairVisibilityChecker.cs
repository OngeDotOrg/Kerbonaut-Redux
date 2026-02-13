using UnityEngine;

namespace KerbonautRedux
{
    public class HairVisibilityChecker : MonoBehaviour
    {
        private Kerbal kerbal;
        private SkinnedMeshRenderer hairRenderer;
        private Renderer headRenderer;
        private bool wasVisible = true;

        public void Initialize(Kerbal kerbal, SkinnedMeshRenderer hairRenderer)
        {
            this.kerbal = kerbal;
            this.hairRenderer = hairRenderer;
            

            FindHeadRenderer();
        }

        private void FindHeadRenderer()
        {
            if (kerbal == null) return;


            string[] headMeshNames = new string[]
            {
                "headMesh01",
                "headMesh",
                "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51"
            };

            SkinnedMeshRenderer[] renderers = kerbal.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer r in renderers)
            {
                foreach (string name in headMeshNames)
                {
                    if (r.name == name)
                    {
                        headRenderer = r;
                        return;
                    }
                }
            }
        }

        public void Update()
        {
            if (hairRenderer == null)
                return;


            bool shouldBeVisible = true;

            if (headRenderer != null)
            {

                shouldBeVisible = headRenderer.enabled;
            }
            else
            {

                shouldBeVisible = IsHairVisibleState();
            }


            if (shouldBeVisible != wasVisible)
            {
                hairRenderer.enabled = shouldBeVisible;
                wasVisible = shouldBeVisible;
            }
        }

        private bool IsHairVisibleState()
        {






            if (kerbal == null)
                return true;



            Part part = kerbal.GetComponent<Part>();
            if (part != null && part.vessel != null)
            {


            }

            return true;
        }
    }
}
