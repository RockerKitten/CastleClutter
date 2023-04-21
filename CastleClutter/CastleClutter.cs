using System.Reflection;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fastJSON;
using BepInEx.Bootstrap;

namespace CastleClutter
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("com.RockerKitten.CastleScepter", BepInDependency.DependencyFlags.SoftDependency)]

    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class CastleClutter : BaseUnityPlugin
    {
        public const string PluginGUID = "com.RockerKitten.CastleClutter";
        public const string PluginName = "CastleClutter";
        public const string PluginVersion = "1.0.0";
        private static ItemDrop fuelWood;
        private static ItemDrop fuelResin;
        private static string TableName;
        private static string CategoryTabName = "Clutter";
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private AssetBundle BuildItAssetBundle { get; set; }
        //private AssetBundle BuildItAssetBundle2 { get; set; }
        //private AudioSource fireAudioSource;

        private Dictionary<BuildItMaterial, BuildItEffectLists> effects;

        private void Awake()
        {
            LoadEmbeddedAssembly("fastJSON.dll");
            this.BuildItAssetBundle = AssetUtils.LoadAssetBundleFromResources("rkc_clutter", Assembly.GetExecutingAssembly());
            //this.BuildItAssetBundle2 = AssetUtils.LoadAssetBundleFromResources("rkc_sign", Assembly.GetExecutingAssembly());
            AddLocalizations();
            PrefabManager.OnVanillaPrefabsAvailable += SetupAssets;
            Jotunn.Logger.LogInfo("CastleClutter has landed");
        }

        private void SetupAssets()
        {

            fuelResin = PrefabManager.Cache.GetPrefab<GameObject>("Resin").GetComponent<ItemDrop>();
            fuelWood = PrefabManager.Cache.GetPrefab<GameObject>("Wood").GetComponent<ItemDrop>();
            
            this.effects = InitializeEffects();
            InitializeBuildItConstructionTools();
            InitializeBuildItAssets();
            PrefabManager.OnVanillaPrefabsAvailable -= SetupAssets;
        }

        private void InitializeBuildItConstructionTools()
        {
            if (Chainloader.PluginInfos.ContainsKey("com.RockerKitten.CastleScepter")||Chainloader.PluginInfos.ContainsKey("com.RockerKitten.CastleStructure")) 
            {
                TableName = "_RKC_CustomTable";
            }
            else 
            {
                TableName = "_HammerPieceTable";
            }

        }
        private void AddLocalizations()
        {
            Localization = LocalizationManager.Instance.GetLocalization();
            Localization.AddTranslation("English", new Dictionary<String, String>
            {
                {"piece_rkc_alchemy","Alchemy Clutter"},{"piece_rkc_banner","Banner"},{"piece_rkc_barrels","Barrels"},{"piece_rkc_bed","Bed"},
                {"piece_rkc_bench","Bench"},{"piece_rkc_books","Books"},{"piece_rkc_bookshelf","Bookshelf"},{"piece_rkc_bottle","Bottle"},
                {"piece_rkc_bowl","Bowl"},{"piece_rkc_broom","Broom"},{"piece_rkc_candle","Candle"},{"piece_rkc_candlestand","Candlestand"},
                {"piece_rkc_chair","Chair"},{"piece_rkc_chest","Chest"},{"piece_rkc_crystal","Crystal"},{"piece_rkc_crystallamp","Crystal Lamp"},
                {"piece_rkc_curtain","Curtain"},{"piece_rkc_fountain","Fountain"},{"piece_rkc_chandelier","Chandelier"},
                {"piece_rkc_inkwell","Inkwell"},{"piece_rkc_hanginglamp","Hanging Lamp"},{"piece_rkc_lamppost","Lamp Post"},{"piece_rkc_pan","Pan"},
                {"piece_rkc_pedestal","Pedestal"},{"piece_rkc_plate","Plate"},{"piece_rkc_podium","Podium"},{"piece_rkc_rug","Rug"},
                {"piece_rkc_scorpion","Scorpion"},{"piece_rkc_wallshelf","Wall Shelf"},{"piece_rkc_sign","Sign"},{"piece_rkc_statue","Statue"},
                {"piece_rkc_table","Table"},{"piece_rkc_sidetable","Side Table"},{"piece_rkc_throne","Throne"},{"piece_rkc_torch","Torch"},
                {"piece_rkc_tree","Tree"},{"piece_rkc_urn","Urn"},{"piece_rkc_walldeco","Wall Decorations"},
                {"piece_rkc_wand","Wand"},{"piece_rkc_weaprack","Weapons Rack"}
                //{"piece_rkc_trowel",""},
            });
        }

        private void InitializeBuildItAssets()
        {
            var buildItAssets = LoadEmbeddedJsonFile<BuildItAssets>("CastleClutter.json");
                foreach (var buildItPiece in buildItAssets.Pieces)
                    {
                        var customPiece = this.BuildCustomPiece(buildItPiece);

                        // load supplemental assets (sfx and vfx)
                        this.AttachEffects(customPiece.PiecePrefab, buildItPiece);

                        PieceManager.Instance.AddPiece(customPiece);
                    }
               
        }

        private Dictionary<BuildItMaterial, BuildItEffectLists> InitializeEffects()
        {
            Dictionary<string, GameObject> effectCache = new Dictionary<string, GameObject>();
            GameObject loadfx(string prefabName)
            {
                if (!effectCache.ContainsKey(prefabName))
                {
                    effectCache.Add(prefabName, PrefabManager.Cache.GetPrefab<GameObject>(prefabName));
                }
                return effectCache[prefabName];
            }
            EffectList createfxlist(params string[] effectsList) => new EffectList { m_effectPrefabs = effectsList.Select(fx => new EffectList.EffectData { m_prefab = loadfx(fx) }).ToArray() };

            var effects = new Dictionary<BuildItMaterial, BuildItEffectLists>
            {
                {
                    BuildItMaterial.Wood,
                    new BuildItEffectLists
                    {
                        Place = createfxlist("sfx_build_hammer_wood", "vfx_Place_stone_wall_2x1"),
                        Break = createfxlist("sfx_wood_break", "vfx_SawDust"),
                        Hit   = createfxlist("vfx_SawDust"),
                        Open  = createfxlist("sfx_door_open"),
                        Close = createfxlist("sfx_door_close"),
                        Fuel  = createfxlist("vfx_HearthAddFuel"),
                    }
                },
                {
                    BuildItMaterial.Stone,
                    new BuildItEffectLists
                    {
                        Place = createfxlist("sfx_build_hammer_stone", "vfx_Place_stone_wall_2x1"),
                        Break = createfxlist("sfx_rock_destroyed", "vfx_Place_stone_wall_2x1"),
                        Hit   = createfxlist("sfx_Rock_Hit"),
                        Open  = createfxlist("sfx_door_open"),
                        Close = createfxlist("sfx_door_close"),
                        Fuel  = createfxlist("vfx_HearthAddFuel"),
                    }
                },
                {
                    BuildItMaterial.Metal,
                    new BuildItEffectLists
                    {
                        Place = createfxlist("sfx_build_hammer_metal", "vfx_Place_stone_wall_2x1"),
                        Break = createfxlist("sfx_rock_destroyed", "vfx_HitSparks"),
                        Hit   = createfxlist("vfx_HitSparks"),
                        Open  = createfxlist("sfx_door_open"),
                        Close = createfxlist("sfx_door_close"),
                        Fuel  = createfxlist("vfx_HearthAddFuel"),
                    }
                },
                {
                    BuildItMaterial.Crystal,
                    new BuildItEffectLists
                    {
                        Place = createfxlist("sfx_build_hammer_crystal", "vfx_Place_stone_wall_2x1"),
                        Break = createfxlist("fx_crystal_destruction"),
                        Hit   = createfxlist("sfx_Rock_Hit"),
                        Open  = createfxlist("sfx_door_open"),
                        Close = createfxlist("sfx_door_close"),
                        Fuel  = createfxlist("vfx_HearthAddFuel"),
                    }
                }
            };

            return effects;
        }

        //private void AddLocalizations()
        //{
        //    CustomLocalization customLocalization = new CustomLocalization();
        //    customLocalization.AddTranslation("English", new Dictionary<String, String>
        //    {
        //        { "piece_wallrkc", "Wall" }
        //    });
        //}

        private CustomPiece BuildCustomPiece(BuildItPiece buildItPiece)
        {
            GameObject buildItPiecePrefab;
            // if (buildItPiece.PrefabName != "rkc_sign")
            // {
               buildItPiecePrefab = this.BuildItAssetBundle.LoadAsset<GameObject>(buildItPiece.PrefabName); 
            // }
            // else
            // {
            //     buildItPiecePrefab = this.BuildItAssetBundle2.LoadAsset<GameObject>(buildItPiece.PrefabName);
            // }
            var pieceConfig = new PieceConfig();
            // TODO: verify token string
            pieceConfig.Name = buildItPiece.DisplayNameToken;
            pieceConfig.Description = buildItPiece.PrefabDescription;
            // NOTE: could move override to json config if needed.
            pieceConfig.AllowedInDungeons = false;
            pieceConfig.PieceTable = TableName;
            pieceConfig.Category = CategoryTabName;
            pieceConfig.Enabled = buildItPiece.Enabled;
            if (!string.IsNullOrWhiteSpace(buildItPiece.RequiredStation))
            {
                pieceConfig.CraftingStation = buildItPiece.RequiredStation;
            }

            var requirements = buildItPiece.Requirements
                .Select(r => new RequirementConfig(r.Item, r.Amount, recover: r.Recover));

            pieceConfig.Requirements = requirements.ToArray();
            var customPiece = new CustomPiece(buildItPiecePrefab, fixReference: false, pieceConfig);
            var material = buildItPiecePrefab.GetComponentsInChildren<Material>();
            foreach (Material mat in material)
            {
                if (mat.name == "replace")
                {
                    mat.shader = Shader.Find("Custom/Piece");
                }
            }
            //Jotunn.Logger.LogInfo(buildItPiecePrefab.name);
            return customPiece;
        }

        private void AttachEffects(GameObject piecePrefab, BuildItPiece buildItPiece)
        {
            var pieceComponent = piecePrefab.GetComponent<Piece>();
            pieceComponent.m_placeEffect = this.effects[buildItPiece.Material].Place;

            var wearComponent = piecePrefab.GetComponent<WearNTear>();
            wearComponent.m_destroyedEffect = this.effects[buildItPiece.Material].Break;
            wearComponent.m_hitEffect = this.effects[buildItPiece.Material].Hit;

            if (piecePrefab.TryGetComponent<Door>(out Door doorComponent))
            {
                doorComponent.m_openEffects = this.effects[buildItPiece.Material].Open;
                doorComponent.m_closeEffects = this.effects[buildItPiece.Material].Close;
            }

            if (piecePrefab.TryGetComponent<Fireplace>(out Fireplace fireplaceComponent))
            {
                fireplaceComponent.m_fuelAddedEffects = this.effects[buildItPiece.Material].Fuel;
                if (buildItPiece.FuelItem == "Resin")
                {
                    fireplaceComponent.m_fuelItem = fuelResin;
                }
                else if (buildItPiece.FuelItem == "Wood")
                {
                    fireplaceComponent.m_fuelItem = fuelWood;
                }
                else
                {
                    Jotunn.Logger.LogInfo("You are missing a fuel type on " + buildItPiece.DisplayNameToken);
                }
                
                //fireAudioSource = piecePrefab.GetComponentInChildren<AudioSource>();
                //fireAudioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
            }

        }

        // LOADING EMBEDDED RESOURCES
        private void LoadEmbeddedAssembly(string assemblyName)
        {
            var stream = GetManifestResourceStream(assemblyName);
            if (stream == null)
            {
                Logger.LogError($"Could not load embedded assembly ({assemblyName})!");
                return;
            }

            using (stream)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                Assembly.Load(data);
            }
        }

        private Stream GetManifestResourceStream(string filename)
        {
            var assembly = Assembly.GetCallingAssembly();
            var fullname = assembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(filename));
            if (!string.IsNullOrEmpty(fullname))
            {
                return assembly.GetManifestResourceStream(fullname);
            }

            return null;
        }

        private T LoadEmbeddedJsonFile<T>(string filename) where T : class
        {
            string jsonFileText = String.Empty;

            using (StreamReader reader = new StreamReader(LoadEmbeddedJsonStream(filename)))
            {
                jsonFileText = reader.ReadToEnd();
            }

            T result;

            try
            {
                var jsonParameters = new JSONParameters
                {
                    AutoConvertStringToNumbers = true,
                };
                result = string.IsNullOrEmpty(jsonFileText) ? null : JSON.ToObject<T>(jsonFileText, jsonParameters);
            }
            catch (Exception)
            {
                Logger.LogError($"Could not parse file '{filename}'! Errors in JSON!");
                throw;
            }

            return result;
        }

        private Stream LoadEmbeddedJsonStream(string filename)
        {
            return this.GetManifestResourceStream(filename);
        }
    }
}

