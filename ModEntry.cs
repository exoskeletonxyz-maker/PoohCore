using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FruitTrees;

namespace PoohCore
{
    public partial class ModEntry : Mod
    {
        public static HashSet<string> ListOfModifiedNPC = new HashSet<string>();
        private static List<string> ListOfMailFlag = new List<string>();

        // Universal gift tastes diisi runtime agar aman di Android
        private static string[] universalLoves = new string[0];
        private static string[] universalHates = new string[0];
        private static string[] universalLikes = new string[0];
        private static string[] universalDislikes = new string[0];
        private static string[] universalNeutrals = new string[0];

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(NPC), "behaviorOnFarmerLocationEntry"),
                    postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.behaviorOnFarmerLocationEntry_Postfix))
                );
                harmony.Patch(
                    AccessTools.Method(typeof(StardewValley.Event), "addActor"),
                    postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.addActor_Postfix))
                );
                harmony.Patch(
                    AccessTools.Method(typeof(NPC), "resetForNewDay"),
                    prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.resetForNewDay_Prefix))
                );
            }
            catch (Exception ex)
            {
                Monitor.Log($"Harmony patch gagal: {ex}", LogLevel.Warn);
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            try
            {
                var api = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
                if (api != null)
                {
                    api.RegisterToken(ModManifest, "GetMailFlagProgressNumber", new GetMailFlagProgressNumberToken());
                    api.RegisterToken(ModManifest, "GetGiftTasteCPTokenFromSomeOne", new GetGiftTasteCPTokenFromSomeOneToken());
                }
                else
                {
                    Monitor.Log("Content Patcher tidak ditemukan, token tidak diregister.", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Gagal register token CP: {ex}", LogLevel.Warn);
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            try
            {
                if (Game1.NPCGiftTastes != null)
                {
                    universalLoves = ArgUtility.SplitBySpace(Game1.NPCGiftTastes.ContainsKey("Universal_Love") ? Game1.NPCGiftTastes["Universal_Love"] : "");
                    universalHates = ArgUtility.SplitBySpace(Game1.NPCGiftTastes.ContainsKey("Universal_Hate") ? Game1.NPCGiftTastes["Universal_Hate"] : "");
                    universalLikes = ArgUtility.SplitBySpace(Game1.NPCGiftTastes.ContainsKey("Universal_Like") ? Game1.NPCGiftTastes["Universal_Like"] : "");
                    universalDislikes = ArgUtility.SplitBySpace(Game1.NPCGiftTastes.ContainsKey("Universal_Dislike") ? Game1.NPCGiftTastes["Universal_Dislike"] : "");
                    universalNeutrals = ArgUtility.SplitBySpace(Game1.NPCGiftTastes.ContainsKey("Universal_Neutral") ? Game1.NPCGiftTastes["Universal_Neutral"] : "");
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Gagal load gift tastes: {ex}", LogLevel.Warn);
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            foreach (NPC npc in Utility.getAllVillagers())
            {
                try
                {
                    if (npc == null) continue;
                    var data = npc.GetData();
                    if (data?.CustomFields != null)
                    {
                        if (data.CustomFields.TryGetValue("poohnhi.PoohCore/WideCharacter", out string wideValue))
                        {
                            if (!string.IsNullOrEmpty(wideValue) && wideValue != "false")
                                ListOfModifiedNPC.Add(npc.Name);
                        }

                        if (data.CustomFields.TryGetValue("poohnhi.PoohCore/HighCharacter", out string highValue))
                        {
                            if (!string.IsNullOrEmpty(highValue) && highValue != "false")
                                ListOfModifiedNPC.Add(npc.Name);
                        }
                    }
                }
                catch { }
            }
        }

        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            try
            {
                foreach (var todayMailFlag in ListOfMailFlag)
                {
                    if (Game1.player.mailReceived.Contains(todayMailFlag))
                    {
                        Game1.player.mailReceived.Remove(todayMailFlag);
                        string todayMailFlagNumber = GetMailFlagProgressNumberFromMailFlag(todayMailFlag);
                        string seenFlag = todayMailFlag + todayMailFlagNumber;
                        string nextFlag = todayMailFlag + GetNextNumberFromString(todayMailFlagNumber);
                        Game1.player.mailReceived.Remove(seenFlag);
                        Game1.player.mailReceived.Add(nextFlag);
                    }
                }
                ListOfMailFlag.Clear();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error di OnDayEnding: {ex}", LogLevel.Warn);
            }
        }

        // Placeholder methods
        private string GetMailFlagProgressNumberFromMailFlag(string flag) => "0";
        private string GetNextNumberFromString(string number) => (int.Parse(number) + 1).ToString();
    }
}
