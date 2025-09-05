using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using UnityEngine;
using static Akula.CustomFunctions;
using static Akula.Plugin;
using static Akula.DescriptionFunctions;
using static Akula.CharacterFunctions;
using System.Text;
using TMPro;
using Obeliskial_Essentials;
using System.Data.Common;

namespace Akula
{
    [HarmonyPatch]
    internal class Traits
    {
        // list of your trait IDs

        public static string[] simpleTraitList = ["trait0", "trait1a", "trait1b", "trait2a", "trait2b", "trait3a", "trait3b", "trait4a", "trait4b"];

        public static string[] myTraitList = simpleTraitList.Select(trait => subclassname.ToLower() + trait).ToArray(); // Needs testing

        public static string trait0 = myTraitList[0];
        // static string trait1b = myTraitList[1];
        public static string trait2a = myTraitList[3];
        public static string trait2b = myTraitList[4];
        public static string trait4a = myTraitList[7];
        public static string trait4b = myTraitList[8];

        // public static int infiniteProctection = 0;
        // public static int bleedInfiniteProtection = 0;
        public static bool isDamagePreviewActive = false;

        public static bool isCalculateDamageActive = false;
        public static int infiniteProctection = 0;

        public static string debugBase = "Binbin - Testing " + heroName + " ";


        public static void DoCustomTrait(string _trait, ref Trait __instance)
        {
            // get info you may need
            Enums.EventActivation _theEvent = Traverse.Create(__instance).Field("theEvent").GetValue<Enums.EventActivation>();
            Character _character = Traverse.Create(__instance).Field("character").GetValue<Character>();
            Character _target = Traverse.Create(__instance).Field("target").GetValue<Character>();
            int _auxInt = Traverse.Create(__instance).Field("auxInt").GetValue<int>();
            string _auxString = Traverse.Create(__instance).Field("auxString").GetValue<string>();
            CardData _castedCard = Traverse.Create(__instance).Field("castedCard").GetValue<CardData>();
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            List<CardData> cardDataList = [];
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();

            if (!IsLivingHero(_character))
            {
                return;
            }
            string traitName = traitData.TraitName;
            string traitId = _trait;


            if (_trait == trait0)
            {
                // Wet on you increases speed by 1 per 2 charges. 
                // Wet on Enemies prevents Bleed from being prevented or removed unless specified. 
                LogDebug($"Handling Trait {traitId}: {traitName}");
            }


            else if (_trait == trait2a)
            {
                // trait2a
                // Block +1 for every 3 Wet on you. 
                // Speed +1 for every 15 Bleed on enemies.
            }



            else if (_trait == trait2b)
            {
                // trait2b:
                // Single hit and Special cards do 50% bonus damage for every energy spent.
                LogDebug($"Handling Trait {traitId}: {traitName}");

            }

            else if (_trait == trait4a)
            {
                // trait 4a;
                // when you play a Defense, reduce your highest cost card by 2 until discarded(3 uses)
                if (CanIncrementTraitActivations(traitId) && _castedCard.HasCardType(Enums.CardType.Defense))// && MatchManager.Instance.energyJustWastedByHero > 0)
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    CardData highestCostCard = GetRandomHighestCostCard(Enums.CardType.None, heroHand);
                    ReduceCardCost(ref highestCostCard, amountToReduce: 2, isPermanent: false);
                    IncrementTraitActivations(traitId);
                }
            }

            else if (_trait == trait4b)
            {
                // trait 4b:
                // Heal yourself for 30% of damage done. 
                // All Damage for all heroes is increased by 1% per Speed. 
                // Your damage is increased by 2% per Speed instead.
                Vampirism(ref _character, _auxInt, 0.3f, _castedCard);
                LogDebug($"Handling Trait {traitId}: {traitName}");
            }

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static bool DoTrait(Enums.EventActivation _theEvent, string _trait, Character _character, Character _target, int _auxInt, string _auxString, CardData _castedCard, ref Trait __instance)
        {
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
                return false;
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            if (Content.medsCustomTraitsSource.Contains(_trait) && myTraitList.Contains(_trait))
            {
                DoCustomTrait(_trait, ref __instance);
                return false;
            }
            return true;
        }

        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Character), "GetTraitDamagePercentModifiers")]
        // public static void GetTraitDamagePercentModifiersPostfix(Enums.DamageType DamageType, ref bool ___useCache, ref float __result, Character __instance, CardData ___cardCasted)

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "GetTraitDamagePercentModifiers")]
        public static void GetTraitDamagePercentModifiersPostfix(Enums.DamageType DamageType, int energyCost, ref float[] __result, Character __instance, CardData ___cardCasted)
        {
            // ___useCache = false;
            if (AtOManager.Instance.TeamHaveTrait(trait4b))
            {
                if (IsLivingHero(__instance) && __instance.HaveTrait(trait4b))
                {
                    int speed = __instance.Speed;
                    __result[1] += 2f * speed;
                }
                else if (IsLivingHero(__instance))
                {
                    int speed = __instance.Speed;
                    __result[1] += 1f * speed;
                }
            }
            if (IsLivingHero(__instance) && __instance.HaveTrait(trait2a))
            {
                if (___cardCasted == null || MatchManager.Instance == null)
                {
                    return;
                }
                // Single hit and Special cards do 50% bonus damage for every energy spent.
                bool isSingleHit = ___cardCasted != null && ___cardCasted.EffectRepeat <= 1 && ___cardCasted.TargetType == Enums.CardTargetType.Single;
                if (isSingleHit || ___cardCasted.CardClass == Enums.CardClass.Special)
                {
                    // int energy = MatchManager.Instance.energyJustWastedByHero;
                    // __result[1] += 50f * energy;
                    __result[1] += 50f * energyCost;
                }



            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "GetAuraCurseQuantityModification")]
        public static void GetAuraCurseQuantityModificationPostfix(string id, Enums.CardClass CC, ref int __result, Character __instance)
        {
            if (__instance == null || __instance.HaveTrait(trait2a) == false || MatchManager.Instance == null)
            {
                return;
            }

            LogDebug("GetAuraCurseQuantityModificationPostfix - handing trait2a");
            switch (id)
            {
                case "wet":
                    // Block +1 for every 3 Wet on you. 
                    int wetCharges = __instance.GetAuraCharges("wet");
                    int blockBonus = wetCharges / 3;
                    __result += blockBonus;
                    LogDebug($"GetAuraCurseQuantityModificationPostfix - trait2a - wetCharges {wetCharges} blockBonus {blockBonus} new __result {__result}");
                    break;
                case "bleed":
                    // Speed +1 for every 15 Bleed on enemies.

                    // int bleedCharges = 0;
                    // foreach (Character enemy in MatchManager.Instance.GetTeamNPC())
                    // {
                    //     bleedCharges += enemy.GetAuraCharges("bleed");
                    // }
                    int bleedCharges = MatchManager.Instance.GetTeamNPC().Sum(enemy => enemy.GetAuraCharges("bleed"));
                    int speedBonus = bleedCharges / 15;
                    __result += speedBonus;
                    LogDebug($"GetAuraCurseQuantityModificationPostfix - trait2a - bleedCharges {bleedCharges} speedBonus {speedBonus} new __result {__result}");

                    break;
            }

        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), "GlobalAuraCurseModificationByTraitsAndItems")]
        // [HarmonyPriority(Priority.Last)]
        public static void GlobalAuraCurseModificationByTraitsAndItemsPostfix(ref AtOManager __instance, ref AuraCurseData __result, string _type, string _acId, Character _characterCaster, Character _characterTarget)
        {
            // LogInfo($"GACM {subclassName}");

            Character characterOfInterest = _type == "set" ? _characterTarget : _characterCaster;
            string traitOfInterest;
            switch (_acId)
            {
                // trait0:
                // Wet on you increases speed by 1 per 2 charges. 
                // Wet on Enemies prevents Bleed from being prevented or removed unless specified. 

                // trait2a
                // Block +1 for every 3 Wet on you. Speed +1 for every 15 Bleed on enemies.

                case "wet":
                    traitOfInterest = trait0;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.ThisHero))
                    {
                        __result.CharacterStatModified = Enums.CharacterStat.Speed;
                        __result.CharacterStatModifiedValuePerStack = 1;
                        __result.CharacterStatChargesMultiplierNeededForOne = 2;
                    }
                    traitOfInterest = trait4a;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.ThisHero))
                    {
                        __result.Removable = false;
                    }
                    break;
                case "bleed":
                    traitOfInterest = trait0;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.Monsters) && characterOfInterest.HasEffect("wet"))
                    {
                        __result.Preventable = false;
                        __result.Removable = false;
                    }
                    break;
            }
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(Character), "HealAuraCurse")]
        // public static void HealAuraCursePrefix(ref Character __instance, AuraCurseData AC, ref int __state)
        // {
        //     LogInfo($"HealAuraCursePrefix {subclassName}");
        //     string traitOfInterest = trait4b;
        //     if (IsLivingHero(__instance) && __instance.HaveTrait(traitOfInterest) && AC == GetAuraCurseData("stealth"))
        //     {
        //         __state = Mathf.FloorToInt(__instance.GetAuraCharges("stealth") * 0.25f);
        //         // __instance.SetAuraTrait(null, "stealth", 1);

        //     }

        // }

        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Character), "HealAuraCurse")]
        // public static void HealAuraCursePostfix(ref Character __instance, AuraCurseData AC, int __state)
        // {
        //     LogInfo($"HealAuraCursePrefix {subclassName}");
        //     string traitOfInterest = trait4b;
        //     if (IsLivingHero(__instance) && __instance.HaveTrait(traitOfInterest) && AC == GetAuraCurseData("stealth") && __state > 0)
        //     {
        //         // __state = __instance.GetAuraCharges("stealth");
        //         __instance.SetAuraTrait(null, "stealth", __state);
        //     }

        // }




        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        public static void CalculateDamagePrePostForThisCharacterPrefix()
        {
            isDamagePreviewActive = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        public static void CalculateDamagePrePostForThisCharacterPostfix()
        {
            isDamagePreviewActive = false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        public static void SetDamagePreviewPrefix()
        {
            isDamagePreviewActive = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        public static void SetDamagePreviewPostfix()
        {
            isDamagePreviewActive = false;
        }

        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Character), nameof(Character.SetEvent))]
        // public static void SetEventPostfix(
        //     Enums.EventActivation theEvent,
        //     Character target = null,
        //     int auxInt = 0,
        //     string auxString = "")
        // {
        //     if (theEvent == Enums.EventActivation.BeginTurnCardsDealt && AtOManager.Instance.TeamHaveTrait(trait2b))
        //     {
        //         string cardToPlay = "tacticianexpectedprophecy";
        //         PlayCardForFree(cardToPlay);
        //     }

        // }





        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(CardData), nameof(CardData.SetDescriptionNew))]
        // public static void SetDescriptionNewPostfix(ref CardData __instance, bool forceDescription = false, Character character = null, bool includeInSearch = true)
        // {
        //     // LogInfo("executing SetDescriptionNewPostfix");
        //     if (__instance == null)
        //     {
        //         LogDebug("Null Card");
        //         return;
        //     }
        //     if (!Globals.Instance.CardsDescriptionNormalized.ContainsKey(__instance.Id))
        //     {
        //         LogError($"missing card Id {__instance.Id}");
        //         return;
        //     }


        //     if (__instance.CardName == "Mind Maze")
        //     {
        //         StringBuilder stringBuilder1 = new StringBuilder();
        //         LogDebug($"Current description for {__instance.Id}: {stringBuilder1}");
        //         string currentDescription = Globals.Instance.CardsDescriptionNormalized[__instance.Id];
        //         stringBuilder1.Append(currentDescription);
        //         // stringBuilder1.Replace($"When you apply", $"When you play a Mind Spell\n or apply");
        //         stringBuilder1.Replace($"Lasts one turn", $"Lasts two turns");
        //         BinbinNormalizeDescription(ref __instance, stringBuilder1);
        //     }
        // }

    }
}

