using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class XandraCoreRpSetup : MonoBehaviour
{
    [Serializable]
    public class MaterialSet
    {
        public Material face;
        public Material skin;
        public Material teeth;
        public Material eye;
        public Material tankTop;
        public Material skirt;
        public Material boots;
        public Material hair;
        public Material lashes;
        public Material vitreous;
    }

    [SerializeField] Transform characterRoot;
    [SerializeField] AudioToFaceSample sample;
    [SerializeField] AudioRecord audioRecord;

    [SerializeField] MaterialSet builtIn;
    [SerializeField] MaterialSet urp;

    void OnEnable()
    {
        PushCharacterLights();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += EditorApply;
            return;
        }
#endif
        ApplyNow(true);
    }

    void Update()
    {
        PushCharacterLights();
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall -= EditorApply;
#endif
        ClearCharacterLightsIfLast();
    }

#if UNITY_EDITOR
    void EditorApply()
    {
        if (this == null)
        {
            return;
        }

        ApplyNow(false);
    }
#endif

    void ApplyNow(bool bindSample)
    {
        ResolveCharacter();
        ApplyMaterials();
        PushCharacterLights();
        if (bindSample && Application.isPlaying)
        {
            BindSample();
        }
    }

    void ResolveCharacter()
    {
        if (characterRoot != null)
        {
            return;
        }

        GameObject found = GameObject.Find("ARKit");
        if (found != null)
        {
            characterRoot = found.transform;
            return;
        }

        GameObject[] roots = gameObject.scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform match = FindChildByName(roots[i].transform, "ARKit");
            if (match != null)
            {
                characterRoot = match;
                return;
            }
        }
    }

    static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    MaterialSet ActiveSet()
    {
        if (XandraPipelineMaterialUtil.IsUrp() && urp != null && urp.face != null)
        {
            return urp;
        }

        return builtIn;
    }

    [ContextMenu("Apply Sample Materials")]
    public void ApplyMaterials()
    {
        ResolveCharacter();
        if (characterRoot == null)
        {
            Debug.LogWarning("[SumeruAI] XandraCoreRpSetup: character root 'ARKit' was not found.");
            return;
        }

        MaterialSet slots = ActiveSet();
        if (slots == null)
        {
            Debug.LogWarning("[SumeruAI] XandraCoreRpSetup: assign BuiltIn / URP material sets.");
            return;
        }

        Renderer[] renderers = characterRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            bool changed = false;
            for (int m = 0; m < mats.Length; m++)
            {
                Material mapped = MapMaterial(mats[m], slots);
                if (mapped != null)
                {
                    mapped = XandraPipelineMaterialUtil.Apply(mapped);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(mapped);
                    }
#endif
                }

                if (mapped != null && mapped != mats[m])
                {
                    mats[m] = mapped;
                    changed = true;
                }
            }

            if (!changed && mats.Length == 1)
            {
                Material byObject = MapRendererName(renderers[i].gameObject.name, slots);
                if (byObject != null)
                {
                    byObject = XandraPipelineMaterialUtil.Apply(byObject);
                }

                if (byObject != null && byObject != mats[0])
                {
                    mats[0] = byObject;
                    changed = true;
                }
            }

            if (changed)
            {
                renderers[i].sharedMaterials = mats;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(renderers[i]);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderers[i]);
                }
#endif
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    void BindSample()
    {
        if (sample == null)
        {
            sample = GetComponent<AudioToFaceSample>();
        }

        if (sample == null || characterRoot == null)
        {
            return;
        }

        SkinnedMeshRenderer[] all = characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        List<SkinnedMeshRenderer> faces = new List<SkinnedMeshRenderer>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].sharedMesh != null && all[i].sharedMesh.blendShapeCount > 0)
            {
                faces.Add(all[i]);
            }
        }

        sample.Bind(faces.ToArray(), characterRoot, audioRecord);
    }

    static Material MapRendererName(string objectName, MaterialSet slots)
    {
        if (string.IsNullOrEmpty(objectName) || slots == null)
        {
            return null;
        }

        switch (objectName.ToLowerInvariant())
        {
            case "boots":
                return slots.boots;
            case "hair":
                return slots.hair;
            case "body":
                return slots.skin;
            case "skirt":
                return slots.skirt;
            case "tanktop":
            case "tank top":
                return slots.tankTop;
            case "lashes":
            case "eyelash":
            case "eyelashes":
                return slots.lashes;
            case "eye":
            case "eyes":
                return slots.eye;
            default:
                return null;
        }
    }

    static Material MapMaterial(Material source, MaterialSet slots)
    {
        if (source == null || slots == null)
        {
            return null;
        }

        switch (XandraPipelineMaterialUtil.SlotKey(source.name))
        {
            case "Face":
                return slots.face;
            case "Skin":
                return slots.skin;
            case "Teeth":
                return slots.teeth;
            case "Eye":
                return slots.eye;
            case "TankTop":
                return slots.tankTop;
            case "Skirt":
                return slots.skirt;
            case "Boots":
                return slots.boots;
            case "Hair":
                return slots.hair;
            case "Lashes":
                return slots.lashes;
            case "Vitreous":
                return slots.vitreous;
            default:
                return null;
        }
    }

    void PushCharacterLights()
    {
        Shader.SetGlobalFloat("_AtfUseUrpLight", XandraPipelineMaterialUtil.IsUrp() ? 1f : 0f);

        Light[] lights = FindObjectsOfType<Light>();
        Light main = PickMainDirectional(lights);
        Light extra0 = null;
        Light extra1 = null;
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null || light == main || light.type != LightType.Directional)
            {
                continue;
            }

            if (light.gameObject.scene != gameObject.scene || !light.isActiveAndEnabled)
            {
                continue;
            }

            if (extra0 == null)
            {
                extra0 = light;
            }
            else if (extra1 == null)
            {
                extra1 = light;
                break;
            }
        }

        SetExtraLight(0, extra0);
        SetExtraLight(1, extra1);
    }

    Light PickMainDirectional(Light[] lights)
    {
        Light sun = RenderSettings.sun;
        if (sun != null && sun.type == LightType.Directional && sun.isActiveAndEnabled &&
            sun.gameObject.scene == gameObject.scene)
        {
            return sun;
        }

        Light best = null;
        float bestIntensity = -1f;
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null || light.type != LightType.Directional || !light.isActiveAndEnabled)
            {
                continue;
            }

            if (light.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            if (light.intensity > bestIntensity)
            {
                best = light;
                bestIntensity = light.intensity;
            }
        }

        return best;
    }

    void ClearCharacterLightsIfLast()
    {
        XandraCoreRpSetup[] setups = FindObjectsOfType<XandraCoreRpSetup>();
        for (int i = 0; i < setups.Length; i++)
        {
            if (setups[i] != null && setups[i] != this && setups[i].isActiveAndEnabled)
            {
                return;
            }
        }

        SetExtraLight(0, null);
        SetExtraLight(1, null);
    }

    static void SetExtraLight(int index, Light light)
    {
        string dirName = index == 0 ? "_AtfExtraLightDir0" : "_AtfExtraLightDir1";
        string colName = index == 0 ? "_AtfExtraLightColor0" : "_AtfExtraLightColor1";
        if (light == null || !light.isActiveAndEnabled || light.intensity <= 0f)
        {
            Shader.SetGlobalVector(dirName, Vector4.zero);
            Shader.SetGlobalVector(colName, Vector4.zero);
            return;
        }

        Vector3 dir = -light.transform.forward;
        Color rgb = light.color * light.intensity;
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            rgb = rgb.linear;
        }

        Shader.SetGlobalVector(dirName, dir);
        Shader.SetGlobalVector(colName, new Vector4(rgb.r, rgb.g, rgb.b, 0f));
    }
}
#if UNITY_EDITOR
[InitializeOnLoad]
static class XandraCoreRpSetupSceneHook
{
    static XandraCoreRpSetupSceneHook()
    {
        EditorSceneManager.sceneOpened += (scene, mode) =>
        {
            EditorApplication.delayCall += ApplyIfCoreRpScene;
        };
        EditorApplication.delayCall += ApplyIfCoreRpScene;
    }

    static void ApplyIfCoreRpScene()
    {
        XandraCoreRpSetup[] setups = UnityEngine.Object.FindObjectsOfType<XandraCoreRpSetup>();
        for (int i = 0; i < setups.Length; i++)
        {
            if (setups[i] != null)
            {
                setups[i].ApplyMaterials();
            }
        }
    }
}
#endif
