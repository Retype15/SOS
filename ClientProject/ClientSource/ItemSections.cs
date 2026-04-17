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
    public class SectionBuilder
    {
        private readonly GUIListBox targetPanel;
        private readonly Action<string> onBadgeClick;
        private readonly SOSController controller;
        private readonly Action<Prefab> onPrimaryClick;
        private readonly Action<Prefab> onSecondaryClick;

        private GUILayoutGroup? currentLayout;
        private int rowsCreated = 0;

        public SectionBuilder(GUIListBox targetPanel, Action<string> onBadgeClick, SOSController controller, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            this.targetPanel = targetPanel;
            this.onBadgeClick = onBadgeClick;
            this.controller = controller;
            this.onPrimaryClick = onPrimary;
            this.onSecondaryClick = onSecondary;
        }

        public void StartSection(string title, Color color)
        {
            currentLayout = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0f), targetPanel.Content.RectTransform))
            { AbsoluteSpacing = 2, CanBeFocused = false };

            var titleBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), title, font: GUIStyle.SubHeadingFont, textColor: color, textAlignment: Alignment.Center) { CanBeFocused = false };
            titleBlock.RectTransform.MinSize = new Point(0, 30);
            titleBlock.RectTransform.MaxSize = new Point(int.MaxValue, 30);

            rowsCreated = 0;
        }

        public void AddRow(string label, string value, Color valColor)
        {
            if (string.IsNullOrEmpty(value) || currentLayout == null) return;

            var row = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), isHorizontal: true) { CanBeFocused = false };
            row.RectTransform.MinSize = new Point(0, 18);
            row.RectTransform.MaxSize = new Point(int.MaxValue, 18);

            _ = new GUITextBlock(new RectTransform(new Vector2(0.45f, 1f), row.RectTransform), label, font: GUIStyle.SmallFont, textColor: Color.Gray) { CanBeFocused = false };
            _ = new GUITextBlock(new RectTransform(new Vector2(0.55f, 1f), row.RectTransform), value, font: GUIStyle.SmallFont, textColor: valColor, textAlignment: Alignment.Right) { CanBeFocused = false };
            rowsCreated++;
        }

        public void AddBadgeRow(string label, IEnumerable<string> tags, char? filterPrefix = null)
        {
            if (tags == null || !tags.Any() || currentLayout == null) return;

            var row = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), isHorizontal: true) { CanBeFocused = false };
            row.RectTransform.MinSize = new Point(0, 24);
            row.RectTransform.MaxSize = new Point(int.MaxValue, 24);

            _ = new GUITextBlock(new RectTransform(new Vector2(0.40f, 1f), row.RectTransform), label, font: GUIStyle.SmallFont, textColor: Color.Gray) { CanBeFocused = false };
            var badgeContainer = new GUIFrame(new RectTransform(new Vector2(0.60f, 1f), row.RectTransform), style: null) { CanBeFocused = false };

            void clickAction(string val)
            {
                string query = filterPrefix.HasValue ? $"{filterPrefix}{val}" : val;
                onBadgeClick?.Invoke(query);
            }

            GUIBadgeList.Create(badgeContainer.RectTransform, tags, clickAction);
            rowsCreated++;
        }

        public void AddDropdown(string label, IEnumerable<string> tags, IEnumerable<Prefab> items)
        {
            if (currentLayout == null || !items.Any()) return;
            _ = new GUIDesplegableBox(currentLayout, onBadgeClick, label, tags, items, controller, onPrimaryClick, onSecondaryClick);
            rowsCreated++;
        }

        public void AddFullWidthText(string text, Color color)
        {
            if (currentLayout == null || string.IsNullOrEmpty(text)) return;
            var block = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), RichString.Rich(text), textColor: color, font: GUIStyle.SmallFont, wrap: true) { CanBeFocused = false, };

            int textHeight = (int)block.TextSize.Y + 10;
            block.RectTransform.MinSize = new Point(0, textHeight);
            block.RectTransform.MaxSize = new Point(int.MaxValue, textHeight);
            rowsCreated++;
        }

        public void EndSection()
        {
            if (currentLayout == null) return;
            if (rowsCreated == 0 && currentLayout.CountChildren <= 1)
            {
                targetPanel.Content.RemoveChild(currentLayout);
            }
            else
            {
                int totalHeight = 0;
                foreach (var child in currentLayout.Children)
                {
                    totalHeight += child.Rect.Height + currentLayout.AbsoluteSpacing;
                }
                currentLayout.RectTransform.MinSize = new Point(0, totalHeight + 10);
                currentLayout.RectTransform.MaxSize = new Point(int.MaxValue, totalHeight + 10);
            }
            currentLayout = null;
        }
    }

    public class ItemAnalysis
    {
        public Identifier ItemId { get; }
        public List<BaseStatSection> Sections { get; } = [];


        // MARK: Adders
        public ItemAnalysis(Prefab item)
        {
            ItemId = item.Identifier;
            // ItemPrefab Oriented
            AddSection(new GeneralSection(), item);
            AddSection(new EconomySection(), item);
            AddSection(new WeaponSection(), item);
            AddSection(new EquipmentSection(), item);
            AddSection(new MedicalSection(), item);
            AddSection(new UtilitySection(), item);
            AddSection(new ContainerSection(), item);

            // AfflictionPrefab Oriented
            AddSection(new AfflictionEffectsSection(), item);
            AddSection(new AfflictionTreatmentSection(), item);

            // Description
            AddSection(new DescriptionSection(), item);
        }

        private void AddSection(BaseStatSection section, Prefab prefab)
        {
            switch (prefab)
            {
                case ItemPrefab item:
                    section.Analyze(item);
                    break;

                case AfflictionPrefab affliction:
                    section.Analyze(affliction);
                    break;

                default:
                    break;
            }

            if (section.HasData) Sections.Add(section);
        }
    }

    public abstract class BaseStatSection
    {
        public abstract bool HasData { get; }
        public abstract void Analyze(ItemPrefab item);
        public abstract void Analyze(AfflictionPrefab item);
        public abstract void Draw(SectionBuilder builder);
    }

    // MARK: Sections

    // MARK: General
    public class GeneralSection : BaseStatSection
    {
        private Prefab? prefab;
        public override bool HasData => prefab != null;

        // Item
        private string cargoBox = "";
        private readonly List<string> hazards = [];

        // Affliction
        private float activationThreshold;
        private float treatmentThreshold;
        private string causeOfDeath = "";

        public override void Analyze(ItemPrefab item)
        {
            this.prefab = item;
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
        }

        public override void Analyze(AfflictionPrefab affliction)
        {
            this.prefab = affliction;
            if (affliction.configElement != null)
            {
                activationThreshold = affliction.configElement.GetAttributeFloat("activationthreshold", 0f);
                treatmentThreshold = affliction.configElement.GetAttributeFloat("treatmentthreshold", 0f);

                string codRaw = affliction.configElement.GetAttributeString("causeofdeathdescription", "");
                if (!string.IsNullOrEmpty(codRaw))
                    causeOfDeath = TextManager.Get(codRaw).Fallback(codRaw).Value;
            }
        }

        public override void Draw(SectionBuilder builder)
        {
            if (prefab == null) return;

            builder.StartSection(TextSOS.Get("sos.window.section_general", "GENERAL").Value, Color.Gold);

            builder.AddBadgeRow(TextSOS.Get("sos.item.id", "ID:").Value, [prefab.Identifier.Value], '!');

            string modName = prefab.ContentPackage?.Name ?? "Vanilla";
            builder.AddBadgeRow("Mod:", [modName], '@');

            if (prefab is ItemPrefab item)
            {
                if (!item.Aliases.IsEmpty) builder.AddRow(TextSOS.Get("sos.item.aliases", "Aliases:").Value, string.Join(", ", item.Aliases), Color.DarkGray);
                builder.AddBadgeRow(TextSOS.Get("sos.item.category", "Category:").Value, item.Category.ToString().Split(','), '#');
                if (!string.IsNullOrEmpty(cargoBox)) builder.AddRow(TextSOS.Get("sos.item.cargo_box", "Cargo Box:").Value, TextManager.Get("EntityName." + cargoBox).Fallback(cargoBox).Value, Color.BurlyWood);
                builder.AddRow(TextSOS.Get("sos.item.max_stack", "Max Stack:").Value, item.MaxStackSize.ToString(), Color.White);
                if (hazards.Count > 0) builder.AddRow(TextSOS.Get("sos.item.hazards", "Hazards:").Value, string.Join(", ", hazards), Color.Salmon);
                builder.AddBadgeRow(TextSOS.Get("sos.item.tags", "TAGS:").Value, item.Tags.Select(t => t.Value), '$');
            }
            else if (prefab is AfflictionPrefab aff)
            {
                builder.AddBadgeRow("Type:", [aff.AfflictionType.ToString()], '#');

                builder.AddRow("Max Strength:", aff.MaxStrength.ToValue(), Color.White);

                if (activationThreshold > 0)
                    builder.AddRow("Activation Threshold:", activationThreshold.ToValue(), Color.Yellow);
                if (treatmentThreshold > 0)
                    builder.AddRow("Treatment Threshold:", treatmentThreshold.ToValue(), Color.LightGreen);

                if (aff.LimbSpecific)
                    builder.AddRow("Limb Specific:", "Yes", Color.Gray);

                if (!string.IsNullOrEmpty(aff.IndicatorLimb.ToString()))
                    builder.AddRow("Indicator Limb:", aff.IndicatorLimb.ToString(), Color.Gray);

                if (!string.IsNullOrEmpty(causeOfDeath))
                    builder.AddFullWidthText($"Death Cause: {causeOfDeath}", Color.Crimson);
            }

            builder.EndSection();
        }
    }

    // MARK: Economy
    public class EconomySection : BaseStatSection
    {
        private int price;
        private bool canBuy;
        private bool canSell;
        private int minDifficulty;
        private Identifier requiredFaction = Identifier.Empty;

        public override bool HasData => price > 0 || canBuy;

        public override void Analyze(ItemPrefab item)
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
        public override void Analyze(AfflictionPrefab item) { }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.window.section_economy", "ECONOMY").Value, Color.Gold);

            builder.AddRow(TextSOS.Get("sos.item.base_price", "Base Price:").Value, $"{price} mk", Color.Yellow);

            builder.AddRow(TextSOS.Get("sos.item.can_buy", "Can be Bought:").Value, canBuy ? TextSOS.Get("sos.gen.yes", "Yes").Value : TextSOS.Get("sos.gen.no", "No").Value,
                canBuy ? Color.LightGreen : Color.Salmon);

            builder.AddRow(TextSOS.Get("sos.item.can_sell", "Can be Sold:").Value, canSell ? TextSOS.Get("sos.gen.yes", "Yes").Value : TextSOS.Get("sos.gen.no", "No").Value, canSell ? Color.LightGreen : Color.Salmon);

            if (minDifficulty > 0)
                builder.AddRow(TextSOS.Get("sos.item.min_difficulty", "Min. Difficulty:").Value, minDifficulty.ToString(), Color.White);

            if (requiredFaction != Identifier.Empty)
            {
                string factionName = TextManager.Get("FactionName." + requiredFaction).Fallback(requiredFaction.Value).Value;
                builder.AddRow(TextSOS.Get("sos.item.required_faction", "Required Faction:").Value, factionName, Color.Cyan);
            }

            builder.EndSection();
        }
    }

    // MARK: weapons
    public class WeaponSection : BaseStatSection
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
            public string Name = "";
            public float Strength;
            public float Probability;
        }

        public override bool HasData => afflictions.Count > 0 || penetration > 0 || structureDamage > 0 || itemDamage > 0 || reload > 0 || isThrowable || explosionRange > 0;

        public override void Analyze(ItemPrefab item)
        {
            if (item.ConfigElement == null) return;

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
        public override void Analyze(AfflictionPrefab item) { }

        private void ParseAffliction(XElement element, float prob)
        {
            string id = element.GetAttributeString("identifier", "");
            float strength = element.GetAttributeFloat("strength", 0f);
            if (strength <= 0 || string.IsNullOrEmpty(id)) return;

            afflictions.Add(new AfflictionData
            {
                Name = TextManager.Get("AfflictionName." + id).Fallback(id).Value,
                Strength = strength,
                Probability = prob
            });
        }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.window.section_weapon", "COMBAT STATS").Value, Color.Gold);

            //pep
            if (reload > 0) builder.AddRow(isAutomatic ? "Fire Rate:" : "Reload:", $"{reload}s", Color.Cyan);
            if (powerUse > 0) builder.AddRow("Power Use:", $"{powerUse}kW", Color.Orange);
            if (range > 0) builder.AddRow("Range:", range.ToMeters(), Color.LightGray);
            if (explosionRange > 0) builder.AddRow("Explosion Radius:", explosionRange.ToMeters(), Color.Orange);
            if (penetration > 0) builder.AddRow("Armor Penetration:", $"{(int)(penetration * 100)}%", Color.Orange);
            if (projectileCount > 1) builder.AddRow("Projectiles:", $"x{projectileCount}", Color.LightGray);
            if (maxTargets > 1) builder.AddRow("Max Targets:", maxTargets.ToString(), Color.LightGray);
            if (structureDamage > 0) builder.AddRow("Structure Damage:", structureDamage.ToValue(), Color.Salmon);
            if (itemDamage > 0) builder.AddRow("Item Damage:", itemDamage.ToValue(), Color.Salmon);
            if (severProb > 0) builder.AddRow("Dismember Chance:", $"{(int)(severProb * 100)}%", Color.Crimson);
            if (spread > 0) builder.AddRow("Base Spread:", $"{spread:0.#}°", Color.LightGray);
            if (dmgModifier != 1f) builder.AddRow("Dmg. Multiplier:", $"x{dmgModifier:0.#}", Color.LightGreen);
            if (isThrowable) builder.AddRow("Type:", "Throwable", Color.White);

            var grouped = afflictions.GroupBy(a => a.Name);
            foreach (var group in grouped)
            {
                string val = string.Join(" | ", group.Select(a =>
                    a.Probability < 1.0f ? $"{a.Strength} ({(int)(a.Probability * 100)}%)" : a.Strength.ToValue()));

                builder.AddRow(group.Key + ":", val, Color.Salmon);
            }

            builder.EndSection();
        }
    }

    // MARK: equipements
    public class EquipmentSection : BaseStatSection
    {
        private readonly List<string> equipSlots = [];
        private readonly List<string> statModifiers = [];
        private readonly Dictionary<string, List<string>> aggregatedResistances = [];

        private float maxPressure = 0f;
        private bool deflectsProjectiles = false;
        private int durability = 0;

        public override bool HasData => equipSlots.Count > 0 || statModifiers.Count > 0 ||
                                       aggregatedResistances.Count > 0 || maxPressure > 0 || durability > 0;

        public override void Analyze(ItemPrefab item)
        {
            int health = (int)Math.Floor(item.Health);
            if (health > 0 && health != 100 && health < 100000) durability = health;

            if (item.ConfigElement == null) return;

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
        public override void Analyze(AfflictionPrefab item) { }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.window.section_equipment", "AS EQUIPMENT").Value, Color.Gold);

            if (durability > 0)
                builder.AddRow(TextSOS.Get("sos.equip.max_durability", "Max Durability:").Value, durability.ToString(), Color.White);

            if (maxPressure > 0)
                builder.AddRow(TextSOS.Get("sos.equip.pressure_protection", "Pressure Protection:").Value, maxPressure.ToMeters(), Color.DeepSkyBlue);

            if (deflectsProjectiles)
                builder.AddRow(TextSOS.Get("sos.equip.armor_special", "Armor Special:").Value, TextSOS.Get("sos.equip.deflect_projectiles", "Deflects Projectiles").Value, Color.LightGray);

            foreach (var mod in statModifiers.Distinct())
            {
                var parts = mod.Split(':');
                Color color = parts[1].Trim().StartsWith('-') ? Color.Salmon : Color.LightGreen;
                builder.AddRow(parts[0] + ":", parts[1], color);
            }

            foreach (var res in aggregatedResistances)
            {
                builder.AddRow(res.Key + " Res:", string.Join(" | ", res.Value), Color.LightGreen);
            }

            if (equipSlots.Count > 0)
            {
                var uniqueSlots = equipSlots
                    .SelectMany(s => s.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries))
                    .Distinct();

                builder.AddBadgeRow(TextSOS.Get("sos.equip.equips_in", "Equips In:").Value, uniqueSlots, '&');
            }

            builder.EndSection();
        }
    }

    // MARK: Medical
    public class MedicalSection : BaseStatSection
    {
        private int medicalSkillReq = 0;
        private readonly List<string> suitableTreatments = [];

        private readonly Dictionary<string, float> alwaysHeals = [];
        private readonly Dictionary<string, float> alwaysCauses = [];
        private readonly Dictionary<string, float> successHeals = [];
        private readonly Dictionary<string, float> successCauses = [];
        private readonly Dictionary<string, float> failureHeals = [];
        private readonly Dictionary<string, float> failureCauses = [];

        public override bool HasData => suitableTreatments.Count > 0 || alwaysHeals.Count > 0 || successHeals.Count > 0 || alwaysCauses.Count > 0 || successCauses.Count > 0;

        public override void Analyze(ItemPrefab item)
        {
            if (item.ConfigElement == null) return;

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
                        suitableTreatments.Add($"{GetAfflictionName(idOrType)} ({sign}{suit})");
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
                                AddStat(isFailure ? failureHeals : (isSuccess ? successHeals : alwaysHeals), affName, totalAmount);
                            else
                                AddStat(isFailure ? failureCauses : (isSuccess ? successCauses : alwaysCauses), affName, totalAmount);
                        }
                    }
                }
            }
        }
        public override void Analyze(AfflictionPrefab item) { }

        private static void AddStat(Dictionary<string, float> dict, string name, float amount)
        {
            if (dict.ContainsKey(name)) dict[name] += amount;
            else dict[name] = amount;
        }

        private static string GetAfflictionName(string idOrType)
        {
            var loc = TextManager.Get("AfflictionName." + idOrType);
            if (loc.Loaded && !loc.Value.Contains("AfflictionName.")) return loc.Value;

            if (idOrType.Length > 0) return char.ToUpper(idOrType[0]) + idOrType[1..];
            return idOrType;
        }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.window.section_medical", "MEDICAL").Value, Color.Gold);

            if (medicalSkillReq > 0)
                builder.AddRow(TextSOS.Get("sos.med.skill_req", "Medical Skill Req:").Value, medicalSkillReq.ToString(), Color.Orange);

            if (suitableTreatments.Count > 0)
            {
                string label = TextSOS.Get("sos.med.suitable", "Recommended:").Value;
                string values = string.Join(", ", suitableTreatments);
                builder.AddFullWidthText($"‖color:180,180,180‖{label}‖color:end‖ {values}", Color.LightSkyBlue);
            }

            void DrawEffectCascading(string label, Dictionary<string, float> dict, Color valColor)
            {
                if (dict.Count == 0) return;

                string items = string.Join(", ", dict.Select(kvp => $"{kvp.Key} ({kvp.Value.ToValue()})"));

                string combinedText = $"{label.SetColor(new Color(160, 160, 160))} {items}";

                builder.AddFullWidthText(combinedText, valColor);
            }

            DrawEffectCascading(TextSOS.Get("sos.med.always_heals", "Always Heals:").Value, alwaysHeals, Color.LightGreen);
            DrawEffectCascading(TextSOS.Get("sos.med.always_causes", "Always Applies:").Value, alwaysCauses, Color.Salmon);

            DrawEffectCascading(TextSOS.Get("sos.med.success_heals", "On Success Heals:").Value, successHeals, Color.LightGreen);
            DrawEffectCascading(TextSOS.Get("sos.med.success_causes", "On Success Applies:").Value, successCauses, Color.Salmon);

            DrawEffectCascading(TextSOS.Get("sos.med.failure_heals", "On Failure Heals:").Value, failureHeals, Color.DarkSeaGreen);
            DrawEffectCascading(TextSOS.Get("sos.med.failure_causes", "On Failure Applies:").Value, failureCauses, Color.Crimson);

            builder.EndSection();
        }
    }

    // MARK: utility
    public class UtilitySection : BaseStatSection
    {
        private readonly Dictionary<string, string> deviceProperties = [];

        public override bool HasData => deviceProperties.Count > 0;

        public override void Analyze(ItemPrefab item)
        {
            if (item.ConfigElement == null) return;

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

        public override void Analyze(AfflictionPrefab item) { }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.window.section_utility", "UTILITY").Value, Color.Gold);

            foreach (var prop in deviceProperties)
            {
                builder.AddRow(prop.Key + ":", prop.Value, Color.Cyan);
            }

            builder.EndSection();
        }
    }

    // MARK: container
    public class ContainerSection : BaseStatSection
    {
        private string capacity = "";
        private readonly HashSet<string> acceptedTags = [];
        private readonly List<string> spawnLocations = [];
        private List<Prefab> compatibleItems = [];

        public override bool HasData => !string.IsNullOrEmpty(capacity) || compatibleItems.Count > 0 || spawnLocations.Count > 0;

        public override void Analyze(ItemPrefab item)
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

        public override void Analyze(AfflictionPrefab item) { }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.window.section_container", "CONTAINERS").Value, Color.Gold);

            if (!string.IsNullOrEmpty(capacity))
            {
                builder.AddRow(TextSOS.Get("sos.container.capacity", "Capacity:").Value,
                    TextSOS.Get("sos.container.slots", "[amount] Slots").Replace("[amount]", capacity).Value, Color.White);
            }

            if (compatibleItems.Count > 0)
            {
                builder.AddDropdown(
                    TextSOS.Get("sos.container.accepts", "Accepts:").Value,
                    acceptedTags,
                    compatibleItems);
            }

            if (spawnLocations.Count > 0)
            {
                builder.AddBadgeRow(TextSOS.Get("sos.container.contained", "Contained_by:").Value, spawnLocations);
            }

            builder.EndSection();
        }
    }

    // MARK: Affliction effects
    public class AfflictionEffectsSection : BaseStatSection
    {
        private AfflictionPrefab? aff;
        private readonly List<string> effectsList = [];

        public override bool HasData => effectsList.Count > 0;
        public override void Analyze(ItemPrefab item) { }

        public override void Analyze(AfflictionPrefab item)
        {
            aff = item;
            if (aff.configElement == null) return;

            foreach (var effect in aff.configElement.GetChildElements("Effect"))
            {
                float minStr = effect.GetAttributeFloat("minstrength", 0f);
                float maxStr = effect.GetAttributeFloat("maxstrength", 0f);
                float strengthChange = effect.GetAttributeFloat("strengthchange", 0f);
                float maxVitDecrease = effect.GetAttributeFloat("maxvitalitydecrease", 0f);

                if (maxVitDecrease == 0 && strengthChange == 0 && !effect.Elements().Any()) continue;

                string range = $"[{minStr:0.#} - {maxStr:0.#}]";
                string desc = "";

                if (maxVitDecrease > 0) desc += $"Vit: -{maxVitDecrease.ToValue()}  ";
                if (strengthChange != 0) desc += $"Str: {(strengthChange >= 0 ? "+" : "-")}{strengthChange.ToValue()}/s  ";

                foreach (var se in effect.GetChildElements("StatusEffect"))
                {
                    foreach (var subAff in se.GetChildElements("Affliction"))
                    {
                        string id = subAff.GetAttributeString("identifier", "");
                        float amt = subAff.GetAttributeFloat("amount", 0f);
                        if (!string.IsNullOrEmpty(id)) desc += $"\n  ↳ +{id} ({amt})";
                    }
                    foreach (var subAff in se.GetChildElements("ReduceAffliction"))
                    {
                        string id = subAff.GetAttributeString("identifier", "");
                        float amt = subAff.GetAttributeFloat("amount", 0f);
                        if (!string.IsNullOrEmpty(id)) desc += $"\n  ↳ -{id} ({amt})";
                    }
                }

                if (!string.IsNullOrWhiteSpace(desc))
                    effectsList.Add($"‖color:180,180,180‖Severity {range}:‖color:end‖\n{desc.Trim()}");
            }
        }

        public override void Draw(SectionBuilder builder)
        {
            if (effectsList.Count == 0) return;
            builder.StartSection("EFFECTS BY SEVERITY", Color.Gold);
            foreach (var eff in effectsList)
            {
                builder.AddFullWidthText(eff, Color.Salmon);
            }
            builder.EndSection();
        }
    }

    // MARK: Affliction Treatments
    public class AfflictionTreatmentSection : BaseStatSection
    {
        private AfflictionPrefab? aff;
        private List<Prefab> healers = [];

        public override bool HasData => healers.Count > 0;
        public override void Analyze(ItemPrefab item) { }

        public override void Analyze(AfflictionPrefab item)
        {
            aff = item;
            string affId = aff.Identifier.Value.ToLowerInvariant();
            string affType = aff.AfflictionType.ToString().ToLowerInvariant();

            healers = [.. ItemPrefab.Prefabs.Where(p =>
            {
                if (p.ConfigElement == null) return false;
                return p.ConfigElement.Descendants().Any(e =>
                {
                    string n = e.Name.ToString().ToLowerInvariant();
                    if (n == "suitabletreatment")
                    {
                        string idOrType = e.GetAttributeString("identifier", e.GetAttributeString("type", "")).ToLowerInvariant();
                        float suit = e.GetAttributeFloat("suitability", 0f);
                        if (suit > 0 && (idOrType == affId || idOrType == affType)) return true;
                    }
                    else if (n == "statuseffect")
                    {
                        foreach (var sub in e.Elements())
                        {
                            string subName = sub.Name.ToString().ToLowerInvariant();
                            if (subName == "reduceaffliction")
                            {
                                string idOrType = sub.GetAttributeString("identifier", sub.GetAttributeString("type", "")).ToLowerInvariant();
                                if (idOrType == affId || idOrType == affType) return true;
                            }
                            else if (subName == "affliction")
                            {
                                string idOrType = sub.GetAttributeString("identifier", sub.GetAttributeString("type", "")).ToLowerInvariant();
                                float amount = sub.GetAttributeFloat("amount", 0f);
                                if (amount < 0 && (idOrType == affId || idOrType == affType)) return true;
                            }
                        }
                    }
                    return false;
                });
            }).OrderBy(p => p.Name.Value).Cast<Prefab>()];
        }

        public override void Draw(SectionBuilder builder)
        {
            if (aff == null || healers.Count == 0) return;
            builder.StartSection(TextSOS.Get("sos.affliction.treatments", "EFFECTIVE TREATMENTS").Value, Color.SpringGreen);
            builder.AddDropdown(TextSOS.Get("sos.affliction.curedby", "Cured by:").Value, [], healers);
            builder.EndSection();
        }
    }

    //MARK: descrption
    public class DescriptionSection : BaseStatSection
    {
        private string text = "";
        public override bool HasData => !string.IsNullOrEmpty(text);
        public override void Analyze(ItemPrefab item) => text = item.Description?.Value ?? "";

        public override void Analyze(AfflictionPrefab affliction)
        {
            text = string.Join("\n\n", affliction.Descriptions.Select(d => $"({d.MinStrength.ToString().SetColor(Color.Orange)}-{d.MaxStrength.ToString().SetColor(Color.OrangeRed)}) {d.Target.ToString().SetColor(Color.Blue)}: {d.Text}"));
        }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.item.description", "DESCRIPTION").Value, Color.Gold);

            builder.AddFullWidthText(text, Color.LightGray);

            builder.EndSection();
        }
    }
}