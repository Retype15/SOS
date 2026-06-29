// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{

    public abstract class CenterPanelTab
    {
        public virtual string TabTooltip => "";

        public virtual GUIButton CreateTabButton(string text, RectTransform parent, bool isActive, Action onClick)
        {
            Vector2 textSize = GUIStyle.SmallFont.MeasureString(text);
            int width = (int)textSize.X + 24;

            var tabBtn = new GUIButton(new RectTransform(new Point(width, 32), parent), text, style: "MainMenuNotificationButton") //MainMenuNotificationButton,
            {
                Selected = isActive,
                ToolTip = TextSOS.Get(TabTooltip, ""),
                OnClicked = (_, _) => { onClick(); return true; },
            };
            //tabBtn.ExBlink(3f, 0.5f, 1f, 0.5f).WaitFinish();

            return tabBtn;
        }
    }

    // MARK: Item Recipes Tab
    public class ItemCenterPanelTab : CenterPanelTab, ITab
    {
        public string TabName => TextSOS.Get("sos.tab.recipes", "RECIPES").Value;
        public override string TabTooltip => "sos.tab.recipes_tooltip";
        private GUIFrame? _container;

        public bool CanHandle(Prefab prefab) => prefab is ItemPrefab;

        public void Initialize(GUIComponent parentContainer)
        {
            _container = new GUIFrame(new RectTransform(Vector2.One, parentContainer.RectTransform), style: null) { Visible = false };
        }

        public void Activate(Prefab prefab, SOSController controller, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (_container == null || prefab is not ItemPrefab item) return;
            _container.Visible = true;
            _container.ClearChildren();

            var craft = RecipeAnalyzer.GetCraftingRecipes(item);
            var decon = RecipeAnalyzer.GetDeconstructionOutputs(item);
            var uses = RecipeAnalyzer.GetUsesAsIngredient(item);
            var sources = RecipeAnalyzer.GetSourcesFromDeconstruction(item);

            var recipeSplit = new GUILayoutGroup(new RectTransform(Vector2.One, _container.RectTransform), isHorizontal: true)
            {
                Stretch = true,
                RelativeSpacing = 0.02f
            };

            // obtain
            var obtainContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.49f, 1f), recipeSplit.RectTransform)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), obtainContainer.RectTransform), TextSOS.Get("sos.window.obtain", "OBTAIN"), font: GUIStyle.SubHeadingFont, textColor: Color.LightGreen, textAlignment: Alignment.Center);
            var colObtain = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), obtainContainer.RectTransform), style: null) { Spacing = 5, Color = Color.Black * 0.2f };

            // usage
            var usageContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.49f, 1f), recipeSplit.RectTransform)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), usageContainer.RectTransform), TextSOS.Get("sos.window.usage", "USAGE"), font: GUIStyle.SubHeadingFont, textColor: Color.Cyan, textAlignment: Alignment.Center);
            var colUsage = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), usageContainer.RectTransform), style: null) { Spacing = 5, Color = Color.Black * 0.2f };

            CardBuilder.UIMachineGroup GetOrCreateMachineGroup(Dictionary<string, CardBuilder.UIMachineGroup> dict, IEnumerable<Identifier> machineIds, string fallbackName)
            {
                string key = machineIds.Any() ? string.Join(", ", machineIds.Select(id => CardBuilder.ResolveMachineName(id)).OrderBy(s => s)) : fallbackName;
                if (!dict.TryGetValue(key, out CardBuilder.UIMachineGroup? value))
                {
                    value = new CardBuilder.UIMachineGroup { MachineName = key };
                    if (machineIds.Any(id => id == "vendingmachine"))
                    {
                        value.IsVendingMachine = true;
                        value.PriceString = (PrefabAdapter.DefaultPrice(item)?.Price ?? 0).ToString();
                    }
                    dict[key] = value;
                }
                return value;
            }

            // fill obtain
            var obtainGroups = new Dictionary<string, CardBuilder.UIMachineGroup>();
            foreach (var r in craft ?? [])
                GetOrCreateMachineGroup(obtainGroups, r.SuitableFabricatorIdentifiers, TextSOS.Get("sos.recipe.hand", "Hand").Value)
                    .AddCard(new CardBuilder.CraftRecipeCard(r, item, controller, onPrimary, onSecondary));

            var groupedSources = sources?.GroupBy(s => new { SourceId = s.Item.Identifier, MachineKey = string.Join(",", s.DeconstructItem.RequiredDeconstructor.Select(id => id.Value).OrderBy(x => x)), OtherItemsKey = string.Join(",", s.DeconstructItem.RequiredOtherItem.Select(id => id.Value).OrderBy(x => x)) })
                .Select(group => new GroupedSource { SourceItem = group.First().Item, MachineIds = group.First().DeconstructItem.RequiredDeconstructor, RequiredOtherItems = [.. group.First().DeconstructItem.RequiredOtherItem], TotalCommonness = group.Sum(g => g.DeconstructItem.Commonness), Amount = group.First().DeconstructItem.Amount, IsRandom = group.First().Item.RandomDeconstructionOutput }).ToList();

            foreach (var src in groupedSources ?? [])
                GetOrCreateMachineGroup(obtainGroups, src.MachineIds ?? [], CardBuilder.ResolveMachineName("deconstructor".ToIdentifier()))
                    .AddCard(new CardBuilder.SourceRecipeCard(src, onPrimary, onSecondary));

            foreach (var group in obtainGroups.Values) group.Draw(colObtain);

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
                GetOrCreateMachineGroup(usageDict, usage.MachineIds ?? [], TextSOS.Get("sos.recipe.hand", "Hand").Value)
                    .AddCard(new CardBuilder.UsageRecipeCard(usage, onPrimary, onSecondary));

            foreach (var group in usageDict.Values) group.Draw(colUsage);
        }

        public void Deactivate()
        {
            if (_container != null) _container.Visible = false;
        }
    }

    // MARK: - Clinic SIM
    public class AfflictionCenterPanelTab : CenterPanelTab, ITab
    {
        public const int MENU_WIDTH = 280;
        public const int MENU_HEIGHT = 220;

        public string TabName => TextSOS.Get("sos.tab.simulator", "SIMULATOR").Value;
        public override string TabTooltip => "sos.tab.simulator_tooltip";
        private GUIFrame? _container;
        private static GUIComponent? activeAfflictionMenu;
        private static Prefab? CurrentPrefab;
        private GUIListBox? _affList;

        private Action<Prefab>? _onPrimary;

        public bool CanHandle(Prefab prefab) => prefab is ItemPrefab || prefab is AfflictionPrefab;

        public void Initialize(GUIComponent parentContainer)
        {
            _container = new GUIFrame(new RectTransform(Vector2.One, parentContainer.RectTransform), style: null) { Visible = false };

            var simView = new GUIFrame(new RectTransform(new Vector2(1f, 0.88f), _container.RectTransform, Anchor.TopCenter), style: null)
            {
                CanBeFocused = false
            };

            _ = new GUICustomComponent(new RectTransform(Vector2.Zero, simView.RectTransform),
                onUpdate: (dt, c) =>
                {
                    ClinicalSimulatorManager.Update(dt);

                    if (activeAfflictionMenu != null)
                    {
                        if (PlayerInput.PrimaryMouseButtonClicked() && !activeAfflictionMenu.Rect.Contains(PlayerInput.MousePosition.ToPoint()))
                        {
                            activeAfflictionMenu.Parent?.RemoveChild(activeAfflictionMenu);
                            activeAfflictionMenu = null;
                        }
                    }

                    var health = ClinicalSimulatorManager.Patient?.CharacterHealth;
                    if (health == null) return;

                    if (ClinicalSimulatorManager.HealthWindowField?.GetValue(health) is GUIFrame nativeWindow && nativeWindow.RectTransform.Parent != simView.RectTransform)
                    {
                        AttachNativeWindow(nativeWindow, simView);
                    }

                    bool primaryClicked = PlayerInput.PrimaryMouseButtonClicked();
                    bool secondaryClicked = PlayerInput.SecondaryMouseButtonClicked();

                    if ((primaryClicked || secondaryClicked) && simView.Rect.Contains(PlayerInput.MousePosition.ToPoint()))
                    {
                        // iconos de afliccion
                        if (GUI.MouseOn?.UserData is Affliction highlightedAff)
                        {
                            if (primaryClicked) _onPrimary?.Invoke(highlightedAff.Prefab);
                            else if (secondaryClicked) ShowSimulationMenu(_container, highlightedAff.Prefab, null);
                            return;
                        }

                        // Lista de aflicciones
                        if (_affList != null && _affList.Rect.Contains(PlayerInput.MousePosition.ToPoint()))
                        {
                            var hovered = _affList.Content.Children.FirstOrDefault(child => child.Rect.Contains(PlayerInput.MousePosition.ToPoint()));
                            if (hovered?.UserData is Affliction affFromList)
                            {
                                if (primaryClicked) _onPrimary?.Invoke(affFromList.Prefab);
                                else if (secondaryClicked) ShowSimulationMenu(_container, affFromList.Prefab, null);
                                return;
                            }
                        }

                        // Partes del cuerpo
                        if (secondaryClicked && ClinicalSimulatorManager.HighlightedLimbField?.GetValue(health) is int highlighted && highlighted >= 0)
                        {
                            var limbHealth = health.limbHealths[highlighted];
                            var limb = health.Character.AnimController.Limbs.FirstOrDefault(l => health.GetMatchingLimbHealth(l) == limbHealth);
                            if (limb != null && CurrentPrefab != null)
                                ShowSimulationMenu(_container, CurrentPrefab, limb);
                        }
                    }
                });

            var toolbar = new GUIFrame(new RectTransform(new Vector2(1f, 0f), _container.RectTransform, Anchor.BottomCenter), style: "GUIFrameBottom")
            {
                RectTransform = { MinSize = new Point(0, 38), MaxSize = new Point(int.MaxValue, 38) }
            };
            var tools = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0f), toolbar.RectTransform, Anchor.Center) { MinSize = new Point(0, 26), MaxSize = new Point(int.MaxValue, 26) }, isHorizontal: true) { Stretch = true, AbsoluteSpacing = 15 };

            var hpText = new GUITextBlock(new RectTransform(new Vector2(0.15f, 1f), tools.RectTransform), "HP: 100%", font: GUIStyle.SubHeadingFont, textAlignment: Alignment.Center);
            _ = new GUICustomComponent(new RectTransform(Vector2.Zero, tools.RectTransform), onUpdate: (dt, c) =>
            {
                if (ClinicalSimulatorManager.Patient != null)
                {
                    int v = (int)ClinicalSimulatorManager.Patient.CharacterHealth.Vitality;
                    hpText.Text = (v > -100) ? $"HP: {v}%" : TextManager.Get("deceased").ToUpper();
                    hpText.TextColor = v <= 0 ? Color.Red : (v < 50 ? Color.Orange : Color.Lime);
                }
            });

            var playBtn = new GUIButton(new RectTransform(new Vector2(0.25f, 1f), tools.RectTransform), "START", style: "DeviceButton")
            {
                OnDrawToolTip = component => component.ToolTip = TextSOS.Get(
                    ClinicalSimulatorManager.IsPlaying ? "sos.sim.pause_tooltip" : "sos.sim.play_tooltip",
                    ClinicalSimulatorManager.IsPlaying ? "Pauses the clinical simulation." : "Starts the clinical simulation."),
                OnClicked = (_, _) => { ClinicalSimulatorManager.HasPlaying(!ClinicalSimulatorManager.IsPlaying); return true; }
            };
            _ = new GUICustomComponent(new RectTransform(Vector2.Zero, playBtn.RectTransform), onUpdate: (dt, c) =>
            {
                playBtn.Text = ClinicalSimulatorManager.IsPlaying ? "PAUSE" : "START";
            });

            var speedDropdown = new GUIDropDown(new RectTransform(new Vector2(0.25f, 1f), tools.RectTransform), text: "Speed", elementCount: 4)
            {
                ToolTip = TextSOS.Get("sos.aff.speedDropdown", "More than x1 are still in progress. \nIt's work fine in Singleplayer, but not at all in Multiplayer."),
                OnSelected = (comp, obj) =>
                {
                    string s = (string)obj;
                    ClinicalSimulatorManager.SetTimeScale(s switch { "x1" => 1f, "x2" => 2f, "x5" => 5f, "x10" => 10f, _ => 1f });
                    return true;
                },
            };
            foreach (string s in new[] { "x1", "x2", "x5", "x10" }) speedDropdown.AddItem(s, s);
            speedDropdown.SelectItem("x1");

            var actionBtn = new GUIButton(new RectTransform(new Vector2(0.25f, 1f), tools.RectTransform), "DROP", style: "DeviceButton")
            {
                OnDrawToolTip = component => component.ToolTip = TextSOS.Get(
                    ClinicalSimulatorManager.HasStarted ? "sos.sim.drop_tooltip" : "sos.sim.reset_tooltip",
                    ClinicalSimulatorManager.HasStarted ? "Discards the current patient." : "Resets the patient's health to its initial state."),
                OnClicked = (_, _) =>
                {
                    if (ClinicalSimulatorManager.HasStarted)
                    {
                        ClinicalSimulatorManager.DiscardPatient();
                    }
                    else
                    {
                        if (ClinicalSimulatorManager.Patient != null && ClinicalSimulatorManager.Patient.IsDead)
                            ClinicalSimulatorManager.DiscardPatient();
                        else
                            ClinicalSimulatorManager.ResetPatient();
                    }
                    return true;
                }
            };
            _ = new GUICustomComponent(new RectTransform(Vector2.Zero, actionBtn.RectTransform), onUpdate: (dt, c) =>
            {
                actionBtn.Text = ClinicalSimulatorManager.HasStarted ? "DROP" : "RESET";
            });
        }

        private void AttachNativeWindow(GUIFrame nativeWindow, GUIFrame simView)
        {
            ClinicalSimulatorManager.RegisterHijackedWindow(nativeWindow);

            nativeWindow.RectTransform.Parent = simView.RectTransform;
            var scaleBasisField = typeof(RectTransform).GetField("scaleBasis", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            scaleBasisField?.SetValue(nativeWindow.RectTransform, ScaleBasis.Normal);

            nativeWindow.RectTransform.IsFixedSize = false;
            nativeWindow.RectTransform.RelativeSize = Vector2.One;
            nativeWindow.RectTransform.AbsoluteOffset = Point.Zero;
            nativeWindow.RectTransform.Anchor = Anchor.Center;
            nativeWindow.RectTransform.Pivot = Pivot.Center;

            nativeWindow.IgnoreLayoutGroups = true;
            nativeWindow.Color = Color.Transparent;
            nativeWindow.ApplyStyle(null);

            if (nativeWindow.Children.FirstOrDefault() is GUILayoutGroup internalVerticalLayout)
            {
                internalVerticalLayout.RectTransform.RelativeSize = Vector2.One;
                internalVerticalLayout.Stretch = true;
                var children = internalVerticalLayout.Children.ToList();

                if (children.Count > 0)
                {
                    children[0].RectTransform.RelativeSize = new Vector2(1f, 0.10f);

                    var subChildren = children[0].Children.ToList();
                    if (subChildren.Count > 2)
                    {
                        // Person Icon
                        subChildren[0].RectTransform.RelativeSize = new Vector2(0.22f, 1.0f);
                        // Name 
                        subChildren[1].RectTransform.RelativeSize = new Vector2(0.56f, 1.0f);
                        // Profession Icon
                        subChildren[2].RectTransform.RelativeSize = new Vector2(0.22f, 1.0f);
                    }
                }

                // Health Bar
                if (children.Count > 1) children[1].RectTransform.RelativeSize = new Vector2(1f, 0.05f);

                // Spacing
                if (children.Count > 2) children[2].RectTransform.RelativeSize = new Vector2(1f, 0.02f);

                // Body
                if (children.Count > 3 && children[3] is GUILayoutGroup bodyArea)
                {
                    bodyArea.RectTransform.RelativeSize = new Vector2(1f, 0.70f);
                    bodyArea.Stretch = true;
                    var parts = bodyArea.Children.ToList();
                    foreach (var bodyPart in parts)
                    {
                        if (bodyPart is GUICustomComponent) bodyPart.RectTransform.RelativeSize = new Vector2(0.5f, 1f);
                        if (bodyPart is GUIListBox affList) { affList.RectTransform.RelativeSize = new Vector2(0.4f, 1f); _affList = affList; }
                    }
                }

                // Treatment List
                if (children.Count > 5) children[5].RectTransform.RelativeSize = new Vector2(1f, 0.13f);

                internalVerticalLayout.RectTransform.RecalculateChildren(true, true);
            }
        }

        public void Activate(Prefab prefab, SOSController controller, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (_container == null) return;
            CurrentPrefab = prefab;
            _onPrimary = onPrimary;

            if (ClinicalSimulatorManager.Patient == null || ClinicalSimulatorManager.Patient.Removed)
            {
                ClinicalSimulatorManager.Initialize(controller.DummyDeathCount, controller.DummyCharacterXML);
            }

            _container.Visible = true;
        }

        public void Deactivate()
        {
            if (_container != null) _container.Visible = false;
        }

        private static void ShowSimulationMenu(GUIFrame mainFrame, Prefab prefab, Limb? targetLimb)
        {
            activeAfflictionMenu?.Parent?.RemoveChild(activeAfflictionMenu);
            activeAfflictionMenu = null;

            Vector2 mousePos = PlayerInput.MousePosition;
            int x = (int)mousePos.X;
            int y = (int)mousePos.Y;

            //if (x + MENU_WIDTH > GameMain.GraphicsWidth) x = GameMain.GraphicsWidth - MENU_WIDTH;
            //if (y + MENU_HEIGHT > GameMain.GraphicsHeight) y = GameMain.GraphicsHeight - MENU_HEIGHT;

            int relX = x - mainFrame.Rect.X;
            int relY = y - mainFrame.Rect.Y;

            activeAfflictionMenu = new GUIFrame(new RectTransform(new Point(MENU_WIDTH, MENU_HEIGHT), mainFrame.RectTransform) { AbsoluteOffset = new Point(relX, relY) }, style: "GUIFrame")
            {
                CanBeFocused = true,
                UserData = "SimMenu"
            };

            var menuLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), activeAfflictionMenu.RectTransform, Anchor.Center)) { Stretch = true, AbsoluteSpacing = 8 };

            string targetName = targetLimb != null ? targetLimb.type.ToString().ToUpper() : "OVERALL";
            string title = $"{prefab.Name().ToUpper()} ON {targetName}";

            var titleBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0.25f), menuLayout.RectTransform), title, font: GUIStyle.SubHeadingFont, textAlignment: Alignment.Center);
            titleBlock.Text = ToolBox.LimitString(titleBlock.Text, titleBlock.Font, titleBlock.Rect.Width);

            if (prefab is AfflictionPrefab affPrefab)
            {
                _ = new GUIButton(new RectTransform(new Vector2(1f, 0.18f), menuLayout.RectTransform), "SET MAX (100%)", style: "GUIButtonSmall")
                {
                    ToolTip = TextSOS.Get("sos.sim.set_max_tooltip", "Sets this affliction's strength to maximum."),
                    OnClicked = (_, _) => { ClinicalSimulatorManager.SetAfflictionStrength(affPrefab, affPrefab.MaxStrength, targetLimb); activeAfflictionMenu?.Parent?.RemoveChild(activeAfflictionMenu); activeAfflictionMenu = null; return true; }
                };

                var sliderLayout = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.18f), menuLayout.RectTransform), isHorizontal: true) { Stretch = true, AbsoluteSpacing = 5 };
                _ = new GUITextBlock(new RectTransform(new Vector2(0.2f, 1f), sliderLayout.RectTransform), "VAL", font: GUIStyle.SmallFont);
                var slider = new GUIScrollBar(new RectTransform(new Vector2(0.8f, 1f), sliderLayout.RectTransform), barSize: 0.1f, style: "GUISlider")
                {
                    Step = 0.01f,
                    OnMoved = (sb, val) =>
                    {
                        ClinicalSimulatorManager.SetAfflictionStrength(affPrefab, val * affPrefab.MaxStrength, targetLimb);
                        return true;
                    }
                };

                var currentAff = targetLimb != null ?
                    ClinicalSimulatorManager.Patient?.CharacterHealth.GetAffliction(affPrefab.Identifier, targetLimb) :
                    ClinicalSimulatorManager.Patient?.CharacterHealth.GetAffliction(affPrefab.Identifier);

                if (currentAff != null) slider.BarScroll = currentAff.Strength / affPrefab.MaxStrength;

                _ = new GUIButton(new RectTransform(new Vector2(1f, 0.18f), menuLayout.RectTransform), "REMOVE", style: "GUIButtonSmall")
                {
                    ToolTip = TextSOS.Get("sos.sim.remove_tooltip", "Removes this affliction from the patient."),
                    OnClicked = (_, _) => { ClinicalSimulatorManager.RemoveAffliction(affPrefab, targetLimb); activeAfflictionMenu?.Parent?.RemoveChild(activeAfflictionMenu); activeAfflictionMenu = null; return true; }
                };
            }
            else if (prefab is ItemPrefab itemPrefab)
            {
                _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.35f), menuLayout.RectTransform), "Simulate item usage on this limb.", font: GUIStyle.SmallFont, wrap: true, textAlignment: Alignment.Center);

                _ = new GUIButton(new RectTransform(new Vector2(1f, 0.25f), menuLayout.RectTransform), "USE / APPLY", style: "GUIButton")
                {
                    ToolTip = TextSOS.Get("sos.sim.apply_item_tooltip", "Simulates using this item on the patient to see its effects."),
                    OnClicked = (_, _) =>
                    {
                        ClinicalSimulatorManager.ApplyMockItem(itemPrefab, targetLimb);
                        activeAfflictionMenu?.Parent?.RemoveChild(activeAfflictionMenu);
                        activeAfflictionMenu = null;
                        return true;
                    }
                };
            }

            _ = new GUIButton(new RectTransform(new Vector2(1f, 0.18f), menuLayout.RectTransform), "CLOSE", style: "GUIButtonSmall")
            {
                ToolTip = TextSOS.Get("sos.misc.close_button", "Closes this menu."),
                OnClicked = (_, _) => { activeAfflictionMenu?.Parent?.RemoveChild(activeAfflictionMenu); activeAfflictionMenu = null; return true; }
            };
        }
    }
}