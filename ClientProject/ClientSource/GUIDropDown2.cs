// This file has been modified from game basecode.
// Original code extracted from (here:)[https://github.com/FakeFishGames/Barotrauma/blob/c8657caefa5c6928013a5152ac0ad301d99b405c/Barotrauma/BarotraumaClient/ClientSource/GUI/GUIDropDown.cs)
// All rigths from @FakeFishGames.

using Barotrauma;
using EventInput;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SOS
{
    public class GUIDropDown2 : GUIComponent, IKeyboardSubscriber
    {
        public delegate bool OnSelectedHandler(GUIComponent selected, object obj);

        public OnSelectedHandler OnSelected;
        public OnSelectedHandler AfterSelected;
        public OnDroppedHandler OnDropped;
        public delegate bool OnDroppedHandler(GUIDropDown2 dropDown, object obj);

        private readonly GUIButton button;
        private readonly GUIImage icon;
        private readonly GUIListBox listBox;

        private RectTransform currentHighestParent;
        private List<RectTransform> parentHierarchy = new List<RectTransform>();

        private readonly bool selectMultiple;

        private bool dropped;
        public bool Dropped
        {
            get => dropped;
            set
            {
                if (dropped == value) return;
                dropped = value;
                if (icon != null)
                {
                    icon.SpriteEffects = dropped
                        ? SpriteEffects.FlipVertically
                        : SpriteEffects.None;
                }
            }
        }

        public object SelectedItemData
        {
            get
            {
                if (listBox.SelectedComponent == null) return null;
                return listBox.SelectedComponent.UserData;
            }
        }

        public override bool Enabled
        {
            get { return listBox.Enabled; }
            set { listBox.Enabled = value; }
        }

        public bool ButtonEnabled
        {
            get { return button.Enabled; }
            set 
            { 
                button.Enabled = value;
                if (icon != null) { icon.Enabled = value; }
            }
        }

        public GUIComponent SelectedComponent => listBox.SelectedComponent;

        public override bool Selected
        {
            get => Dropped;
            set => Dropped = value;
        }

        public GUIListBox ListBox => listBox;

        public object SelectedData => listBox.SelectedComponent.UserData;

        public int SelectedIndex
        {
            get
            {
                if (listBox.SelectedComponent == null) return -1;
                return listBox.Content.GetChildIndex(listBox.SelectedComponent);
            }
        }

        public void ReceiveTextInput(char inputChar) => GUI.KeyboardDispatcher.Subscriber = null;
        public void ReceiveTextInput(string text) { }
        public void ReceiveCommandInput(char command) { }
        public void ReceiveEditingInput(string text, int start, int length) { }

        public void ReceiveSpecialInput(Keys key)
        {
            switch (key)
            {
                case Keys.Up:
                case Keys.Down:
                    listBox.ReceiveSpecialInput(key);
                    GUI.KeyboardDispatcher.Subscriber = this;
                    break;
                default:
                    GUI.KeyboardDispatcher.Subscriber = null;
                    break;
            }
        }

        private readonly List<object> selectedDataMultiple = new List<object>();
        public IEnumerable<object> SelectedDataMultiple => selectedDataMultiple;

        private readonly List<int> selectedIndexMultiple = new List<int>();
        public IEnumerable<int> SelectedIndexMultiple => selectedIndexMultiple;

        public bool MustSelectAtLeastOne;

        public override RichString ToolTip
        {
            get => base.ToolTip;
            set
            {
                base.ToolTip = value;
                button.ToolTip = value;
                listBox.ToolTip = value;
            }
        }

        public GUIImage DropDownIcon => icon;

        public GUIDropDown2(RectTransform rectT, int elementCount = 4, string style = "", bool selectMultiple = false, bool dropAbove = false, int? listBoxWidth = null, bool? expandToRight = null) : base(style, rectT)
        {
            HoverCursor = CursorState.Hand;
            CanBeFocused = true;

            this.selectMultiple = selectMultiple;

            button = new GUIButton(new RectTransform(Vector2.One, rectT), "", Alignment.Center, style: "GUIDropDown")
            {
                OnClicked = OnClicked
            };
            GUIStyle.Apply(button, "", this);

            bool isRight;
            if (expandToRight.HasValue)
            {
                isRight = expandToRight.Value;
            }
            else
            {
                isRight = rectT.Anchor != Anchor.TopRight && rectT.Anchor != Anchor.CenterRight && rectT.Anchor != Anchor.BottomRight;
            }

            Anchor listAnchor;
            Pivot listPivot;

            if (isRight)
            {
                listAnchor = dropAbove ? Anchor.TopLeft : Anchor.BottomLeft;
                listPivot = dropAbove ? Pivot.BottomLeft : Pivot.TopLeft;
            }
            else
            {
                listAnchor = dropAbove ? Anchor.TopRight : Anchor.BottomRight;
                listPivot = dropAbove ? Pivot.BottomRight : Pivot.TopRight;
            }

            int finalWidth = listBoxWidth ?? (rectT.Parent != null ? rectT.Parent.Rect.Width : rectT.Rect.Width);
            int finalHeight = rectT.Rect.Height * MathHelper.Clamp(elementCount, 2, 10);

            listBox = new GUIListBox(new RectTransform(new Point(finalWidth, finalHeight), rectT, listAnchor, listPivot)
            { IsFixedSize = true }, style: null)
            {
                Enabled = !selectMultiple,
                PlaySoundOnSelect = true,
                Padding = new Vector4(2, 2, 2, 2)
            };

            if (!selectMultiple)
            {
                listBox.AfterSelected = (component, obj) =>
                {
                    SelectItem(component, obj);
                    AfterSelected?.Invoke(component, obj);
                    return true;
                };
            }
            GUIStyle.Apply(listBox, "GUIListBox", this);
            GUIStyle.Apply(listBox.ContentBackground, "GUIListBox", this);

            if (button.Style.ChildStyles.ContainsKey("dropdownicon".ToIdentifier()))
            {
                icon = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), button.RectTransform, Anchor.Center, Pivot.Center, scaleBasis: ScaleBasis.BothHeight), null, scaleToFit: true);
                icon.ApplyStyle(button.Style.ChildStyles["dropdownicon".ToIdentifier()]);
            }

            currentHighestParent = FindHighestParent();
            currentHighestParent.GUIComponent.OnAddedToGUIUpdateList += AddListBoxToGUIUpdateList;
            rectT.ParentChanged += _ => RefreshListBoxParent();
        }

        private RectTransform FindHighestParent()
        {
            parentHierarchy.Clear();
            parentHierarchy = new List<RectTransform>() { RectTransform.Parent };
            RectTransform parent = parentHierarchy.Last();
            while (parent?.Parent != null)
            {
                parentHierarchy.Add(parent.Parent);
                parent = parent.Parent;
            }

            for (int i = parentHierarchy.Count - 1; i > 0; i--)
            {
                if (parentHierarchy[i] is GUICanvas ||
                    parentHierarchy[i].GUIComponent == null ||
                    parentHierarchy[i].GUIComponent.Style == null ||
                    parentHierarchy[i].GUIComponent == Screen.Selected?.Frame)
                {
                    parentHierarchy.RemoveAt(i);
                }
                else { break; }
            }
            return parentHierarchy.Count > 0 ? parentHierarchy.Last() : RectTransform;
        }

        public GUIComponent AddItem(LocalizedString text, object userData, LocalizedString toolTip, Color? color, Color? textColor)
        {
            toolTip ??= "";
            int itemHeight = Math.Max(button.Rect.Height, 24);

            if (selectMultiple)
            {
                var frame = new GUIFrame(new RectTransform(new Point(listBox.Content.Rect.Width, itemHeight), listBox.Content.RectTransform) { IsFixedSize = false }, style: "ListBoxElement", color: color)
                {
                    UserData = userData,
                    ToolTip = toolTip
                };
                var tickBox = new GUITickBox(new RectTransform(new Vector2(1.0f, 0.8f), frame.RectTransform, anchor: Anchor.CenterLeft) { MaxSize = new Point(int.MaxValue, (int)(itemHeight * 0.8f)) }, text)
                {
                    UserData = userData,
                    ToolTip = toolTip,
                    OnSelected = (GUITickBox tb) =>
                    {
                        if (MustSelectAtLeastOne && selectedDataMultiple.Count <= 1 && !tb.Selected)
                        {
                            tb.Selected = true;
                            return false;
                        }

                        if (OnSelected != null && !OnSelected.Invoke(tb.Parent, tb.Parent.UserData)) { return false; }

                        selectedDataMultiple.Clear();
                        selectedIndexMultiple.Clear();
                        int i = 0;
                        foreach (GUIComponent child in ListBox.Content.Children)
                        {
                            var tickBoxItem = child.GetChild<GUITickBox>();
                            if (tickBoxItem is { Selected: true })
                            {
                                selectedDataMultiple.Add(child.UserData);
                                selectedIndexMultiple.Add(i);
                            }
                            i++;
                        }
                        AfterSelected?.Invoke(tb.Parent, SelectedData);
                        return true;
                    }
                };
                return frame;
            }
            else
            {
                return new GUITextBlock(new RectTransform(new Point(listBox.Content.Rect.Width, itemHeight), listBox.Content.RectTransform) { IsFixedSize = false }, text, style: "ListBoxElement", color: color, textColor: textColor)
                {
                    UserData = userData,
                    ToolTip = toolTip
                };
            }
        }

        public override void ClearChildren() => listBox.ClearChildren();

        public IEnumerable<GUIComponent> GetChildren() => listBox.Content.Children;

        private bool SelectItem(GUIComponent component, object obj)
        {
            if (selectMultiple)
            {
                foreach (GUIComponent child in ListBox.Content.Children)
                {
                    var tickBox = child.GetChild<GUITickBox>();
                    if (Equals(obj, child.UserData) && tickBox != null) { tickBox.Selected = true; }
                }
            }
            OnSelected?.Invoke(component, obj);
            Dropped = false;
            return true;
        }

        public void SelectItem(object userData)
        {
            if (selectMultiple)
            {
                SelectItem(listBox.Content.FindChild(userData), userData);
            }
            else
            {
                listBox.Select(userData);
            }
            AfterSelected?.Invoke(SelectedComponent, SelectedData);
        }

        public void Select(int index)
        {
            if (selectMultiple)
            {
                var child = listBox.Content.GetChild(index);
                if (child != null) { SelectItem(null, child.UserData); }
            }
            else
            {
                listBox.Select(index);
            }
            AfterSelected?.Invoke(this, SelectedData);
        }

        private bool wasOpened;

        private bool OnClicked(GUIComponent component, object obj)
        {
            if (wasOpened) return false;
            wasOpened = true;
            Dropped = !Dropped;
            if (Dropped && Enabled)
            {
                OnDropped?.Invoke(this, UserData);
                listBox.UpdateScrollBarSize();
                listBox.UpdateDimensions();
                GUI.KeyboardDispatcher.Subscriber = this;
            }
            else if (GUI.KeyboardDispatcher.Subscriber == this)
            {
                GUI.KeyboardDispatcher.Subscriber = null;
            }
            return true;
        }

        public void RefreshListBoxParent()
        {
            if (currentHighestParent != null && currentHighestParent.GUIComponent != null)
            {
                currentHighestParent.GUIComponent.OnAddedToGUIUpdateList -= AddListBoxToGUIUpdateList;
            }
            if (RectTransform.Parent == null) { return; }
            currentHighestParent = FindHighestParent();
            if (currentHighestParent?.GUIComponent != null)
            {
                currentHighestParent.GUIComponent.OnAddedToGUIUpdateList += AddListBoxToGUIUpdateList;
            }
        }
        
        private void AddListBoxToGUIUpdateList(GUIComponent parent)
        {
            for (int i = 1; i < parentHierarchy.Count; i++)
            {
                if (parentHierarchy[i] != null && parentHierarchy[i - 1] != null && parentHierarchy[i].IsParentOf(parentHierarchy[i - 1], recursive: false))
                {
                    continue;
                }
                parent.OnAddedToGUIUpdateList -= AddListBoxToGUIUpdateList;
                return;
            }

            if (Dropped) { listBox.AddToGUIUpdateList(false, 1); }
        }

        public override void DrawManually(SpriteBatch spriteBatch, bool alsoChildren = false, bool recursive = true)
        {
            if (!Visible) return;
            AutoDraw = false;
            Draw(spriteBatch);
            if (alsoChildren) { button.DrawManually(spriteBatch, alsoChildren, recursive); }
        }

        public override void AddToGUIUpdateList(bool ignoreChildren = false, int order = 0)
        {
            base.AddToGUIUpdateList(true, order);
            if (!ignoreChildren) { button.AddToGUIUpdateList(false, order); }
        }

        public override void Update(float deltaTime)
        {
            if (!Visible) return;
            wasOpened = false;
            base.Update(deltaTime);
            if (Dropped && PlayerInput.PrimaryMouseButtonClicked())
            {
                Rectangle listBoxRect = listBox.Rect;
                if (!listBoxRect.Contains(PlayerInput.MousePosition) && !button.Rect.Contains(PlayerInput.MousePosition))
                {
                    Dropped = false;
                    if (GUI.KeyboardDispatcher.Subscriber == this) { GUI.KeyboardDispatcher.Subscriber = null; }
                }
            }
        }
    }
}