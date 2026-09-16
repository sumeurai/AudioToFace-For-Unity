using UnityEngine;
using UnityEngine.Rendering;

static class XandraPipelineMaterialUtil
{
    public enum Kind
    {
        Skin,
        Opaque,
        Hair,
        Lashes,
        Transparent
    }

    public static bool IsHdrp()
    {
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null)
        {
            return false;
        }

        string typeName = pipeline.GetType().FullName;
        return !string.IsNullOrEmpty(typeName) && typeName.Contains("HDRenderPipeline");
    }

    public static bool IsUrp()
    {
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null)
        {
            return false;
        }

        string typeName = pipeline.GetType().FullName;
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        return typeName.Contains("Universal") || typeName.Contains("Lightweight");
    }

    public static Kind KindOf(Material source)
    {
        if (source == null)
        {
            return Kind.Opaque;
        }

        switch (SlotKey(source.name))
        {
            case "Face":
            case "Skin":
                return Kind.Skin;
            case "Hair":
                return Kind.Hair;
            case "Lashes":
                return Kind.Lashes;
            case "Vitreous":
                return Kind.Transparent;
            default:
                return Kind.Opaque;
        }
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        return name.Replace(" (Instance)", "")
            .Replace(" (URP)", "")
            .Replace(" (Built-in)", "")
            .Trim();
    }

    public static string SlotKey(string name)
    {
        string n = NormalizeName(name);
        if (n.StartsWith("HDRP_"))
        {
            n = n.Substring(5);
        }
        else if (n.StartsWith("Lit_"))
        {
            n = n.Substring(4);
        }
        else if (n.StartsWith("M_"))
        {
            n = n.Substring(2);
        }

        switch (n)
        {
            case "Face":
            case "Skin_F_Face_A":
                return "Face";
            case "Skin":
            case "Skin_F_Body_A":
            case "X3D_HF2_Head_AshleySG9":
                return "Skin";
            case "Teeth":
            case "Skin_Mouth":
                return "Teeth";
            case "Eye":
            case "Eyes_Blue":
                return "Eye";
            case "TankTop":
            case "Ashley_Top1":
                return "TankTop";
            case "Skirt":
            case "Ashley_Skirt":
            case "Ashley_Top":
            case "Bikini_A":
                return "Skirt";
            case "Boots":
            case "Leather":
            case "Ashley_Boots":
                return "Boots";
            case "Hair":
            case "Hair 1":
            case "HairCards":
            case "I_Hair_Blonde":
                return "Hair";
            case "Lashes":
                return "Lashes";
            case "Vitreous":
            case "Fluid":
                return "Vitreous";
            default:
                return n;
        }
    }

    public static Material Apply(Material source)
    {
        if (source == null || IsHdrp())
        {
            return source;
        }

        Kind kind = KindOf(source);
        if (kind == Kind.Hair || kind == Kind.Lashes)
        {
            SetFloat(source, "_BumpScale", kind == Kind.Lashes ? 0.2f : 0.45f);
            SetFloat(source, "_Cutoff", kind == Kind.Lashes ? 0.08f : 0.07f);
            SetFloat(source, "_Glossiness", kind == Kind.Lashes ? 0.1f : 0.45f);
            SetFloat(source, "_FlowStrength", 0.35f);
            source.renderQueue = 3000;
            source.SetOverrideTag("RenderType", "Transparent");
            return source;
        }

        if (kind == Kind.Skin)
        {
            SetFloat(source, "_Skin", 1f);
            SetFloat(source, "_Glossiness", 0.36f);
            SetFloat(source, "_Metallic", 0f);
            source.EnableKeyword("_SKIN_ON");
            source.DisableKeyword("_EYE_ON");
            return source;
        }

        if (kind == Kind.Transparent)
        {
            SetColor(source, "_Color", new Color(1f, 1f, 1f, 0.04f));
            SetFloat(source, "_Glossiness", 0.85f);
            source.renderQueue = 3000;
            return source;
        }

        string slot = SlotKey(source.name);
        if (slot == "Eye")
        {
            SetFloat(source, "_Glossiness", 0.62f);
            SetFloat(source, "_Metallic", 0f);
            SetFloat(source, "_Eye", 1f);
            source.EnableKeyword("_EYE_ON");
            source.DisableKeyword("_SKIN_ON");
            return source;
        }

        source.DisableKeyword("_SKIN_ON");
        SetFloat(source, "_Skin", 0f);
        return source;
    }

    static Texture GetTexture(Material mat, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (mat.HasProperty(names[i]))
            {
                Texture tex = mat.GetTexture(names[i]);
                if (tex != null)
                {
                    return tex;
                }
            }
        }

        return null;
    }

    static Color GetColor(Material mat, string a, string b, Color fallback)
    {
        if (mat.HasProperty(a))
        {
            return mat.GetColor(a);
        }

        if (mat.HasProperty(b))
        {
            return mat.GetColor(b);
        }

        return fallback;
    }

    static float GetFloat(Material mat, string name, float fallback)
    {
        if (mat.HasProperty(name))
        {
            return mat.GetFloat(name);
        }

        return fallback;
    }

    static void SetTexture(Material mat, string name, Texture texture)
    {
        if (mat.HasProperty(name))
        {
            mat.SetTexture(name, texture);
        }
    }

    static void SetColor(Material mat, string name, Color color)
    {
        if (mat.HasProperty(name))
        {
            mat.SetColor(name, color);
        }
    }

    static void SetFloat(Material mat, string name, float value)
    {
        if (mat.HasProperty(name))
        {
            mat.SetFloat(name, value);
        }
    }
}
