using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using StardewValley.Extensions;
using static System.Collections.Specialized.BitVector32;
using StardewValley.Delegates;
using StardewValley.TokenizableStrings;
using StardewValley.GameData.Characters;
using StardewValley.ItemTypeDefinitions;
using Microsoft.Xna.Framework.Media;
using System.Linq;
using static StardewValley.GameStateQuery;
using StardewValley.Internal;
using StardewValley.Objects;

namespace PoohCore
{
    public partial class ModEntry
    {
        public static string GetMailFlagProgressNumberFromMailFlag(string mailFlag)
        {
            string currentNum = "0";
            foreach (string s in Game1.player.mailReceived)
            {
                if (s.Contains(mailFlag) && s.Length != mailFlag.Length)
                {
                    string getNumMailFlag = s.Replace(mailFlag, "");
                    int num = Int32.Parse(getNumMailFlag);
                    currentNum = num.ToString();
                    break;
                }
            }
            return currentNum;
        }
        public static string GetNextNumberFromString(string number)
        {
            int num = Int32.Parse(number);
            num += 1;
            return num.ToString();
        }
        internal class GetGiftTasteCPTokenFromSomeOneToken
        {
            private string NPCName = "";
            private string relativecheck = "false";
            public bool AllowsInput()
            {
                return true;
            }
            public bool RequiresInput()
            {
                return true;
            }
            public bool CanHaveMultipleValues(string input = null)
            {
                return false;
            }
            public bool UpdateContext()
            {
                string check = "";
                string oldCheck = this.NPCName;
                string relativecheck = "";
                string oldrelativeCheck = this.relativecheck;
                return (check != oldCheck || relativecheck != oldrelativeCheck);
            }
            public IEnumerable<string> GetValues(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    yield break;
                string[] tempInput = input.Split(' ');
                string NPCInternalName = tempInput[0];
                string giftTaste = tempInput[1];
                string relativeCheck = tempInput[2];
                string excludeId = tempInput[3];
                string[] excludeIdList;
                if (excludeId != "none")
                    excludeIdList = excludeId.Split('-');
                else
                    excludeIdList = "".Split(' ');
                this.relativecheck = relativeCheck;
                yield return GetGiftTasteCPTokenFromSomeOne(NPCInternalName, giftTaste, relativeCheck, excludeIdList);
            }
        }
        internal class GetMailFlagProgressNumberToken
        {
            private string MailFlag = "";
            public bool AllowsInput()
            {
                return true;
            }
            public bool RequiresInput()
            {
                return true;
            }
            public bool CanHaveMultipleValues(string input = null)
            {
                return false;
            }
            public bool UpdateContext()
            {
                string check = "";
                string oldCheck = this.MailFlag;
                return check != oldCheck;
            }
            public IEnumerable<string> GetValues(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    yield break;
                string[] tempInput = input.Split(' ');
                string thisMailFlag = tempInput[0];
                this.MailFlag = thisMailFlag;
                if (!ModEntry.ListOfMailFlag.Contains(thisMailFlag))
                {
                    ModEntry.ListOfMailFlag.Add(thisMailFlag);
                }
                if (GetNextNumberFromString(GetMailFlagProgressNumberFromMailFlag(thisMailFlag)) != null && GetNextNumberFromString(GetMailFlagProgressNumberFromMailFlag(thisMailFlag)) != "")
                    yield return GetNextNumberFromString(GetMailFlagProgressNumberFromMailFlag(thisMailFlag));
                else yield return "1";
            }
        }
        
        public static int ParseGiftTaste(string giftTasteString)
        {
            int tempNum = 9;
            switch (giftTasteString)
            {
                case "loved":
                case "love":
                    tempNum = 1;
                    break;
                case "liked":
                case "like":
                    tempNum = 3;
                    break;
                case "disliked":
                case "dislike":
                    tempNum = 5;
                    break;
                case "hated":
                case "hate":
                    tempNum = 7;
                    break;
            }
            return tempNum;
        }

        public static string GetIDGenerator(string internalNPC, string giftTaste, string[] excludeIdList, int randomSeed)
        {
            int randomCounter = 1;
            Random r = Utility.CreateDaySaveRandom(Game1.stats.DaysPlayed * 10 * randomCounter * randomSeed);

            // giftTaste is loved, liked, ... hated
            // internalNPC is the internal NPC name that it try to get gift taste
            NPC npc = Game1.getCharacterFromName(internalNPC);
            if (npc != null)
            {
                CharacterData npcData = npc.GetData();
                Dictionary<string, string> npcGiftTastes = DataLoader.NpcGiftTastes(Game1.content);
                if (npcGiftTastes.TryGetValue(internalNPC, out var rawGiftTasteData))
                {
                    string[] rawGiftTasteFields = rawGiftTasteData.Split('/');
                    string[] Items = ArgUtility.SplitBySpace(ArgUtility.Get(rawGiftTasteFields, ParseGiftTaste(giftTaste)));
                    string[] FinalItems = Items;
                    switch (ParseGiftTaste(giftTaste))
                    {
                        case 1:
                            FinalItems = Items.Union(ModEntry.universalLoves).ToArray();
                            break;
                        case 3:
                            FinalItems = Items.Union(ModEntry.universalLikes).ToArray();
                            break;
                        case 5:
                            FinalItems = Items.Union(ModEntry.universalDislikes).ToArray();
                            break;
                        case 7:
                            FinalItems = Items.Union(ModEntry.universalHates).ToArray();
                            break;
                        case 9:
                            FinalItems = Items.Union(ModEntry.universalNeutrals).ToArray();
                            break;
                    }
                    string item;
                    item = r.Choose(FinalItems);
                    // if the item chosen is in excludeIdList, choose again
                    while (Array.IndexOf(excludeIdList, item) > -1)
                    {
                        randomCounter += 1;
                        r = Utility.CreateRandom(Game1.stats.DaysPlayed * 75 * randomCounter * randomSeed);
                        item = r.Choose(Items);
                    }
                    return item;
                }
            }
            return "74";
        }
        public static string GetOneRandomGiftTasteItem(string internalNPC, string giftTaste, string[] excludeIdList, int initRandomSeed)
        {
            var ExcludeIDs = new HashSet<string>(excludeIdList);
            int randomSeed = initRandomSeed;
            int tryGetFiveRandom = 0;
            // giftTaste is loved, liked, ... hated
            // internalNPC is the internal NPC name that it try to get gift taste
            NPC npc = Game1.getCharacterFromName(internalNPC);
            if (npc != null)
            {
                string getIDStraightFromGiftTaste = GetIDGenerator(internalNPC, giftTaste, excludeIdList, randomSeed);
                //string query = "RANDOM_ITEMS (O)" + " " + GetIDGenerator + " " + GetIDGenerator;
                string query = "RANDOM_ITEMS (O)";
                Item getItemFromId = ItemRegistry.Create(getIDStraightFromGiftTaste);
                while (tryGetFiveRandom < 5)
                {
                    Item getOneRandomItem = ItemQueryResolver.TryResolveRandomItem(query, null, true, ExcludeIDs);
                    tryGetFiveRandom++;
                    randomSeed++;
                    if(npc.getGiftTasteForThisItem(getOneRandomItem) == ParseGiftTaste(giftTaste)-1)
                        return getOneRandomItem.ItemId;
                }
                while (true)
                {
                    if (npc.getGiftTasteForThisItem(getItemFromId) == ParseGiftTaste(giftTaste) - 1)
                        return getItemFromId.ItemId;
                    getIDStraightFromGiftTaste = GetIDGenerator(internalNPC, giftTaste, excludeIdList, randomSeed);
                    getItemFromId = ItemRegistry.Create(getIDStraightFromGiftTaste);
                    randomSeed++;
                }
            }
            return "74";
        }

        public static string GetGiftTasteCPTokenFromSomeOne(string NPCInternalName, string giftTaste, string relativeCheck, string[] excludeIdList)
        {
            string replacement = "";
            replacement += " |originalCharacterName=" + NPCInternalName;
            try
            {
                NPC npc = Game1.getCharacterFromName(NPCInternalName);
                if (relativeCheck == "true" || relativeCheck == "True")
                {
                    string relativeName = "";
                    string relativeTitle = "";
                    string relativeDisplayName = "";
                    if (npc != null)
                    {
                        CharacterData npcData = npc.GetData();
                        while (npcData?.FriendsAndFamily != null && Utility.TryGetRandom(npcData.FriendsAndFamily, out relativeName, out relativeTitle))
                        {
                            NPC relativenpc = Game1.getCharacterFromName(relativeName);
                            if (relativenpc != null)
                            {
                                relativeDisplayName = relativenpc.displayName;
                                break;
                            }
                        }
                        replacement += " |relativeCharacterName=" + relativeName;
                        if (relativeTitle != "" && relativeTitle != relativeDisplayName && relativeTitle != relativeName)
                        {
                            replacement += " |relativeHasSpecialTitle=" + "true";
                            replacement += " |relativeCharacterTitle=" + TokenParser.ParseText(relativeTitle);
                        }
                        else
                        {
                            replacement += " |relativeHasSpecialTitle=" + "false";
                            replacement += " |relativeCharacterTitle=" + relativeDisplayName;
                        }
                    }
                    string itemId = GetOneRandomGiftTasteItem(relativeName, giftTaste, excludeIdList, 88);
                    replacement += " |GiftTasteItemId=" + itemId;
                }
            }
            catch { }
            if (relativeCheck != "true" && relativeCheck != "True")
            {
                string itemId = GetOneRandomGiftTasteItem(NPCInternalName, giftTaste, excludeIdList, 11);
                replacement += " |GiftTasteItemId=" + itemId;
            }
            return replacement;
        }
    }
}
    