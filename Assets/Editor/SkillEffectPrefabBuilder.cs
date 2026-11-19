using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SkillEffectPrefabBuilder
{
    private const string EffectFolder = "Assets/Resources/SkillEffects";
    private const string MaterialPath = EffectFolder + "/Effect_Glow.mat";
    private const int EffectCount = 20;

    static SkillEffectPrefabBuilder()
    {
        EditorApplication.delayCall += EnsureEffectPrefabs;
    }

    [MenuItem("Tools/Card Effects/Rebuild Missing Effect Prefabs")]
    public static void EnsureEffectPrefabs()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "SkillEffects");

        Material material = CreateGlowMaterial();
        if (material == null)
        {
            Debug.LogWarning("Card effect prefabs were not generated because CardEffectParticleGlow.shader is unavailable.");
            return;
        }

        for (int index = 1; index <= EffectCount; index++)
        {
            string prefabPath = $"{EffectFolder}/Effect_{index}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                continue;

            GameObject root = new GameObject($"Effect_{index}");
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.58f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 1.65f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startColor = Color.HSVToRGB(Mathf.Repeat(index * 0.137f, 1f), 0.45f, 1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;
            shape.arc = 360f;

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 21;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Material CreateGlowMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Custom/CardEffectParticleGlow");
        if (shader == null) return null;

        Material material = new Material(shader)
        {
            name = "Effect_Glow",
            renderQueue = 3000
        };
        material.SetTexture("_MainTex",
            AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = Path.Combine(parent, child).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
