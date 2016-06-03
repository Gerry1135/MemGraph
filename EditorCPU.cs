using System;
using System.Collections;
using UnityEngine;
using KSP.IO;

namespace MemGraph
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorCPUFix : MonoBehaviour
    {
        const String configFilename = "editorcpu.cfg";

        bool killCrew = false;
        bool killProps = false;
        bool killAnims = false;
        bool killLights = false;

        public void Start()
        {
            if (Graph.IsOpen())
                base.StartCoroutine(DisableDoodads());
        }

        private IEnumerator DisableDoodads()
        {
            if (File.Exists<EditorCPUFix>(configFilename))
            {
                String[] lines = File.ReadAllLines<EditorCPUFix>(configFilename);

                for (int i = 0; i < lines.Length; i++)
                {
                    print("EditorCPU: " + lines[i]);
                    String[] line = lines[i].Split('=');
                    if (line.Length == 2)
                    {
                        String key = line[0].Trim();
                        String val = line[1].Trim();
                        if (key == "killCrew")
                            ReadBool(val, ref killCrew);
                        else if (key == "killProps")
                            ReadBool(val, ref killProps);
                        else if (key == "killAnims")
                            ReadBool(val, ref killAnims);
                        else if (key == "killLights")
                            ReadBool(val, ref killLights);
                    }
                }
            }

            GameObject gameObject = null;

            while (EditorDriver.fetch == null)
            {
                yield return null;
            }

            if (killCrew)
            {
                string name = "VABCrew";
                if (EditorDriver.editorFacility == EditorFacility.SPH)
                {
                    name = "SPHCrew";
                }
                if (GameSettings.SHOW_SPACE_CENTER_CREW)
                {
                    print("EditorCPU: Killing " + name);
                    while (gameObject == null)
                    {
                        yield return null;
                        gameObject = GameObject.Find(name);
                    }
                    gameObject?.SetActive(false);
                }
            }

            gameObject = null;
            while (gameObject == null)
            {
                yield return null;
                gameObject = GameObject.Find("model_props");
            }
            if (killProps)
            {
                print("EditorCPU: Killing model_props");
                gameObject.SetActive(false);
            }

            if (EditorDriver.editorFacility == EditorFacility.SPH)
            {
                if (killLights)
                {
                    print("EditorCPU: Killing model_sph_interior_lights_v16 and Lighting_Baked");
                    gameObject = GameObject.Find("model_sph_interior_lights_v16");
                    gameObject?.SetActive(false);

                    gameObject = GameObject.Find("Lighting_Baked");
                    gameObject?.SetActive(false);
                }
            }
            else
            {
                if (killAnims)
                {
                    print("EditorCPU: Killing model_vab_prop_truck_01 and model_vab_elevators");
                    gameObject = GameObject.Find("model_vab_prop_truck_01");
                    gameObject?.SetActive(false);

                    gameObject = GameObject.Find("model_vab_elevators");
                    gameObject?.SetActive(false);
                }

                if (killLights)
                {
                    print("EditorCPU: Killing VAB_Interior_BakeLights and model_vab_interior_lights_flood_v16");
                    gameObject = GameObject.Find("VAB_Interior_BakeLights");
                    if (gameObject != null)
                    {
                        gameObject.SetActive(false);
                    }
                    gameObject = GameObject.Find("model_vab_interior_lights_flood_v16");
                    if (gameObject != null)
                    {
                        gameObject.SetActive(false);
                    }
                }
            }

            yield break;
        }

        void ReadBool(String str, ref bool variable)
        {
            bool value = false;
            if (Boolean.TryParse(str, out value))
                variable = value;
        }
    }
}
