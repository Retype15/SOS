// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.GUI;

namespace SOS.Panels.ItemPanel
{
    // MARK: Item Recipes Tab
    [AutoRegister]
    public class ItemPanelTab : ISOSTab, IDisposable
    {
        public double Order => 0;
        public string TabName => Texts.Get("sos.tab.recipes", "RECIPES").Value;
        public string ToolTip => Texts.Get("sos.tab.recipes_tooltip").Value;
        private GUIFrame? _container;
        private GUIListBox? _colObtain;
        private GUIListBox? _colUsage;

        private Prefab? _currentPrefab;
        private Action<Prefab>? _onPrimary;
        private Action<Prefab>? _onSecondary;

        private static bool needsAnim = true;

        public bool CanHandle(Prefab prefab) => prefab is ItemPrefab;

        public void Init(GUIComponent parentContainer)
        {
            _container = new GUIFrame(new RectTransform(Vector2.One, parentContainer.RectTransform), style: null) { Visible = false };
        }

        public void Show(Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            _currentPrefab = prefab;
            _onPrimary = onPrimary;
            _onSecondary = onSecondary;

            if (_container == null || prefab is not ItemPrefab item) return;
            _container.Visible = true;

            if (!RecipeAnalyzer.DataInitialized)
            {
                _container.ClearChildren();
                _ = new GUITextBlock(new RectTransform(Vector2.One, _container.RectTransform), Texts.Get("sos.tab.recipes.analyzing", "Analyzing recipe dependency graph..."), font: GUIStyle.SubHeadingFont, textAlignment: Alignment.Center);
                RecipeAnalyzer.Initialize(onComplete: () =>
                {
                    if (_container != null && _container.Visible && _currentPrefab != null)
                    {
                        Show(_currentPrefab, _onPrimary!, _onSecondary!);
                    }
                });
                return;
            }

            _container.ClearChildren();

            var recipeSplit = new GUILayoutGroup(new RectTransform(Vector2.One, _container.RectTransform), isHorizontal: true)
            {
                Stretch = true,
                RelativeSpacing = 0.02f
            };

            // obtain
            var obtainContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.49f, 1f), recipeSplit.RectTransform)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), obtainContainer.RectTransform), Texts.Get("sos.window.obtain", "OBTAIN"), font: GUIStyle.SubHeadingFont, textColor: Color.LightGreen, textAlignment: Alignment.Center);
            _colObtain = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), obtainContainer.RectTransform), style: null) { Spacing = 5, Color = Color.Black * 0.2f };

            // usage
            var usageContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.49f, 1f), recipeSplit.RectTransform)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), usageContainer.RectTransform), Texts.Get("sos.window.usage", "USAGE"), font: GUIStyle.SubHeadingFont, textColor: Color.Cyan, textAlignment: Alignment.Center);
            _colUsage = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), usageContainer.RectTransform), style: null) { Spacing = 5, Color = Color.Black * 0.2f };

            var craft = RecipeAnalyzer.GetCraftingRecipes(item);
            var decon = RecipeAnalyzer.GetDeconstructionOutputs(item);
            var uses = RecipeAnalyzer.GetUsesAsIngredient(item);
            var sources = RecipeAnalyzer.GetSourcesFromDeconstruction(item);

            CardBuilder.UIMachineGroup GetOrCreateMachineGroup(Dictionary<string, CardBuilder.UIMachineGroup> dict, IEnumerable<Identifier> machineIds, string fallbackName)
            {
                string key = machineIds.Any() ? string.Join(", ", machineIds.Select(id => CardBuilder.ResolveMachineName(id)).OrderBy(s => s)) : fallbackName;
                if (!dict.TryGetValue(key, out CardBuilder.UIMachineGroup? value))
                {
                    value = new CardBuilder.UIMachineGroup { MachineName = key };
                    if (machineIds.Any(id => id == "vendingmachine"))
                    {
                        value.IsVendingMachine = true;
                        value.PriceString = (item.defaultPrice?.Price ?? 0).ToString();
                    }
                    dict[key] = value;
                }
                return value;
            }

            // fill obtain
            var obtainGroups = new Dictionary<string, CardBuilder.UIMachineGroup>();
            var controller = SOSController.Instance;
            foreach (var r in craft ?? [])
                GetOrCreateMachineGroup(obtainGroups, r.SuitableFabricatorIdentifiers, Texts.Get("sos.recipe.hand", "Hand").Value)
                    .AddCard(new CardBuilder.CraftRecipeCard(r, item, controller, onPrimary, onSecondary));

            var groupedSources = sources?.GroupBy(s => new { SourceId = s.Item.Identifier, MachineKey = string.Join(",", s.DeconstructItem.RequiredDeconstructor.Select(id => id.Value).OrderBy(x => x)), OtherItemsKey = string.Join(",", s.DeconstructItem.RequiredOtherItem.Select(id => id.Value).OrderBy(x => x)) })
                .Select(group => new GroupedSource { SourceItem = group.First().Item, MachineIds = group.First().DeconstructItem.RequiredDeconstructor, RequiredOtherItems = [.. group.First().DeconstructItem.RequiredOtherItem], TotalCommonness = group.Sum(g => g.DeconstructItem.Commonness), Amount = group.First().DeconstructItem.Amount, IsRandom = group.First().Item.RandomDeconstructionOutput }).ToList();

            foreach (var src in groupedSources ?? [])
                GetOrCreateMachineGroup(obtainGroups, src.MachineIds ?? [], CardBuilder.ResolveMachineName("deconstructor".ToIdentifier()))
                    .AddCard(new CardBuilder.SourceRecipeCard(src, onPrimary, onSecondary));

            foreach (var group in obtainGroups.Values) group.Draw(_colObtain);

            // f usage
            var usageDict = new Dictionary<string, CardBuilder.UIMachineGroup>();
            if (decon?.Count > 0)
            {
                foreach (var machineDecons in decon.GroupBy(di => string.Join(",", di.RequiredDeconstructor.Select(id => id.Value).OrderBy(s => s))))
                {
                    var mg = GetOrCreateMachineGroup(usageDict, machineDecons.First().RequiredDeconstructor, CardBuilder.ResolveMachineName("deconstructor".ToIdentifier()));
                    var deconList = machineDecons.ToList();

                    if (item.RandomDeconstructionOutput) mg.AddCard(new CardBuilder.DeconOutputCard(item, deconList, onPrimary, onSecondary));
                    else foreach (var output in deconList.GroupBy(di => di.ItemIdentifier).Select(g => new { ID = g.Key, Amount = g.Max(di => di.Amount), Weight = g.Sum(di => di.Commonness) }))
                            mg.AddCard(new CardBuilder.SingleDeconOutputCard(item, output.ID, output.Amount, output.Weight, onPrimary, onSecondary));
                }
            }

            var groupedUses = uses?.GroupBy(u => string.Join(",", u.Recipe.SuitableFabricatorIdentifiers.Select(id => id.Value).OrderBy(s => s)))
                .SelectMany(mg => mg.GroupBy(u => u.Item.Identifier).Select(ig => new GroupedUsage { TargetItem = ig.First().Item, MachineIds = [.. ig.First().Recipe.SuitableFabricatorIdentifiers], AmountCreated = ig.First().Recipe.Amount, AmountRequired = ig.First().Recipe.RequiredItems.FirstOrDefault(ri => ri.ItemPrefabs.Any(p => p.Identifier == item.Identifier))?.Amount ?? 1 })).ToList();

            foreach (var usage in groupedUses ?? [])
                GetOrCreateMachineGroup(usageDict, usage.MachineIds ?? [], Texts.Get("sos.recipe.hand", "Hand").Value)
                    .AddCard(new CardBuilder.UsageRecipeCard(usage, onPrimary, onSecondary));

            foreach (var group in usageDict.Values) group.Draw(_colUsage);

            if (needsAnim)
            {
                recipeSplit.ExFadeIn(1f, alsoChildren: true);
                needsAnim = false;
            }
        }

        public void Hide()
        {
            if (_container != null) _container.Visible = false;
        }

        public void Dispose()
        {
            _container?.Parent?.RemoveChild(_container);
            _colObtain = null;
            _colUsage = null;
            GC.SuppressFinalize(this);
        }
    }
}