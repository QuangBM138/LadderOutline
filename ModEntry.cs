using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ogahalo00.Stardew.LadderOutline
{
    public enum MineExitType
    {
        Ladder,
        Shaft
    }

    public sealed class MineExitInfo
    {
        public Vector2 Tile { get; }
        public MineExitType Type { get; set; }
        public Vector2 WorldPixel => Tile * Game1.tileSize;
        public Vector2 WorldCenter => (Tile + new Vector2(0.5f, 0.5f)) * Game1.tileSize;

        public MineExitInfo(Vector2 tile, MineExitType type)
        {
            Tile = tile;
            Type = type;
        }
    }

    internal sealed class PointerDrawInfo
    {
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
        public float Rotation { get; set; }
        public Color Color { get; set; }
        public MineExitType Type { get; set; }
        public float DistanceTiles { get; set; }
    }

    public sealed class ModConfig
    {
        // General
        public bool EnableMod { get; set; } = true;
        public KeybindList ToggleKey { get; set; } = KeybindList.Parse("None");
        public bool DebugMode { get; set; } = false;

        // Outline Settings
        public bool EnableOutline { get; set; } = true;
        public string OutlineColor { get; set; } = "Green";
        public string ShaftOutlineColor { get; set; } = "Cyan";
        public float OutlineOpacity { get; set; } = 0.6f;
        public int OutlineThickness { get; set; } = 3;

        // Pointer (Direction Indicator) Settings
        public bool EnablePointer { get; set; } = true;
        public string PointerStyle { get; set; } = "ScreenEdge"; // "ScreenEdge", "PlayerOrbit", "Both"
        public bool ShowOffscreenOnly { get; set; } = true;
        public bool ShowOnscreenMarker { get; set; } = true;
        public bool ShowDistance { get; set; } = true;
        public bool ShowTargetType { get; set; } = true;
        public string LadderPointerColor { get; set; } = "Green";
        public string ShaftPointerColor { get; set; } = "Cyan";
        public float PointerScale { get; set; } = 4.0f;
        public bool PulseAnimation { get; set; } = true;
    }

    internal sealed class ModEntry : Mod
    {
        private ModConfig Config = null!;
        private readonly Dictionary<Vector2, MineExitInfo> mineExits = new();
        private readonly List<KeyValuePair<Point, bool>> ladderQueue = new();
        private bool skippedFirstTick = false;
        private Texture2D? pixelTexture;
        private bool hasCheckedInitialLadders = false;

        private static readonly string[] AllowedColors =
        {
            "Green", "Cyan", "Yellow", "Red", "Blue", "Purple", "Orange", "White", "Pink"
        };

        private static readonly string[] AllowedPointerStyles =
        {
            "ScreenEdge", "PlayerOrbit", "Both"
        };

        public override void Entry(IModHelper helper)
        {
            try
            {
                // Read configuration
                Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();

                // Initialize 1x1 white texture for drawing rectangle borders
                InitializeTexture();

                // Register SMAPI events
                helper.Events.GameLoop.GameLaunched += OnGameLaunched;
                helper.Events.World.LocationListChanged += OnLocationListChanged;
                helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
                helper.Events.Player.Warped += OnPlayerWarped;
                helper.Events.Display.RenderedWorld += OnRenderedWorld;
                helper.Events.Display.RenderedHud += OnRenderedHud;
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                helper.Events.Input.ButtonPressed += OnButtonPressed;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in Entry: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            InitializeGenericModConfigMenu(Helper);
        }

        private void InitializeTexture()
        {
            try
            {
                pixelTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                pixelTexture.SetData(new[] { Color.White });
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error initializing texture: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                pixelTexture = null;
            }
        }

        private void InitializeGenericModConfigMenu(IModHelper helper)
        {
            try
            {
                var gmcmApi = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (gmcmApi == null)
                {
                    Monitor.Log("Generic Mod Config Menu mod not found or API unavailable.", LogLevel.Debug);
                    return;
                }

                gmcmApi.Register(
                    mod: ModManifest,
                    reset: ResetConfig,
                    save: SaveConfig,
                    titleScreenOnly: false
                );

                AddOptions(gmcmApi);
                Monitor.Log("Successfully registered with Generic Mod Config Menu.", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error initializing GMCM: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void ResetConfig()
        {
            Config = new ModConfig();
            Helper.WriteConfig(Config);
        }

        private void SaveConfig()
        {
            try
            {
                Helper.WriteConfig(Config);
                Monitor.Log("Configuration saved successfully.", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error saving configuration: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void AddOptions(IGenericModConfigMenuApi api)
        {
            // === General Settings ===
            api.AddSectionTitle(ModManifest, () => Helper.Translation.Get("config.section.general"));

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.enable-mod.name"),
                tooltip: () => Helper.Translation.Get("config.enable-mod.tooltip"),
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );

            api.AddKeybindList(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.toggle-key.name"),
                tooltip: () => Helper.Translation.Get("config.toggle-key.tooltip"),
                getValue: () => Config.ToggleKey,
                setValue: value => Config.ToggleKey = value
            );

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.debug-mode.name"),
                tooltip: () => Helper.Translation.Get("config.debug-mode.tooltip"),
                getValue: () => Config.DebugMode,
                setValue: value => Config.DebugMode = value
            );

            // === Outline Settings ===
            api.AddSectionTitle(ModManifest, () => Helper.Translation.Get("config.section.outline"));

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.enable-outline.name"),
                tooltip: () => Helper.Translation.Get("config.enable-outline.tooltip"),
                getValue: () => Config.EnableOutline,
                setValue: value => Config.EnableOutline = value
            );

            api.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.outline-color.name"),
                tooltip: () => Helper.Translation.Get("config.outline-color.tooltip"),
                getValue: () => Config.OutlineColor,
                setValue: value => Config.OutlineColor = value,
                allowedValues: AllowedColors,
                formatAllowedValue: val => Helper.Translation.Get($"color.{val.ToLowerInvariant()}").Default(val)
            );

            api.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.shaft-outline-color.name"),
                tooltip: () => Helper.Translation.Get("config.shaft-outline-color.tooltip"),
                getValue: () => Config.ShaftOutlineColor,
                setValue: value => Config.ShaftOutlineColor = value,
                allowedValues: AllowedColors,
                formatAllowedValue: val => Helper.Translation.Get($"color.{val.ToLowerInvariant()}").Default(val)
            );

            api.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.outline-opacity.name"),
                tooltip: () => Helper.Translation.Get("config.outline-opacity.tooltip"),
                getValue: () => Config.OutlineOpacity,
                setValue: value => Config.OutlineOpacity = value,
                min: 0.1f,
                max: 1.0f,
                interval: 0.1f
            );

            api.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.outline-thickness.name"),
                tooltip: () => Helper.Translation.Get("config.outline-thickness.tooltip"),
                getValue: () => Config.OutlineThickness,
                setValue: value => Config.OutlineThickness = value,
                min: 1,
                max: 6,
                interval: 1
            );

            // === Direction Pointer Settings ===
            api.AddSectionTitle(ModManifest, () => Helper.Translation.Get("config.section.pointer"));

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.enable-pointer.name"),
                tooltip: () => Helper.Translation.Get("config.enable-pointer.tooltip"),
                getValue: () => Config.EnablePointer,
                setValue: value => Config.EnablePointer = value
            );

            api.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.pointer-style.name"),
                tooltip: () => Helper.Translation.Get("config.pointer-style.tooltip"),
                getValue: () => Config.PointerStyle,
                setValue: value => Config.PointerStyle = value,
                allowedValues: AllowedPointerStyles,
                formatAllowedValue: val => Helper.Translation.Get($"pointer-style.{val.ToLowerInvariant()}").Default(val)
            );

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.show-offscreen-only.name"),
                tooltip: () => Helper.Translation.Get("config.show-offscreen-only.tooltip"),
                getValue: () => Config.ShowOffscreenOnly,
                setValue: value => Config.ShowOffscreenOnly = value
            );

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.show-onscreen-marker.name"),
                tooltip: () => Helper.Translation.Get("config.show-onscreen-marker.tooltip"),
                getValue: () => Config.ShowOnscreenMarker,
                setValue: value => Config.ShowOnscreenMarker = value
            );

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.show-distance.name"),
                tooltip: () => Helper.Translation.Get("config.show-distance.tooltip"),
                getValue: () => Config.ShowDistance,
                setValue: value => Config.ShowDistance = value
            );

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.show-target-type.name"),
                tooltip: () => Helper.Translation.Get("config.show-target-type.tooltip"),
                getValue: () => Config.ShowTargetType,
                setValue: value => Config.ShowTargetType = value
            );

            api.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.ladder-pointer-color.name"),
                tooltip: () => Helper.Translation.Get("config.ladder-pointer-color.tooltip"),
                getValue: () => Config.LadderPointerColor,
                setValue: value => Config.LadderPointerColor = value,
                allowedValues: AllowedColors,
                formatAllowedValue: val => Helper.Translation.Get($"color.{val.ToLowerInvariant()}").Default(val)
            );

            api.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.shaft-pointer-color.name"),
                tooltip: () => Helper.Translation.Get("config.shaft-pointer-color.tooltip"),
                getValue: () => Config.ShaftPointerColor,
                setValue: value => Config.ShaftPointerColor = value,
                allowedValues: AllowedColors,
                formatAllowedValue: val => Helper.Translation.Get($"color.{val.ToLowerInvariant()}").Default(val)
            );

            api.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.pointer-scale.name"),
                tooltip: () => Helper.Translation.Get("config.pointer-scale.tooltip"),
                getValue: () => Config.PointerScale,
                setValue: value => Config.PointerScale = value,
                min: 2.0f,
                max: 8.0f,
                interval: 0.5f
            );

            api.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.pulse-animation.name"),
                tooltip: () => Helper.Translation.Get("config.pulse-animation.tooltip"),
                getValue: () => Config.PulseAnimation,
                setValue: value => Config.PulseAnimation = value
            );
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree) return;

            try
            {
                if (Config.ToggleKey.IsBound && Config.ToggleKey.IsDown())
                {
                    Config.EnableMod = !Config.EnableMod;
                    Helper.WriteConfig(Config);

                    string statusMsg = Config.EnableMod
                        ? Helper.Translation.Get("hud.toggled-enabled").ToString()
                        : Helper.Translation.Get("hud.toggled-disabled").ToString();

                    Game1.addHUDMessage(new HUDMessage(statusMsg, HUDMessage.newQuest_type));
                    Monitor.Log($"Mod toggled to {(Config.EnableMod ? "enabled" : "disabled")} via key {Config.ToggleKey}.", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnButtonPressed: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsWorldReady) return;

            try
            {
                if (ladderQueue.Count > 0 && skippedFirstTick)
                {
                    var ladder = ladderQueue[0];
                    Monitor.Log($"Processing delayed exit at {ladder.Key} (shaft: {ladder.Value}).", LogLevel.Trace);
                    OnLadderLocationAdded(ladder.Key, ladder.Value);
                    ladderQueue.RemoveAt(0);
                }
                skippedFirstTick = true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnOneSecondUpdateTicked: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnLocationListChanged(object? sender, LocationListChangedEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsWorldReady) return;

            try
            {
                foreach (GameLocation location in e.Added)
                {
                    if (!location.IsActiveLocation() || location is not MineShaft shaft)
                        continue;

                    Monitor.Log($"Location added: Mine level {shaft.mineLevel}.", LogLevel.Trace);
                    skippedFirstTick = false;
                    FindLadders(shaft);
                    RegisterDynamicLadders(shaft);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnLocationListChanged: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void RegisterDynamicLadders(MineShaft shaft)
        {
            try
            {
                // Dynamic ladder/shaft generated by game (break rock, monster death)
                var generatedLadders = Helper.Reflection.GetField<NetPointDictionary<bool, NetBool>>(shaft, "createLadderDownEvent", false)?.GetValue();
                if (generatedLadders != null)
                {
                    generatedLadders.OnValueAdded -= OnLadderLocationAdded;
                    generatedLadders.OnValueAdded += OnLadderLocationAdded;

                    foreach (var ladder in generatedLadders.Pairs)
                    {
                        if (!ladderQueue.Any(kv => kv.Key == ladder.Key))
                            ladderQueue.Add(ladder);
                    }
                }

                // Ladder placed by player (staircase item)
                var placedLadders = Helper.Reflection.GetField<NetVector2Dictionary<bool, NetBool>>(shaft, "createLadderAtEvent", false)?.GetValue();
                if (placedLadders != null)
                {
                    placedLadders.OnValueAdded -= OnLadderPlaced;
                    placedLadders.OnValueAdded += OnLadderPlaced;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error registering dynamic ladders: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnPlayerWarped(object? sender, WarpedEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsWorldReady) return;

            try
            {
                ladderQueue.Clear();
                mineExits.Clear();
                hasCheckedInitialLadders = false;

                if (e.NewLocation is MineShaft shaft)
                {
                    Monitor.Log($"Warped to mine level {shaft.mineLevel}, finding ladders and shafts.", LogLevel.Trace);
                    FindLadders(shaft);
                    RegisterDynamicLadders(shaft);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnPlayerWarped: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsWorldReady) return;

            try
            {
                if (Game1.currentLocation is MineShaft shaft)
                {
                    // Check shortly after warp
                    if (!hasCheckedInitialLadders && e.IsMultipleOf(30))
                    {
                        FindLadders(shaft);
                        hasCheckedInitialLadders = true;
                    }
                    // Periodic check once per second (60 ticks) to catch any silent spawns
                    else if (e.IsMultipleOf(60))
                    {
                        FindLadders(shaft);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnUpdateTicked: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void FindLadders(MineShaft shaft)
        {
            if (shaft?.map == null) return;

            try
            {
                int ladderCount = 0;
                int shaftCount = 0;

                foreach (var layer in shaft.map.Layers)
                {
                    if (layer?.Tiles == null) continue;

                    for (int x = 0; x < layer.LayerWidth; x++)
                    {
                        for (int y = 0; y < layer.LayerHeight; y++)
                        {
                            var tile = layer.Tiles[x, y];
                            if (tile == null) continue;

                            // 173 = Ladder down, 174 = Shaft / jumping hole
                            if (tile.TileIndex == 173)
                            {
                                var tilePos = new Vector2(x, y);
                                if (!mineExits.TryGetValue(tilePos, out var existing) || existing.Type != MineExitType.Ladder)
                                {
                                    mineExits[tilePos] = new MineExitInfo(tilePos, MineExitType.Ladder);
                                    ladderCount++;
                                    if (Config.DebugMode)
                                    {
                                        string msg = Helper.Translation.Get("hud.debug.ladder-found", new { x, y }).ToString();
                                        Game1.addHUDMessage(new HUDMessage(msg, HUDMessage.newQuest_type));
                                    }
                                }
                            }
                            else if (tile.TileIndex == 174)
                            {
                                var tilePos = new Vector2(x, y);
                                if (!mineExits.TryGetValue(tilePos, out var existing) || existing.Type != MineExitType.Shaft)
                                {
                                    mineExits[tilePos] = new MineExitInfo(tilePos, MineExitType.Shaft);
                                    shaftCount++;
                                    if (Config.DebugMode)
                                    {
                                        string msg = Helper.Translation.Get("hud.debug.shaft-found", new { x, y }).ToString();
                                        Game1.addHUDMessage(new HUDMessage(msg, HUDMessage.newQuest_type));
                                    }
                                }
                            }
                        }
                    }
                }

                if ((ladderCount > 0 || shaftCount > 0) && Config.DebugMode)
                {
                    Monitor.Log($"Found {ladderCount} ladders and {shaftCount} shafts in level {shaft.mineLevel}. Total tracked: {mineExits.Count}.", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in FindLadders: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnLadderLocationAdded(Point point, bool shaft)
        {
            try
            {
                var tilePos = new Vector2(point.X, point.Y);
                var type = shaft ? MineExitType.Shaft : MineExitType.Ladder;
                mineExits[tilePos] = new MineExitInfo(tilePos, type);

                Monitor.Log($"Added dynamic exit: {(shaft ? "Shaft" : "Ladder")} at tile {point}.", LogLevel.Trace);
                if (Config.DebugMode)
                {
                    string typeName = shaft
                        ? Helper.Translation.Get("pointer.shaft").ToString()
                        : Helper.Translation.Get("pointer.ladder").ToString();

                    string msg = Helper.Translation.Get("hud.debug.new-exit", new { type = typeName, x = point.X, y = point.Y }).ToString();
                    Game1.addHUDMessage(new HUDMessage(msg, HUDMessage.newQuest_type));
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnLadderLocationAdded: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnLadderPlaced(Vector2 point, bool shaft)
        {
            try
            {
                // Player placed staircase is always a ladder
                mineExits[point] = new MineExitInfo(point, MineExitType.Ladder);
                Monitor.Log($"Added placed ladder at tile {point}.", LogLevel.Trace);
                if (Config.DebugMode)
                {
                    string msg = Helper.Translation.Get("hud.debug.placed-ladder", new { x = (int)point.X, y = (int)point.Y }).ToString();
                    Game1.addHUDMessage(new HUDMessage(msg, HUDMessage.newQuest_type));
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnLadderPlaced: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsWorldReady || mineExits.Count == 0 || Game1.currentLocation is not MineShaft)
                return;

            try
            {
                // 1. Draw Outlines
                if (Config.EnableOutline && pixelTexture != null)
                {
                    foreach (var exit in mineExits.Values)
                    {
                        var rect = Game1.GlobalToLocal(
                            Game1.viewport,
                            new Rectangle((int)exit.WorldPixel.X, (int)exit.WorldPixel.Y, Game1.tileSize, Game1.tileSize)
                        );

                        // Culling
                        if (rect.Right < 0 || rect.Bottom < 0 || rect.Left > Game1.viewport.Width || rect.Top > Game1.viewport.Height)
                            continue;

                        Color color = exit.Type == MineExitType.Ladder
                            ? GetColor(Config.OutlineColor, Config.OutlineOpacity)
                            : GetColor(Config.ShaftOutlineColor, Config.OutlineOpacity);

                        DrawRectangle(e.SpriteBatch, rect, color, Config.OutlineThickness);
                    }
                }

                // 2. Draw On-Screen Floating Markers (bouncing arrow above ladder/hole when visible in world)
                if (Config.EnablePointer && Config.ShowOnscreenMarker)
                {
                    float bob = Config.PulseAnimation
                        ? (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 200.0) * 4f
                        : 0f;

                    Rectangle arrowSource = new Rectangle(412, 495, 5, 4);
                    Vector2 arrowOrigin = new Vector2(2.5f, 2f);
                    float downRotation = (float)Math.PI;

                    foreach (var exit in mineExits.Values)
                    {
                        var centerScreen = Game1.GlobalToLocal(Game1.viewport, exit.WorldCenter);

                        // Culling
                        if (centerScreen.X < -64 || centerScreen.Y < -64 || centerScreen.X > Game1.viewport.Width + 64 || centerScreen.Y > Game1.viewport.Height + 64)
                            continue;

                        Vector2 markerPos = new Vector2(centerScreen.X, centerScreen.Y - 48f + bob);
                        Color color = exit.Type == MineExitType.Ladder
                            ? GetColor(Config.LadderPointerColor, 1.0f)
                            : GetColor(Config.ShaftPointerColor, 1.0f);

                        // Drop shadow
                        e.SpriteBatch.Draw(
                            Game1.mouseCursors,
                            markerPos + new Vector2(2, 2),
                            arrowSource,
                            Color.Black * 0.75f,
                            downRotation,
                            arrowOrigin,
                            Config.PointerScale,
                            SpriteEffects.None,
                            1f
                        );

                        // Tinted arrow
                        e.SpriteBatch.Draw(
                            Game1.mouseCursors,
                            markerPos,
                            arrowSource,
                            color,
                            downRotation,
                            arrowOrigin,
                            Config.PointerScale,
                            SpriteEffects.None,
                            1f
                        );
                    }
                }

                // 3. Draw Player Orbit Pointers (arrows around player pointing towards each ladder/shaft)
                if (Config.EnablePointer && (Config.PointerStyle == "PlayerOrbit" || Config.PointerStyle == "Both") && Game1.player != null)
                {
                    Vector2 playerCenter = new Vector2(Game1.player.Position.X + 32f, Game1.player.Position.Y + 32f);
                    Rectangle arrowSource = new Rectangle(412, 495, 5, 4);
                    Vector2 arrowOrigin = new Vector2(2.5f, 2f);

                    float pulse = Config.PulseAnimation
                        ? (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0) * 4f
                        : 0f;

                    foreach (var exit in mineExits.Values)
                    {
                        Vector2 diff = exit.WorldCenter - playerCenter;
                        if (diff == Vector2.Zero) continue;

                        Vector2 dir = Vector2.Normalize(diff);
                        float angle = (float)Math.Atan2(dir.Y, dir.X);
                        float rotation = angle + (float)(Math.PI / 2.0);

                        Vector2 orbitWorldPos = playerCenter + dir * (60f + pulse);
                        Vector2 orbitScreenPos = Game1.GlobalToLocal(Game1.viewport, orbitWorldPos);

                        Color color = exit.Type == MineExitType.Ladder
                            ? GetColor(Config.LadderPointerColor, 1.0f)
                            : GetColor(Config.ShaftPointerColor, 1.0f);

                        // Shadow
                        e.SpriteBatch.Draw(
                            Game1.mouseCursors,
                            orbitScreenPos + new Vector2(1, 1),
                            arrowSource,
                            Color.Black * 0.75f,
                            rotation,
                            arrowOrigin,
                            Config.PointerScale,
                            SpriteEffects.None,
                            1f
                        );

                        // Arrow
                        e.SpriteBatch.Draw(
                            Game1.mouseCursors,
                            orbitScreenPos,
                            arrowSource,
                            color,
                            rotation,
                            arrowOrigin,
                            Config.PointerScale,
                            SpriteEffects.None,
                            1f
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnRenderedWorld: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Config.EnableMod || !Config.EnablePointer || !Context.IsWorldReady || mineExits.Count == 0 || Game1.currentLocation is not MineShaft || Game1.player == null)
                return;

            // Only draw screen-edge pointers if style is ScreenEdge or Both
            if (Config.PointerStyle != "ScreenEdge" && Config.PointerStyle != "Both")
                return;

            try
            {
                Vector2 playerCenter = new Vector2(Game1.player.Position.X + 32f, Game1.player.Position.Y + 32f);
                Rectangle safeArea = Utility.getSafeArea();
                Vector2 screenCenter = new Vector2(safeArea.Center.X, safeArea.Center.Y);

                float margin = 48f;
                float minX = safeArea.Left + margin;
                float maxX = safeArea.Right - margin;
                float minY = safeArea.Top + margin;
                float maxY = safeArea.Bottom - margin;

                var pointersToDraw = new List<PointerDrawInfo>();

                foreach (var exit in mineExits.Values)
                {
                    Vector2 diff = exit.WorldCenter - playerCenter;
                    float distPixels = diff.Length();
                    float distTiles = distPixels / (float)Game1.tileSize;

                    bool isOnScreen = Utility.isOnScreen(exit.WorldCenter, Game1.tileSize);

                    // If configured to only show offscreen pointers and this target is already on screen, skip
                    if (isOnScreen && Config.ShowOffscreenOnly)
                        continue;

                    Vector2 dir = diff;
                    if (dir != Vector2.Zero)
                        dir.Normalize();
                    else
                        dir = new Vector2(0, -1);

                    // Ray-box intersection from screen center to safe area edges
                    float tX = dir.X > 0 ? (maxX - screenCenter.X) / dir.X : (dir.X < 0 ? (minX - screenCenter.X) / dir.X : float.MaxValue);
                    float tY = dir.Y > 0 ? (maxY - screenCenter.Y) / dir.Y : (dir.Y < 0 ? (minY - screenCenter.Y) / dir.Y : float.MaxValue);
                    float t = Math.Min(tX, tY);
                    Vector2 pointerPos = screenCenter + dir * t;

                    float angle = (float)Math.Atan2(dir.Y, dir.X);
                    float spriteRotation = angle + (float)(Math.PI / 2.0);

                    Color color = exit.Type == MineExitType.Ladder
                        ? GetColor(Config.LadderPointerColor, 1.0f)
                        : GetColor(Config.ShaftPointerColor, 1.0f);

                    pointersToDraw.Add(new PointerDrawInfo
                    {
                        Position = pointerPos,
                        Direction = dir,
                        Rotation = spriteRotation,
                        Color = color,
                        Type = exit.Type,
                        DistanceTiles = distTiles
                    });
                }

                if (pointersToDraw.Count == 0) return;

                // Adjust positions slightly to prevent overlap if multiple pointers are close
                if (pointersToDraw.Count > 1)
                {
                    for (int i = 0; i < pointersToDraw.Count; i++)
                    {
                        for (int j = i + 1; j < pointersToDraw.Count; j++)
                        {
                            float dist = Vector2.Distance(pointersToDraw[i].Position, pointersToDraw[j].Position);
                            if (dist < 42f)
                            {
                                Vector2 tangent = new Vector2(-pointersToDraw[j].Direction.Y, pointersToDraw[j].Direction.X);
                                float offset = (42f - dist) / 2f + 2f;
                                pointersToDraw[i].Position -= tangent * offset;
                                pointersToDraw[j].Position += tangent * offset;
                            }
                        }
                    }
                }

                float pulse = Config.PulseAnimation
                    ? (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0) * 4f
                    : 0f;

                Rectangle arrowSource = new Rectangle(412, 495, 5, 4);
                Vector2 arrowOrigin = new Vector2(2.5f, 2f);

                foreach (var p in pointersToDraw)
                {
                    Vector2 drawPos = p.Position + p.Direction * pulse;
                    drawPos = Utility.makeSafe(drawPos, new Vector2(32, 32));

                    // Drop shadow for arrow
                    e.SpriteBatch.Draw(
                        Game1.mouseCursors,
                        drawPos + new Vector2(2, 2),
                        arrowSource,
                        Color.Black * 0.7f,
                        p.Rotation,
                        arrowOrigin,
                        Config.PointerScale,
                        SpriteEffects.None,
                        1f
                    );

                    // Tinted arrow
                    e.SpriteBatch.Draw(
                        Game1.mouseCursors,
                        drawPos,
                        arrowSource,
                        p.Color,
                        p.Rotation,
                        arrowOrigin,
                        Config.PointerScale,
                        SpriteEffects.None,
                        1f
                    );

                    // Draw text label & distance if enabled
                    string label = "";
                    if (Config.ShowTargetType)
                    {
                        label = p.Type == MineExitType.Ladder
                            ? Helper.Translation.Get("pointer.ladder").ToString()
                            : Helper.Translation.Get("pointer.shaft").ToString();
                    }
                    if (Config.ShowDistance)
                    {
                        string distStr = $"{(int)Math.Round(p.DistanceTiles)}m";
                        label = string.IsNullOrEmpty(label) ? distStr : $"{label} {distStr}";
                    }

                    if (!string.IsNullOrEmpty(label))
                    {
                        SpriteFont font = Game1.smallFont;
                        Vector2 textSize = font.MeasureString(label);

                        // Position label inwards toward screen center
                        Vector2 textPos = drawPos - p.Direction * (20f + textSize.Length() * 0.15f) - textSize / 2f;
                        textPos = Utility.makeSafe(textPos, textSize);

                        Utility.drawTextWithShadow(
                            e.SpriteBatch,
                            label,
                            font,
                            textPos,
                            p.Color
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnRenderedHud: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private static Color GetColor(string colorName, float opacity = 1.0f)
        {
            float alpha = Math.Clamp(opacity, 0.0f, 1.0f);
            return colorName?.Trim().ToLowerInvariant() switch
            {
                "red" => Color.Red * alpha,
                "blue" => Color.DodgerBlue * alpha,
                "green" => Color.LimeGreen * alpha,
                "yellow" => Color.Gold * alpha,
                "white" => Color.White * alpha,
                "purple" => Color.MediumPurple * alpha,
                "cyan" => Color.Cyan * alpha,
                "orange" => Color.Orange * alpha,
                "pink" => Color.HotPink * alpha,
                _ => Color.LimeGreen * alpha
            };
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 3)
        {
            if (pixelTexture == null) return;

            try
            {
                thickness = Math.Clamp(thickness, 1, 6);
                spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
                spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
                spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
                spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in DrawRectangle: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }
    }
}
