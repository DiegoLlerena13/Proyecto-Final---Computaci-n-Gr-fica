using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using System.Linq;
using System.Collections.Generic;

public static class XRConfigurator
{
    [MenuItem("Tools/Configure XR")]
    public static void Configure()
    {
        Debug.Log("[XRConfigurator] Starting XR Configuration...");

        // 1. Get or create XRGeneralSettingsPerBuildTarget using public APIs
        var settingsPerBuildTarget = GetOrCreateSettings();
        if (settingsPerBuildTarget == null)
        {
            Debug.LogError("[XRConfigurator] Failed to get or create XRGeneralSettingsPerBuildTarget.");
            return;
        }
        
        // 2. Get or create Android settings
        var androidSettings = settingsPerBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (androidSettings == null)
        {
            Debug.Log("[XRConfigurator] Creating default settings for Android...");
            settingsPerBuildTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            androidSettings = settingsPerBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        }
        
        // 3. Get or create manager settings for Android
        var managerSettings = settingsPerBuildTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        if (managerSettings == null)
        {
            Debug.Log("[XRConfigurator] Creating default manager settings for Android...");
            settingsPerBuildTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            managerSettings = settingsPerBuildTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        }

        // 4. Load the ARCoreLoader asset
        string loaderPath = "Assets/XR/Loaders/ARCoreLoader.asset";
        XRLoader arCoreLoader = AssetDatabase.LoadAssetAtPath<XRLoader>(loaderPath);
        if (arCoreLoader == null)
        {
            Debug.LogError("[XRConfigurator] ARCoreLoader not found at " + loaderPath);
            return;
        }

        // 5. Assign ARCoreLoader to manager settings if not already assigned
        if (!managerSettings.activeLoaders.Contains(arCoreLoader))
        {
            Debug.Log("[XRConfigurator] Adding ARCoreLoader to active loaders...");
            managerSettings.TryAddLoader(arCoreLoader);
            EditorUtility.SetDirty(managerSettings);
            EditorUtility.SetDirty(settingsPerBuildTarget);
            AssetDatabase.SaveAssets();
            Debug.Log("[XRConfigurator] Successfully assigned ARCoreLoader to Android XR settings!");
        }
        else
        {
            Debug.Log("[XRConfigurator] ARCoreLoader already assigned.");
        }

        // 6. Ensure URP Renderers have ARBackgroundRendererFeature
        ConfigureURPRenderers();
    }

    private static void ConfigureURPRenderers()
    {
        Debug.Log("[XRConfigurator] Checking URP Renderers for AR Background Feature...");
        
        string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>(path);
            if (rendererData == null) continue;

            // Check if it already has the ARBackgroundRendererFeature
            bool hasFeature = false;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature != null && feature.GetType().Name == "ARBackgroundRendererFeature")
                {
                    hasFeature = true;
                    break;
                }
            }

            if (!hasFeature)
            {
                Debug.Log($"[XRConfigurator] Adding ARBackgroundRendererFeature to {rendererData.name}...");
                // Create instance of the feature
                var arFeature = ScriptableObject.CreateInstance<UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature>();
                arFeature.name = "ARBackgroundRendererFeature";
                
                // Add to asset
                AssetDatabase.AddObjectToAsset(arFeature, rendererData);
                
                // Add to list
                rendererData.rendererFeatures.Add(arFeature);
                
                // Set dirty and save
                EditorUtility.SetDirty(rendererData);
                AssetDatabase.SaveAssets();
                Debug.Log($"[XRConfigurator] Added and saved ARBackgroundRendererFeature to {rendererData.name}.");
            }
            else
            {
                Debug.Log($"[XRConfigurator] {rendererData.name} already has ARBackgroundRendererFeature.");
            }
        }
    }

    private static XRGeneralSettingsPerBuildTarget GetOrCreateSettings()
    {
        XRGeneralSettingsPerBuildTarget settings = null;
        
        // Try to get from EditorBuildSettings config
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out settings);
        
        if (settings == null)
        {
            // Try to find the asset in the database
            var guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                if (settings != null)
                {
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
                }
            }
        }
        
        if (settings == null)
        {
            // Create a new instance and save it
            settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            if (!AssetDatabase.IsValidFolder("Assets/XR"))
            {
                AssetDatabase.CreateFolder("Assets", "XR");
            }
            AssetDatabase.CreateAsset(settings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            AssetDatabase.SaveAssets();
        }
        
        return settings;
    }
}

