// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Xml.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.GUI;

namespace SOS.StatSections
{
    // MARK: General
    [AutoRegister(order: 0)]
    public class GeneralSection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab == null) return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.window.section_general", "GENERAL").Value, Color.Gold);

            l.BadgeRow(Texts.Get("sos.item.id", "ID:").Value, [prefab.Identifier.Value], filterPrefix: '!', onSearchFilter: SectionHelper.SetSearchFilter);

            string modName = prefab.ContentPackage?.Name ?? "Vanilla";
            l.BadgeRow(Texts.Get("sos.item.mod", "Mod:").Value, [modName], filterPrefix: '@', onSearchFilter: SectionHelper.SetSearchFilter);

            if (prefab is ItemPrefab item)
            {
                if (!item.Aliases.IsEmpty) l.BadgeRow(Texts.Get("sos.item.aliases", "Aliases:").Value, item.Aliases, onSearchFilter: SectionHelper.SetSearchFilter);
                l.BadgeRow(Texts.Get("sos.item.category", "Category:").Value, item.Category.ToString().Split(','), filterPrefix: '#', onSearchFilter: SectionHelper.SetSearchFilter);

                if (item.ConfigElement != null)
                {
                    string cargoBox = item.ConfigElement.GetAttributeString("cargocontaineridentifier", "");
                    if (!string.IsNullOrEmpty(cargoBox))
                        l.SelectorRow(Texts.Get("sos.item.cargo_box", "Cargo Box:").Value, [cargoBox], onPrimary: onPrimary, onSecondary: onSecondary, onSearchFilter: SectionHelper.SetSearchFilter);

                    var hazards = new List<string>();
                    foreach (var child in item.ConfigElement.Descendants())
                    {
                        string n = child.Name.ToString().ToLowerInvariant();
                        if (n == "fire") hazards.Add(Texts.Get("sos.item.causes_fire", "Causes Fire").Value);
                        if (n == "statuseffect" && child.GetAttributeFloat("oxygen", 0f) < -100f) hazards.Add(Texts.Get("sos.item.drains_oxygen", "Drains Oxygen").Value);
                    }
                    if (hazards.Count > 0) l.BadgeRow(Texts.Get("sos.item.hazards", "Hazards:").Value, hazards, onSearchFilter: SectionHelper.SetSearchFilter);
                }

                l.Row(Texts.Get("sos.item.max_stack", "Max Stack:").Value, item.MaxStackSize.ToString(), Color.White);
                l.BadgeRow(Texts.Get("sos.item.tags", "TAGS:").Value, item.Tags.Select(t => t.Value), filterPrefix: '$', onSearchFilter: SectionHelper.SetSearchFilter);
            }
            else if (prefab is AfflictionPrefab aff)
            {
                bool isBuff = aff.IsBuff;
                float scannerThreshold = aff.ShowInHealthScannerThreshold;
                float iconThreshold = aff.ShowIconThreshold;
                float baseHealCost = aff.BaseHealCost;
                float healMultiplier = aff.HealCostMultiplier;
                float medSkillGain = aff.MedicalSkillGain;

                float activationThreshold = 0f;
                float treatmentThreshold = 0f;
                string causeOfDeath = "";

                if (aff.configElement != null)
                {
                    activationThreshold = aff.configElement.GetAttributeFloat("activationthreshold", 0f);
                    treatmentThreshold = aff.configElement.GetAttributeFloat("treatmentthreshold", 0f);
                    causeOfDeath = aff.configElement.GetAttributeString("causeofdeathdescription", "");
                }

                l.Row(Texts.Get("sos.affliction.classification", "Classification:").Value, isBuff ? Texts.Get("sos.affliction.buff", "Buff").Value : Texts.Get("sos.affliction.debuff", "Debuff").Value, isBuff ? Color.LightGreen : Color.Salmon);
                l.BadgeRow(Texts.Get("sos.affliction.type", "Type:").Value, [aff.AfflictionType.ToString()], filterPrefix: '#', onSearchFilter: SectionHelper.SetSearchFilter);
                l.Row(Texts.Get("sos.affliction.max_strength", "Max Strength:").Value, aff.MaxStrength.ToValue(), Color.White);

                if (activationThreshold > 0) l.Row(Texts.Get("sos.affliction.activation_threshold", "Activation Threshold:").Value, activationThreshold.ToValue(), Color.Yellow);
                if (iconThreshold > 0 && iconThreshold < 1000) l.Row(Texts.Get("sos.affliction.icon_threshold", "Icon Appears At:").Value, iconThreshold.ToValue(), Color.Cyan);
                if (scannerThreshold > 0 && scannerThreshold < 1000) l.Row(Texts.Get("sos.affliction.scanner_threshold", "Scanner Detects At:").Value, scannerThreshold.ToValue(), Color.Cyan);
                if (treatmentThreshold > 0) l.Row(Texts.Get("sos.affliction.treatment_threshold", "AI Treats At:").Value, treatmentThreshold.ToValue(), Color.LightGreen);

                float totalCost = baseHealCost * healMultiplier;
                if (totalCost > 0) l.Row(Texts.Get("sos.affliction.heal_cost", "Clinic Heal Cost:").Value, $"~{(int)totalCost} mk", Color.Gold);
                if (medSkillGain > 0) l.Row(Texts.Get("sos.affliction.exp_gain", "Medical Exp Gain:").Value, $"+{medSkillGain.ToValue()}", Color.MediumPurple);

                if (aff.LimbSpecific) l.Row(Texts.Get("sos.affliction.limb_specific", "Limb Specific:").Value, Texts.Get("sos.gen.yes", "Yes").Value, Color.Gray);
                if (!string.IsNullOrEmpty(aff.IndicatorLimb.ToString()) && aff.IndicatorLimb.ToString() != "None")
                    l.Row(Texts.Get("sos.affliction.indicator_limb", "Indicator Limb:").Value, aff.IndicatorLimb.ToString(), Color.Gray);

                if (!string.IsNullOrEmpty(causeOfDeath))
                    l.RichText($"{Texts.Get("sos.affliction.death_cause", "Death Cause:").Value} {causeOfDeath}".SetColor(Color.Crimson));
            }
            return true;
        }
    }

    // MARK: Economy
    [AutoRegister(order: 1)]
    public class EconomySection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab is not ItemPrefab item) return false;

            var priceInfo = item.DefaultPrice;
            if (priceInfo == null) return false;

            int price = priceInfo.Price;
            bool canBuy = item.CanBeBought;
            bool canSell = item.CanBeSold;
            int minDifficulty = priceInfo.MinLevelDifficulty;
            Identifier requiredFaction = priceInfo.RequiredFaction;

            if (price <= 0 && !canBuy) return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.window.section_economy", "ECONOMY").Value, Color.Gold);

            l.Row(Texts.Get("sos.item.base_price", "Base Price:").Value, $"{price} mk", Color.Yellow);

            string yes = Texts.Get("sos.gen.yes", "Yes").Value;
            string no = Texts.Get("sos.gen.no", "No").Value;

            l.Row(Texts.Get("sos.item.can_buy", "Can be Bought:").Value, canBuy ? yes : no,
                canBuy ? Color.LightGreen : Color.Salmon);

            l.Row(Texts.Get("sos.item.can_sell", "Can be Sold:").Value, canSell ? yes : no, canSell ? Color.LightGreen : Color.Salmon);

            if (minDifficulty > 0)
                l.Row(Texts.Get("sos.item.min_difficulty", "Min. Difficulty:").Value, minDifficulty.ToString(), Color.White);

            if (requiredFaction != Identifier.Empty)
            {
                string factionName = TextManager.Get("FactionName." + requiredFaction).Fallback(requiredFaction.Value).Value;
                l.BadgeRow(Texts.Get("sos.item.required_faction", "Required Faction:").Value, [factionName], onSearchFilter: SectionHelper.SetSearchFilter);
            }
            return true;
        }
    }

    // MARK: weapons
    [AutoRegister(order: 2)]
    public class WeaponSection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab is not ItemPrefab item || item.ConfigElement == null) return false;

            float penetration = 0f;
            int maxTargets = 1;
            int projectileCount = 1;
            float structureDamage = 0f;
            float itemDamage = 0f;
            float reload = 0f;
            float range = 0f;
            float explosionRange = 0f;
            float powerUse = 0f;
            bool isAutomatic = false;
            float spread = 0f;
            float dmgModifier = 1f;
            float severProb = 0f;
            bool isThrowable = false;

            var afflictions = new List<AfflictionData>();

            foreach (var element in item.ConfigElement.Descendants())
            {
                string n = element.Name.ToString().ToLowerInvariant();

                if (n == "rangedweapon" || n == "meleeweapon" || n == "meleehandheld" || n == "projectile" || n == "weapon")
                {
                    reload = element.GetAttributeFloat("reload", reload);
                    range = element.GetAttributeFloat("range", range);
                    powerUse = element.GetAttributeFloat("powerconsumption", powerUse);
                    spread = Math.Max(spread, element.GetAttributeFloat("spread", 0f));
                    dmgModifier = element.GetAttributeFloat("weapondamagemodifier", dmgModifier);
                    penetration = Math.Max(penetration, element.GetAttributeFloat("penetration", 0f));

                    if (n == "projectile")
                    {
                        maxTargets = Math.Max(maxTargets, element.GetAttributeInt("maxtargetstohit", 1));
                        int pCount = element.GetAttributeInt("projectilecount", 1);
                        if (pCount == 1) pCount = element.GetAttributeInt("hitscancount", 1);
                        projectileCount = Math.Max(projectileCount, pCount);
                    }

                    if (element.GetAttributeBool("holdtrigger", false)) isAutomatic = true;
                }

                if (n == "explosion")
                {
                    explosionRange = Math.Max(explosionRange, element.GetAttributeFloat("range", 0f));
                    structureDamage = Math.Max(structureDamage, element.GetAttributeFloat("structuredamage", 0f));
                    itemDamage = Math.Max(itemDamage, element.GetAttributeFloat("itemdamage", 0f));
                    severProb = Math.Max(severProb, element.GetAttributeFloat("severlimbsprobability", 0f));

                    foreach (var aff in element.Elements().Where(e => e.Name.ToString().Equals("affliction", StringComparison.OrdinalIgnoreCase)))
                    {
                        ParseAffliction(aff, 1.0f, afflictions);
                    }
                }

                if (n == "attack")
                {
                    structureDamage = Math.Max(structureDamage, element.GetAttributeFloat("structuredamage", 0f));
                    itemDamage = Math.Max(itemDamage, element.GetAttributeFloat("itemdamage", 0f));
                    severProb = Math.Max(severProb, element.GetAttributeFloat("severlimbsprobability", 0f));
                    penetration = Math.Max(penetration, element.GetAttributeFloat("penetration", 0f));

                    foreach (var aff in element.Elements().Where(e => e.Name.ToString().Equals("affliction", StringComparison.OrdinalIgnoreCase)))
                    {
                        ParseAffliction(aff, 1.0f, afflictions);
                    }
                }

                if (n == "statuseffect")
                {
                    float prob = element.GetAttributeFloat("probability", 1.0f);
                    foreach (var aff in element.Elements().Where(e => e.Name.ToString().Equals("affliction", StringComparison.OrdinalIgnoreCase)))
                    {
                        ParseAffliction(aff, prob, afflictions);
                    }
                }

                if (n == "throwable") isThrowable = true;
            }

            if (afflictions.Count == 0 && penetration <= 0 && structureDamage <= 0 && itemDamage <= 0 && reload <= 0 && !isThrowable && explosionRange <= 0)
                return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.window.section_weapon", "AS WEAPON").Value, Color.Gold);

            if (reload > 0) l.Row(isAutomatic ? Texts.Get("sos.weapon.fire_rate", "Fire Rate:").Value : Texts.Get("sos.weapon.reload", "Reload:").Value, $"{reload}s", Color.Cyan);
            if (powerUse > 0) l.Row(Texts.Get("sos.weapon.power_use", "Power Use:").Value, $"{powerUse}kW", Color.Orange);
            if (range > 0) l.Row(Texts.Get("sos.weapon.range", "Range:").Value, range.ToMeters(), Color.LightGray);
            if (explosionRange > 0) l.Row(Texts.Get("sos.weapon.explosion_radius", "Explosion Radius:").Value, explosionRange.ToMeters(), Color.Orange);
            if (penetration > 0) l.Row(Texts.Get("sos.weapon.armor_penetration", "Armor Penetration:").Value, $"{(int)(penetration * 100)}%", Color.Orange);
            if (projectileCount > 1) l.Row(Texts.Get("sos.weapon.projectiles", "Projectiles:").Value, $"x{projectileCount}", Color.LightGray);
            if (maxTargets > 1) l.Row(Texts.Get("sos.weapon.max_targets", "Max Targets:").Value, maxTargets.ToString(), Color.LightGray);
            if (structureDamage > 0) l.Row(Texts.Get("sos.weapon.structure_damage", "Structure Damage:").Value, structureDamage.ToValue(), Color.Salmon);
            if (itemDamage > 0) l.Row(Texts.Get("sos.weapon.item_damage", "Item Damage:").Value, itemDamage.ToValue(), Color.Salmon);
            if (severProb > 0) l.Row(Texts.Get("sos.weapon.dismember_chance", "Dismember Chance:").Value, $"{(int)(severProb * 100)}%", Color.Crimson);
            if (spread > 0) l.Row(Texts.Get("sos.weapon.base_spread", "Base Spread:").Value, $"{spread:0.#}°", Color.LightGray);
            if (dmgModifier != 1f) l.Row(Texts.Get("sos.weapon.dmg_multiplier", "Dmg. Multiplier:").Value, $"x{dmgModifier:0.#}", Color.LightGreen);
            if (isThrowable) l.Row(Texts.Get("sos.weapon.type", "Type:").Value, Texts.Get("sos.weapon.throwable", "Throwable").Value, Color.White);

            var grouped = afflictions.GroupBy(a => a.Identifier);
            foreach (var group in grouped)
            {
                var first = group.First();
                string label = first.Name + ":";
                var ids = group.Select(a => a.Identifier);
                var displayNames = group.Select(a => a.Probability < 1.0f ? $"{a.Strength} ({(int)(a.Probability * 100)}%)" : a.Strength.ToValue());

                l.BadgeRow(label, ids, displayNames, linkColor: Color.Salmon, onSearchFilter: SectionHelper.SetSearchFilter);
            }
            return true;
        }

        private class AfflictionData
        {
            internal string Identifier = "";
            internal string Name = "";
            internal float Strength;
            internal float Probability;
        }

        private static void ParseAffliction(XElement element, float prob, List<AfflictionData> list)
        {
            string id = element.GetAttributeString("identifier", "");
            float strength = element.GetAttributeFloat("strength", 0f);
            if (strength <= 0 || string.IsNullOrEmpty(id)) return;

            list.Add(new AfflictionData
            {
                Identifier = id,
                Name = TextManager.Get("AfflictionName." + id).Fallback(id).Value,
                Strength = strength,
                Probability = prob
            });
        }
    }

    // MARK: equipements
    [AutoRegister(order: 3)]
    public class EquipmentSection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab is not ItemPrefab item) return false;

            var equipSlots = new List<string>();
            var statModifiers = new List<string>();
            var aggregatedResistances = new Dictionary<string, List<string>>();

            float maxPressure = 0f;
            bool deflectsProjectiles = false;
            int durability = 0;

            int health = (int)Math.Floor(item.Health);
            if (health > 0 && health != 100 && health < 100000) durability = health;

            if (item.ConfigElement != null)
            {
                foreach (var element in item.ConfigElement.Descendants())
                {
                    string n = element.Name.ToString().ToLowerInvariant();

                    if (n == "wearable" || n == "holdable")
                    {
                        string s = element.GetAttributeString("slots", "");
                        if (!string.IsNullOrEmpty(s)) equipSlots.Add(s.Replace("+", ", "));
                    }

                    if (n == "statuseffect")
                    {
                        maxPressure = Math.Max(maxPressure, element.GetAttributeFloat("PressureProtection", 0f));
                    }

                    if (n == "statvalue")
                    {
                        string type = element.GetAttributeString("stattype", "");
                        float val = element.GetAttributeFloat("value", 0f);
                        if (!string.IsNullOrEmpty(type) && val != 0f)
                        {
                            string sign = val > 0 ? "+" : "";
                            statModifiers.Add($"{type}: {sign}{Math.Round(val * 100)}%");
                        }
                    }

                    if (n == "damagemodifier")
                    {
                        if (element.GetAttributeBool("deflectprojectiles", false)) deflectsProjectiles = true;

                        float mult = element.GetAttributeFloat("damagemultiplier", 1f);
                        if (mult < 1f)
                        {
                            string raw = element.GetAttributeString("afflictionidentifiers", element.GetAttributeString("afflictiontypes", "General"));
                            foreach (var affId in raw.Split(','))
                            {
                                string trimmed = affId.Trim();
                                if (string.IsNullOrEmpty(trimmed)) continue;

                                string name = TextManager.Get("AfflictionName." + trimmed).Fallback(trimmed).Value;
                                if (!aggregatedResistances.ContainsKey(name)) aggregatedResistances[name] = [];

                                aggregatedResistances[name].Add($"{(int)Math.Round((1f - mult) * 100)}%");
                            }
                        }
                    }
                }
            }

            if (equipSlots.Count == 0 && statModifiers.Count == 0 && aggregatedResistances.Count == 0 && maxPressure <= 0 && durability <= 0)
                return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.window.section_equipment", "EQUIPMENT").Value, Color.Gold);

            if (durability > 0)
                l.Row(Texts.Get("sos.equip.max_durability", "Max Durability:").Value, durability.ToString(), Color.White);

            if (maxPressure > 0)
                l.Row(Texts.Get("sos.equip.pressure_protection", "Pressure Protection:").Value, maxPressure.ToMeters(), Color.DeepSkyBlue);

            if (deflectsProjectiles)
                l.Row(Texts.Get("sos.equip.armor_special", "Armor Special:").Value, Texts.Get("sos.equip.deflect_projectiles", "Deflects Projectiles").Value, Color.LightGray);

            foreach (var mod in statModifiers.Distinct())
            {
                var parts = mod.Split(':');
                Color color = parts[1].Trim().StartsWith('-') ? Color.Salmon : Color.LightGreen;
                l.Row(parts[0] + ":", parts[1], color);
            }

            foreach (var res in aggregatedResistances)
            {
                l.BadgeRow($"{res.Key} {Texts.Get("sos.equip.res_suffix", "Res:").Value}", [res.Key], [string.Join(", ", res.Value)], linkColor: Color.LightGreen, onSearchFilter: SectionHelper.SetSearchFilter);
            }

            if (equipSlots.Count > 0)
            {
                var uniqueSlots = equipSlots
                    .SelectMany(s => s.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries))
                    .Distinct();

                l.BadgeRow(Texts.Get("sos.equip.equips_in", "Equips In:").Value, uniqueSlots, filterPrefix: '&', onSearchFilter: SectionHelper.SetSearchFilter);
            }
            return true;
        }
    }

    // MARK: Medical
    [AutoRegister(order: 4)]
    public class MedicalSection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab is not ItemPrefab item || item.ConfigElement == null) return false;

            int medicalSkillReq = 0;
            var suitableTreatments = new List<(string Identifier, string DisplayName)>();

            var alwaysHeals = new Dictionary<string, (string Name, float Amount)>();
            var alwaysCauses = new Dictionary<string, (string Name, float Amount)>();
            var successHeals = new Dictionary<string, (string Name, float Amount)>();
            var successCauses = new Dictionary<string, (string Name, float Amount)>();
            var failureHeals = new Dictionary<string, (string Name, float Amount)>();
            var failureCauses = new Dictionary<string, (string Name, float Amount)>();

            foreach (var element in item.ConfigElement.Descendants())
            {
                string n = element.Name.ToString().ToLowerInvariant();

                if (n == "suitabletreatment")
                {
                    string idOrType = element.GetAttributeString("identifier", element.GetAttributeString("type", ""));
                    float suit = element.GetAttributeFloat("suitability", 0f);
                    if (!string.IsNullOrEmpty(idOrType))
                    {
                        string sign = suit > 0 ? "+" : "";
                        suitableTreatments.Add((idOrType, $"{GetAfflictionName(idOrType)} ({sign}{suit})"));
                    }
                }

                else if (n == "requiredskill" && element.GetAttributeString("identifier", "") == "medical")
                {
                    medicalSkillReq = Math.Max(medicalSkillReq, element.GetAttributeInt("level", 0));
                }

                else if (n == "statuseffect")
                {
                    string type = element.GetAttributeString("type", "").ToLowerInvariant();
                    string target = element.GetAttributeString("target", "").ToLowerInvariant();

                    if (!target.Contains("usetarget") && !target.Contains("character") && !target.Contains("limb")) continue;

                    bool isFailure = type == "onfailure";
                    bool isSuccess = type == "onsuccess";
                    bool isAlways = !isFailure && !isSuccess;

                    float duration = element.GetAttributeFloat("duration", 1f);

                    foreach (var sub in element.Elements())
                    {
                        string subName = sub.Name.ToString().ToLowerInvariant();
                        if (subName == "affliction" || subName == "reduceaffliction")
                        {
                            string idOrType = sub.GetAttributeString("identifier", sub.GetAttributeString("type", ""));
                            if (string.IsNullOrEmpty(idOrType)) continue;

                            float rawAmount = sub.GetAttributeFloat("amount", sub.GetAttributeFloat("strength", 0f));
                            float totalAmount = rawAmount * duration;

                            bool isHeal = subName == "reduceaffliction" || totalAmount < 0;
                            totalAmount = Math.Abs(totalAmount);

                            string affName = GetAfflictionName(idOrType);

                            if (isHeal)
                                AddStat(isFailure ? failureHeals : (isSuccess ? successHeals : alwaysHeals), idOrType, affName, totalAmount);
                            else
                                AddStat(isFailure ? failureCauses : (isSuccess ? successCauses : alwaysCauses), idOrType, affName, totalAmount);
                        }
                    }
                }
            }

            if (suitableTreatments.Count == 0 && alwaysHeals.Count == 0 && successHeals.Count == 0 && alwaysCauses.Count == 0 && successCauses.Count == 0)
                return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.window.section_medical", "MEDICAL").Value, Color.Gold);

            if (medicalSkillReq > 0)
                l.BadgeRow(Texts.Get("sos.med.skill_req", "Medical Skill Req:").Value, [medicalSkillReq.ToString()], ["medical " + medicalSkillReq.ToString()], linkColor: Color.Orange, onSearchFilter: SectionHelper.SetSearchFilter);

            if (suitableTreatments.Count > 0)
            {
                l.BadgeRow(
                    Texts.Get("sos.med.suitable", "Recommended:").Value,
                    suitableTreatments.Select(t => t.Identifier),
                    suitableTreatments.Select(t => t.DisplayName),
                    linkColor: Color.LightSkyBlue,
                    onSearchFilter: SectionHelper.SetSearchFilter
                );
            }

            void DrawHyperlinkEffect(string label, Dictionary<string, (string Name, float Amount)> dict, Color linkColor)
            {
                if (dict.Count == 0) return;

                var ids = dict.Keys;
                var displayNames = dict.Select(kvp => $"{kvp.Value.Name} ({kvp.Value.Amount.ToValue()})");

                l.BadgeRow(label, ids, displayNames, linkColor: linkColor, onSearchFilter: SectionHelper.SetSearchFilter);
            }

            DrawHyperlinkEffect(Texts.Get("sos.med.always_heals", "Always Heals:").Value, alwaysHeals, Color.LightGreen);
            DrawHyperlinkEffect(Texts.Get("sos.med.always_causes", "Always Applies:").Value, alwaysCauses, Color.Salmon);

            DrawHyperlinkEffect(Texts.Get("sos.med.success_heals", "On Success Heals:").Value, successHeals, Color.LightGreen);
            DrawHyperlinkEffect(Texts.Get("sos.med.success_causes", "On Success Applies:").Value, successCauses, Color.Salmon);

            DrawHyperlinkEffect(Texts.Get("sos.med.failure_heals", "On Failure Heals:").Value, failureHeals, Color.DarkSeaGreen);
            DrawHyperlinkEffect(Texts.Get("sos.med.failure_causes", "On Failure Applies:").Value, failureCauses, Color.Crimson);
            return true;
        }

        private static void AddStat(Dictionary<string, (string Name, float Amount)> dict, string id, string name, float amount)
        {
            if (dict.TryGetValue(id, out (string Name, float Amount) current))
            {
                dict[id] = (current.Name, current.Amount + amount);
            }
            else dict[id] = (name, amount);
        }

        private static string GetAfflictionName(string idOrType)
        {
            var loc = TextManager.Get("AfflictionName." + idOrType);
            if (loc.Loaded && !loc.Value.Contains("AfflictionName.")) return loc.Value;

            if (idOrType.Length > 0) return char.ToUpper(idOrType[0]) + idOrType[1..];
            return idOrType;
        }
    }

    // MARK: utility
    [AutoRegister(order: 5)]
    public class UtilitySection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab is not ItemPrefab item || item.ConfigElement == null) return false;

            var deviceProperties = new Dictionary<string, string>();

            foreach (var child in item.ConfigElement.Descendants())
            {
                string n = child.Name.ToString().ToLowerInvariant();

                if (n == "wificomponent" && child.GetAttribute("range") != null)
                    deviceProperties[Texts.Get("sos.util.radio_range", "Radio Range").Value] = child.GetAttributeFloat("range", 0).ToMeters();

                if (n == "lightcomponent" && child.GetAttribute("range") != null)
                    deviceProperties[Texts.Get("sos.util.light_range", "Light Range").Value] = child.GetAttributeFloat("range", 0).ToMeters();

                if (n == "pump" && child.GetAttribute("maxflow") != null)
                    deviceProperties[Texts.Get("sos.util.pump_flow", "Pump Max Flow").Value] = child.GetAttributeFloat("maxflow", 0).ToMeters();

                if (n == "sonar" && child.GetAttribute("range") != null)
                    deviceProperties[Texts.Get("sos.util.sonar_range", "Sonar Range").Value] = child.GetAttributeFloat("range", 0).ToMeters();
            }

            if (deviceProperties.Count == 0) return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.window.section_utility", "UTILITY").Value, Color.Gold);

            foreach (var prop in deviceProperties)
                l.Row(prop.Key + ":", prop.Value, Color.Cyan);
            return true;
        }
    }

    // MARK: container
    [AutoRegister(order: 6)]
    public class ContainerSection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab is not ItemPrefab item) return false;

            string capacity = "";
            var acceptedTags = new HashSet<string>();
            var spawnLocations = new List<string>();
            var compatibleItems = new List<Prefab>();

            if (item.ConfigElement != null)
            {
                foreach (var child in item.ConfigElement.Descendants())
                {
                    string n = child.Name.ToString().ToLowerInvariant();

                    if (n == "itemcontainer" || n == "magazine")
                    {
                        string cap = child.GetAttributeString("capacity", "");
                        if (!string.IsNullOrEmpty(cap)) capacity = cap;
                    }

                    if (n == "containable")
                    {
                        string itemsAttr = child.GetAttributeString("items", "");
                        if (!string.IsNullOrEmpty(itemsAttr))
                        {
                            foreach (var tag in itemsAttr.Split(','))
                            {
                                string trimmed = tag.Trim();
                                if (!string.IsNullOrEmpty(trimmed)) acceptedTags.Add(trimmed);
                            }
                        }
                    }
                }
            }

            if (item.PreferredContainers != null && !item.PreferredContainers.IsDefaultOrEmpty)
            {
                foreach (var container in item.PreferredContainers)
                {
                    foreach (var primary in container.Primary)
                    {
                        string locName = TextManager.Get("EntityName." + primary).Fallback(primary.Value).Value;
                        if (!spawnLocations.Contains(locName)) spawnLocations.Add(locName);
                    }
                }
            }

            if (acceptedTags.Count > 0)
            {
                compatibleItems = [.. ItemPrefab.Prefabs.Where(p =>
                    acceptedTags.Contains(p.Identifier.Value) ||
                    p.Tags.Any(t => acceptedTags.Contains(t.Value))
                ).OrderBy(p => p.Name.Value)];
            }

            if (string.IsNullOrEmpty(capacity) && compatibleItems.Count == 0 && spawnLocations.Count == 0)
                return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.window.section_container", "CONTAINER").Value, Color.Gold);

            if (!string.IsNullOrEmpty(capacity))
            {
                l.Row(Texts.Get("sos.container.capacity", "Capacity:").Value,
                    Texts.Get("sos.container.slots", "[amount] Slots").Replace("[amount]", capacity).Value, Color.White);
            }

            if (compatibleItems.Count > 0)
            {
                _ = new GUIDesplegableBox(contentPanel.Content, SectionHelper.SetSearchFilter,
                    Texts.Get("sos.container.accepts", "Accepts:").Value,
                    acceptedTags, compatibleItems, onPrimary, onSecondary);
            }

            if (spawnLocations.Count > 0)
            {
                l.BadgeRow(Texts.Get("sos.container.contained", "Contained by:").Value, spawnLocations, onSearchFilter: SectionHelper.SetSearchFilter);
            }
            return true;
        }
    }

    // MARK: Affliction effects
    [AutoRegister(order: 7)]
    public class AfflictionEffectsSection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab is not AfflictionPrefab aff || aff.configElement == null) return false;

            var phases = new List<PhaseData>();
            var periodicPhases = new List<PhaseData>();

            foreach (var element in aff.configElement.GetChildElements("Effect"))
            {
                var phase = new PhaseData
                {
                    Range = $"{element.GetAttributeFloat("minstrength", 0f).ToValue()} - {element.GetAttributeFloat("maxstrength", 0f).ToValue()}",
                    StrengthChange = element.GetAttributeFloat("strengthchange", 0f)
                };

                // vit
                float vitMin = element.GetAttributeFloat("minvitalitydecrease", 0f);
                float vitMax = element.GetAttributeFloat("maxvitalitydecrease", 0f);
                bool isPercent = element.GetAttributeBool("multiplybymaxvitality", false);

                if (vitMax > 0)
                {
                    string vitStr = vitMin == vitMax ? vitMax.ToValue() : $"{vitMin.ToValue()} to {vitMax.ToValue()}";
                    vitStr += isPercent ? "%" : " pts";
                    phase.Stats.Add($"{Texts.Get("sos.affliction.max_hp_penalty", "Max HP Penalty:").Value} -{vitStr.SetColor(Color.OrangeRed)}");
                }

                // modf
                float speedMin = element.GetAttributeFloat("minspeedmultiplier", 1f);
                float speedMax = element.GetAttributeFloat("maxspeedmultiplier", 1f);
                if (speedMin != 1f || speedMax != 1f)
                {
                    string speedStr = speedMin == speedMax ? speedMax.ToValue() : $"{speedMin.ToValue()} to {speedMax.ToValue()}";
                    phase.Stats.Add($"{Texts.Get("sos.affliction.speed", "Speed:").Value} x{speedStr.SetColor((speedMax >= 1f) ? Color.LimeGreen : Color.OrangeRed)}");
                }

                // effects
                List<string> effectList = [];
                if (element.GetAttributeFloat("maxscreendistort", 0f) > 0) effectList.Add("disort");
                if (element.GetAttributeFloat("maxscreenblur", 0f) > 0) effectList.Add("blur");
                if (element.GetAttributeFloat("maxradialdistort", 0f) > 0) effectList.Add("radial");
                if (element.GetAttributeFloat("maxchromaticaberration", 0f) > 0) effectList.Add("chroma");
                if (effectList.Count > 0)
                    phase.Stats.Add($"{Texts.Get("sos.affliction.visual_distortions", "Visual Distortions").Value} ({string.Join(", ", effectList)})".SetColor(Color.Orange));

                float convulse = element.GetAttributeFloat("convulseamount", 0f);
                if (convulse > 0) phase.Stats.Add($"{Texts.Get("sos.affliction.convulsions", "Convulsions/Spasms").Value} ({convulse})".SetColor(Color.OrangeRed));

                // res
                string resList = element.GetAttributeString("resistancefor", "");
                if (!string.IsNullOrEmpty(resList))
                {
                    float resMin = element.GetAttributeFloat("minresistance", 0f);
                    float resMax = element.GetAttributeFloat("maxresistance", 0f);
                    string resStr = resMin == resMax ? $"{(resMax * 100):0.#}%" : $"{(resMin * 100):0.#}% to {(resMax * 100):0.#}%";

                    Color resColor = resMax > 0 ? Color.LightGreen : Color.Salmon;
                    phase.Resistances.Add($"{resList.Replace(",", ", ")} ({resStr.SetColor(resColor)})");
                }

                ParseStatusEffects(element, phase);

                if (phase.Stats.Count > 0 || phase.Resistances.Count > 0 || phase.LinkedAfflictions.Count > 0 || phase.Events.Count > 0 || phase.StrengthChange != 0)
                    phases.Add(phase);
            }

            foreach (var element in aff.configElement.GetChildElements("PeriodicEffect"))
            {
                var phase = new PhaseData
                {
                    Range = $"{Texts.Get("sos.affliction.interval", "Interval:").Value} {element.GetAttributeFloat("mininterval", 1f).ToValue()}s - {element.GetAttributeFloat("maxinterval", 1f).ToValue()}s"
                };

                float minStr = element.GetAttributeFloat("minstrength", 0f);
                float maxStr = element.GetAttributeFloat("maxstrength", 0f);
                if (minStr > 0 || maxStr > 0)
                {
                    phase.Range += $" ({Texts.Get("sos.affliction.str", "Str:").Value} {minStr.ToValue()} - {maxStr.ToValue()})";
                }

                ParseStatusEffects(element, phase);

                if (phase.LinkedAfflictions.Count > 0 || phase.Events.Count > 0)
                    periodicPhases.Add(phase);
            }

            if (phases.Count == 0 && periodicPhases.Count == 0)
                return false;

            if (phases.Count > 0)
            {
                using var l = new GUILayoutBuilder(contentPanel);
                l.Header(Texts.Get("sos.affliction.effects_header", "EFFECTS BY STRENGTH PHASE").Value, Color.Gold);

                foreach (var phase in phases)
                {
                    l.RichText($"{Texts.Get("sos.affliction.strength_range", "Strength Range:").Value} {phase.Range.SetColor(Color.Orange)}");

                    if (phase.StrengthChange != 0)
                    {
                        string trend = phase.StrengthChange > 0
                            ? $"{Texts.Get("sos.affliction.worsens", "Worsens:").Value} +{phase.StrengthChange}/s".SetColor(Color.Salmon)
                            : $"{Texts.Get("sos.affliction.natural_healing", "Natural Healing:").Value} {phase.StrengthChange}/s".SetColor(Color.LightGreen);
                        l.RichText($"  -> {trend}");
                    }

                    if (phase.Stats.Count > 0)
                        l.RichText($"  -> {string.Join(" | ", phase.Stats)}");

                    if (phase.Resistances.Count > 0)
                        l.RichText($"  -> {Texts.Get("sos.affliction.resistances", "Resistances:").Value} {string.Join(" | ", phase.Resistances)}");

                    if (phase.Events.Count > 0)
                        l.RichText($"  -> {string.Join(", ", phase.Events).SetColor(Color.MediumPurple)}");

                    if (phase.LinkedAfflictions.Count > 0)
                    {
                        l.SelectorRow($"  -> {Texts.Get("sos.affliction.triggers", "Triggers:").Value}",
                            phase.LinkedAfflictions.Select(a => a.ID),
                            phase.LinkedAfflictions.Select(a => a.Name.SetColor(a.Theme)),
                            fallbackFilterPrefix: '!',
                            onPrimary: onPrimary,
                            onSecondary: onSecondary,
                            onSearchFilter: SectionHelper.SetSearchFilter);
                    }

                    l.RichText(" ");
                }
            }

            if (periodicPhases.Count > 0)
            {
                using var l = new GUILayoutBuilder(contentPanel);
                l.Header(Texts.Get("sos.affliction.periodic_header", "PERIODIC EVENTS").Value, Color.MediumPurple);
                foreach (var phase in periodicPhases)
                {
                    l.RichText($"{Texts.Get("sos.affliction.frequency", "Frequency:").Value} {phase.Range.SetColor(Color.Cyan)}");

                    if (phase.Events.Count > 0)
                        l.RichText($"  -> {string.Join(", ", phase.Events).SetColor(Color.MediumPurple)}");

                    if (phase.LinkedAfflictions.Count > 0)
                    {
                        l.SelectorRow($"  -> {Texts.Get("sos.affliction.triggers", "Triggers:").Value}",
                            phase.LinkedAfflictions.Select(a => a.ID),
                            phase.LinkedAfflictions.Select(a => a.Name.SetColor(a.Theme)),
                            fallbackFilterPrefix: '!',
                            onPrimary: onPrimary,
                            onSecondary: onSecondary,
                            onSearchFilter: SectionHelper.SetSearchFilter);
                    }
                    l.RichText(" ");
                }
            }
            return true;
        }

        private class PhaseData
        {
            public string Range = "";
            public float StrengthChange;
            public List<string> Stats = [];
            public List<string> Resistances = [];
            public List<string> Events = [];
            public List<(string ID, string Name, Color Theme)> LinkedAfflictions = [];
        }

        private static void ParseStatusEffects(Barotrauma.ContentXElement parentElement, PhaseData phase)
        {
            bool hasSounds = false;
            bool hasParticles = false;
            bool hasExplosion = false;
            bool hasAnimations = false;

            foreach (var se in parentElement.GetChildElements("StatusEffect"))
            {
                if (se.GetChildElements("Sound").Any()) hasSounds = true;
                if (se.GetChildElements("ParticleEmitter").Any()) hasParticles = true;
                if (se.GetChildElements("Explosion").Any()) hasExplosion = true;
                if (se.GetChildElements("TriggerAnimation").Any()) hasAnimations = true;

                foreach (var sub in se.Elements())
                {
                    string n = sub.Name.ToString().ToLowerInvariant();
                    if (n == "affliction" || n == "reduceaffliction")
                    {
                        string id = sub.GetAttributeString("identifier", sub.GetAttributeString("type", ""));
                        if (string.IsNullOrEmpty(id)) continue;

                        float amt = sub.GetAttributeFloat("amount", sub.GetAttributeFloat("strength", 0f));
                        float prob = sub.GetAttributeFloat("probability", 1f);

                        var targetAff = AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == id);
                        string displayName = targetAff != null ? targetAff.Name.Value : id;

                        bool isHeal = n == "reduceaffliction" || amt < 0;
                        string sign = isHeal ? "-" : "+";
                        string probStr = prob < 1f ? $" ({prob * 100:0.#}%)" : "";

                        string finalName = $"{displayName} {sign}{Math.Abs(amt).ToValue()}{probStr}";
                        Color theme = isHeal ? Color.LightGreen : Color.OrangeRed;

                        phase.LinkedAfflictions.Add((id, finalName, theme));
                    }
                }
            }

            if (hasSounds) phase.Events.Add(Texts.Get("sos.affliction.event_sounds", "Triggers Sounds/Noises").Value);
            if (hasParticles) phase.Events.Add(Texts.Get("sos.affliction.event_particles", "Spawns Particles").Value);
            if (hasExplosion) phase.Events.Add(Texts.Get("sos.affliction.event_explosion", "Causes Explosion").Value);
            if (hasAnimations) phase.Events.Add(Texts.Get("sos.affliction.event_animations", "Forces Animations").Value);
        }
    }

    // MARK: Affliction Treatments
    [AutoRegister(order: 8)]
    public class AfflictionTreatmentSection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab is not AfflictionPrefab affliction) return false;

            var highEff = new List<ItemPrefab>();
            var medEff = new List<ItemPrefab>();
            var lowEff = new List<ItemPrefab>();
            var harmful = new List<ItemPrefab>();
            var blockers = new List<string>();

            if (affliction.IgnoreTreatmentIfAfflictedBy != null)
            {
                foreach (var blockerId in affliction.IgnoreTreatmentIfAfflictedBy)
                    blockers.Add(blockerId.Value);
            }

            if (affliction.TreatmentSuitabilities != null)
            {
                foreach (var kvp in affliction.TreatmentSuitabilities)
                {
                    var item = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier == kvp.Key);
                    if (item == null) continue;

                    float suit = kvp.Value;
                    var target = suit switch
                    {
                        >= 50f => highEff,
                        >= 20f => medEff,
                        > 0f => lowEff,
                        < 0f => harmful,
                        _ => null
                    };
                    target?.Add(item);
                }
            }

            if (affliction.TreatmentSuitabilities != null)
            {
                int CompareSuitDesc(ItemPrefab a, ItemPrefab b) => affliction.TreatmentSuitabilities[b.Identifier].CompareTo(affliction.TreatmentSuitabilities[a.Identifier]);
                int CompareSuitAsc(ItemPrefab a, ItemPrefab b) => affliction.TreatmentSuitabilities[a.Identifier].CompareTo(affliction.TreatmentSuitabilities[b.Identifier]);
                highEff.Sort(CompareSuitDesc);
                medEff.Sort(CompareSuitDesc);
                lowEff.Sort(CompareSuitDesc);
                harmful.Sort(CompareSuitAsc);
            }

            if (highEff.Count == 0 && medEff.Count == 0 && lowEff.Count == 0 && harmful.Count == 0 && blockers.Count == 0)
                return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.window.section_treatments", "TREATMENTS & MEDICATION").Value, Color.SpringGreen);

            if (blockers.Count > 0)
            {
                var displayNames = blockers.Select(b =>
                    AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == b)?.Name.Value ?? b);

                l.SelectorRow(Texts.Get("sos.affliction.blockedby", "Treatment Blocked By:").Value, blockers, displayNames,
                    fallbackFilterPrefix: '!',
                    onPrimary: onPrimary,
                    onSecondary: onSecondary,
                    onSearchFilter: SectionHelper.SetSearchFilter);
            }

            void DrawRow(string label, List<ItemPrefab> items, Color? labelColor = null)
            {
                if (items.Count == 0) return;

                var ids = items.Select(i => i.Identifier.Value);
                var names = items.Select(i => $"{i.Name.Value} ({affliction.TreatmentSuitabilities?[i.Identifier]:0})");

                l.SelectorRow(label, ids, names,
                    fallbackFilterPrefix: '!',
                    labelColor: labelColor,
                    onPrimary: onPrimary,
                    onSecondary: onSecondary,
                    onSearchFilter: SectionHelper.SetSearchFilter);
            }

            DrawRow(Texts.Get("sos.affliction.highlyeffective", "Highly Effective:").Value, highEff);
            DrawRow(Texts.Get("sos.affliction.effective", "Effective:").Value, medEff);
            DrawRow(Texts.Get("sos.affliction.alternative", "Alternative / Weak:").Value, lowEff);

            if (harmful.Count > 0)
            {
                l.RichText(Texts.Get("sos.affliction.contraindicated_warn", "WARNING: The following items worsen the condition!").Value.SetColor(Color.Salmon));
                DrawRow(Texts.Get("sos.affliction.contraindicated", "Contraindicated:").Value, harmful, labelColor: Color.Salmon);
            }
            return true;
        }
    }

    // MARK: Description
    [AutoRegister(order: 9)]
    public class DescriptionSection : ISOSStatSection
    {
        public bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            string? text = prefab switch
            {
                ItemPrefab item => item.Description?.Value ?? "",
                AfflictionPrefab affliction => string.Join("\n\n", affliction.Descriptions.Select(d => $"({d.MinStrength.ToString().SetColor(Color.Orange)}-{d.MaxStrength.ToString().SetColor(Color.OrangeRed)}) {d.Target.ToString().SetColor(Color.BlueViolet)}: {d.Text}")),
                _ => null
            };

            if (string.IsNullOrEmpty(text)) return false;

            using var l = new GUILayoutBuilder(contentPanel);
            l.Header(Texts.Get("sos.item.description", "DESCRIPTION").Value, Color.Gold);
            l.RichText(RichString.Rich(text));
            return true;
        }
    }

    internal static class SectionHelper
    {
        public static void SetSearchFilter(string tag) => API.Emit(CommKeys.SetSearchFilter, tag);
    }
}