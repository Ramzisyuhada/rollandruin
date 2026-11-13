using System;
using System.Collections.Generic;
using System.Linq;
using RollAndRuin.App;
using RollAndRuin.Core;
using RollAndRuin.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RollAndRuin.EditorTools
{
    /// <summary>
    /// Menu "Roll & Ruin/Build Game Scene": menyusun scene Game lengkap (kamera, papan, pion,
    /// UI, EventSystem) dan mengisi semua referensi sprite. Aman dijalankan ulang; scene lama ditimpa.
    /// </summary>
    public static class GameSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string Sprites = SpriteImportSettings.SpritesRoot;

        private static readonly Color Background = new Color(0.09f, 0.08f, 0.11f);

        [MenuItem("Roll & Ruin/Build Game Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureSpritesImported();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Sistem
            var systems = new GameObject("Systems");
            var animations = systems.AddComponent<AnimationQueue>();
            CreateGlobalLight(systems.transform);

            // Papan dan pion
            var boardView = new GameObject("Board").AddComponent<BoardView>();
            ConfigureBoard(boardView);
            var playerPawn = CreatePawn("PlayerPawn", "Pawns/pawn_player", new Vector3(-0.18f, 0.1f, 0f), 10);
            var rivalPawn = CreatePawn("RivalPawn", "Pawns/pawn_rival", new Vector3(0.18f, 0.1f, 0f), 9);

            // Kamera
            var camera = CreateCamera();
            var fitter = camera.gameObject.AddComponent<BoardCameraFitter>();
            fitter.SetBoard(boardView);

            // UI
            var ui = CreateUi();
            CreateEventSystem();

            // Bootstrap
            var game = new GameObject("Game").AddComponent<GameBootstrap>();
            game.Bind(boardView, playerPawn, ui.Dice, ui.Hud, ui.Choices, animations, fitter);
            game.BindRival(rivalPawn);
            EditorUtility.SetDirty(game);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            Debug.Log($"[Roll & Ruin] Scene dibuat: {ScenePath}. Tekan Play untuk mencoba.");
        }

        // ── Sprite ───────────────────────────────────────────────────────────

        private static void EnsureSpritesImported()
        {
            var probe = AssetImporter.GetAtPath(Sprites + "Tiles/floor_a.png") as TextureImporter;
            if (probe != null && probe.textureType != TextureImporterType.Sprite)
                SpriteImportSettings.ReimportAll();
        }

        private static Sprite S(string relativePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + relativePath + ".png");
            if (sprite == null) Debug.LogWarning($"[Roll & Ruin] Sprite tidak ditemukan: {relativePath}");
            return sprite;
        }

        // ── Papan ────────────────────────────────────────────────────────────

        private static void ConfigureBoard(BoardView view)
        {
            var so = new SerializedObject(view);

            SetSprites(so.FindProperty("floorSprites"),
                S("Tiles/floor_a"), S("Tiles/floor_b"), S("Tiles/floor_cracked"), S("Tiles/floor_mossy"));

            var icons = new (TileType, string)[]
            {
                (TileType.BossDoor, "Tiles/wall_brick"),
                (TileType.Shop, "BoardIcons/shop_bag"),
                (TileType.Chest, "BoardIcons/chest_closed"),
                (TileType.Event, "BoardIcons/event_scroll"),
                (TileType.Curse, "BoardIcons/curse_skull"),
                (TileType.CampFire, "BoardIcons/campfire"),
                (TileType.Enemy, "Enemies/enemy_goblin"),
                (TileType.Trap, "BoardIcons/trap_spikes"),
            };
            var iconProp = so.FindProperty("tileIcons");
            iconProp.arraySize = icons.Length;
            for (int i = 0; i < icons.Length; i++)
            {
                var el = iconProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("type").enumValueIndex = (int)icons[i].Item1;
                el.FindPropertyRelative("sprite").objectReferenceValue = S(icons[i].Item2);
            }

            so.FindProperty("highlightSprite").objectReferenceValue = S("Tiles/tile_highlight");
            so.FindProperty("dangerSprite").objectReferenceValue = S("Tiles/tile_danger");

            var jumps = new (JumpKind kind, string body, string head, string tail)[]
            {
                (JumpKind.Snake, "Snakes/snake_green_body", "Snakes/snake_green_head", "Snakes/snake_green_tail"),
                (JumpKind.SnakeGold, "Snakes/snake_gold_body", "Snakes/snake_gold_head", "Snakes/snake_gold_tail"),
                (JumpKind.SnakePoison, "Snakes/snake_poison_body", "Snakes/snake_poison_head", "Snakes/snake_poison_tail"),
                (JumpKind.LadderStone, "Ladders/ladder_stone", null, null),
                (JumpKind.LadderRope, "Ladders/ladder_rope", null, null),
                (JumpKind.LadderLift, "Ladders/lift_chain", "Ladders/lift_cabin", null),
            };
            var jumpProp = so.FindProperty("jumpSprites");
            jumpProp.arraySize = jumps.Length;
            for (int i = 0; i < jumps.Length; i++)
            {
                var el = jumpProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("kind").enumValueIndex = (int)jumps[i].kind;
                el.FindPropertyRelative("body").objectReferenceValue = S(jumps[i].body);
                el.FindPropertyRelative("head").objectReferenceValue = jumps[i].head != null ? S(jumps[i].head) : null;
                el.FindPropertyRelative("tail").objectReferenceValue = jumps[i].tail != null ? S(jumps[i].tail) : null;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PawnView CreatePawn(string name, string sprite, Vector3 offset, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.sortingOrder = order;
            var pawn = go.AddComponent<PawnView>();
            var so = new SerializedObject(pawn);
            so.FindProperty("offset").vector3Value = offset;
            so.ApplyModifiedPropertiesWithoutUndo();
            return pawn;
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, -1.2f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            go.AddComponent<AudioListener>();
            return cam;
        }

        /// <summary>
        /// URP 2D Renderer menggelapkan sprite tanpa cahaya 2D. Light2D ditambahkan lewat reflection
        /// supaya asmdef Editor tidak perlu bergantung langsung ke paket URP.
        /// </summary>
        private static void CreateGlobalLight(Transform parent)
        {
            var type = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) return;
            var go = new GameObject("Global Light 2D");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent(type);
            try
            {
                var lightTypeProp = type.GetProperty("lightType");
                if (lightTypeProp != null && lightTypeProp.CanWrite)
                    lightTypeProp.SetValue(light, Enum.Parse(lightTypeProp.PropertyType, "Global"));
                type.GetProperty("intensity")?.SetValue(light, 1f);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Roll & Ruin] Global Light 2D perlu diatur manual (Light Type = Global): {e.Message}");
            }
        }

        // ── UI ───────────────────────────────────────────────────────────────

        private struct UiRefs
        {
            public HudView Hud;
            public DiceHandView Dice;
            public ChoicePanelView Choices;
        }

        private static UiRefs CreateUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.layer = 5;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            var panelSprite = S("UI/panel_stone_9slice");
            var woodButton = S("UI/button_wood_9slice");
            var goldButton = S("UI/button_gold_9slice");

            // Bar atas
            var top = UiFactory.CreateImage("TopBar", root, panelSprite);
            UiFactory.AnchorBand(top.rectTransform, true, 150f);
            var hp = UiFactory.CreateText("HP", top.transform, "HP", 40, TextAnchor.MiddleLeft);
            SetHorizontalSlice(hp.rectTransform, 0f, 0.36f, 30f);
            var gold = UiFactory.CreateText("Gold", top.transform, "Gold", 40, TextAnchor.MiddleCenter, UiFactory.GoldColor);
            SetHorizontalSlice(gold.rectTransform, 0.36f, 0.58f, 0f);
            var floor = UiFactory.CreateText("Floor", top.transform, "Lantai", 34, TextAnchor.MiddleRight);
            SetHorizontalSlice(floor.rectTransform, 0.58f, 1f, 30f);

            var message = UiFactory.CreateText("Message", root, "", 40);
            UiFactory.AnchorBand(message.rectTransform, true, 100f, 160f, 30f);
            var info = UiFactory.CreateText("Info", root, "", 30, TextAnchor.MiddleCenter, UiFactory.DimColor);
            UiFactory.AnchorBand(info.rectTransform, true, 80f, 260f, 30f);

            // Zona jempol
            var bottom = UiFactory.CreateImage("BottomPanel", root, panelSprite);
            UiFactory.AnchorBand(bottom.rectTransform, false, 600f);

            var diceRow = UiFactory.CreateRect("DiceRow", bottom.transform);
            diceRow.anchorMin = new Vector2(0f, 0.36f);
            diceRow.anchorMax = new Vector2(1f, 1f);
            diceRow.offsetMin = new Vector2(30f, 0f);
            diceRow.offsetMax = new Vector2(-30f, -30f);
            var layout = diceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            var diceView = diceRow.gameObject.AddComponent<DiceHandView>();
            ConfigureDice(diceView, diceRow, woodButton);

            var roll = UiFactory.CreateButton("RollButton", bottom.transform, goldButton, "LEMPAR", 48, out var rollLabel);
            var rrt = (RectTransform)roll.transform;
            rrt.anchorMin = new Vector2(0.2f, 0.05f);
            rrt.anchorMax = new Vector2(0.8f, 0.31f);
            rrt.offsetMin = rrt.offsetMax = Vector2.zero;
            rollLabel.color = new Color(0.25f, 0.15f, 0.05f);

            var hud = canvasGo.AddComponent<HudView>();
            hud.Bind(hp, gold, floor, message, info, roll, rollLabel);
            EditorUtility.SetDirty(hud);

            var choices = CreateChoicePanel(root, panelSprite, woodButton);
            return new UiRefs { Hud = hud, Dice = diceView, Choices = choices };
        }

        private static void ConfigureDice(DiceHandView view, RectTransform container, Sprite button)
        {
            var so = new SerializedObject(view);
            so.FindProperty("container").objectReferenceValue = container;
            so.FindProperty("buttonSprite").objectReferenceValue = button;
            so.FindProperty("defaultIcon").objectReferenceValue = S("Dice/die_white_0");

            var skins = new (string id, string sprite)[]
            {
                (DiceFactory.BiasaId, "Dice/die_white_6"),
                (DiceFactory.AmanId, "Dice/die_blue_1"),
                (DiceFactory.NekatId, "Dice/die_red_6"),
                (DiceFactory.GenapId, "Dice/die_purple_2"),
                (DiceFactory.GanjilId, "Dice/die_purple_1"),
                (DiceFactory.MantapId, "Dice/die_blue_4"),
                (DiceFactory.KompasId, "Dice/die_gold_3"),
                (DiceFactory.KembarId, "Dice/die_white_2"),
                (DiceFactory.EmasId, "Dice/die_gold_6"),
                (DiceFactory.RaksasaId, "Dice/die_red_4"),
            };
            var prop = so.FindProperty("skins");
            prop.arraySize = skins.Length;
            for (int i = 0; i < skins.Length; i++)
            {
                var el = prop.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("dieId").stringValue = skins[i].id;
                el.FindPropertyRelative("icon").objectReferenceValue = S(skins[i].sprite);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ChoicePanelView CreateChoicePanel(Transform root, Sprite panelSprite, Sprite button)
        {
            var dim = UiFactory.CreateImage("ChoicePanel", root, null, new Color(0f, 0f, 0f, 0.7f));
            UiFactory.Stretch(dim.rectTransform);

            var box = UiFactory.CreateImage("Box", dim.transform, panelSprite);
            var brt = box.rectTransform;
            brt.anchorMin = new Vector2(0.06f, 0.2f);
            brt.anchorMax = new Vector2(0.94f, 0.82f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;

            var title = UiFactory.CreateText("Title", box.transform, "Judul", 56, TextAnchor.MiddleCenter, UiFactory.GoldColor);
            UiFactory.AnchorBand(title.rectTransform, true, 130f, 30f, 40f);
            var body = UiFactory.CreateText("Body", box.transform, "", 36, TextAnchor.UpperCenter);
            UiFactory.AnchorBand(body.rectTransform, true, 300f, 170f, 50f);

            var options = UiFactory.CreateRect("Options", box.transform);
            options.anchorMin = new Vector2(0f, 0f);
            options.anchorMax = new Vector2(1f, 1f);
            options.offsetMin = new Vector2(60f, 50f);
            options.offsetMax = new Vector2(-60f, -490f);
            var layout = options.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var view = dim.gameObject.AddComponent<ChoicePanelView>();
            view.Bind(dim.gameObject, title, body, options, button);
            EditorUtility.SetDirty(view);
            dim.gameObject.SetActive(false);
            return view;
        }

        private static void SetHorizontalSlice(RectTransform rt, float from, float to, float padding)
        {
            rt.anchorMin = new Vector2(from, 0f);
            rt.anchorMax = new Vector2(to, 1f);
            rt.offsetMin = new Vector2(padding, 0f);
            rt.offsetMax = new Vector2(-padding, 0f);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static void SetSprites(SerializedProperty prop, params Sprite[] sprites)
        {
            prop.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.path != ScenePath && !s.path.EndsWith("SampleScene.unity"))
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
