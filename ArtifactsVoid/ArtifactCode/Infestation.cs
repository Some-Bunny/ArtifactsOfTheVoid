using RoR2;
using UnityEngine;
using System.Reflection;
using BepInEx.Configuration;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using R2API;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections;
using System.Linq;
using Mono.Cecil.Cil;

namespace ArtifactsOfTheVoid.Artifacts
{
    public class ArtifactOfInfestation
    {
        private static string ArtifactLangTokenName => "ARTIFACT_OF_INFEST";
        private static string ArtifactName => "Artifact of Infestation";
        private static string ArtifactDescription => "When enabled, causes most interactables to spawn Void Infestors when used.";

        private static ArtifactDef InfestationArtifactDefinition;
        public bool ArtifactEnabled => RunArtifactManager.instance.IsArtifactEnabled(InfestationArtifactDefinition);

        public static ArtifactOfInfestation Inst;
        private CharacterSpawnCard voidRaidCrabPhase2 = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset").WaitForCompletion();
        public CharacterSpawnCard InfestorPrefab = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/EliteVoid/cscVoidInfestor.asset").WaitForCompletion();
        public void Init()
        {
            Inst = this;
            LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_NAME", ArtifactName);
            LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION", ArtifactDescription);

            InfestationArtifactDefinition = ScriptableObject.CreateInstance<ArtifactDef>();


            var texEn = Tools.GetTextureFromResource("ArtifactsOfTheVoid/Textures/infestorEnabled.png");
            var _SpriteEn = Sprite.Create(texEn, new Rect(0,0, texEn.width, texEn.height), new Vector2(0.5f, 0.5f));

            var texDis = Tools.GetTextureFromResource("ArtifactsOfTheVoid/Textures/infestorDisabled.png");
            var _SpriteDis = Sprite.Create(texDis, new Rect(0, 0, texDis.width, texDis.height), new Vector2(0.5f, 0.5f));


            InfestationArtifactDefinition.smallIconSelectedSprite = _SpriteEn;
            InfestationArtifactDefinition.smallIconDeselectedSprite = _SpriteDis;


            InfestationArtifactDefinition.cachedName = "ARTIFACT_" + ArtifactLangTokenName;
            InfestationArtifactDefinition.nameToken = "ARTIFACT_" + ArtifactLangTokenName + "_NAME";
            InfestationArtifactDefinition.descriptionToken = "ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION";


            
            GlobalEventManager.OnInteractionsGlobal += GlobalEventManager_OnInteractionsGlobal;



            ContentAddition.AddArtifactDef(InfestationArtifactDefinition);


           
            FunkyMode = ArtifactsVoidPlugin.configurationFile.Bind<bool>("Artifact: " + ArtifactName, "Funky Mode", false, "get funky oh yeahhh");
            CrabSurprise = ArtifactsVoidPlugin.configurationFile.Bind<float>("Artifact: " + ArtifactName, "Crab Surprise", 0, "The chance of a Giant Enemy Crab appearing is low... but never zero. Unless it's been set to zero. Value should range from 0-1. Preferably use low values like 0.005.");
        }



        public static ConfigEntry<bool> FunkyMode;
        public static ConfigEntry<float> CrabSurprise;






        /*
        private void RouletteChestController_EjectPickupServer(On.RoR2.RouletteChestController.orig_EjectPickupServer orig, RouletteChestController self, UniquePickup pickupIndex)
        {
            if (ArtifactEnabled == true)
            {

            }
            orig(self, pickupIndex);
        }
        */



        public class VoidInfestorContainer
        {
            public VoidInfestorContainer(Type type, float _Chance = 1, int _Amount = 6, float _InitialDelay = 0.1f, float _DelayAfterEachInfestor = 0.25f)
            {
                daType = type;
                Amount = _Amount;
                chance = _Chance;
                InitialDelay = _InitialDelay;
                DelayAfterEachInfestor = _DelayAfterEachInfestor;
            }
            public VoidInfestorContainer(Type type, int _Amount = 6, float _InitialDelay = 0.1f, float _DelayAfterEachInfestor = 0.25f)
            {
                daType = type;
                Amount = _Amount;
                InitialDelay = _InitialDelay;
                DelayAfterEachInfestor = _DelayAfterEachInfestor;
                chance = 1;
            }
            public Type daType = typeof(TeleporterInteraction);
            public int Amount = 6;
            public Vector3 Offset = new Vector3(0, 0);
            public float chance = 1;
            public float InitialDelay = 0.1f;
            public float DelayAfterEachInfestor = 0.25f;
            public Func<IInteractable, int> additionalConditional;
            public Func<IInteractable, float> modifyDelay;
        }


        public static Dictionary<ItemTier, int> itemCount = new Dictionary<ItemTier, int>()
        {
            {ItemTier.Boss, 4},
            {ItemTier.Lunar, 4},
            {ItemTier.NoTier, 0},
            {ItemTier.Tier1, 1},
            {ItemTier.Tier2, 3},
            {ItemTier.Tier3, 8},
            {ItemTier.VoidBoss, 6},
            {ItemTier.VoidTier1, 2},
            {ItemTier.VoidTier2, 4},
            {ItemTier.VoidTier3, 10},
            {ItemTier.FoodTier, 6},
        };





        #region Basic Spawn Code

        public static VoidInfestorContainer CanTriggerInfestorSpawn(GameObject gameObject)
        {
            //__ is stupid
            foreach (var container in containers)
            {
                if (gameObject.GetComponent(container.daType) != null)
                {
                    if (UnityEngine.Random.value < container.chance)
                    {
                        return container;
                    }
                }
            }

            return null;
        }

        public static VoidInfestorContainer CanTriggerInfestorSpawn<T>(T interactable) where T : Type
        {
            var a = containers.Where(aie => aie.daType == interactable);
            if (a != null && a.Count() > 0)
            {
                var __ = a.First();
                if (__ != null)
                {
                    if (FunkyMode.Value && UnityEngine.Random.value < 0.3)
                    {
                        if (UnityEngine.Random.value < UnityEngine.Random.value)
                        {
                            return __;
                        }
                    }
                    else if(UnityEngine.Random.value < __.chance)
                    {
                        return __;
                    }
                }
            }
            return null;
        }

        private void GlobalEventManager_OnInteractionsGlobal(Interactor arg1, IInteractable arg2, GameObject arg3)
        {
            if (ArtifactEnabled == true)
            {
                var _ = CanTriggerInfestorSpawn(arg3);
                if (_ != null)
                {
                    arg1.StartCoroutine(Delay(_, arg2, arg3, arg1));
                }
            }
        }

        public IEnumerator Delay(VoidInfestorContainer container, IInteractable interactable, GameObject arg3, MonoBehaviour interactor, int? OverrideAmount = null, float overrideInitialDelay = -1, float overrideDelatAfterEachInfestor = -1, float offsetX = 0, float offsetY = 0, float offsetZ = 0, Transform overrtansTrans = null)
        {
            if (Run.instance.IsExpansionEnabled(RoR2.DLC1Content.Items.BearVoid.requiredExpansion) == false)
            {
                yield break;
            }

            if (arg3 == null)
            {
                Debug.Log("WHAT");
            }
            if (Run.instance.IsExpansionEnabled(RoR2.DLC1Content.Items.BearVoid.requiredExpansion) == false) { yield break; }

            float e = 0; 
            float d = overrideInitialDelay > -1 ? overrideInitialDelay : container != null ? container.InitialDelay : 1;
            if (container != null)
            {
                if (container.modifyDelay != null)
                {
                    d += container.modifyDelay(interactable);
                }
            }

            if (FunkyMode.Value)
            {
                if (UnityEngine.Random.value < 0.05)
                {
                    d = UnityEngine.Random.Range(12, 30);
                }
                else if (UnityEngine.Random.value < 0.5)
                {
                    d = UnityEngine.Random.Range(0.1f, 4);
                }
                else
                {
                    d *= UnityEngine.Random.Range(0.25f, 2f);
                }
            }



            while (e < d)
            {
                e += Time.deltaTime;
                if (arg3 == null) { yield break; }
                yield return null;
            }
            ModelLocator component2 = arg3.GetComponent<ModelLocator>();
            Transform transform2;
            if (component2 == null)
            {
                transform2 = null;
            }
            else
            {
                Transform modelTransform = component2.modelTransform;
                if (modelTransform == null)
                {
                    transform2 = null;
                }
                else
                {
                    ChildLocator component3 = modelTransform.GetComponent<ChildLocator>();
                    transform2 = ((component3 != null) ? component3.FindChild("FireworkOrigin") : null);
                }
            }
            int amount = OverrideAmount ?? container.Amount;
            float daei = overrideDelatAfterEachInfestor > -1 ? overrideDelatAfterEachInfestor : container != null ? container.DelayAfterEachInfestor : 0.25f;
            if (container != null)
            {
                if (OverrideAmount == -1 && container.additionalConditional != null && interactable != null)
                {
                    amount += container.additionalConditional(interactable);
                }
            }

            if (overrtansTrans) { transform2 = overrtansTrans; }

            interactor.StartCoroutine(this.SpawnInfestorAmount(arg3.transform, amount,
                container != null ? container.Offset.x : offsetX,
                container != null ? container.Offset.y : offsetY,
                container != null ? container.Offset.z : offsetZ,
                transform2, daei));
            yield break;
        }


        public IEnumerator SpawnInfestorAmount(Transform transformPosition, int Amount = 1, float x = 0, float y = 0, float z = 0, Transform Th = null, float delay = 0.1f)
        {
            if (ArtifactEnabled == true)
            {
                if (FunkyMode.Value)
                {
                    if (UnityEngine.Random.value < 0.1)
                    {
                        Amount = UnityEngine.Random.Range(Mathf.Max(Amount, 8), Mathf.Max(Amount + 4, 16));
                    }
                    else
                    {
                        Amount = UnityEngine.Random.Range(Mathf.Max(Amount, 1), Mathf.Max(Amount + 4, 5));
                    }

                    if (UnityEngine.Random.value < 0.2)
                    {
                        delay *= 0.01f;
                    }
                    else if (UnityEngine.Random.value < 0.5)
                    {
                        delay = UnityEngine.Random.Range(delay * 0.1f, delay * 10);
                    }
                    else
                    {
                        delay = UnityEngine.Random.Range(delay * 0.33f, delay * 3);
                    }
                }
                if (transformPosition.gameObject == null) { yield break; }
                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Direct,
                    minDistance = 1f,
                    maxDistance = 3f,
                    spawnOnTarget = Th ?? transformPosition.transform,
                    position = Th != null ? Th.position + new Vector3(x, y, z) : transformPosition.position + new Vector3(x, y, z)
                };
                ulong seed = Run.instance.seed ^ (ulong)((long)Run.instance.stageClearCount);
                Xoroshiro128Plus rng = new Xoroshiro128Plus(seed);
                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(CrabSurprise.Value > UnityEngine.Random.value ? voidRaidCrabPhase2 : InfestorPrefab, placementRule, rng);
                directorSpawnRequest.ignoreTeamMemberLimit = true;
                directorSpawnRequest.teamIndexOverride = TeamIndex.Void;
                directorSpawnRequest.onSpawnedServer += OnSp;
                for (int i = 0; i < Amount; i++)
                {
                    float e = 0; float d = delay;
                    while (e < d)
                    {
                        e += Time.deltaTime;
                        if (transformPosition.gameObject == null) { yield break; }
                        yield return null;
                    }
                    DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                }
            }
        }

        public void OnSp(SpawnCard.SpawnResult b)
        {

            if (b.spawnedInstance)
            {
                var body = b.spawnedInstance.GetComponent<CharacterBody>();
                if (body) { body.AddTimedBuff(RoR2Content.Buffs.ArmorBoost, 3.5f); }
            }
        }

        #endregion







        public static List<VoidInfestorContainer> containers = new List<VoidInfestorContainer>()
        {
            new VoidInfestorContainer(typeof(ShrineRestackBehavior), 4, 1, 0.5f),
            new VoidInfestorContainer(typeof(ShrineBossBehavior), 3, 1, 3),
            new VoidInfestorContainer(typeof(ShrinePlaceTotem), 0),
            new VoidInfestorContainer(typeof(ShrineCleanseBehavior), 3, 0.1f, 0.2f), //Cleansing Pool?
            new VoidInfestorContainer(typeof(ShrineCombatBehavior), 3, 0.25f, 0.25f), //Combat Shrine
            new VoidInfestorContainer(typeof(TimedChestController), 12, 1.25f, 0.1f),
            new VoidInfestorContainer(typeof(BarrelInteraction), 0.25f, 1),
            new VoidInfestorContainer(typeof(PortalStatueBehavior), 3, 0.25f, 0.25f),
            new VoidInfestorContainer(typeof(RouletteChestController), 0, 0.1f, 0.25f),
            new VoidInfestorContainer(typeof(ScrapperController), 0, 3.25f, 0.3f),
            new VoidInfestorContainer(typeof(ShrineCombatBehavior), 3, 0.5f, 0.5f),
            new VoidInfestorContainer(typeof(ShrineHealingBehavior), 1, 0.125f, 0f),
            new VoidInfestorContainer(typeof(ShrineRebirthController), 16, 0.5f, 0.025f),


            new VoidInfestorContainer(typeof(ShrineBloodBehavior), 1) { additionalConditional = (_) => 
            {
                if (_ is ShrineBloodBehavior shrine)
                {
                    return shrine.purchaseCount;
                }
                return 0;
            } },

            new VoidInfestorContainer(typeof(VendingMachineBehavior), 1, 1, 0.1f) { additionalConditional = (_) =>
            {
                if (_ is VendingMachineBehavior vending)
                {
                    if (UnityEngine.Random.value < (((float)Mathf.Max(vending.purchaseCount - 4, 0) * 0.0833f)))
                    {
                        return 1;
                    }
                }
                return 0;
            } },

            new VoidInfestorContainer(typeof(ShrineHealingBehavior), 0, 0.25f, 0.5f) { additionalConditional = (_) =>
            {
                if (_ is ShrineHealingBehavior healing)
                {
                    return healing.purchaseCount;
                }
                return 0;
            } },

            new VoidInfestorContainer(typeof(TeleporterInteraction), 8, 5, 2.5f) { additionalConditional = (_) =>
            {
                if (_ is TeleporterInteraction healing)
                {
                    return healing.shrineBonusStacks * 4;
                }
                return 0;
            } },



            new VoidInfestorContainer(typeof(ShopTerminalBehavior), 0, 0.1f, 0.1f){ modifyDelay = (_) => 
            {
                if (_ is ShopTerminalBehavior t)
                {
                    if (t.gameObject.name.Contains("Duplicator")) { return 2.66f; }
                }
                return 0;
            },     
            additionalConditional = (_) =>
            {
                if (_ is ShopTerminalBehavior shop)
                {
                    return GetAmountFromPickupIndex(shop.pickupDisplay.pickupState);
                }

                return 0;
            }},


            new VoidInfestorContainer(typeof(ChestBehavior), 0, 0.5f, 0.1f){ additionalConditional = (_) =>
            {
                if (_ is ChestBehavior chest)
                {
                    return GetAmountFromPickupIndex(chest.currentPickup);
                }
                return 0;
            }},

            new VoidInfestorContainer(typeof(OptionChestBehavior), 0, 0.1f, 0.33f){ additionalConditional = (_) =>
            {
                return 0;
            }},
        };

        public static int GetAmountFromPickupIndex(UniquePickup uniquePickup)
        {
            return GetAmountFromPickupIndex(uniquePickup.pickupIndex);
        }
        public static int GetAmountFromPickupIndex(PickupIndex pickupIndex)
        {
            int i = 0;
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef != null && (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None || pickupDef.itemTier != ItemTier.NoTier))
            {

                itemCount.TryGetValue(pickupDef.itemTier, out i);
                if (RoR2.EquipmentCatalog.allEquipment.Contains(pickupDef.equipmentIndex))
                {
                    i += 3;
                }
            }
            return i;
        }
        public static int GetAmountFromPickupIndex(ItemTier tier)
        {
            int i = 0;
            itemCount.TryGetValue(tier, out i);
            return i;
        }


        #region Converting every fucking thing to a Harmony Patch / Transpiler
        [HarmonyPatch(typeof(EntityStates.DeepVoidPortalBattery.Charging), nameof(EntityStates.DeepVoidPortalBattery.Charging.OnEnter))]
        public class Charging_OnEnter_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(EntityStates.DeepVoidPortalBattery.Charging __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    if (VoidStageMissionController.instance)
                    {
                        VoidStageMissionController.instance.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, null, __instance.gameObject, VoidStageMissionController.instance, 8, 3, 5));
                    }
                }
            }
        }
        [HarmonyPatch(typeof(EntityStates.Missions.Arena.NullWard.WardOnAndReady), nameof(EntityStates.Missions.Arena.NullWard.WardOnAndReady.OnEnter))]
        public class WardOnAndReady_OnEnter_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(EntityStates.Missions.Arena.NullWard.WardOnAndReady __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    __instance.purchaseInteraction.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, null, __instance.purchaseInteraction.gameObject, VoidStageMissionController.instance, 8, 3, 5));

                }
            }
        }
        [HarmonyPatch(typeof(EntityStates.Missions.Moon.MoonBatteryActive), nameof(EntityStates.Missions.Moon.MoonBatteryActive.OnEnter))]
        public class MoonBatteryActive_OnEnter_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(EntityStates.Missions.Moon.MoonBatteryActive __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    __instance.chargeIndicatorController.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, null, __instance.gameObject, __instance.chargeIndicatorController, 5, 5, 6f));

                }
            }
        }
        [HarmonyPatch(typeof(RoR2.RouletteChestController), nameof(RoR2.RouletteChestController.EjectPickupServer))]
        public class RouletteChestController_EjectPickupServer_Patch
        {
            [HarmonyPrefix]
            public static void Postfix(RoR2.RouletteChestController __instance, UniquePickup pickup)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    var _ = CanTriggerInfestorSpawn(__instance.gameObject);
                    if (_ != null)
                    {
                        int i = ArtifactOfInfestation.GetAmountFromPickupIndex(pickup);
                        __instance.StartCoroutine(ArtifactOfInfestation.Inst.Delay(_, __instance as IInteractable, __instance.gameObject, __instance, i));
                    }
                }
            }
        }
        [HarmonyPatch(typeof(RoR2.ScrapperController), nameof(RoR2.ScrapperController.PopRewardPickupQueue))]
        public class ScrapperController_PopRewardPickupQueue_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(RoR2.ScrapperController __instance, ref UniquePickup __result)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    var _ = CanTriggerInfestorSpawn(__instance.gameObject);
                    if (_ != null)
                    {
                        int i = ArtifactOfInfestation.GetAmountFromPickupIndex(__result);
                        __instance.StartCoroutine(ArtifactOfInfestation.Inst.Delay(_, __instance as IInteractable, __instance.gameObject, __instance, i));
                    }
                }
            }
        }



        [HarmonyPatch]
        private static class ChestBehaviorBaseItemDrop
        {
            [HarmonyPatch(typeof(ChestBehavior), nameof(ChestBehavior.BaseItemDrop))]
            [HarmonyILManipulator]
            private static void Bomk(ILContext il)
            {
                ILCursor cursor = new ILCursor(il);

                if (!cursor.TryGotoNext(MoveType.After,
                    instr => instr.MatchCall<RoR2.PickupDropletController>("CreatePickupDroplet")))

                    return;



                cursor.Emit(OpCodes.Ldarg, 0);
                cursor.Emit(OpCodes.Ldloc, 4);
                cursor.Emit(OpCodes.Call, typeof(ChestBehaviorBaseItemDrop).GetMethod("Pop", BindingFlags.Static | BindingFlags.NonPublic));

                //cursor.Emit(OpCodes.Ldarg_1);
            }
            private static void Pop(ChestBehavior chestBehavior, GenericPickupController.CreatePickupInfo pop)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled)
                {
                    int am = GetAmountFromPickupIndex(pop.pickup);
                    chestBehavior.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, chestBehavior as IInteractable, chestBehavior.gameObject, chestBehavior, am, 0.25f));
                }
            }
        }

        [HarmonyPatch(typeof(RoR2.HalcyoniteShrineInteractable), nameof(RoR2.HalcyoniteShrineInteractable.DrainConditionMet))]
        public class HalcyoniteShrineInteractable_DrainConditionMet_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(RoR2.HalcyoniteShrineInteractable __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    var _ = CanTriggerInfestorSpawn(__instance.gameObject);
                    if (_ != null)
                    {
                        ArtifactsVoidPlugin.Inst.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, __instance as IInteractable, __instance.gameObject, __instance, __instance.rewardOptionCount, 0.25f));
                    }
                }
            }
        }



        [HarmonyPatch]
        private static class OptionChestBehaviorItemDrop
        {
            [HarmonyPatch(typeof(OptionChestBehavior), nameof(OptionChestBehavior.ItemDrop))]
            [HarmonyILManipulator]
            private static void Bomk(ILContext il)
            {
                ILCursor cursor = new ILCursor(il);

                if (!cursor.TryGotoNext(MoveType.After,
                    instr => instr.MatchCall<UnityEngine.Networking.NetworkServer>("get_active"),
                    instr => instr.MatchLdstr("[Server] function 'System.Void RoR2.OptionChestBehavior::ItemDrop()' called on client"),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdloc(0),
                    instr => instr.MatchLdloc(0),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdarg(0))) ///I have no idea if OptionChestBehavior is actually used anywhere in the game but I am gonna do this just in case

                    return;
                cursor.Emit(OpCodes.Ldloc, 0);
                cursor.Emit(OpCodes.Ldarg, 0);
                cursor.Emit(OpCodes.Call, typeof(OptionChestBehaviorItemDrop).GetMethod("Pop", BindingFlags.Static | BindingFlags.NonPublic));
            }
            private static void Pop(OptionChestBehavior chestBehavior, GenericPickupController.CreatePickupInfo pop)
            {
                int am = GetAmountFromPickupIndex(pop.pickup);
                if (ArtifactOfInfestation.Inst.ArtifactEnabled)
                {
                    chestBehavior.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, chestBehavior as IInteractable, chestBehavior.gameObject, chestBehavior, am, 0.25f));
                }
            }
        }


        [HarmonyPatch(typeof(RoR2.ShopTerminalBehavior), "DropPickup", new Type[] { typeof(bool),  })]
        public class ShopTerminalBehavior_DropPickup_Patch
        {
            [HarmonyPrefix]
            public static void Postfix(RoR2.ShopTerminalBehavior __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    int am = GetAmountFromPickupIndex(__instance.pickup);
                    __instance.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, __instance as IInteractable, __instance.gameObject, __instance, am, 0.25f));
                }
            }
        }
        [HarmonyPatch(typeof(RoR2.DroneVendorTerminalBehavior), nameof(RoR2.DroneVendorTerminalBehavior.DispatchDrone))]
        public class DroneVendorTerminalBehavior_DispatchDrone_Patch
        {
            [HarmonyPrefix]
            public static void Postfix(RoR2.DroneVendorTerminalBehavior __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    PickupDef pickupDef = PickupCatalog.GetPickupDef(__instance._cachedPickup.pickupIndex);
                    DroneDef droneDef = DroneCatalog.GetDroneDef(pickupDef.droneIndex);
                    int am = GetAmountFromPickupIndex(droneDef.tier);
                    __instance.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, __instance as IInteractable, __instance.gameObject, __instance, am, 0.25f));
                }
            }
        }

        [HarmonyPatch]
        private static class ShrineChanceBehaviorAddShrineStack
        {
            [HarmonyPatch(typeof(ShrineChanceBehavior), nameof(ShrineChanceBehavior.AddShrineStack))]
            [HarmonyILManipulator]
            private static void iojueafdijonefdijnlfda(ILContext il)
            {
                ILCursor cursor = new ILCursor(il);

                if (!cursor.TryGotoNext(MoveType.After,
                    
                    instr => instr.MatchLdstr("SHRINE_CHANCE_SUCCESS_MESSAGE")))
                    return;

                if (!cursor.TryGotoNext(MoveType.After,

                    instr => instr.MatchLdcR4(20)))
                    return;

                //instr => instr.MatchLdcR4(20))

                cursor.Emit(OpCodes.Ldarg, 0);
                cursor.Emit(OpCodes.Ldloc, 0);
                cursor.Emit(OpCodes.Call, typeof(ShrineChanceBehaviorAddShrineStack).GetMethod("HEEELP", BindingFlags.Static | BindingFlags.NonPublic));
            }
            private static void HEEELP(ShrineChanceBehavior chestBehavior, UniquePickup pop)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled)
                {
                    int am = GetAmountFromPickupIndex(pop);
                    chestBehavior.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, chestBehavior as IInteractable, chestBehavior.gameObject, chestBehavior, am, 0.25f));
                }
            }
        }

        [HarmonyPatch]
        private static class ScrapperScrappingToIdleOnEnter
        {
            [HarmonyPatch(typeof(EntityStates.Scrapper.ScrappingToIdle), nameof(EntityStates.Scrapper.ScrappingToIdle.OnEnter))]
            [HarmonyILManipulator]
            private static void fuck(ILContext il)
            {
                ILCursor cursor = new ILCursor(il);

                if (!cursor.TryGotoNext(MoveType.After,

                    instr => instr.MatchLdstr("Base")))
                    return;

                if (!cursor.TryGotoNext(MoveType.After,

                    instr => instr.MatchCall<UnityEngine.Networking.NetworkServer>("get_active")))
                    return;



                if (!cursor.TryGotoNext(MoveType.After,

                    instr => instr.MatchCall<RoR2.PickupDropletController>("CreatePickupDroplet")))
                    return;

                //instr => instr.MatchLdcR4(20))

                cursor.Emit(OpCodes.Ldarg, 0);
                cursor.Emit(OpCodes.Ldloc, 0);
                cursor.Emit(OpCodes.Call, typeof(ScrapperScrappingToIdleOnEnter).GetMethod("HEEELP", BindingFlags.Static | BindingFlags.NonPublic));
            }
            private static void HEEELP(EntityStates.Scrapper.ScrappingToIdle chestBehavior, UniquePickup pop)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled)
                {
                    int am = GetAmountFromPickupIndex(pop);
                    chestBehavior.scrapperController.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, chestBehavior.scrapperController as IInteractable, chestBehavior.gameObject, chestBehavior.scrapperController, am, 0.25f));
                }
            }
        }


        [HarmonyPatch(typeof(EntityStates.DroneScrapper.DroneScrappingToIdle), nameof(EntityStates.DroneScrapper.DroneScrappingToIdle.DropPickup))]
        public class DroneScrappingToIdle_DropPickup
        {
            [HarmonyPostfix]
            public static void Postfix(EntityStates.DroneScrapper.DroneScrappingToIdle __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    int am = GetAmountFromPickupIndex(__instance.scrapperController.lastScrappedTier);
                    __instance.scrapperController.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, __instance.scrapperController as IInteractable, __instance.scrapperController.gameObject, __instance.scrapperController, am, 0.125f));
                }
            }
        }

        [HarmonyPatch(typeof(EntityStates.Geode.GeodeShatter), nameof(EntityStates.Geode.GeodeShatter.OnEnter))]
        public class EntityStatesGeode_GeodeShatter_OnEnter
        {
            [HarmonyPostfix]
            public static void Postfix(EntityStates.Geode.GeodeShatter __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    int am = 1;
                    if (__instance.geodeController.ShouldDropReward)
                    {

                        am += 3;
                    }
                    if (__instance.geodeController.ShouldRegenerate)
                    {
                        am /= 3;
                    }
                    am = Mathf.Max(1, am);
                    __instance.geodeController.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, __instance.geodeController as IInteractable, __instance.geodeController.gameObject, __instance.geodeController, am, 0.125f));
                }
            }
        }
        [HarmonyPatch(typeof(EntityStates.Duplicator.Duplicating), nameof(EntityStates.Duplicator.Duplicating.DropDroplet))]
        public class EntityStatesDuplicatorDuplicating_DropDroplet
        {
            [HarmonyPostfix]
            public static void Postfix(EntityStates.Duplicator.Duplicating __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    var a = __instance.GetComponent<ShopTerminalBehavior>();
                    int am = GetAmountFromPickupIndex(a.pickup);
                    a.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, a as IInteractable, __instance.gameObject, a, am * Mathf.Max(1, a.debt * a.dropAmount), 0.25f));
                }
            }
        }


        [HarmonyPatch]
        private static class PickupDistributorBehaviorDrop
        {
            [HarmonyPatch(typeof(PickupDistributorBehavior), nameof(PickupDistributorBehavior.Drop))]
            [HarmonyILManipulator]
            private static void fuck(ILContext il)
            {
                ILCursor cursor = new ILCursor(il);

                if (!cursor.TryGotoNext(MoveType.After,

                    instr => instr.MatchConvI4()))
                    return;

                if (!cursor.TryGotoNext(MoveType.After,

                    instr => instr.MatchLdnull()))
                    return;



                if (!cursor.TryGotoNext(MoveType.After,

                    instr => instr.MatchLdfld<RoR2.PickupDistributorBehavior>("itemsDropped")))
                    return;

                if (!cursor.TryGotoNext(MoveType.After,

                    instr => instr.MatchCall<RoR2.PickupDropletController>("CreatePickupDroplet")))
                    return;

                cursor.Emit(OpCodes.Ldarg, 0);
                cursor.Emit(OpCodes.Ldloc, 4);
                cursor.Emit(OpCodes.Call, typeof(PickupDistributorBehaviorDrop).GetMethod("HEEELP", BindingFlags.Static | BindingFlags.NonPublic));
            }
            private static void HEEELP(PickupDistributorBehavior chestBehavior, RoR2.GenericPickupController.CreatePickupInfo pop)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled)
                {
                    int am = GetAmountFromPickupIndex(pop.pickup);
                    chestBehavior.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, chestBehavior as IInteractable, chestBehavior.gameObject, chestBehavior, am, 0.25f));
                }
            }
        }
        [HarmonyPatch(typeof(EntityStates.Missions.RepurposedCrater.PowerOrbPedestalManager), nameof(EntityStates.Missions.RepurposedCrater.PowerOrbPedestalManager.DoInteractResult))]
        public class PowerOrbPedestalManager_DoInteractResult
        {
            [HarmonyPostfix]
            public static void Postfix(EntityStates.Missions.RepurposedCrater.PowerOrbPedestalManager __instance)
            {
                if (ArtifactOfInfestation.Inst.ArtifactEnabled == true)
                {
                    __instance.StartCoroutine(ArtifactOfInfestation.Inst.Delay(null, __instance as IInteractable, __instance.gameObject, __instance, 5, 1f, 0.5f));
                }
            }
        }

        /*


        */

        #endregion

    }
}
