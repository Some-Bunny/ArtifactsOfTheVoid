using ArtifactsOfTheVoid.Artifacts;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtifactsOfTheVoid
{
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class ArtifactsVoidPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "SomeBunny";
        public const string PluginName = "ArtifactsOfTheVoid";
        public const string PluginVersion = "1.1.0";

        public static ArtifactsVoidPlugin Inst;

        public static ConfigFile configurationFile;


        public void Awake()
        {
            new Harmony(PluginGUID).PatchAll();
            configurationFile = Config;
            Inst = this;
            Log.Init(Logger);
            ArtifactOfInfestation infestation = new ArtifactOfInfestation();
            infestation.Init();
            ArtifactOfNullification nullification = new ArtifactOfNullification();
            nullification.Init();
            ArtifactOfInvasion artifactOfInvasion = new ArtifactOfInvasion();
            artifactOfInvasion.Init();
        }
    }
}
