using R2API.Utils;
using RoR2;
using UnityEngine;
using BepInEx.Configuration;
using R2API;
using UnityEngine.AddressableAssets;
using HarmonyLib;


namespace ArtifactsOfTheVoid.Artifacts
{
    public class ArtifactOfInvasion
    {
        private static string ArtifactLangTokenName => "ARTIFACT_OF_INVASION";
        private static string ArtifactName => "Artifact of Invasion";
        private static string ArtifactDescription => "Drastically increases Void Seed spawn rate and spawn amount.";

        private static ArtifactDef InvasionArtifactDefinition;
        public bool ArtifactEnabled => RunArtifactManager.instance.IsArtifactEnabled(InvasionArtifactDefinition);



        public InteractableSpawnCard VoidCampSpawnCard;
        public DirectorCard VoidDirectorCard = null;
        private static float Mult;
        private static ArtifactOfInvasion Inst;
        public void Init()
        {
            Inst = this;
            LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_NAME", ArtifactName);
            LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION", ArtifactDescription);

            InvasionArtifactDefinition = ScriptableObject.CreateInstance<ArtifactDef>();
            var texEn = Tools.GetTextureFromResource("ArtifactsOfTheVoid/Textures/invasionEnabled.png");
            var _SpriteEn = Sprite.Create(texEn, new Rect(0, 0, texEn.width, texEn.height), new Vector2(0.5f, 0.5f));

            var texDis = Tools.GetTextureFromResource("ArtifactsOfTheVoid/Textures/invasionDisabled.png");
            var _SpriteDis = Sprite.Create(texDis, new Rect(0, 0, texDis.width, texDis.height), new Vector2(0.5f, 0.5f));


            InvasionArtifactDefinition.smallIconSelectedSprite = _SpriteEn;
            InvasionArtifactDefinition.smallIconDeselectedSprite = _SpriteDis;

            InvasionArtifactDefinition.cachedName = "ARTIFACT_" + ArtifactLangTokenName;
            InvasionArtifactDefinition.nameToken = "ARTIFACT_" + ArtifactLangTokenName + "_NAME";
            InvasionArtifactDefinition.descriptionToken = "ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION";
            ContentAddition.AddArtifactDef(InvasionArtifactDefinition);

            VoidCampSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>((object)"RoR2/DLC1/VoidCamp/iscVoidCamp.asset").WaitForCompletion();
            VoidCampSpawnCard.maxSpawnsPerStage = 5;
            Mult = VoidCampSpawnCard.directorCreditCost;


            VoidDirectorCard = new DirectorCard();

            MaximumSeedAmount = ArtifactsVoidPlugin.configurationFile.Bind<int>("Artifact: " + ArtifactName, "Max Void Seed Amount", 5, "The maximum amount of Void Seeds that can be generated.");
            SeedSelectionWeight = ArtifactsVoidPlugin.configurationFile.Bind<int>("Artifact: " + ArtifactName, "Void Seed Selection Weight", 1000, "The probability this gets selected to generate compared to other interactables.");
            DirectorCreditMult = ArtifactsVoidPlugin.configurationFile.Bind<float>("Artifact: " + ArtifactName, "Void Seed Director Credit Mult", 0.5f, "The multiplier for director credits Void Seeds use."); 
            Stage1Free = ArtifactsVoidPlugin.configurationFile.Bind<bool>("Artifact: " + ArtifactName, "Stage 1 Free", false, "Prevent first stage from having seeds?"); 

            /*
            On.RoR2.DirectorCardCategorySelection.GenerateDirectorCardWeightedSelection += (_, __) =>
            {
                if (Run.instance.IsExpansionEnabled(RoR2.DLC1Content.Items.BearVoid.requiredExpansion) == false) 
                {
                    return _(__);
                }


                WeightedSelection<DirectorCard> weightedSelection = new WeightedSelection<DirectorCard>(8);
                for (int i = 0; i < __.categories.Length; i++)
                {
                    ref DirectorCardCategorySelection.Category ptr = ref __.categories[i];
                    float num = __.SumAllWeightsInCategory(ptr);
                    float num2 = ptr.selectionWeight / num;
                    if (num > 0f)
                    {
                        foreach (DirectorCard directorCard in ptr.cards)
                        {
                            if (ArtifactEnabled)
                            {
                                if (directorCard.spawnCard != null && directorCard.spawnCard.prefab == VoidCampSpawnCard.prefab)
                                {
                                    if (Stage1Free.Value == false)
                                    {
                                        directorCard.minimumStageCompletions =  -1;
                                    }
                                    VoidCampSpawnCard.maxSpawnsPerStage = MaximumSeedAmount.Value;
                                    directorCard.spawnCard = VoidCampSpawnCard;
                                    VoidCampSpawnCard.directorCreditCost = Mathf.RoundToInt(Mult / DirectorCreditMult.Value);
                                    directorCard.selectionWeight = SeedSelectionWeight.Value;
                                }
                            }
                            if (directorCard.IsAvailable())
                            {
                                float weight = (float)directorCard.selectionWeight * num2;
                                weightedSelection.AddChoice(directorCard, weight);
                            }
                        }
                    }
                }
                return weightedSelection;
            };
            */
        }
        public static ConfigEntry<int> MaximumSeedAmount;
        public static ConfigEntry<int> SeedSelectionWeight;
        public static ConfigEntry<float> DirectorCreditMult;
        public static ConfigEntry<bool> Stage1Free;

        [HarmonyPatch(typeof(RoR2.DirectorCardCategorySelection), nameof(RoR2.DirectorCardCategorySelection.GenerateDirectorCardWeightedSelection))]
        public class GenerateDirectorCardWeightedSelection_Patch
        {
            [HarmonyPrefix]
            public static bool Postfix(DirectorCardCategorySelection __instance, ref WeightedSelection<DirectorCard> __result)
            {          
                if (ArtifactOfInvasion.Inst.ArtifactEnabled)
                {
                    WeightedSelection<DirectorCard> weightedSelection = new WeightedSelection<DirectorCard>(8);
                    for (int i = 0; i < __instance.categories.Length; i++)
                    {
                        ref DirectorCardCategorySelection.Category ptr = ref __instance.categories[i];
                        float num = __instance.SumAllWeightsInCategory(ptr);
                        float num2 = ptr.selectionWeight / num;
                        if (num > 0f)
                        {
                            foreach (DirectorCard directorCard in ptr.cards)
                            {
                                if (directorCard.spawnCard != null && directorCard.spawnCard.prefab == ArtifactOfInvasion.Inst.VoidCampSpawnCard.prefab)
                                {
                                    if (Stage1Free.Value == false)
                                    {
                                        directorCard.minimumStageCompletions = -1;
                                    }
                                    ArtifactOfInvasion.Inst.VoidCampSpawnCard.maxSpawnsPerStage = MaximumSeedAmount.Value;
                                    directorCard.spawnCard = ArtifactOfInvasion.Inst.VoidCampSpawnCard;
                                    ArtifactOfInvasion.Inst.VoidCampSpawnCard.directorCreditCost = Mathf.RoundToInt(Mult / DirectorCreditMult.Value);
                                    directorCard.selectionWeight = SeedSelectionWeight.Value;
                                }
                                if (directorCard.IsAvailable())
                                {
                                    float weight = (float)directorCard.selectionWeight * num2;
                                    weightedSelection.AddChoice(directorCard, weight);
                                }
                            }
                        }
                    }
                    __result = weightedSelection;
                    return false;
                }
                return true;
            }
        }


    }
}
