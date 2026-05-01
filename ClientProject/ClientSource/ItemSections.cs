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
            currentLayout = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0f), targetPanel.Content.RectTransform, Anchor.TopCenter))
            {
                AbsoluteSpacing = 2,
                CanBeFocused = false,
                Stretch = true
            };

            var titleBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), title, font: GUIStyle.SubHeadingFont, textColor: color, textAlignment: Alignment.Left) { CanBeFocused = false };
            titleBlock.RectTransform.MinSize = new Point(0, 30);
            titleBlock.RectTransform.MaxSize = new Point(int.MaxValue, 30);
            titleBlock.Padding = new Vector4(10, 0, 0, 0);

            rowsCreated = 0;
        }

        public void AddRow(string label, string value, Color valColor)
        {
            if (string.IsNullOrEmpty(value) || currentLayout == null) return;

            var row = new GUIButton(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), style: null) { CanBeFocused = false };
            _ = new GUITextBlock(new RectTransform(new Vector2(0.40f, 1f), row.RectTransform, Anchor.CenterLeft), label, font: GUIStyle.SmallFont, textColor: Color.Gray) { CanBeFocused = false };
            var valBlock = new GUITextBlock(new RectTransform(new Vector2(0.60f, 1f), row.RectTransform, Anchor.CenterRight), value, font: GUIStyle.SmallFont, textColor: valColor, textAlignment: Alignment.Right, wrap: true) { CanBeFocused = false };

            void UpdateHeight()
            {
                int h = Math.Max(20, (int)valBlock.TextSize.Y + 4);
                row.RectTransform.MinSize = new Point(0, h);
                row.RectTransform.MaxSize = new Point(int.MaxValue, h);
            }
            UpdateHeight();
            valBlock.RectTransform.SizeChanged += UpdateHeight;

            rowsCreated++;
        }

        public void AddBadgeRow(string label, IEnumerable<string> values, IEnumerable<string>? displayNames = null, char? filterPrefix = null, Color? linkColor = null)
        {
            if (values == null || !values.Any() || currentLayout == null) return;

            var valList = values.ToList();
            var dispList = displayNames?.ToList();

            var data = valList.Select((val, i) => new
            {
                Target = filterPrefix.HasValue ? $"{filterPrefix}{val}" : val,
                Display = dispList != null && i < dispList.Count ? dispList[i] : val
            }).ToList();

            var row = new GUIButton(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), style: null) { CanBeFocused = false };
            _ = new GUITextBlock(new RectTransform(new Vector2(0.40f, 1f), row.RectTransform, Anchor.CenterLeft), label, font: GUIStyle.SmallFont, textColor: Color.Gray) { CanBeFocused = false };

            RichString rich = data.JoinToRichString(", ", d => d.Display, d => linkColor ?? Color.LightSkyBlue);

            var textBlock = new GUITextBlock(new RectTransform(new Vector2(0.60f, 0f), row.RectTransform, Anchor.CenterRight), "", wrap: true, font: GUIStyle.SmallFont, textAlignment: Alignment.TopLeft);
            textBlock.SetRichText(rich);

            textBlock.BindHyperlinks(data, onPrimaryClick: d => onBadgeClick?.Invoke(d.Target));

            void UpdateLayout()
            {
                // Spring?
                int maxW = (int)(row.Rect.Width * 0.60f);
                if (maxW > 0)
                {
                    textBlock.RectTransform.NonScaledSize = new Point(maxW, (int)textBlock.TextSize.Y);
                    float lineH = GUIStyle.SmallFont.LineHeight;
                    if (textBlock.TextSize.Y <= lineH * 1.5f)
                    {
                        int paddingH = (int)(textBlock.Padding.X + textBlock.Padding.Z);
                        int fitW = Math.Min((int)Math.Ceiling(textBlock.TextSize.X + paddingH) + 4, maxW);
                        textBlock.RectTransform.NonScaledSize = new Point(fitW, (int)textBlock.TextSize.Y);
                    }
                }

                int h = Math.Max(24, (int)textBlock.TextSize.Y + 4);
                row.RectTransform.MinSize = new Point(0, h);
                row.RectTransform.MaxSize = new Point(int.MaxValue, h);
            }
            UpdateLayout();
            row.RectTransform.SizeChanged += UpdateLayout;

            if (data.Count == 1)
            {
                var single = data[0];
                row.CanBeFocused = true;
                row.HoverCursor = CursorState.Hand;
                row.OnClicked = (comp, obj) => { onBadgeClick?.Invoke(single.Target); return true; };
            }

            rowsCreated++;
        }

        public void AddSelectorBadgeRow(string label, IEnumerable<string> ids, IEnumerable<string>? displayNames = null, char? fallbackFilterPrefix = null)
        {
            if (ids == null || !ids.Any() || currentLayout == null) return;

            var idList = ids.ToList();
            var nameList = displayNames?.ToList();

            var data = new List<object>();
            for (int i = 0; i < idList.Count; i++)
            {
                string id = idList[i];
                Prefab? found = (Prefab?)AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == id)
                             ?? ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == id);

                if (found != null) data.Add(found);
                else data.Add(fallbackFilterPrefix.HasValue ? $"{fallbackFilterPrefix}{id}" : id);
            }

            string GetText(object obj, int index) => obj is Prefab p ? p.SafeName(Color.White).Name : ((nameList != null && index < nameList.Count) ? nameList[index] : obj.ToString()!);
            Color GetColor(object obj) => obj is Prefab p ? p.IconColor() : Color.LightSkyBlue;

            var row = new GUIButton(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), style: null) { CanBeFocused = false };
            _ = new GUITextBlock(new RectTransform(new Vector2(0.40f, 1f), row.RectTransform, Anchor.CenterLeft), label, font: GUIStyle.SmallFont, textColor: Color.Gray) { CanBeFocused = false };

            RichString rich = data.JoinToRichString(", ", obj => GetText(obj, data.IndexOf(obj)), GetColor);

            var textBlock = new GUITextBlock(new RectTransform(new Vector2(0.60f, 0f), row.RectTransform, Anchor.CenterRight), "", wrap: true, font: GUIStyle.SmallFont, textAlignment: Alignment.TopLeft);
            textBlock.SetRichText(rich);

            textBlock.BindHyperlinks(
                data,
                onPrimaryClick: obj => { if (obj is Prefab p) onPrimaryClick?.Invoke(p); else onBadgeClick?.Invoke(obj.ToString()!); },
                onSecondaryClick: obj => { if (obj is Prefab p) onSecondaryClick?.Invoke(p); }
            );

            void UpdateLayout()
            {
                int maxW = (int)(row.Rect.Width * 0.60f);
                if (maxW > 0)
                {
                    textBlock.RectTransform.NonScaledSize = new Point(maxW, (int)textBlock.TextSize.Y);
                    float lineH = GUIStyle.SmallFont.LineHeight;
                    if (textBlock.TextSize.Y <= lineH * 1.5f)
                    {
                        int paddingH = (int)(textBlock.Padding.X + textBlock.Padding.Z);
                        int fitW = Math.Min((int)Math.Ceiling(textBlock.TextSize.X + paddingH) + 4, maxW);
                        textBlock.RectTransform.NonScaledSize = new Point(fitW, (int)textBlock.TextSize.Y);
                    }
                }

                int h = Math.Max(24, (int)textBlock.TextSize.Y + 4);
                row.RectTransform.MinSize = new Point(0, h);
                row.RectTransform.MaxSize = new Point(int.MaxValue, h);
            }
            UpdateLayout();
            row.RectTransform.SizeChanged += UpdateLayout;

            if (data.Count == 1)
            {
                var single = data[0];
                row.CanBeFocused = true;
                row.HoverCursor = CursorState.Hand;
                row.OnClicked = (comp, obj) =>
                {
                    if (single is Prefab p) onPrimaryClick?.Invoke(p);
                    else onBadgeClick?.Invoke(single.ToString()!);
                    return true;
                };
                row.OnSecondaryClicked = (comp, obj) =>
                {
                    if (single is Prefab p) onSecondaryClick?.Invoke(p);
                    return true;
                };
            }

            rowsCreated++;
        }

        public void AddDropdown(string label, IEnumerable<string> tags, IEnumerable<Prefab> items)
        {
            if (currentLayout == null || !items.Any()) return;
            _ = new GUIDesplegableBox(currentLayout, onBadgeClick, label, tags, items, controller, onPrimaryClick, onSecondaryClick);
            rowsCreated++;
        }

        public void AddFullWidthText(RichString text)
        {
            if (currentLayout == null || text.IsNullOrEmpty()) return;
            var block = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), currentLayout.RectTransform), RichString.Rich(text), font: GUIStyle.SmallFont, wrap: true, textAlignment: Alignment.Left) { CanBeFocused = false, };

            void UpdateHeight()
            {
                int h = (int)block.TextSize.Y + 10;
                block.RectTransform.MinSize = new Point(0, h);
                block.RectTransform.MaxSize = new Point(int.MaxValue, h);
            }
            UpdateHeight();
            block.RectTransform.SizeChanged += UpdateHeight;
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
        public Identifier PrefabId { get; }
        public List<BaseStatSection> Sections { get; } = [];


        // MARK: Adders
        public ItemAnalysis(Prefab prefab)
        {
            PrefabId = prefab.Identifier;
            // All
            AddSection(new GeneralSection(), prefab);

            // Item Oriented
            if (prefab is ItemPrefab)
            {
                AddSection(new EconomySection(), prefab);
                AddSection(new WeaponSection(), prefab);
                AddSection(new EquipmentSection(), prefab);
                AddSection(new MedicalSection(), prefab);
                AddSection(new UtilitySection(), prefab);
                AddSection(new ContainerSection(), prefab);
            }


            // Affliction Oriented
            if (prefab is AfflictionPrefab)
            {
                AddSection(new AfflictionEffectsSection(), prefab);
                AddSection(new AfflictionTreatmentSection(), prefab);
            }
            // Description
            AddSection(new DescriptionSection(), prefab);
        }

        private void AddSection(BaseStatSection section, Prefab prefab)
        {
            section.Analyze(prefab);

            if (section.HasData) Sections.Add(section);
        }
    }

    public abstract class BaseStatSection
    {
        public abstract bool HasData { get; }
        public abstract void Analyze(Prefab item);
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

        public override void Analyze(Prefab prefab)
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

        }

        public override void Draw(SectionBuilder builder)
        {
            if (prefab == null) return;

            builder.StartSection(TextSOS.Get("sos.window.section_general", "GENERAL").Value, Color.Gold);

            builder.AddBadgeRow(TextSOS.Get("sos.item.id", "ID:").Value, [prefab.Identifier.Value], filterPrefix: '!');

            string modName = prefab.ContentPackage?.Name ?? "Vanilla";
            builder.AddBadgeRow("Mod:", [modName], filterPrefix: '@');

            if (prefab is ItemPrefab item)
            {
                if (!item.Aliases.IsEmpty) builder.AddBadgeRow(TextSOS.Get("sos.item.aliases", "Aliases:").Value, item.Aliases);
                builder.AddBadgeRow(TextSOS.Get("sos.item.category", "Category:").Value, item.Category.ToString().Split(','), filterPrefix: '#');
                if (!string.IsNullOrEmpty(cargoBox)) builder.AddSelectorBadgeRow(TextSOS.Get("sos.item.cargo_box", "Cargo Box:").Value, [cargoBox]);
                builder.AddRow(TextSOS.Get("sos.item.max_stack", "Max Stack:").Value, item.MaxStackSize.ToString(), Color.White);
                if (hazards.Count > 0) builder.AddBadgeRow(TextSOS.Get("sos.item.hazards", "Hazards:").Value, hazards);
                builder.AddBadgeRow(TextSOS.Get("sos.item.tags", "TAGS:").Value, item.Tags.Select(t => t.Value), filterPrefix: '$');
            }
            else if (prefab is AfflictionPrefab aff)
            {
                builder.AddBadgeRow("Type:", [aff.AfflictionType.ToString()], filterPrefix: '#');

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
                    builder.AddFullWidthText($"Death Cause: {causeOfDeath}".SetColor(Color.Crimson));
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

        public override void Analyze(Prefab prefab)
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

        }

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
                builder.AddBadgeRow(TextSOS.Get("sos.item.required_faction", "Required Faction:").Value, [factionName]);
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
            public string Identifier = "";
            public string Name = "";
            public float Strength;
            public float Probability;
        }

        public override bool HasData => afflictions.Count > 0 || penetration > 0 || structureDamage > 0 || itemDamage > 0 || reload > 0 || isThrowable || explosionRange > 0;

        public override void Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
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

            var grouped = afflictions.GroupBy(a => a.Identifier);
            foreach (var group in grouped)
            {
                var first = group.First();
                string label = first.Name + ":";
                var ids = group.Select(a => a.Identifier);
                var displayNames = group.Select(a => a.Probability < 1.0f ? $"{a.Strength} ({(int)(a.Probability * 100)}%)" : a.Strength.ToValue());

                builder.AddBadgeRow(label, ids, displayNames, linkColor: Color.Salmon);
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

        public override void Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
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
        }

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
                builder.AddBadgeRow(res.Key + " Res:", [res.Key], [string.Join(", ", res.Value)], linkColor: Color.LightGreen);
            }

            if (equipSlots.Count > 0)
            {
                var uniqueSlots = equipSlots
                    .SelectMany(s => s.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries))
                    .Distinct();

                builder.AddBadgeRow(TextSOS.Get("sos.equip.equips_in", "Equips In:").Value, uniqueSlots, filterPrefix: '&');
            }

            builder.EndSection();
        }
    }

    // MARK: Medical
    public class MedicalSection : BaseStatSection
    {
        private int medicalSkillReq = 0;
        private readonly List<(string Identifier, string DisplayName)> suitableTreatments = [];

        private readonly Dictionary<string, (string Name, float Amount)> alwaysHeals = [];
        private readonly Dictionary<string, (string Name, float Amount)> alwaysCauses = [];
        private readonly Dictionary<string, (string Name, float Amount)> successHeals = [];
        private readonly Dictionary<string, (string Name, float Amount)> successCauses = [];
        private readonly Dictionary<string, (string Name, float Amount)> failureHeals = [];
        private readonly Dictionary<string, (string Name, float Amount)> failureCauses = [];

        public override bool HasData => suitableTreatments.Count > 0 || alwaysHeals.Count > 0 || successHeals.Count > 0 || alwaysCauses.Count > 0 || successCauses.Count > 0;

        public override void Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
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

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.window.section_medical", "MEDICAL").Value, Color.Gold);

            if (medicalSkillReq > 0)
                builder.AddBadgeRow(TextSOS.Get("sos.med.skill_req", "Medical Skill Req:").Value, [medicalSkillReq.ToString()], ["medical " + medicalSkillReq.ToString()], linkColor: Color.Orange);

            if (suitableTreatments.Count > 0)
            {
                builder.AddBadgeRow(
                    TextSOS.Get("sos.med.suitable", "Recommended:").Value,
                    suitableTreatments.Select(t => t.Identifier),
                    suitableTreatments.Select(t => t.DisplayName),
                    linkColor: Color.LightSkyBlue
                );
            }

            void DrawHyperlinkEffect(string label, Dictionary<string, (string Name, float Amount)> dict, Color linkColor)
            {
                if (dict.Count == 0) return;

                var ids = dict.Keys;
                var displayNames = dict.Select(kvp => $"{kvp.Value.Name} ({kvp.Value.Amount.ToValue()})");

                builder.AddBadgeRow(label, ids, displayNames, linkColor: linkColor);
            }

            DrawHyperlinkEffect(TextSOS.Get("sos.med.always_heals", "Always Heals:").Value, alwaysHeals, Color.LightGreen);
            DrawHyperlinkEffect(TextSOS.Get("sos.med.always_causes", "Always Applies:").Value, alwaysCauses, Color.Salmon);

            DrawHyperlinkEffect(TextSOS.Get("sos.med.success_heals", "On Success Heals:").Value, successHeals, Color.LightGreen);
            DrawHyperlinkEffect(TextSOS.Get("sos.med.success_causes", "On Success Applies:").Value, successCauses, Color.Salmon);

            DrawHyperlinkEffect(TextSOS.Get("sos.med.failure_heals", "On Failure Heals:").Value, failureHeals, Color.DarkSeaGreen);
            DrawHyperlinkEffect(TextSOS.Get("sos.med.failure_causes", "On Failure Applies:").Value, failureCauses, Color.Crimson);

            builder.EndSection();
        }
    }

    // MARK: utility
    public class UtilitySection : BaseStatSection
    {
        private readonly Dictionary<string, string> deviceProperties = [];

        public override bool HasData => deviceProperties.Count > 0;

        public override void Analyze(Prefab prefab)
        {
            if (prefab is ItemPrefab item)
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
        }

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

        public override void Analyze(Prefab prefab)
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
        }

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
        private class EffectGroup
        {
            public string Range = "";
            public List<string> Stats = [];
            public List<(string ID, string Name)> Causes = [];
            public List<(string ID, string Name)> Heals = [];
        }

        private readonly List<EffectGroup> effects = [];

        public override bool HasData => effects.Count > 0;

        public override void Analyze(Prefab prefab)
        {
            if (prefab is not AfflictionPrefab aff || aff.configElement == null) return;
            effects.Clear();

            foreach (var element in aff.configElement.GetChildElements("Effect"))
            {
                var group = new EffectGroup
                {
                    Range = $"{element.GetAttributeFloat("minstrength", 0f).ToValue()} - {element.GetAttributeFloat("maxstrength", 0f).ToValue()}"
                };

                float vit = element.GetAttributeFloat("maxvitalitydecrease", 0f);
                if (vit > 0) group.Stats.Add($"Vit: {vit.ToValue().SetColor((vit >= 0) ? Color.LimeGreen : Color.OrangeRed)}");

                float regen = element.GetAttributeFloat("strengthchange", 0f);
                if (regen != 0) group.Stats.Add($"Regen: {$"{regen.ToSignedValue()}/s".SetColor((regen >= 0) ? Color.GreenYellow : Color.OrangeRed)}");

                float speed = element.GetAttributeFloat("minspeedmultiplier", 1f);
                if (speed != 1f) group.Stats.Add($"Speed: {('x' + speed.ToValue()).SetColor((speed >= 1f) ? Color.LimeGreen : Color.OrangeRed)}");

                foreach (var se in element.GetChildElements("StatusEffect"))
                {
                    foreach (var sub in se.Elements())
                    {
                        string n = sub.Name.ToString().ToLowerInvariant();
                        if (n == "affliction" || n == "reduceaffliction")
                        {
                            string id = sub.GetAttributeString("identifier", sub.GetAttributeString("type", "???"));
                            float amt = sub.GetAttributeFloat("amount", sub.GetAttributeFloat("strength", 0f));

                            // TODO: Remember these...
                            var targetAff = AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == id);
                            string displayName = targetAff != null ? targetAff.Name.Value : id;
                            string finalName = $"{displayName} ({Math.Abs(amt).ToValue()})";

                            if (n == "reduceaffliction" || amt < 0)
                                group.Heals.Add((id, finalName));
                            else
                                group.Causes.Add((id, finalName));
                        }
                    }
                }

                if (group.Stats.Count > 0 || group.Causes.Count > 0 || group.Heals.Count > 0)
                    effects.Add(group);
            }
        }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection("EFFECTS BY SEVERITY", Color.Gold);

            foreach (var group in effects)
            {
                builder.AddFullWidthText($"Str: {group.Range.SetColor(Color.Orange)}");

                if (group.Stats.Count > 0)
                    builder.AddFullWidthText($"  -> {string.Join(", ", group.Stats).SetColor(Color.Salmon)}".SetColor(Color.Gray));

                if (group.Causes.Count > 0)
                {
                    builder.AddSelectorBadgeRow("  -> Causes:",
                        group.Causes.Select(c => c.ID),
                        group.Causes.Select(c => c.Name.SetColor(Color.OrangeRed)), '!');
                }

                if (group.Heals.Count > 0)
                {
                    builder.AddSelectorBadgeRow("  -> Heals:",
                        group.Heals.Select(h => h.ID),
                        group.Heals.Select(h => h.Name.SetColor(Color.LightGreen)), '!');
                }

                builder.AddFullWidthText(" ");
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

        public override void Analyze(Prefab prefab)
        {
            if (prefab is AfflictionPrefab affliction)
            {
                aff = affliction;
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

        public override void Analyze(Prefab prefab)
        {
            text = prefab switch
            {
                ItemPrefab item => item.Description?.Value ?? "",
                AfflictionPrefab affliction => string.Join("\n\n", affliction.Descriptions.Select(d => $"({d.MinStrength.ToString().SetColor(Color.Orange)}-{d.MaxStrength.ToString().SetColor(Color.OrangeRed)}) {d.Target.ToString().SetColor(Color.BlueViolet)}: {d.Text}")),
                _ => ""
            };
        }

        public override void Draw(SectionBuilder builder)
        {
            builder.StartSection(TextSOS.Get("sos.item.description", "DESCRIPTION").Value, Color.Gold);

            builder.AddFullWidthText(RichString.Rich(text));

            builder.EndSection();
        }
    }
}