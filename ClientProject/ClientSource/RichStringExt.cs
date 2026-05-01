// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.
// NOTE: This file contains AI generated code, all content 
//   marked as AI generated are free to use without licence 
//   requirements.

#pragma warning disable IDE0130
#pragma warning disable IDE0079
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public static class RichStringExt
    {
        public static RichString Rich(this string text) => RichString.Rich(text);

        public static string SetHyperlink(this string text, Color? color = null)
            => text.SetColor(color ?? Color.LightSkyBlue);

        public static LocalizedString SetHyperlink(this LocalizedString text, Color? color = null)
            => text.SetColor(color ?? Color.LightSkyBlue);

        public static string SetColor(this string text, string colorName)
        => $"‖color:{colorName}‖{text}‖end‖";

        public static string SetColor(this string text, Color color)
            => $"‖color:{color.R},{color.G},{color.B}‖{text}‖end‖";

        public static string SetColorHex(this string text, string hexCode)
            => $"‖color:{hexCode}‖{text}‖end‖";

        public static string SetBold(this string text)
            => $"‖bold‖{text}‖end‖";

        public static string SetItalic(this string text)
            => $"‖italic‖{text}‖end‖";

        public static string SetUnderline(this string text)
            => $"‖underline‖{text}‖end‖";

        public static string SetStrikethrough(this string text)
            => $"‖strikethrough‖{text}‖end‖";

        public static LocalizedString SetColor(this LocalizedString text, string colorName)
        => $"‖color:{colorName}‖{text}‖end‖";

        public static LocalizedString SetColor(this LocalizedString text, Color color)
            => $"‖color:{color.R},{color.G},{color.B}‖{text}‖end‖";

        public static LocalizedString SetColorHex(this LocalizedString text, string hexCode)
            => $"‖color:{hexCode}‖{text}‖end‖";

        public static LocalizedString SetBold(this LocalizedString text)
            => $"‖bold‖{text}‖end‖";

        public static LocalizedString SetItalic(this LocalizedString text)
            => $"‖italic‖{text}‖end‖";

        public static LocalizedString SetUnderline(this LocalizedString text)
            => $"‖underline‖{text}‖end‖";

        public static LocalizedString SetStrikethrough(this LocalizedString text)
            => $"‖strikethrough‖{text}‖end‖";

    }

    public static class HyperlinkExtensions
    {
        public static RichString JoinToRichString<T>(
            this IEnumerable<T> items,
            string separator,
            Func<T, string> textSelector,
            Func<T, Color> colorSelector)
        {
            var sb = new System.Text.StringBuilder();
            var list = items.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                Color color = colorSelector(item);
                string text = textSelector(item).Replace("‖", "");

                // Formateo simple por color.
                sb.Append(text.SetColor(color));

                if (i < list.Count - 1) sb.Append(separator);
            }

            return RichString.Rich(sb.ToString());
        }

        public static void BindHyperlinks<T>(
            this GUITextBlock textBlock,
            IEnumerable<T> items,
            Action<T> onPrimaryClick,
            Action<T>? onSecondaryClick = null)
        {
            var list = items.ToList();

            void ApplyLinks()
            {
                if (textBlock.RichTextData == null) return;
                textBlock.ClickableAreas.Clear();

                int index = 0;
                foreach (var data in textBlock.RichTextData)
                {
                    if (data.StartIndex >= data.EndIndex || data.StartIndex < 0 || data.Color == null) continue;
                    if (index >= list.Count) break;

                    var target = list[index];
                    textBlock.ClickableAreas.Add(new GUITextBlock.ClickableArea()
                    {
                        Data = data,
                        OnClick = (tb, area) => { onPrimaryClick?.Invoke(target); },
                        OnSecondaryClick = onSecondaryClick != null ? ((tb, area) => { onSecondaryClick.Invoke(target); }) : null
                    });
                    index++;
                }
            }

            ApplyLinks();
            textBlock.RectTransform.SizeChanged += ApplyLinks;
        }
    }

    public static class FloatExt
    {
        public static string ToMeters(this float value) => (value / 10f).ToValue() + 'm';
        public static string ToValue(this float value) => value.ToString("0.###");
        public static string ToSignedValue(this float value) => (value > 0) ? '+' + value.ToValue() : value.ToValue();
    }

    public static class PrefabExt
    {

        public static (string Name, Color TextColor) SafeName(this Prefab? prefab, Color defaultColor)
        {
            return prefab switch
            {
                ItemPrefab item => item.Name.IsNullOrEmpty()
                                        ? ($"[{item.Identifier}]", Color.Red)
                                        : (item.Name.Value, defaultColor),
                AfflictionPrefab affliction => affliction.Name.IsNullOrEmpty()
                                        ? ($"[{affliction.Identifier}]", Color.Red)
                                        : (affliction.Name.Value, defaultColor),
                _ => (TextSOS.Get("sos.gen.unknown", "???").Value, defaultColor),
            };
        }
        public static string Name(this Prefab prefab)
        {
            return prefab switch
            {
                ItemPrefab item => item.Name.ToString(),
                AfflictionPrefab affliction => affliction.Name.ToString(),
                _ => prefab.Identifier.ToString()
            };
        }
        public static Sprite? Icon(this Prefab? prefab)
        {
            return prefab switch
            {
                ItemPrefab item => item.InventoryIcon ?? item.sprite,
                AfflictionPrefab affliction => affliction.Icon,
                _ => null
            };
        }
        public static Color IconColor(this Prefab? prefab)
        {
            return prefab switch
            {
                ItemPrefab item => item.InventoryIconColor,
                AfflictionPrefab affliction => affliction.IconColors?.FirstOrDefault(Color.White),
                _ => null
            } ?? Color.White;
        }
    }


    // AI Generated Code
    public static class GUIComponentExt
    {
        // =========================================================================
        // MARK: - MÉTODOS DE EXTENSIÓN INICIALES (Arrancan la cadena desde el GUIComponent)
        // =========================================================================

        public static GUIAnimSequence Wait(this GUIComponent component, float duration) => new GUIAnimSequence(component).Wait(duration);

        public static GUIAnimSequence ExLerpColor(this GUIComponent component, Color targetColor, float duration, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExLerpColor(targetColor, duration, alsoChildren);

        public static GUIAnimSequence ExLerpTextColor(this GUIComponent component, Color? startColor, Color finalColor, float duration, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExLerpTextColor(startColor, finalColor, duration, alsoChildren);

        public static GUIAnimSequence ExShake(this GUIComponent component, float duration, float intensity = 5.0f, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExShake(duration, intensity, alsoChildren);

        public static GUIAnimSequence ExFadeOut(this GUIComponent component, float duration, float targetFactor = 0.0f, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExFadeOut(duration, targetFactor, alsoChildren);

        public static GUIAnimSequence ExFadeIn(this GUIComponent component, float duration, float? targetFactor = null, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExFadeIn(duration, targetFactor, alsoChildren);

        public static GUIAnimSequence ExPulsate(this GUIComponent component, Vector2 startScale, Vector2 endScale, float duration, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExPulsate(startScale, endScale, duration, alsoChildren);

        public static GUIAnimSequence Execute(this GUIComponent component, Action action)
            => new GUIAnimSequence(component).Execute(action);

        public static GUIAnimSequence ExGlitch(this GUIComponent component, float duration, float intensity = 1.0f, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExGlitch(duration, intensity, alsoChildren);

        public static GUIAnimSequence ExFlash(this GUIComponent component, Color? color = null, float duration = 1.5f, bool useRectangleFlash = false, bool useCircularFlash = false, Vector2? flashRectInflate = null, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExFlash(color, duration, useRectangleFlash, useCircularFlash, flashRectInflate, alsoChildren);

        public static GUIAnimSequence ExBlink(this GUIComponent component, float duration, float minAlpha = 0.0f, float maxAlpha = 1.0f, float interval = 0.5f, bool alsoChildren = false)
            => new GUIAnimSequence(component).ExBlink(duration, minAlpha, maxAlpha, interval, alsoChildren);

        public static GUIAnimSequence LogMsg(this GUIComponent component, Color color)
            => new GUIAnimSequence(component).LogMsg(color);
    }

    /// <summary>
    /// Gestiona una cadena de animaciones secuenciales y paralelas para un GUIComponent.
    /// </summary>
    public class GUIAnimSequence
    {
        public GUIComponent Component { get; private set; }
        private static readonly Dictionary<GUIComponent, Color> designColors = [];
        private static readonly Dictionary<GUIComponent, Color> designTextColors = [];
        private readonly float originalAlpha;
        private Color originalBaseColor;

        // La secuencia se divide en "Pasos". Cada paso es una lista de acciones paralelas.
        private readonly List<List<Func<IEnumerable<CoroutineStatus>>>> steps = [];
        private List<Func<IEnumerable<CoroutineStatus>>> currentStep = [];

        public GUIAnimSequence(GUIComponent component)
        {
            Component = component;
            // Capturamos los colores de diseño (RGB + Alpha original)
            if (!designColors.ContainsKey(component) || component.Color.A > 1)
            {
                designColors[component] = component.Color;
            }
            if (component is GUITextBlock tb)
            {
                if (!designTextColors.ContainsKey(tb) || tb.TextColor.A > 1)
                {
                    designTextColors[tb] = tb.TextColor;
                }
            }

            originalBaseColor = designColors.TryGetValue(component, out var c) ? c : component.Color;
            originalAlpha = originalBaseColor.A / 255f;
            if (originalAlpha < 0.001f) originalAlpha = 1.0f;

            // Arranca la evaluación en segundo plano automáticamente
            CoroutineManager.StartCoroutine(ExecuteSequence());
        }

        // =========================================================================
        // MARK: - CONTROLADORES DE FLUJO
        // =========================================================================

        /// <summary>
        /// Agrega una acción al bloque paralelo actual.
        /// </summary>
        private void AddAction(Func<IEnumerable<CoroutineStatus>> action)
        {
            currentStep.Add(action);
            if (!steps.Contains(currentStep))
            {
                steps.Add(currentStep);
            }
        }

        /// <summary>
        /// Sella las animaciones actuales. Todo lo que se encadene después esperará a que estas terminen.
        /// </summary>
        public GUIAnimSequence WaitFinish()
        {
            if (currentStep.Count > 0)
            {
                // Prepara un nuevo bloque paralelo vacío
                currentStep = [];
            }
            return this;
        }

        /// <summary>
        /// Pausa la ejecución de la secuencia por un tiempo determinado.
        /// </summary>
        public GUIAnimSequence Wait(float duration)
        {
            WaitFinish(); // Cerramos cualquier tarea paralela actual
            AddAction(() => GUIAnimSequence.DoWait(duration)); // Agregamos la espera como tarea
            WaitFinish(); // Cerramos la espera para que lo siguiente inicie DESPUÉS de ella
            return this;
        }



        // =========================================================================
        // MARK: - ANIMACIONES ENCADENABLES
        // =========================================================================

        public GUIAnimSequence ExLerpColor(Color targetColor, float duration, bool alsoChildren = false)
        {
            AddAction(() =>
            {
                if (alsoChildren) foreach (var child in Component.GetAllChildren()) child.ExLerpColor(targetColor, duration, false);
                return DoLerpColor(targetColor, duration);
            });
            return this;
        }

        public GUIAnimSequence ExLerpTextColor(Color? startColor, Color finalColor, float duration, bool alsoChildren = false)
        {
            AddAction(() =>
            {
                if (alsoChildren) foreach (var child in Component.GetAllChildren()) child.ExLerpTextColor(startColor, finalColor, duration, false);
                return DoLerpTextColor(startColor, finalColor, duration);
            });
            return this;
        }

        public GUIAnimSequence ExShake(float duration, float intensity = 5.0f, bool alsoChildren = false)
        {
            AddAction(() =>
            {
                if (alsoChildren) foreach (var child in Component.GetAllChildren()) child.ExShake(duration, intensity, false);
                return DoShake(duration, intensity);
            });
            return this;
        }

        public GUIAnimSequence ExFadeIn(float duration, float? targetFactor = null, bool alsoChildren = false)
        {
            // Aplicación inmediata del estado inicial (Alpha factor 0) para evitar parpadeo
            if (steps.Count == 0 || (steps.Count == 1 && currentStep.Count == 0))
            {
                GUIAnimSequence.ApplyAlphaInternal(Component, 0f);
                if (alsoChildren)
                {
                    foreach (var child in Component.GetAllChildren())
                    {
                        // IMPORTANTE: Capturamos el diseño del hijo ANTES de ponerlo en 0
                        if (!designColors.ContainsKey(child) || child.Color.A > 0) designColors[child] = child.Color;
                        if (child is GUITextBlock tb && (!designTextColors.ContainsKey(tb) || tb.TextColor.A > 0)) designTextColors[tb] = tb.TextColor;

                        GUIAnimSequence.ApplyAlphaInternal(child, 0f);
                    }
                }
            }

            AddAction(() =>
            {
                if (alsoChildren)
                {
                    foreach (var child in Component.GetAllChildren()) child.ExFadeIn(duration, targetFactor, false);
                }
                float finalFactor = targetFactor ?? 1.0f;
                return DoFadeInRecursive(duration, finalFactor);
            });
            return this;
        }

        public GUIAnimSequence ExFadeOut(float duration, float targetFactor = 0.0f, bool alsoChildren = false)
        {
            AddAction(() =>
            {
                if (alsoChildren)
                {
                    foreach (var child in Component.GetAllChildren()) child.ExFadeOut(duration, targetFactor, false);
                }
                return DoFadeOutRecursive(duration, targetFactor);
            });
            return this;
        }

        private IEnumerable<CoroutineStatus> DoFadeInRecursive(float duration, float targetFactor)
        {
            float t = 0f;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                GUIAnimSequence.ApplyAlphaInternal(Component, MathHelper.Lerp(0f, targetFactor, Math.Min(1.0f, t / duration)));
                yield return CoroutineStatus.Running;
            }
            GUIAnimSequence.ApplyAlphaInternal(Component, targetFactor);
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoFadeOutRecursive(float duration, float targetFactor)
        {
            float startFactor = Component.Color.A / (float)Math.Max((byte)1, originalBaseColor.A);
            float t = 0f;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                GUIAnimSequence.ApplyAlphaInternal(Component, MathHelper.Lerp(startFactor, targetFactor, Math.Min(1.0f, t / duration)));
                yield return CoroutineStatus.Running;
            }
            GUIAnimSequence.ApplyAlphaInternal(Component, targetFactor);
            yield return CoroutineStatus.Success;
        }

        private static void ApplyAlphaInternal(GUIComponent comp, float factor)
        {
            if (designColors.TryGetValue(comp, out Color dc))
            {
                comp.Color = new Color(dc.R, dc.G, dc.B, (byte)(dc.A * factor));
            }
            else
            {
                Color c = comp.Color;
                comp.Color = new Color(c.R, c.G, c.B, (byte)(c.A * factor));
            }

            if (comp is GUITextBlock tb)
            {
                if (designTextColors.TryGetValue(tb, out Color dtc))
                {
                    tb.TextColor = new Color(dtc.R, dtc.G, dtc.B, (byte)(dtc.A * factor));
                }
            }
        }

        public GUIAnimSequence ExPulsate(Vector2 startScale, Vector2 endScale, float duration, bool alsoChildren = false)
        {
            AddAction(() =>
            {
                if (alsoChildren) foreach (var child in Component.GetAllChildren()) child.ExPulsate(startScale, endScale, duration, false);
                return DoPulsate(startScale, endScale, duration);
            });
            return this;
        }

        /// <summary>
        /// Ejecuta una acción de C# inmediatamente cuando la secuencia llega a este punto.
        /// </summary>
        public GUIAnimSequence Execute(Action action)
        {
            AddAction(() => GUIAnimSequence.DoExecute(action));
            return this;
        }

        public GUIAnimSequence ExGlitch(float duration, float intensity = 1.0f, bool alsoChildren = false)
        {
            AddAction(() =>
            {
                if (alsoChildren) foreach (var child in Component.GetAllChildren()) child.ExGlitch(duration, intensity, false);
                return DoGlitch(duration, intensity);
            });
            return this;
        }

        public GUIAnimSequence ExFlash(Color? color = null, float duration = 1.5f, bool useRectangleFlash = false, bool useCircularFlash = false, Vector2? flashRectInflate = null, bool alsoChildren = false)
        {
            AddAction(() =>
            {
                if (alsoChildren) foreach (var child in Component.GetAllChildren()) child.ExFlash(color, duration, useRectangleFlash, useCircularFlash, flashRectInflate, true);
                return DoFlash(color, duration, useRectangleFlash, useCircularFlash, flashRectInflate);
            });
            return this;
        }

        public GUIAnimSequence ExBlink(float duration, float minAlpha = 0.0f, float maxAlpha = 1.0f, float interval = 0.5f, bool alsoChildren = false)
        {
            AddAction(() =>
            {
                if (alsoChildren) foreach (var child in Component.GetAllChildren()) child.ExBlink(duration, minAlpha, maxAlpha, interval, false);
                return DoBlink(duration, minAlpha, maxAlpha, interval);
            });
            return this;
        }

        public GUIAnimSequence LogMsg(Color color)
        {
            AddAction(() => DoLogMsg(color));
            return this;
        }

        // =========================================================================
        // MARK: - CORRUTINAS INTERNAS (LÓGICA MATEMÁTICA)
        // =========================================================================

        private static IEnumerable<CoroutineStatus> DoWait(float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                yield return CoroutineStatus.Running;
            }
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoLerpColor(Color targetColor, float duration)
        {
            Color startColor = Component.Color;
            float t = 0f;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                float progress = Math.Min(1.0f, t / duration);
                GUIAnimSequence.ApplyColorRGB(Component, Color.Lerp(startColor, targetColor, progress));
                yield return CoroutineStatus.Running;
            }
            GUIAnimSequence.ApplyColorRGB(Component, targetColor);
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoLerpTextColor(Color? startColor, Color finalColor, float duration)
        {
            if (Component is not GUITextBlock textBlock) { yield return CoroutineStatus.Success; yield break; }
            Color initial = startColor ?? textBlock.TextColor;
            float t = 0f;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                textBlock.TextColor = Color.Lerp(initial, finalColor, Math.Min(1.0f, t / duration));
                yield return CoroutineStatus.Running;
            }
            textBlock.TextColor = finalColor;
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoShake(float duration, float intensity)
        {
            float t = 0f;
            Random rand = new();
            Point originalOffset = Component.RectTransform.ScreenSpaceOffset;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                float currentIntensity = MathHelper.Lerp(intensity, 0, t / duration);
                int offsetX = rand.Next(-(int)currentIntensity, (int)currentIntensity);
                int offsetY = rand.Next(-(int)currentIntensity, (int)currentIntensity);
                Component.RectTransform.ScreenSpaceOffset = new Point(originalOffset.X + offsetX, originalOffset.Y + offsetY);
                yield return CoroutineStatus.Running;
            }
            Component.RectTransform.ScreenSpaceOffset = originalOffset;
            Component.ForceLayoutRecalculation();
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoFadeIn(float duration, float targetAlpha)
        {
            float startAlpha = Component.Color.A / 255f;
            float t = 0f;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                GUIAnimSequence.ApplyAlpha(Component, MathHelper.Lerp(startAlpha, targetAlpha, Math.Min(1.0f, t / duration)));
                yield return CoroutineStatus.Running;
            }
            GUIAnimSequence.ApplyAlpha(Component, targetAlpha);
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoFadeOut(float duration, float targetAlpha)
        {
            float startAlpha = Component.Color.A / 255f;
            float t = 0f;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                float currentAlpha = MathHelper.Lerp(startAlpha, targetAlpha, Math.Min(1.0f, t / duration));
                GUIAnimSequence.ApplyAlpha(Component, currentAlpha);
                yield return CoroutineStatus.Running;
            }
            GUIAnimSequence.ApplyAlpha(Component, targetAlpha);
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoPulsate(Vector2 startScale, Vector2 endScale, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                Component.RectTransform.Scale = Vector2.Lerp(startScale, endScale, t / duration);
                yield return CoroutineStatus.Running;
            }
            Component.RectTransform.Scale = endScale;
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoGlitch(float duration, float intensity)
        {
            float t = 0f;
            Random rand = new();
            Color origColor = Component.Color;
            Vector2 origUV = Component.UVOffset;

            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                float currentIntensity = Math.Max(0, intensity * (1.0f - (t / duration)));
                if (rand.NextDouble() < 0.1 * currentIntensity)
                    Component.Color = rand.NextDouble() > 0.5 ? Color.Cyan : Color.Magenta;
                else
                    Component.Color = origColor;

                Component.UVOffset = new Vector2(
                    (float)(rand.NextDouble() * 2.0 - 1.0) * 100f * currentIntensity,
                    (float)(rand.NextDouble() * 2.0 - 1.0) * 10f * currentIntensity
                );
                yield return CoroutineStatus.Running;
            }
            Component.Color = origColor;
            Component.UVOffset = origUV;
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoFlash(Color? color, float duration, bool useRectangleFlash, bool useCircularFlash, Vector2? flashRectInflate)
        {
            Component.Flash(color, duration, useRectangleFlash, useCircularFlash, flashRectInflate);
            float t = 0f;
            while (t < duration) { t += CoroutineManager.DeltaTime; yield return CoroutineStatus.Running; }
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoBlink(float duration, float minFactor, float maxFactor, float interval)
        {
            float t = 0f;
            while (t < duration)
            {
                t += CoroutineManager.DeltaTime;
                float phase = (t / interval) * MathHelper.TwoPi;
                float alphaScale = (float)(Math.Sin(phase - MathHelper.PiOver2) + 1.0) / 2.0f;
                float currentFactor = MathHelper.Lerp(minFactor, maxFactor, alphaScale);
                GUIAnimSequence.ApplyAlphaInternal(Component, currentFactor);
                yield return CoroutineStatus.Running;
            }
            GUIAnimSequence.ApplyAlphaInternal(Component, 1.0f);
            yield return CoroutineStatus.Success;
        }

        private IEnumerable<CoroutineStatus> DoLogMsg(Color? color)
        {
            RLogger.LogDebug($"Anim Sequence Progress on {Component.GetType().Name} ({(Component as GUITextBlock)?.Text ?? "NoText"})", color);
            yield return CoroutineStatus.Success;
        }

        private static IEnumerable<CoroutineStatus> DoExecute(Action action)
        {
            try { action?.Invoke(); }
            catch (Exception e) { DebugConsole.ThrowError("Error en ejecución de secuencia GUI", e); }
            yield return CoroutineStatus.Success;
        }

        private static void ApplyAlpha(GUIComponent comp, float alpha)
        {
            GUIAnimSequence.ApplyAlphaInternal(comp, alpha);
        }

        private static void ApplyColorRGB(GUIComponent comp, Color rgb)
        {
            // Mantiene el Alpha que el componente ya tenía
            comp.Color = new Color(rgb.R, rgb.G, rgb.B, comp.Color.A);
        }

        // =========================================================================
        // MARK: - MOTOR DE EJECUCIÓN MAESTRO
        // =========================================================================

        private IEnumerable<CoroutineStatus> ExecuteSequence()
        {
            // Pausamos un frame al inicio. Esto permite que el hilo principal lea todo el comando
            // object.Wait().Lerp().Shake() en una sola línea antes de empezar a ejecutarlos.
            yield return CoroutineStatus.Running;

            int stepIndex = 0;
            while (stepIndex < steps.Count)
            {
                var step = steps[stepIndex];

                // Iniciamos todas las corrutinas de este paso (bloque paralelo)
                var activeCoroutines = new List<IEnumerator<CoroutineStatus>>();
                foreach (var action in step)
                {
                    activeCoroutines.Add(action().GetEnumerator());
                }

                // Tickeamos manualmente las corrutinas paralelas hasta que todas acaben
                while (activeCoroutines.Count > 0)
                {
                    for (int i = activeCoroutines.Count - 1; i >= 0; i--)
                    {
                        // Si MoveNext es false o devuelve Success, la corrutina terminó
                        if (!activeCoroutines[i].MoveNext() || activeCoroutines[i].Current == CoroutineStatus.Success)
                        {
                            activeCoroutines.RemoveAt(i);
                        }
                    }

                    if (activeCoroutines.Count > 0)
                    {
                        yield return CoroutineStatus.Running;
                    }
                }

                stepIndex++;
            }

            yield return CoroutineStatus.Success;
        }
    }
}