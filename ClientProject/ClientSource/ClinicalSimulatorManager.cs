// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace SOS
{
    public static class ClinicalSimulatorManager
    {
        public static Character? Patient { get; private set; }
        public static bool IsPlaying = false;
        public static float TimeScale = 1f;
        public static bool HasStarted = true;

        private static List<(AfflictionPrefab prefab, float strength, LimbType limb)>? snapshot;

        // refection
        public static readonly FieldInfo? HealthWindowField = typeof(CharacterHealth).GetField("healthWindow", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo? healthBiologicalUpdateMethod = typeof(CharacterHealth).GetMethod("Update", [typeof(float)]);
        private static readonly FieldInfo? selectedCharField = typeof(Character).GetField("selectedCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo? tooltipField = typeof(CharacterHealth).GetField("afflictionTooltip", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo? HighlightedLimbField = typeof(CharacterHealth).GetField("highlightedLimbIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo? HighlightedAfflictionField = typeof(CharacterHealth).GetField("highlightedAffliction", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly FieldInfo? TreatmentContainerField = typeof(CharacterHealth).GetField("recommendedTreatmentContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly FieldInfo? SelectedLimbIndexField = typeof(CharacterHealth).GetField("selectedLimbIndex", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo? openHealthWindowField = typeof(CharacterHealth).GetField("openHealthWindow", BindingFlags.NonPublic | BindingFlags.Static);

        public static void Initialize()
        {
            if (Patient != null) return;

            var prefab = CharacterPrefab.FindBySpeciesName("human".ToIdentifier());
            if (prefab == null) return;

            CharacterInfo info = new(prefab.Identifier, name: "Dummy Daniel", originalName: "SOS Dummy", jobOrJobPrefab: JobPrefab.Get("medicaldoctor"));
            if (info.IsFemale) info.Name = "Dummy Daniella";
            else info.Name = "Dummy Daniel";

            Patient = Character.Create(prefab, new Vector2(0, -10000), "SOS_DUMMY", info, isRemotePlayer: false, hasAi: false, createNetworkEvent: false, spawnInitialItems: false);
            Character.CharacterList.Remove(Patient);
            Patient.FreeID();

            Patient.AnimController.Frozen = true;
            Patient.DisableInteract = true;
            Patient.NeedsAir = false;

            EnforceDummyInertia(Patient);

            Patient.CharacterHealth.SetHealthBarVisibility(false);
            if (Patient.CharacterHealth.SuicideButton != null) Patient.CharacterHealth.SuicideButton.Visible = false;

            if (Patient.CharacterHealth.CPRButton != null) Patient.CharacterHealth.CPRButton.Visible = false;
        }

        private static void EnforceDummyInertia(Character dummy)
        {
            if (dummy == null) return;

            dummy.AnimController.SimplePhysicsEnabled = true;

            dummy.InvisibleTimer = float.MaxValue;

            if (dummy.AiTarget != null)
            {
                dummy.AiTarget.InDetectable = true;
                dummy.AiTarget.SoundRange = 0f;
                dummy.AiTarget.SightRange = 0f;
            }
        }

        public static void Update(float deltaTime)
        {
            if (Patient?.CharacterHealth == null || Patient.Removed) return;

            if (Patient.InvisibleTimer < 1.0f) Patient.InvisibleTimer = float.MaxValue;

            Patient.CharacterHealth.UpdateClientSpecific(deltaTime);

            if (IsPlaying)
            {
                if (HasStarted)
                {
                    TakeSnapshot();
                    HasStarted = false;
                }
                float dt = deltaTime * TimeScale;
                healthBiologicalUpdateMethod?.Invoke(Patient.CharacterHealth, [dt]);
            }

            var controlled = Character.Controlled;
            if (controlled == null) return;

            var prevSelected = selectedCharField?.GetValue(controlled) as Character;
            selectedCharField?.SetValue(controlled, Patient);

            openHealthWindowField?.SetValue(null, Patient.CharacterHealth);

            Patient.CharacterHealth.UpdateHUD(deltaTime);

            if (tooltipField?.GetValue(Patient.CharacterHealth) is GUIComponent tooltip)
            {
                tooltip.AddToGUIUpdateList(ignoreChildren: false, order: 100000);
            }

            if (TreatmentContainerField?.GetValue(Patient.CharacterHealth) is GUIListBox treatmentList)
            {
                foreach (GUIComponent component in treatmentList.Content.Children)
                {
                    if (component.GetChild<GUIButton>() is GUIButton treatmentBtn && treatmentBtn.UserData is ItemPrefab medPrefab)
                    {
                        bool wasDisabled = !treatmentBtn.Enabled;

                        treatmentBtn.Enabled = true;
                        foreach (var child in treatmentBtn.Children) child.Enabled = true;

                        treatmentBtn.OnClicked = (btn, userData) =>
                        {
                            int selectedIndex = (int)(SelectedLimbIndexField?.GetValue(Patient.CharacterHealth) ?? -1);
                            Limb? targetLimb = selectedIndex >= 0 ? Patient.AnimController.Limbs.FirstOrDefault(l => l.HealthIndex == selectedIndex) : null;

                            ApplyMockItem(medPrefab, targetLimb);
                            return true;
                        };

                        if (wasDisabled)
                        {
                            string colorHex = Color.LightSkyBlue.ToStringHex();
                            treatmentBtn.ToolTip = RichString.Rich($"‖color:{colorHex}‖[SIMULATION MODE]‖color:end‖\n‖color:255,255,255,255‖{medPrefab.Name.Value}‖color:end‖\n{medPrefab.Description}");
                        }
                    }
                }
            }

            selectedCharField?.SetValue(controlled, prevSelected);
        }

        public static void ApplyMockItem(ItemPrefab prefab, Limb? targetLimb)
        {
            if (Patient == null) return;
            Item? mockItem = null;
            try
            {
                mockItem = new Item(prefab, Vector2.Zero, null);

                Patient.Inventory?.TryPutItem(mockItem, Patient, CharacterInventory.AnySlot, false, false);

                var user = Character.Controlled ?? Patient;
                var target = targetLimb ?? Patient.AnimController.MainLimb;

                var meleeWeapon = mockItem.GetComponent<Barotrauma.Items.Components.MeleeWeapon>();
                var projectile = mockItem.GetComponent<Barotrauma.Items.Components.Projectile>();

                if (meleeWeapon?.Attack != null)
                {
                    meleeWeapon.Attack.DoDamageToLimb(
                        attacker: user,
                        targetLimb: target,
                        worldPosition: target.WorldPosition,
                        deltaTime: 1.0f,
                        playSound: true
                    );
                }
                else if (projectile?.Attack != null)
                {
                    projectile.Attack.DoDamageToLimb(
                        attacker: user,
                        targetLimb: target,
                        worldPosition: target.WorldPosition,
                        deltaTime: 1.0f,
                        playSound: true
                    );
                }
                else
                {
                    foreach (var ic in mockItem.Components)
                    {
                        if (!ic.HasRequiredContainedItems(user, addMessage: false)) continue;

                        bool success = Rand.Range(0.0f, 0.5f) < ic.DegreeOfSuccess(user);
                        var conditionalType = success ? ActionType.OnSuccess : ActionType.OnFailure;

                        ic.ApplyStatusEffects(conditionalType, 1.0f, Patient, target, useTarget: Patient, user: user);
                        ic.ApplyStatusEffects(ActionType.OnUse, 1.0f, Patient, target, useTarget: Patient, user: user);
                    }
                }

                SoundPlayer.PlayUISound(GUISoundType.Select);
            }
            catch (Exception e)
            {
                LuaCsLogger.LogError($"[SOS] Simulation Mock Item Error: {e.Message}");
            }
            finally
            {
                if (mockItem != null && !mockItem.Removed)
                {
                    mockItem.ParentInventory?.RemoveItem(mockItem);
                    Entity.Spawner?.AddItemToRemoveQueue(mockItem);
                }
            }
        }

        public static void InjectAffliction(AfflictionPrefab prefab, float strength, Limb? targetLimb = null)
        {
            if (Patient?.CharacterHealth == null) return;
            var inst = prefab.Instantiate(strength);
            var limb = targetLimb ?? Patient.AnimController.GetLimb(prefab.IndicatorLimb != LimbType.None ? prefab.IndicatorLimb : LimbType.Torso) ?? Patient.AnimController.MainLimb;
            Patient.CharacterHealth.ApplyAffliction(limb, inst);
        }

        public static void RemoveAffliction(AfflictionPrefab prefab, Limb? targetLimb = null)
        {
            if (Patient?.CharacterHealth == null) return;

            if (targetLimb != null)
            {
                var aff = Patient.CharacterHealth.GetAffliction(prefab.Identifier, targetLimb);
                if (aff != null) Patient.CharacterHealth.ReduceAfflictionOnLimb(targetLimb, prefab.Identifier, aff.Strength);
            }
            else
            {
                var aff = Patient.CharacterHealth.GetAffliction(prefab.Identifier);
                if (aff != null) Patient.CharacterHealth.ReduceAfflictionOnAllLimbs(prefab.Identifier, aff.Strength);
            }
        }

        public static void SetAfflictionStrength(AfflictionPrefab prefab, float strength, Limb? targetLimb = null)
        {
            if (Patient?.CharacterHealth == null) return;

            var aff = targetLimb != null ?
                Patient.CharacterHealth.GetAffliction(prefab.Identifier, targetLimb) :
                Patient.CharacterHealth.GetAffliction(prefab.Identifier);

            if (aff != null)
            {
                float diff = strength - aff.Strength;
                if (diff > 0) InjectAffliction(prefab, diff, targetLimb);
                else
                {
                    if (targetLimb != null) Patient.CharacterHealth.ReduceAfflictionOnLimb(targetLimb, prefab.Identifier, -diff);
                    else Patient.CharacterHealth.ReduceAfflictionOnAllLimbs(prefab.Identifier, -diff);
                }
            }
            else if (strength > 0)
            {
                InjectAffliction(prefab, strength, targetLimb);
            }
        }

        public static void ResetPatient()
        {
            if (Patient == null) return;

            Patient.Revive();
            EnforceDummyInertia(Patient);

            Patient.CharacterHealth?.RemoveAllAfflictions();

            if (snapshot != null)
            {
                foreach (var (prefab, strength, limbType) in snapshot)
                {
                    var limb = Patient.AnimController.GetLimb(limbType);
                    InjectAffliction(prefab, strength, limb);
                }
            }

            Patient.CharacterHealth?.CalculateVitality();
            IsPlaying = false;
            HasStarted = true;
        }

        public static void CleanPatient()
        {
            if (Patient == null) return;
            Patient.Revive();
            EnforceDummyInertia(Patient);
            Patient.CharacterHealth?.RemoveAllAfflictions();
            Patient.CharacterHealth?.CalculateVitality();
            IsPlaying = false;
            HasStarted = true;
            snapshot = null;
        }

        private static void TakeSnapshot()
        {
            if (Patient?.CharacterHealth == null) return;
            snapshot = [];
            foreach (var aff in Patient.CharacterHealth.GetAllAfflictions())
            {
                var limb = Patient.CharacterHealth.GetAfflictionLimb(aff);
                snapshot.Add((aff.Prefab, aff.Strength, limb?.type ?? LimbType.Torso));
            }
        }

        public static void RescueNativeWindow()
        {
            if (Patient?.CharacterHealth == null) return;

            HighlightedLimbField?.SetValue(Patient.CharacterHealth, -1);
            SelectedLimbIndexField?.SetValue(Patient.CharacterHealth, -1);

            if (HealthWindowField?.GetValue(Patient.CharacterHealth) is GUIFrame nativeWindow)
            {
                nativeWindow.RectTransform.Parent = null;
            }
        }

        public static void Destroy()
        {
            RescueNativeWindow();

            if (CharacterHealth.OpenHealthWindow == Patient?.CharacterHealth)
                openHealthWindowField?.SetValue(null, null);

            if (Patient?.Inventory != null)
            {
                foreach (var item in Patient.Inventory.AllItems.ToList())
                {
                    item.Remove();
                }
            }

            if (Patient != null && !Patient.Removed)
            {
                Patient.Remove();
            }
            Patient = null;
        }
    }
}