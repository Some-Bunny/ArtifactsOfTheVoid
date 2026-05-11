using RoR2;
using UnityEngine;
using BepInEx.Configuration;
using RoR2.Projectile;
using System.Collections.Generic;
using RoR2.Audio;
using UnityEngine.Networking;
using System.Collections.ObjectModel;
using HG;
using R2API;



namespace ArtifactsOfTheVoid.Artifacts
{
    public class ArtifactOfNullification
    {
        private static string ArtifactLangTokenName => "ARTIFACT_OF_NULLIFY";
        private static string ArtifactName => "Artifact of Nullification";
        private static string ArtifactDescription => "When enabled, causes every non-Void enemy to spawn a random Void-related implosion on death.";

        private static ArtifactDef NullificationArtifactDefinition;
        public bool ArtifactEnabled => RunArtifactManager.instance.IsArtifactEnabled(NullificationArtifactDefinition);

        public WeightedTypeCollection<int> collection = new WeightedTypeCollection<int>();
        public WeightedTypeCollection<int> collectionTwo = new WeightedTypeCollection<int>();


        public void Init()
        {

            LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_NAME", ArtifactName);
            LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION", ArtifactDescription);

            NullificationArtifactDefinition = ScriptableObject.CreateInstance<ArtifactDef>();


            var texEn = Tools.GetTextureFromResource("ArtifactsOfTheVoid/Textures/nullifyEnabled.png");
            var _SpriteEn = Sprite.Create(texEn, new Rect(0, 0, texEn.width, texEn.height), new Vector2(0.5f, 0.5f));

            var texDis = Tools.GetTextureFromResource("ArtifactsOfTheVoid/Textures/nullifyDisabled.png");
            var _SpriteDis = Sprite.Create(texDis, new Rect(0, 0, texDis.width, texDis.height), new Vector2(0.5f, 0.5f));


            NullificationArtifactDefinition.smallIconSelectedSprite = _SpriteEn;
            NullificationArtifactDefinition.smallIconDeselectedSprite = _SpriteDis;

            NullificationArtifactDefinition.cachedName = "ARTIFACT_" + ArtifactLangTokenName;
            NullificationArtifactDefinition.nameToken = "ARTIFACT_" + ArtifactLangTokenName + "_NAME";
            NullificationArtifactDefinition.descriptionToken = "ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION";
            GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
            ContentAddition.AddArtifactDef(NullificationArtifactDefinition);
            CreateConfig(ArtifactsVoidPlugin.configurationFile);

            collection.elements = new WeightedType<int>[]
            {
                CreateWeightedType(1, VoidReaverBombCardAmount.Value),
                CreateWeightedType(2, VoidDevastatorBombCardAmount.Value),
                CreateWeightedType(3, VoidJailerBombCardAmount.Value),
                CreateWeightedType(4, VoidlingBombCardAmount.Value),
            };
            collectionTwo.elements = new WeightedType<int>[]
            {
                CreateWeightedType(1, VoidReaverBombCardAmountChampion.Value),
                CreateWeightedType(2, VoidJailerBombCardAmountChampion.Value),
                CreateWeightedType(3, VoidDevastatorBombCardAmountChampion.Value),
                CreateWeightedType(4, VoidlingBombCardAmountChampion.Value),
            };
            DummyObject = PrefabAPI.InstantiateClone(new GameObject("DummyObject_Voidling"), "VOIDLING_STORM_DUMMY_COPY", true);
            DummyObject.AddComponent<StormSelfController>();
            DummyObject.AddComponent<NetworkIdentity>();
            PrefabAPI.RegisterNetworkPrefab(DummyObject);
            DummyObject.RegisterNetworkPrefab();

        }

        public static GameObject DummyObject;

        public WeightedType<int> CreateWeightedType(int value, int weight)
        {
            WeightedType<int> type = new WeightedType<int>();
            type.value = value;
            type.Weight = weight;
            return type;
        }

        private void CreateConfig(ConfigFile config)
        {

            VoidReaverBombCardAmount = config.Bind<int>("Artifact: " + ArtifactName, "Void Reaver Bomb Weight", 90, "The amount of weight that a Void Reaver Implosion has for when a death-roll is initiated.");
            VoidDevastatorBombCardAmount = config.Bind<int>("Artifact: " + ArtifactName, "Void Devastator Bomb Weight", 19, "The amount of weight that a Void Jailer Implosion has for when a death-roll is initiated.");
            VoidJailerBombCardAmount = config.Bind<int>("Artifact: " + ArtifactName, "Void Jailer Bomb Weight", 39, "The amount of weight that a Void Devastator Implosion has for when a death-roll is initiated.");
            VoidlingBombCardAmount = config.Bind<int>("Artifact: " + ArtifactName, "Voidling Storm Bomb Weight", 1, "The amount of weight that a Voidling Storm has for when a death-roll is initiated.");

            VoidReaverBombCardAmountChampion = config.Bind<int>("Artifact: " + ArtifactName, "Void Reaver Bomb Weight (Champion Enemies)", 40, "The amount of weight that a Void Reaver Implosion has for when a death-roll is initiated on a Champion enemy.");
            VoidJailerBombCardAmountChampion = config.Bind<int>("Artifact: " + ArtifactName, "Void Devastator Bomb Weight (Champion Enemies)", 10, "The amount of weight that a Void Jailer Implosion has for when a death-roll is initiated on a Champion enemy.");
            VoidDevastatorBombCardAmountChampion = config.Bind<int>("Artifact: " + ArtifactName, "Void Jailer Bomb Weight (Champion Enemies)", 20, "The amount of weight that a Void Devastator Implosion has for when a death-roll is initiated on a Champion enemy.");
            VoidlingBombCardAmountChampion = config.Bind<int>("Artifact: " + ArtifactName, "Voidling Storm Bomb Weight (Champion Enemies)", 5, "The amount of weight that a Voidling Storm has for when a death-roll is initiated on a Champion enemy.");

            StormTime = config.Bind<float>("Artifact: " + ArtifactName, "Voidling Black Hole Lifetime", 15, "How long does the Black Hole last? Note, the longer the lifetime the larger it will become. Default is 15 seconds.");

            NonEnemyDeathSpawnBlackHoles = config.Bind<bool>("Artifact: " + ArtifactName, "Non-Enemies Spawn Void Implosions", false, "Do non-enemy entities spawn Void Implosions?");
            DeadPlayersSpawnImplosions = config.Bind<bool>("Artifact: " + ArtifactName, "Killed Players Spawn Void Implosions", true, "Do players spawn implosions on death?");
            DeadPlayerFinalBreath = config.Bind<bool>("Artifact: " + ArtifactName, "Players Retaliate Heavily", false, "Does player death now have higher consequences for everything around you?");
        }
        public static ConfigEntry<int> VoidReaverBombCardAmount;
        public static ConfigEntry<int> VoidJailerBombCardAmount;
        public static ConfigEntry<int> VoidDevastatorBombCardAmount;
        public static ConfigEntry<int> VoidlingBombCardAmount;

        public static ConfigEntry<int> VoidReaverBombCardAmountChampion;
        public static ConfigEntry<int> VoidJailerBombCardAmountChampion;
        public static ConfigEntry<int> VoidDevastatorBombCardAmountChampion;
        public static ConfigEntry<int> VoidlingBombCardAmountChampion;

        public static ConfigEntry<bool> NonEnemyDeathSpawnBlackHoles;
        public static ConfigEntry<bool> DeadPlayersSpawnImplosions;
        public static ConfigEntry<bool> DeadPlayerFinalBreath;
        public static ConfigEntry<float> StormTime;



        private void OnCharacterDeathGlobal(DamageReport damageReport)
        {
            if (ArtifactEnabled == true && damageReport.victimBody.teamComponent.teamIndex != TeamIndex.Void && damageReport.damageInfo.damageType != DamageType.VoidDeath)
            {
                if (Run.instance.IsExpansionEnabled(RoR2.DLC1Content.Items.BearVoid.requiredExpansion) == false) { return; }

                bool Spawn = true;
                if (damageReport.victimBody.teamComponent.teamIndex == TeamIndex.Neutral)//  BlackListIndexes.Contains(damageReport.victimBody.bodyIndex))
                {
                    Spawn = NonEnemyDeathSpawnBlackHoles.Value == true;
                }
                if (damageReport.victimBody.isPlayerControlled)
                {
                    Spawn = DeadPlayersSpawnImplosions.Value == true;
                }

                if (Spawn)
                {
                    if (damageReport.victimBody.isPlayerControlled && DeadPlayerFinalBreath.Value == true)
                    {
                        SpawnTheStorm(damageReport);
                    }
                    else if(damageReport.victimBody.isChampion)
                    {
                        int obj = collectionTwo.SelectByWeight();
                        switch (obj)
                        {
                            case 1:
                                SpawnVoidBomb(EntityStates.NullifierMonster.DeathState.deathBombProjectile, damageReport);
                                break;
                            case 2:
                                SpawnVoidBomb(EntityStates.VoidMegaCrab.DeathState.deathBombProjectile, damageReport);
                                break;
                            case 3:
                                SpawnVoidBomb(EntityStates.VoidJailer.DeathState.deathBombProjectile, damageReport);
                                break;
                            case 4:
                                SpawnTheStorm(damageReport);
                                break;
                        }
                    }
                    else
                    {
                        int obj = collection.SelectByWeight();
                        switch (obj)
                        {
                            case 1:
                                SpawnVoidBomb(EntityStates.NullifierMonster.DeathState.deathBombProjectile, damageReport);
                                break;
                            case 2:
                                SpawnVoidBomb(EntityStates.VoidMegaCrab.DeathState.deathBombProjectile, damageReport);
                                break;
                            case 3:
                                SpawnVoidBomb(EntityStates.VoidJailer.DeathState.deathBombProjectile, damageReport);
                                break;
                            case 4:
                                SpawnTheStorm(damageReport);
                                break;
                        }
                    }


                }

                
            }
        }

        public void SpawnVoidBomb(GameObject projectilePrefab, DamageReport killedEntity)
        {
            FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
            fireProjectileInfo.projectilePrefab = projectilePrefab;
            fireProjectileInfo.position = killedEntity.victimBody.gameObject.transform.position;
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Vector3.up);
            fireProjectileInfo.owner = killedEntity.victimBody.gameObject;
            fireProjectileInfo.crit = killedEntity.victimBody.RollCrit();
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }
        public static void SpawnTheStorm(DamageReport killedEntity)
        {

            GameObject dummy = GameObject.Instantiate(DummyObject, killedEntity.victimBody.gameObject.transform.position, Quaternion.identity);

            var attack = new EntityStates.VoidRaidCrab.VacuumAttack();
            dummy.transform.position = killedEntity.victimBody.gameObject.transform.position;
            StormSelfController controller = dummy.GetComponent<StormSelfController>();
            controller.killRadius = 1;
            controller.killRadiusCurve = EntityStates.VoidRaidCrab.VacuumAttack.killRadiusCurve;
            controller.loopSound = EntityStates.VoidRaidCrab.VacuumAttack.loopSound;
            controller.losObstructionFactor = EntityStates.VoidRaidCrab.VacuumAttack.losObstructionFactor;
            controller.pullMagnitudeCurve = EntityStates.VoidRaidCrab.VacuumAttack.pullMagnitudeCurve;
            if (NetworkServer.active) { NetworkServer.Spawn(dummy); }

        }
    }
    public class StormSelfController : NetworkBehaviour
    {
        public void Start()
        {
            //Debug.Log("Is this thing even starting?");
            this.losTracker = new CharacterLosTracker();
            this.losTracker.enabled = true;
            this.killSphereVfxHelper = VFXHelper.Rent();
            this.killSphereVfxHelper.vfxPrefabReference = EntityStates.VoidRaidCrab.VacuumAttack.killSphereVfxPrefab;
            pos = this.transform.position;


            this.killSphereVfxHelper.followedTransform = base.transform;
            this.killSphereVfxHelper.useFollowedTransformScale = false;
            this.killSphereVfxHelper.enabled = true;
            this.UpdateKillSphereVfx();
            this.environmentVfxHelper = VFXHelper.Rent();
            this.environmentVfxHelper.vfxPrefabReference = EntityStates.VoidRaidCrab.VacuumAttack.environmentVfxPrefab;


            this.environmentVfxHelper.followedTransform = base.transform;
            this.environmentVfxHelper.useFollowedTransformScale = false;
            this.environmentVfxHelper.enabled = true;



            this.loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, EntityStates.VoidRaidCrab.VacuumAttack.loopSound);
            if (NetworkServer.active)
            {
                this.killSearch = new SphereSearch();
            }
        }

        private bool ServerActive
        {
            get
            {
                return NetworkServer.active;
            }
        }
        bool active = true;
        public void DestroyBomb()
        {
            active = false;
            this.killSphereVfxHelper = VFXHelper.Return(this.killSphereVfxHelper);
            this.environmentVfxHelper = VFXHelper.Return(this.environmentVfxHelper);
            this.losTracker.enabled = false;
            this.losTracker.Dispose();
            this.losTracker = null;
            LoopSoundManager.StopSoundLoopLocal(this.loopPtr);
            Destroy(this.gameObject);
        }

        private float timeSpent;


        public float TimeInSecondsPassed()
        {
            return timeSpent;
        }

        public void Update()
        {
            if (this == null) { return; }
            if (active == false) { return; }

            if (PauseManager.isPaused == false)
            {
                timeSpent += Time.fixedDeltaTime;
            }
            if (timeSpent > ArtifactOfNullification.StormTime.Value)
            {
                DestroyBomb();
                return;
            }
            this.killRadius = TimeInSecondsPassed() * 2f;
            this.UpdateKillSphereVfx();
            Vector3 position = pos;
            this.losTracker.origin = position;
            this.losTracker.Step();
            float num = TimeInSecondsPassed() * 0.8f;

            ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
            for (int i = 0; i < readOnlyInstancesList.Count; i++)
            {
                CharacterBody characterBody = readOnlyInstancesList[i];
                bool flag = this.losTracker.CheckBodyHasLos(characterBody);
                if (characterBody.hasEffectiveAuthority)
                {
                    IDisplacementReceiver component = characterBody.GetComponent<IDisplacementReceiver>();
                    if (component != null)
                    {
                        float num2 = flag ? 1f : this.losObstructionFactor;
                        component.AddDisplacement((position - characterBody.coreTransform.position).normalized * (num * num2 * Time.fixedDeltaTime));
                    }
                }
            }



            if (ServerActive == true)
            {
                List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
                List<HealthComponent> list2 = CollectionPool<HealthComponent, List<HealthComponent>>.RentCollection();


                try
                {
                    this.killSearch.radius = this.killRadius;
                    this.killSearch.origin = position;
                    this.killSearch.mask = LayerIndex.entityPrecise.mask;
                    this.killSearch.RefreshCandidates();
                    this.killSearch.OrderCandidatesByDistance();
                    this.killSearch.FilterCandidatesByDistinctHurtBoxEntities();
                    this.killSearch.GetHurtBoxes(list);
                    for (int j = 0; j < list.Count; j++)
                    {
                        HurtBox hurtBox = list[j];
                        if (hurtBox.healthComponent)
                        {
                            list2.Add(hurtBox.healthComponent);
                        }
                    }
                    for (int k = 0; k < list2.Count; k++)
                    {
                        HealthComponent healthComponent = list2[k];
                        healthComponent.Suicide(null, null, DamageType.VoidDeath);
                    }


                }
                finally
                {
                    list2 = CollectionPool<HealthComponent, List<HealthComponent>>.ReturnCollection(list2);
                    list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
                }
            }
        }

        private void UpdateKillSphereVfx()
        {
            if (this.killSphereVfxHelper.vfxInstanceTransform)
            {
                this.killSphereVfxHelper.vfxInstanceTransform.localScale = Vector3.one * this.killRadius;
            }
        }

        //Literally just stolen from Voidling lmao
        public AnimationCurve killRadiusCurve;
        public AnimationCurve pullMagnitudeCurve;
        public float losObstructionFactor = 0.5f;
        public GameObject killSphereVfxPrefab;
        public GameObject environmentVfxPrefab;
        public LoopSoundDef loopSound;
        public CharacterLosTracker losTracker;
        public VFXHelper killSphereVfxHelper;
        public VFXHelper environmentVfxHelper;
        public SphereSearch killSearch;
        public float killRadius = 1f;
        public Vector3 pos;
        public LoopSoundManager.SoundLoopPtr loopPtr;
    }
}
