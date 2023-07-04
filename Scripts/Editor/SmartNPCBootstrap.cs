#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace SmartNPC
{
    [InitializeOnLoad]
    public class SmartNPCBootstrap
    {
        public static readonly string AssetsPath = "Assets/SmartNPC";
        public static readonly string PackagePath = "Packages/ai.smartnpc.client";
        public static readonly string ConfigRelativePath = "Resources/SmartNPC Connection Config.asset";
        public static readonly string LogoRelativePath = "Images/logo.png";
        public static readonly string SceneRelativePath = "Scenes/SmartNPC Demo.unity";

        static SmartNPCBootstrap()
        {
            if (!AssetDatabase.IsValidFolder(AssetsPath)) AssetDatabase.CreateFolder("Assets", "SmartNPC");
            if (!AssetDatabase.IsValidFolder(AssetsPath + "/Resources")) AssetDatabase.CreateFolder(AssetsPath, "Resources");
            if (!AssetDatabase.IsValidFolder(AssetsPath + "/Images")) AssetDatabase.CreateFolder(AssetsPath, "Images");
            if (!AssetDatabase.IsValidFolder(AssetsPath + "/Scenes")) AssetDatabase.CreateFolder(AssetsPath, "Scenes");

            CreateConfig();
            CopyLogo();
            CopyScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateConfig()
        {
            string path = SmartNPCBootstrap.AssetsPath + "/" + ConfigRelativePath;

            if (!AssetDatabase.LoadAssetAtPath<SmartNPCConnectionConfig>(path)) {
                SmartNPCConnectionConfig config = ScriptableObject.CreateInstance<SmartNPCConnectionConfig>();

                AssetDatabase.CreateAsset(config, path);
            }
        }

        private static void CopyLogo()
        {
            string path = AssetsPath + "/" + LogoRelativePath;

            if (!AssetDatabase.LoadAssetAtPath<Texture>(path)) AssetDatabase.CopyAsset(PackagePath + "/" + LogoRelativePath, path);
        }

        private static void CopyScene()
        {
            string path = AssetsPath + "/" + SceneRelativePath;

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) AssetDatabase.CopyAsset(PackagePath + "/" + SceneRelativePath, path);
        }
    }
}

#endif