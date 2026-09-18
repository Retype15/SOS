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
    [AutoRegister("SOS.Tab.ItemRecipe", 0)]
    public class ItemPanelTab : ISOSTab
    {
        public string Id => "SOS.Tab.ItemRecipe";
        private GUIFrame? _container;
        private GUIListBox? _colObtain;
        private GUIListBox? _colUsage;

        private Prefab? _currentPrefab = null;

        private static bool needsAnim = true;

        public bool CanHandle(Prefab prefab) => prefab is ItemPrefab;

        public void Init(GUIFrame container, GUIButton tabButton)
        {
            _container = container;
        }

        public void Update(Prefab target)
        {
            _currentPrefab = target;
            Rebuild(target);
        }

        private void Rebuild(Prefab target)
        {
            if (_container == null || target is not ItemPrefab item) return;
            if (!RecipeAnalyzer.DataInitialized)
            {
                _container.ClearChildren();
                _ = new GUITextBlock(new RectTransform(Vector2.One, _container.RectTransform), Texts.Get("sos.tab.recipes.analyzing", "Analyzing recipe dependency graph..."), font: GUIStyle.SubHeadingFont, textAlignment: Alignment.Center);
                RecipeAnalyzer.Initialize(onComplete: () =>
                {
                    CrossThread.RequestExecutionOnMainThread(() =>
                    {
                        if (_container != null && _currentPrefab is ItemPrefab pending)
                        {
                            Rebuild(_currentPrefab);
                        }
                    });
                });
                return;
            }

            _container.ClearChildren();

            var onPrimary = Profiles.ProfileHelper.OnPrimary;
            var onSecondary = Profiles.ProfileHelper.OnSecondary;

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

    }
}