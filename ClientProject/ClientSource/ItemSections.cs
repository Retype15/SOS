// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Xml.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public class GeneralSection : ISOSStatSection

    #region Sections

    // MARK: General
    {
        private Prefab? prefab;
        public double Order => 0;
        public string Id => GetType().FullOrName();

        private string cargoBox = "";
        private readonly List<string> hazards = [];

        private bool isBuff;
        private float activationThreshold;
        private float treatmentThreshold;
        private float scannerThreshold;
        private float iconThreshold;
        private float baseHealCost;
        private float healMultiplier;
        private float medSkillGain;
        private string causeOfDeath = "";

        public bool Analyze(Prefab prefab)
        {
            this.prefab = prefab;
            switch (prefab)
            {
                case ItemPrefab item:
                    if (item.ConfigElement != null)
                    {
                        cargoBox = item.ConfigElement.GetAttributeString("cargocontaineridentifier", "");
                        foreach (var child in item.ConfigElement.Descendants())
                        {
                            string n = child.Name.ToString().ToLowerInvariant();
                            if (n == "fire") hazards.Add(TextSOS.Get("sos.item.causes_fire", "Causes Fire").Value);
                            if (n == "statuseffect" && child.GetAttributeFloat("oxygen", 0f) < -100f) hazards.Add(TextSOS.Get("sos.item.drains_oxygen", "Drains Oxygen").Value);
                        }
                    }
                    break;
                case AfflictionPrefab affliction:
                    isBuff = affliction.IsBuff;
                    scannerThreshold = affliction.ShowInHealthScannerThreshold;
                    iconThreshold = affliction.ShowIconThreshold;
                    baseHealCost = affliction.BaseHealCost;
                    healMultiplier = affliction.HealCostMultiplier;
                    medSkillGain = affliction.MedicalSkillGain;

                    if (affliction.configElement != null)
                    {
                        activationThreshold = affliction.configElement.GetAttributeFloat("activationthreshold", 0f);
                        treatmentThreshold = affliction.configElement.GetAttributeFloat("treatmentthreshold", 0f);

                        string codRaw = affliction.configElement.GetAttributeString("causeofdeathdescription", "");
                        if (!string.IsNullOrEmpty(codRaw))
                            causeOfDeath = TextManager.Get(codRaw).Fallback(codRaw).Value;
                    }
                    break;
            }
            return prefab != null;
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (prefab == null) return;

            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.window.section_general", "GENERAL").Value, Color.Gold);

            l.BadgeRow(TextSOS.Get("sos.item.id", "ID:").Value, [prefab.Identifier.Value], filterPrefix: '!', onSearchFilter: SOSController.Instance.SetSearchFilter);

            string modName = prefab.ContentPackage?.Name ?? "Vanilla";
            l.BadgeRow("Mod:", [modName], filterPrefix: '@', onSearchFilter: SOSController.Instance.SetSearchFilter);

            if (prefab is ItemPrefab item)
            {
                if (!item.Aliases.IsEmpty) l.BadgeRow(TextSOS.Get("sos.item.aliases", "Aliases:").Value, item.Aliases, onSearchFilter: SOSController.Instance.SetSearchFilter);
                l.BadgeRow(TextSOS.Get("sos.item.category", "Category:").Value, item.Category.ToString().Split(','), filterPrefix: '#', onSearchFilter: SOSController.Instance.SetSearchFilter);
                if (!string.IsNullOrEmpty(cargoBox)) l.SelectorRow(TextSOS.Get("sos.item.cargo_box", "Cargo Box:").Value, [cargoBox], onPrimary: onPrimary, onSecondary: onSecondary, onSearchFilter: SOSController.Instance.SetSearchFilter);
                l.Row(TextSOS.Get("sos.item.max_stack", "Max Stack:").Value, item.MaxStackSize.ToString(), Color.White);
                if (hazards.Count > 0) l.BadgeRow(TextSOS.Get("sos.item.hazards", "Hazards:").Value, hazards, onSearchFilter: SOSController.Instance.SetSearchFilter);
                l.BadgeRow(TextSOS.Get("sos.item.tags", "TAGS:").Value, item.Tags.Select(t => t.Value), filterPrefix: '$', onSearchFilter: SOSController.Instance.SetSearchFilter);
            }
            else if (prefab is AfflictionPrefab aff)
            {
                l.Row("Classification:", isBuff ? "Buff (Positive)" : "Debuff (Negative)", isBuff ? Color.LightGreen : Color.Salmon);
                l.BadgeRow("Type:", [aff.AfflictionType.ToString()], filterPrefix: '#', onSearchFilter: SOSController.Instance.SetSearchFilter);
                l.Row("Max Strength:", aff.MaxStrength.ToValue(), Color.White);

                if (activationThreshold > 0) l.Row("Activation Threshold:", activationThreshold.ToValue(), Color.Yellow);
                if (iconThreshold > 0 && iconThreshold < 1000) l.Row("Icon Appears At:", iconThreshold.ToValue(), Color.Cyan);
                if (scannerThreshold > 0 && scannerThreshold < 1000) l.Row("Scanner Detects At:", scannerThreshold.ToValue(), Color.Cyan);
                if (treatmentThreshold > 0) l.Row("AI Treats At:", treatmentThreshold.ToValue(), Color.LightGreen);

                float totalCost = baseHealCost * healMultiplier;
                if (totalCost > 0) l.Row("Clinic Heal Cost:", $"~{(int)totalCost} mk", Color.Gold);
                if (medSkillGain > 0) l.Row("Medical Exp Gain:", $"+{medSkillGain.ToValue()}", Color.MediumPurple);

                if (aff.LimbSpecific) l.Row("Limb Specific:", "Yes", Color.Gray);
                if (!string.IsNullOrEmpty(aff.IndicatorLimb.ToString()) && aff.IndicatorLimb.ToString() != "None")
                    l.Row("Indicator Limb:", aff.IndicatorLimb.ToString(), Color.Gray);

                if (!string.IsNullOrEmpty(causeOfDeath))
                    l.RichText($"Death Cause: {causeOfDeath}".SetColor(Color.Crimson));
            }
        }
    }

    // MARK: Economy
    public class EconomySection : ISOSStatSection
    {
        private int price;
        private bool canBuy;
        private bool canSell;
        private int minDifficulty;
        private Identifier requiredFaction = Identifier.Empty;

        public double Order => 10;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
            {
                var priceInfo = item.DefaultPrice;
                if (priceInfo != null)
                {
                    price = priceInfo.Price;
                    canBuy = item.CanBeBought;
                    canSell = item.CanBeSold;
                    minDifficulty = priceInfo.MinLevelDifficulty;
                    requiredFaction = priceInfo.RequiredFaction;
                }
            }
            return price > 0 || canBuy;
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.window.section_economy", "ECONOMY").Value, Color.Gold);

            l.Row(TextSOS.Get("sos.item.base_price", "Base Price:").Value, $"{price} mk", Color.Yellow);

            string yes = TextSOS.Get("sos.gen.yes", "Yes").Value;
            string no = TextSOS.Get("sos.gen.no", "No").Value;

            l.Row(TextSOS.Get("sos.item.can_buy", "Can be Bought:").Value, canBuy ? yes : no,
                canBuy ? Color.LightGreen : Color.Salmon);

            l.Row(TextSOS.Get("sos.item.can_sell", "Can be Sold:").Value, canSell ? yes : no, canSell ? Color.LightGreen : Color.Salmon);

            if (minDifficulty > 0)
                l.Row(TextSOS.Get("sos.item.min_difficulty", "Min. Difficulty:").Value, minDifficulty.ToString(), Color.White);

            if (requiredFaction != Identifier.Empty)
            {
                string factionName = TextManager.Get("FactionName." + requiredFaction).Fallback(requiredFaction.Value).Value;
                l.BadgeRow(TextSOS.Get("sos.item.required_faction", "Required Faction:").Value, [factionName], onSearchFilter: SOSController.Instance.SetSearchFilter);
            }
        }
    }

    // MARK: weapons
    public class WeaponSection : ISOSStatSection
    {
        private float penetration = 0f;
        private int maxTargets = 1;
        private int projectileCount = 1;
        private float structureDamage = 0f;
        private float itemDamage = 0f;
        private float reload = 0f;
        private float range = 0f;
        private float explosionRange = 0f;
        private float powerUse = 0f;
        private bool isAutomatic = false;
        private float spread = 0f;
        private float dmgModifier = 1f;
        private float severProb = 0f;
        private bool isThrowable = false;

        private readonly List<AfflictionData> afflictions = [];

        public class AfflictionData
        {
            public string Identifier = "";
            public string Name = "";
            public float Strength;
            public float Probability;
        }

        public double Order => 20;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
            {

                if (item.ConfigElement == null) return false;

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
                            ParseAffliction(aff, 1.0f);
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
                            ParseAffliction(aff, 1.0f);
                        }
                    }

                    if (n == "statuseffect")
                    {
                        float prob = element.GetAttributeFloat("probability", 1.0f);
                        foreach (var aff in element.Elements().Where(e => e.Name.ToString().Equals("affliction", StringComparison.OrdinalIgnoreCase)))
                        {
                            ParseAffliction(aff, prob);
                        }
                    }

                    if (n == "throwable") isThrowable = true;
                }
            }
            return afflictions.Count > 0 || penetration > 0 || structureDamage > 0 || itemDamage > 0 || reload > 0 || isThrowable || explosionRange > 0;
        }

        private void ParseAffliction(XElement element, float prob)
        {
            string id = element.GetAttributeString("identifier", "");
            float strength = element.GetAttributeFloat("strength", 0f);
            if (strength <= 0 || string.IsNullOrEmpty(id)) return;

            afflictions.Add(new AfflictionData
            {
                Identifier = id,
                Name = TextManager.Get("AfflictionName." + id).Fallback(id).Value,
                Strength = strength,
                Probability = prob
            });
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.window.section_weapon", "COMBAT STATS").Value, Color.Gold);

            if (reload > 0) l.Row(isAutomatic ? "Fire Rate:" : "Reload:", $"{reload}s", Color.Cyan);
            if (powerUse > 0) l.Row("Power Use:", $"{powerUse}kW", Color.Orange);
            if (range > 0) l.Row("Range:", range.ToMeters(), Color.LightGray);
            if (explosionRange > 0) l.Row("Explosion Radius:", explosionRange.ToMeters(), Color.Orange);
            if (penetration > 0) l.Row("Armor Penetration:", $"{(int)(penetration * 100)}%", Color.Orange);
            if (projectileCount > 1) l.Row("Projectiles:", $"x{projectileCount}", Color.LightGray);
            if (maxTargets > 1) l.Row("Max Targets:", maxTargets.ToString(), Color.LightGray);
            if (structureDamage > 0) l.Row("Structure Damage:", structureDamage.ToValue(), Color.Salmon);
            if (itemDamage > 0) l.Row("Item Damage:", itemDamage.ToValue(), Color.Salmon);
            if (severProb > 0) l.Row("Dismember Chance:", $"{(int)(severProb * 100)}%", Color.Crimson);
            if (spread > 0) l.Row("Base Spread:", $"{spread:0.#}°", Color.LightGray);
            if (dmgModifier != 1f) l.Row("Dmg. Multiplier:", $"x{dmgModifier:0.#}", Color.LightGreen);
            if (isThrowable) l.Row("Type:", "Throwable", Color.White);

            var grouped = afflictions.GroupBy(a => a.Identifier);
            foreach (var group in grouped)
            {
                var first = group.First();
                string label = first.Name + ":";
                var ids = group.Select(a => a.Identifier);
                var displayNames = group.Select(a => a.Probability < 1.0f ? $"{a.Strength} ({(int)(a.Probability * 100)}%)" : a.Strength.ToValue());

                l.BadgeRow(label, ids, displayNames, linkColor: Color.Salmon, onSearchFilter: SOSController.Instance.SetSearchFilter);
            }
        }
    }

    // MARK: equipements
    public class EquipmentSection : ISOSStatSection
    {
        private readonly List<string> equipSlots = [];
        private readonly List<string> statModifiers = [];
        private readonly Dictionary<string, List<string>> aggregatedResistances = [];

        private float maxPressure = 0f;
        private bool deflectsProjectiles = false;
        private int durability = 0;

        public double Order => 30;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
            {
                int health = (int)Math.Floor(item.Health);
                if (health > 0 && health != 100 && health < 100000) durability = health;

                if (item.ConfigElement == null) return false;

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
            return equipSlots.Count > 0 || statModifiers.Count > 0 || aggregatedResistances.Count > 0 || maxPressure > 0 || durability > 0;
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.window.section_equipment", "AS EQUIPMENT").Value, Color.Gold);

            if (durability > 0)
                l.Row(TextSOS.Get("sos.equip.max_durability", "Max Durability:").Value, durability.ToString(), Color.White);

            if (maxPressure > 0)
                l.Row(TextSOS.Get("sos.equip.pressure_protection", "Pressure Protection:").Value, maxPressure.ToMeters(), Color.DeepSkyBlue);

            if (deflectsProjectiles)
                l.Row(TextSOS.Get("sos.equip.armor_special", "Armor Special:").Value, TextSOS.Get("sos.equip.deflect_projectiles", "Deflects Projectiles").Value, Color.LightGray);

            foreach (var mod in statModifiers.Distinct())
            {
                var parts = mod.Split(':');
                Color color = parts[1].Trim().StartsWith('-') ? Color.Salmon : Color.LightGreen;
                l.Row(parts[0] + ":", parts[1], color);
            }

            foreach (var res in aggregatedResistances)
            {
                l.BadgeRow(res.Key + " Res:", [res.Key], [string.Join(", ", res.Value)], linkColor: Color.LightGreen, onSearchFilter: SOSController.Instance.SetSearchFilter);
            }

            if (equipSlots.Count > 0)
            {
                var uniqueSlots = equipSlots
                    .SelectMany(s => s.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries))
                    .Distinct();

                l.BadgeRow(TextSOS.Get("sos.equip.equips_in", "Equips In:").Value, uniqueSlots, filterPrefix: '&', onSearchFilter: SOSController.Instance.SetSearchFilter);
            }
        }
    }

    // MARK: Medical
    public class MedicalSection : ISOSStatSection
    {
        private int medicalSkillReq = 0;
        private readonly List<(string Identifier, string DisplayName)> suitableTreatments = [];

        private readonly Dictionary<string, (string Name, float Amount)> alwaysHeals = [];
        private readonly Dictionary<string, (string Name, float Amount)> alwaysCauses = [];
        private readonly Dictionary<string, (string Name, float Amount)> successHeals = [];
        private readonly Dictionary<string, (string Name, float Amount)> successCauses = [];
        private readonly Dictionary<string, (string Name, float Amount)> failureHeals = [];
        private readonly Dictionary<string, (string Name, float Amount)> failureCauses = [];

        public double Order => 40;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
            {
                if (item.ConfigElement == null) return false;

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
            }
            return suitableTreatments.Count > 0 || alwaysHeals.Count > 0 || successHeals.Count > 0 || alwaysCauses.Count > 0 || successCauses.Count > 0;
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

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.window.section_medical", "MEDICAL").Value, Color.Gold);

            if (medicalSkillReq > 0)
                l.BadgeRow(TextSOS.Get("sos.med.skill_req", "Medical Skill Req:").Value, [medicalSkillReq.ToString()], ["medical " + medicalSkillReq.ToString()], linkColor: Color.Orange, onSearchFilter: SOSController.Instance.SetSearchFilter);

            if (suitableTreatments.Count > 0)
            {
                l.BadgeRow(
                    TextSOS.Get("sos.med.suitable", "Recommended:").Value,
                    suitableTreatments.Select(t => t.Identifier),
                    suitableTreatments.Select(t => t.DisplayName),
                    linkColor: Color.LightSkyBlue,
                    onSearchFilter: SOSController.Instance.SetSearchFilter
                );
            }

            void DrawHyperlinkEffect(string label, Dictionary<string, (string Name, float Amount)> dict, Color linkColor)
            {
                if (dict.Count == 0) return;

                var ids = dict.Keys;
                var displayNames = dict.Select(kvp => $"{kvp.Value.Name} ({kvp.Value.Amount.ToValue()})");

                l.BadgeRow(label, ids, displayNames, linkColor: linkColor, onSearchFilter: SOSController.Instance.SetSearchFilter);
            }

            DrawHyperlinkEffect(TextSOS.Get("sos.med.always_heals", "Always Heals:").Value, alwaysHeals, Color.LightGreen);
            DrawHyperlinkEffect(TextSOS.Get("sos.med.always_causes", "Always Applies:").Value, alwaysCauses, Color.Salmon);

            DrawHyperlinkEffect(TextSOS.Get("sos.med.success_heals", "On Success Heals:").Value, successHeals, Color.LightGreen);
            DrawHyperlinkEffect(TextSOS.Get("sos.med.success_causes", "On Success Applies:").Value, successCauses, Color.Salmon);

            DrawHyperlinkEffect(TextSOS.Get("sos.med.failure_heals", "On Failure Heals:").Value, failureHeals, Color.DarkSeaGreen);
            DrawHyperlinkEffect(TextSOS.Get("sos.med.failure_causes", "On Failure Applies:").Value, failureCauses, Color.Crimson);
        }
    }

    // MARK: utility
    public class UtilitySection : ISOSStatSection
    {
        private readonly Dictionary<string, string> deviceProperties = [];

        public double Order => 50;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
            {
                if (item.ConfigElement == null) return false;
                foreach (var child in item.ConfigElement.Descendants())
                {
                    string n = child.Name.ToString().ToLowerInvariant();

                    if (n == "wificomponent" && child.GetAttribute("range") != null)
                        deviceProperties[TextSOS.Get("sos.util.radio_range", "Radio Range").Value] = child.GetAttributeFloat("range", 0).ToMeters();

                    if (n == "lightcomponent" && child.GetAttribute("range") != null)
                        deviceProperties[TextSOS.Get("sos.util.light_range", "Light Range").Value] = child.GetAttributeFloat("range", 0).ToMeters();

                    if (n == "pump" && child.GetAttribute("maxflow") != null)
                        deviceProperties[TextSOS.Get("sos.util.pump_flow", "Pump Max Flow").Value] = child.GetAttributeFloat("maxflow", 0).ToMeters();

                    if (n == "sonar" && child.GetAttribute("range") != null)
                        deviceProperties[TextSOS.Get("sos.util.sonar_range", "Sonar Range").Value] = child.GetAttributeFloat("range", 0).ToMeters();
                }
            }
            return deviceProperties.Count > 0;
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.window.section_utility", "UTILITY").Value, Color.Gold);

            foreach (var prop in deviceProperties)
                l.Row(prop.Key + ":", prop.Value, Color.Cyan);
        }
    }

    // MARK: container
    public class ContainerSection : ISOSStatSection
    {
        private string capacity = "";
        private readonly HashSet<string> acceptedTags = [];
        private readonly List<string> spawnLocations = [];
        private List<Prefab> compatibleItems = [];

        public double Order => 60;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
            {
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
            }
            return !string.IsNullOrEmpty(capacity) || compatibleItems.Count > 0 || spawnLocations.Count > 0;
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.window.section_container", "CONTAINERS").Value, Color.Gold);

            if (!string.IsNullOrEmpty(capacity))
            {
                l.Row(TextSOS.Get("sos.container.capacity", "Capacity:").Value,
                    TextSOS.Get("sos.container.slots", "[amount] Slots").Replace("[amount]", capacity).Value, Color.White);
            }

            if (compatibleItems.Count > 0)
            {
                _ = new GUIDesplegableBox(contentPanel.Content, SOSController.Instance.SetSearchFilter,
                    TextSOS.Get("sos.container.accepts", "Accepts:").Value,
                    acceptedTags, compatibleItems, onPrimary, onSecondary);
            }

            if (spawnLocations.Count > 0)
            {
                l.BadgeRow(TextSOS.Get("sos.container.contained", "Contained_by:").Value, spawnLocations, onSearchFilter: SOSController.Instance.SetSearchFilter);
            }
        }
    }

    // MARK: Affliction effects
    public class AfflictionEffectsSection : ISOSStatSection
    {
        private class PhaseData
        {
            public string Range = "";
            public float StrengthChange;
            public List<string> Stats = [];
            public List<string> Resistances = [];
            public List<string> Events = [];
            public List<(string ID, string Name, Color Theme)> LinkedAfflictions = [];
        }

        private readonly List<PhaseData> phases = [];
        private readonly List<PhaseData> periodicPhases = [];

        public double Order => 70;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            if (prefab is not AfflictionPrefab aff || aff.configElement == null) return false;
            phases.Clear();
            periodicPhases.Clear();

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
                    phase.Stats.Add($"Max HP Penalty: -{vitStr.SetColor(Color.OrangeRed)}");
                }

                // modf
                float speedMin = element.GetAttributeFloat("minspeedmultiplier", 1f);
                float speedMax = element.GetAttributeFloat("maxspeedmultiplier", 1f);
                if (speedMin != 1f || speedMax != 1f)
                {
                    string speedStr = speedMin == speedMax ? speedMax.ToValue() : $"{speedMin.ToValue()} to {speedMax.ToValue()}";
                    phase.Stats.Add($"Speed: x{speedStr.SetColor((speedMax >= 1f) ? Color.LimeGreen : Color.OrangeRed)}");
                }

                // effects
                List<string> effectList = [];
                if (element.GetAttributeFloat("maxscreendistort", 0f) > 0) effectList.Add("disort");
                if (element.GetAttributeFloat("maxscreenblur", 0f) > 0) effectList.Add("blur");
                if (element.GetAttributeFloat("maxradialdistort", 0f) > 0) effectList.Add("radial");
                if (element.GetAttributeFloat("maxchromaticaberration", 0f) > 0) effectList.Add("chroma");
                if (effectList.Count > 0)
                    phase.Stats.Add($"Visual Distortions ({string.Join(", ", effectList)})".SetColor(Color.Orange));

                float convulse = element.GetAttributeFloat("convulseamount", 0f);
                if (convulse > 0) phase.Stats.Add($"Convulsions/Spasms ({convulse})".SetColor(Color.OrangeRed));

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
                    Range = $"Interval: {element.GetAttributeFloat("mininterval", 1f).ToValue()}s - {element.GetAttributeFloat("maxinterval", 1f).ToValue()}s"
                };

                float minStr = element.GetAttributeFloat("minstrength", 0f);
                float maxStr = element.GetAttributeFloat("maxstrength", 0f);
                if (minStr > 0 || maxStr > 0)
                {
                    phase.Range += $" (Str: {minStr.ToValue()} - {maxStr.ToValue()})";
                }

                ParseStatusEffects(element, phase);

                if (phase.LinkedAfflictions.Count > 0 || phase.Events.Count > 0)
                    periodicPhases.Add(phase);
            }
            return phases.Count > 0 || periodicPhases.Count > 0;
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

            if (hasSounds) phase.Events.Add("Triggers Sounds/Noises");
            if (hasParticles) phase.Events.Add("Spawns Particles");
            if (hasExplosion) phase.Events.Add("Causes Explosion");
            if (hasAnimations) phase.Events.Add("Forces Animations");
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (phases.Count > 0)
            {
                using var l = new SectionLayout(contentPanel);
                l.Header("EFFECTS BY STRENGTH PHASE", Color.Gold);

                foreach (var phase in phases)
                {
                    l.RichText($"Strength Range: {phase.Range.SetColor(Color.Orange)}");

                    if (phase.StrengthChange != 0)
                    {
                        string trend = phase.StrengthChange > 0
                            ? $"Worsens: +{phase.StrengthChange}/s".SetColor(Color.Salmon)
                            : $"Natural Healing: {phase.StrengthChange}/s".SetColor(Color.LightGreen);
                        l.RichText($"  -> {trend}");
                    }

                    if (phase.Stats.Count > 0)
                        l.RichText($"  -> {string.Join(" | ", phase.Stats)}");

                    if (phase.Resistances.Count > 0)
                        l.RichText($"  -> Resistances: {string.Join(" | ", phase.Resistances)}");

                    if (phase.Events.Count > 0)
                        l.RichText($"  -> {string.Join(", ", phase.Events).SetColor(Color.MediumPurple)}");

                    if (phase.LinkedAfflictions.Count > 0)
                    {
                        l.SelectorRow("  -> Triggers:",
                            phase.LinkedAfflictions.Select(a => a.ID),
                            phase.LinkedAfflictions.Select(a => a.Name.SetColor(a.Theme)),
                            fallbackFilterPrefix: '!',
                            onPrimary: onPrimary,
                            onSecondary: onSecondary,
                            onSearchFilter: SOSController.Instance.SetSearchFilter);
                    }

                    l.RichText(" ");

                }
            }

            if (periodicPhases.Count > 0)
            {
                using var l = new SectionLayout(contentPanel);
                l.Header("PERIODIC EVENTS", Color.MediumPurple);
                foreach (var phase in periodicPhases)
                {
                    l.RichText($"Frequency: {phase.Range.SetColor(Color.Cyan)}");

                    if (phase.Events.Count > 0)
                        l.RichText($"  -> {string.Join(", ", phase.Events).SetColor(Color.MediumPurple)}");

                    if (phase.LinkedAfflictions.Count > 0)
                    {
                        l.SelectorRow("  -> Triggers:",
                            phase.LinkedAfflictions.Select(a => a.ID),
                            phase.LinkedAfflictions.Select(a => a.Name.SetColor(a.Theme)),
                            fallbackFilterPrefix: '!',
                            onPrimary: onPrimary,
                            onSecondary: onSecondary,
                            onSearchFilter: SOSController.Instance.SetSearchFilter);
                    }
                    l.RichText(" ");

                }
            }
        }
    }

    public class AfflictionTreatmentSection : ISOSStatSection
    {
        private AfflictionPrefab? aff;

        private readonly List<ItemPrefab> highEff = [];
        private readonly List<ItemPrefab> medEff = [];
        private readonly List<ItemPrefab> lowEff = [];
        private readonly List<ItemPrefab> harmful = [];

        private readonly List<string> blockers = [];

        public double Order => 80;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            if (prefab is not AfflictionPrefab affliction) return false;
            aff = affliction;

            if (aff.IgnoreTreatmentIfAfflictedBy != null)
            {
                foreach (var blockerId in aff.IgnoreTreatmentIfAfflictedBy)
                    blockers.Add(blockerId.Value);
            }

            if (aff.TreatmentSuitabilities != null)
            {
                foreach (var kvp in aff.TreatmentSuitabilities)
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

            if (aff.TreatmentSuitabilities != null)
            {
                int CompareSuitDesc(ItemPrefab a, ItemPrefab b) => aff.TreatmentSuitabilities[b.Identifier].CompareTo(aff.TreatmentSuitabilities[a.Identifier]);
                int CompareSuitAsc(ItemPrefab a, ItemPrefab b) => aff.TreatmentSuitabilities[a.Identifier].CompareTo(aff.TreatmentSuitabilities[b.Identifier]);
                highEff.Sort(CompareSuitDesc);
                medEff.Sort(CompareSuitDesc);
                lowEff.Sort(CompareSuitDesc);
                harmful.Sort(CompareSuitAsc);
            }
            return highEff.Count > 0 || medEff.Count > 0 || lowEff.Count > 0 || harmful.Count > 0 || blockers.Count > 0;
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (aff == null) return;

            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.window.section_treatments", "TREATMENTS & MEDICATION").Value, Color.SpringGreen);

            if (blockers.Count > 0)
            {
                var displayNames = blockers.Select(b =>
                    AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == b)?.Name.Value ?? b);

                l.SelectorRow(TextSOS.Get("sos.affliction.blockedby", "Treatment Blocked By:").Value, blockers, displayNames,
                    fallbackFilterPrefix: '!',
                    onPrimary: onPrimary,
                    onSecondary: onSecondary,
                    onSearchFilter: SOSController.Instance.SetSearchFilter);
            }

            void DrawRow(string label, List<ItemPrefab> items, Color? labelColor = null)
            {
                if (items.Count == 0) return;

                var ids = items.Select(i => i.Identifier.Value);
                var names = items.Select(i => $"{i.Name.Value} ({aff.TreatmentSuitabilities[i.Identifier]:0})");

                l.SelectorRow(label, ids, names,
                    fallbackFilterPrefix: '!',
                    labelColor: labelColor,
                    onPrimary: onPrimary,
                    onSecondary: onSecondary,
                    onSearchFilter: SOSController.Instance.SetSearchFilter);
            }

            DrawRow(TextSOS.Get("sos.affliction.highlyeffective", "Highly Effective:").Value, highEff);
            DrawRow(TextSOS.Get("sos.affliction.effective", "Effective:").Value, medEff);
            DrawRow(TextSOS.Get("sos.affliction.alternative", "Alternative / Weak:").Value, lowEff);

            if (harmful.Count > 0)
            {
                l.RichText(TextSOS.Get("sos.affliction.contraindicated_warn", "WARNING: The following items worsen the condition!").Value.SetColor(Color.Salmon));
                DrawRow(TextSOS.Get("sos.affliction.contraindicated", "Contraindicated:").Value, harmful, labelColor: Color.Salmon);
            }
        }
    }

    public class DescriptionSection : ISOSStatSection
    {
        private string? text;
        public double Order => 90;
        public string Id => GetType().FullOrName();

        public bool Analyze(Prefab prefab)
        {
            text = prefab switch
            {
                ItemPrefab item => item.Description?.Value ?? "",
                AfflictionPrefab affliction => string.Join("\n\n", affliction.Descriptions.Select(d => $"({d.MinStrength.ToString().SetColor(Color.Orange)}-{d.MaxStrength.ToString().SetColor(Color.OrangeRed)}) {d.Target.ToString().SetColor(Color.BlueViolet)}: {d.Text}")),
                _ => null
            };
            return !string.IsNullOrEmpty(text);
        }

        public void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (text == null) return;

            using var l = new SectionLayout(contentPanel);
            l.Header(TextSOS.Get("sos.item.description", "DESCRIPTION").Value, Color.Gold);
            l.RichText(RichString.Rich(text));
        }
    }

    #endregion
}