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
    public sealed class ModConfig
    {
        public bool DebugMode { get; set; } = false;
        public bool EnableMod { get; set; } = true;
        public string OutlineColor { get; set; } = "Green";
        public float OutlineOpacity { get; set; } = 0.5f;
        public KeybindList ToggleKey { get; set; } = KeybindList.Parse("None"); // Sử dụng KeybindList thay vì SButton
    }

    internal sealed class ModEntry : Mod
    {
        private ModConfig Config = null!;
        private readonly HashSet<Vector2> ladderPositions = new();
        private readonly List<KeyValuePair<Point, bool>> ladderQueue = new();
        private bool skippedFirstTick = false;
        private Texture2D? pixelTexture;
        private bool hasCheckedInitialLadders = false;

        public override void Entry(IModHelper helper)
        {
            try
            {
                // Đọc config
                Config = helper.ReadConfig<ModConfig>();
                if (Config == null)
                {
                    Monitor.Log("Failed to load config, using default values.", LogLevel.Error);
                    Config = new ModConfig();
                }

                // Khởi tạo texture
                InitializeTexture();

                // Đăng ký sự kiện
                helper.Events.GameLoop.GameLaunched += OnGameLaunched;
                helper.Events.World.LocationListChanged += OnLocationListChanged;
                helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
                helper.Events.Player.Warped += OnPlayerWarped;
                helper.Events.Display.RenderedWorld += OnRenderedWorld;
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
            // Tích hợp Generic Mod Config Menu
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
                // Kiểm tra xem GMCM có được tải không
                var gmcmModInfo = helper.ModRegistry.Get("spacechase0.GenericModConfigMenu");
                if (gmcmModInfo == null)
                {
                    Monitor.Log("Generic Mod Config Menu mod not found in ModRegistry. Please ensure GMCM is installed. Configurations must be set manually in config.json.", LogLevel.Warn);
                    return;
                }

                // Kiểm tra phiên bản GMCM
                var gmcmVersion = gmcmModInfo.Manifest.Version.ToString();
                Monitor.Log($"Found Generic Mod Config Menu version {gmcmVersion}. Attempting to access API.", LogLevel.Debug);
                if (!gmcmModInfo.Manifest.Version.IsNewerThan("1.12.0"))
                {
                    Monitor.Log($"Generic Mod Config Menu version {gmcmVersion} may be outdated. Consider updating to version 1.14.1 or newer for compatibility.", LogLevel.Warn);
                }

                // Lấy API
                var gmcmApi = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (gmcmApi == null)
                {
                    Monitor.Log($"Failed to access Generic Mod Config Menu API for version {gmcmVersion}. Ensure GMCM is compatible with SMAPI 3.18.6.", LogLevel.Warn);
                    return;
                }

                // Đăng ký menu cấu hình
                RegisterModConfigMenu(gmcmApi);
                Monitor.Log("Successfully registered Generic Mod Config Menu.", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error initializing GMCM: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Register the mod configuration menu using GMCM.
        /// </summary>
        private void RegisterModConfigMenu(IGenericModConfigMenuApi api)
        {
            api.Register(
                mod: ModManifest,
                reset: ResetConfig,
                save: SaveConfig,
                titleScreenOnly: false
            );

            AddOptions(api);
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

        /// <summary>
        /// Add all mod configuration options to the GMCM menu.
        /// </summary>
        private void AddOptions(IGenericModConfigMenuApi api)
        {
            api.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Mod",
                tooltip: () => "Enable or disable the ladder outline feature.",
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );

            api.AddBoolOption(
                mod: ModManifest,
                name: () => "Debug Mode",
                tooltip: () => "Show debug messages for ladder detection.",
                getValue: () => Config.DebugMode,
                setValue: value => Config.DebugMode = value
            );

            api.AddTextOption(
                mod: ModManifest,
                name: () => "Outline Color",
                tooltip: () => "Color of the ladder outline.",
                getValue: () => Config.OutlineColor,
                setValue: value => Config.OutlineColor = value,
                allowedValues: new[] { "Red", "Blue", "Green", "Yellow", "White", "Purple", "Cyan", "Orange" }
            );

            api.AddNumberOption(
                mod: ModManifest,
                name: () => "Outline Opacity",
                tooltip: () => "Opacity of the ladder outline (0.0 to 1.0).",
                getValue: () => Config.OutlineOpacity,
                setValue: value => Config.OutlineOpacity = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.1f
            );

            api.AddKeybindList(
                mod: ModManifest,
                name: () => "Toggle Mod Key",
                tooltip: () => "Key to toggle the mod on or off. Set to 'None' to disable.",
                getValue: () => Config.ToggleKey,
                setValue: value => Config.ToggleKey = value
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
                    Game1.addHUDMessage(new HUDMessage($"Ladder Outline {(Config.EnableMod ? "enabled" : "disabled")}.", HUDMessage.newQuest_type));
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
                    Monitor.Log($"Processing delayed ladder at {ladder.Key}.", LogLevel.Trace);
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

                    // Xử lý ladder động
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
                // Ladder được tạo tự động
                var generatedLadders = Helper.Reflection.GetField<NetPointDictionary<bool, NetBool>>(shaft, "createLadderDownEvent", false)?.GetValue();
                if (generatedLadders != null)
                {
                    generatedLadders.OnValueAdded += OnLadderLocationAdded;
                    int count = generatedLadders.Count();
                    Monitor.Log($"Found {count} dynamic ladders in level {shaft.mineLevel}.", LogLevel.Trace);
                    foreach (var ladder in generatedLadders.Pairs)
                    {
                        if (!ladderQueue.Any(kv => kv.Key == ladder.Key))
                            ladderQueue.Add(ladder);
                    }
                }

                // Ladder do người chơi đặt
                var placedLadders = Helper.Reflection.GetField<NetVector2Dictionary<bool, NetBool>>(shaft, "createLadderAtEvent", false)?.GetValue();
                if (placedLadders != null)
                {
                    placedLadders.OnValueAdded += OnLadderLocationAdded;
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
                ladderPositions.Clear();
                hasCheckedInitialLadders = false;

                if (e.NewLocation is MineShaft shaft)
                {
                    Monitor.Log($"Warped to mine level {shaft.mineLevel}, finding ladders.", LogLevel.Trace);
                    FindLadders(shaft);
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
                if (!hasCheckedInitialLadders && e.IsMultipleOf(30) && Game1.currentLocation is MineShaft shaft)
                {
                    Monitor.Log($"Checking ladders after warp in level {shaft.mineLevel}.", LogLevel.Trace);
                    FindLadders(shaft);
                    hasCheckedInitialLadders = true;
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
                foreach (var layer in shaft.map.Layers)
                {
                    if (layer?.Tiles == null) continue;

                    for (int x = 0; x < layer.LayerWidth; x++)
                    {
                        for (int y = 0; y < layer.LayerHeight; y++)
                        {
                            var tile = layer.Tiles[x, y];
                            if (tile?.TileIndex == 173)
                            {
                                var position = new Vector2(x * Game1.tileSize, y * Game1.tileSize);
                                if (ladderPositions.Add(position))
                                {
                                    ladderCount++;
                                    Monitor.Log($"Ladder found at {position} (Tile {x},{y}) on layer {layer.Id}.", LogLevel.Trace);
                                    if (Config.DebugMode)
                                        Game1.addHUDMessage(new HUDMessage($"Ladder at {x},{y} ({layer.Id})", HUDMessage.newQuest_type));
                                }
                            }
                        }
                    }
                }
                Monitor.Log($"Found {ladderCount} ladders in level {shaft.mineLevel}.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in FindLadders: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnLadderLocationAdded(Point point, bool shaft)
        {
            OnLadderLocationAdded(new Vector2(point.X, point.Y), shaft);
        }

        private void OnLadderLocationAdded(Vector2 point, bool shaft)
        {
            try
            {
                var position = point * Game1.tileSize;
                if (ladderPositions.Add(position))
                {
                    Monitor.Log($"Added dynamic ladder at {position}.", LogLevel.Trace);
                    if (Config.DebugMode)
                        Game1.addHUDMessage(new HUDMessage($"Dynamic ladder at {point}!", HUDMessage.newQuest_type));
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnLadderLocationAdded: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsWorldReady || ladderPositions.Count == 0 || pixelTexture == null) return;

            try
            {
                Game1.InUIMode(() =>
                {
                    foreach (var position in ladderPositions)
                    {
                        var rect = new Rectangle((int)position.X, (int)position.Y, Game1.tileSize, Game1.tileSize);
                        rect.Offset(-Game1.viewport.X, -Game1.viewport.Y);
                        var color = GetOutlineColor();
                        DrawRectangle(rect, color);
                    }
                });
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in OnRenderedWorld: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }

        private Color GetOutlineColor()
        {
            try
            {
                float opacity = Math.Clamp(Config.OutlineOpacity, 0.0f, 1.0f);
                return Config.OutlineColor.ToLower() switch
                {
                    "red" => Color.Red * opacity,
                    "blue" => Color.Blue * opacity,
                    "green" => Color.Green * opacity,
                    "yellow" => Color.Yellow * opacity,
                    "white" => Color.White * opacity,
                    "purple" => Color.Purple * opacity,
                    "cyan" => Color.Cyan * opacity,
                    "orange" => Color.Orange * opacity,
                    _ => Color.Red * opacity
                };
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in GetOutlineColor: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                return Color.Red * 0.5f;
            }
        }

        private void DrawRectangle(Rectangle rect, Color color)
        {
            if (pixelTexture == null) return;

            try
            {
                Game1.spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Top, rect.Width, 3), color);
                Game1.spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Bottom - 3, rect.Width, 3), color);
                Game1.spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Top, 3, rect.Height), color);
                Game1.spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - 3, rect.Top, 3, rect.Height), color);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in DrawRectangle: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }
    }
}