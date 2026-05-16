using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EOTF.Core.DecalSystem
{
    [StaticConstructorOnStartup]
    public sealed class DialogEditDecals : Window
    {
        private readonly Pawn _pawn;
        private DecalProfileSet _profileSet;
        private readonly DecalProfileSet _original;
        private readonly List<DecalSymbolDef> _symbols;

        private int _selectedHelmetIndex, _selectedArmorIndex;

        private bool _committed;

        private DecalSlot _curTab = DecalSlot.Armor;
        private Vector2 _armorScrollPosition;
        private Vector2 _helmetScrollPosition;

        private float _viewRectHeight;
        private float _colorsHeight;

        private List<Color>? _allColors;

        private Rot4 _previewRot = Rot4.South;

        private readonly Dictionary<string, Texture2D> _thumbCache = new Dictionary<string, Texture2D>();

        private static readonly Vector2 ButSize = new Vector2(200f, 40f);
        private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);
        private const float PortraitZoom = 1.3f;
        private const float TabMargin = 18f;
        private const float IconSize = 60f;
        private const float LeftRectPercent = 0.3f;

        public override Vector2 InitialSize => new Vector2(950f, 750f);

        public DialogEditDecals(Pawn pawn)
        {
            _pawn = pawn;
            forcePause = true;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;

            _profileSet = DecalUtil.ReadProfileSetFrom(_pawn);
            _original = _profileSet;
            _symbols = DecalUtil.AllSymbols();
            _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath);
            _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath);
            SyncSelection();
            DecalUtil.SetLiveEditFull(_pawn, _profileSet);
        }

        public override void Close(bool doCloseSound = true)
        {
            DecalUtil.EndLiveEdit(_pawn, _committed, _original);
            base.Close(doCloseSound);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (_pawn.Destroyed) { Close(false); return; }

            // Title — same as vanilla
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect);
            titleRect.height = Text.LineHeight * 2f;
            Widgets.Label(titleRect, "EOTF_StyleDecalsTitle".Translate(_pawn.Name.ToStringShort));
            Text.Font = GameFont.Small;
            inRect.yMin = titleRect.yMax + 4f;

            // Left 30% — pawn preview
            Rect leftRect = inRect;
            leftRect.width *= LeftRectPercent;
            leftRect.yMax -= ButSize.y + 4f;
            DrawPawn(leftRect);

            // Right 70% — tabs
            Rect rightRect = inRect;
            rightRect.xMin = leftRect.xMax + 10f;
            rightRect.yMax -= ButSize.y + 4f;
            DrawTabs(rightRect);

            // Footer
            DrawBottomButtons(inRect);
        }

        // ═══════════════════════════════════════════════════════════════
        // DrawPawn — portrait only, controls moved into tabs
        // ═══════════════════════════════════════════════════════════════
        private void DrawPawn(Rect rect)
        {
            // Portrait fills the entire left column
            Widgets.BeginGroup(rect);
            Rect innerPortrait = new Rect(0f, 0f, rect.width, rect.height).ContractedBy(4f);
            RenderTexture portrait = PortraitsCache.Get(
                _pawn,
                new Vector2(innerPortrait.width, innerPortrait.height),
                _previewRot,
                PortraitOffset,
                PortraitZoom,
                supersample: true,
                compensateForUIScale: true,
                renderHeadgear: true,
                renderClothes: true);
            GUI.DrawTexture(innerPortrait, portrait);
            Widgets.EndGroup();
        }

        // ═══════════════════════════════════════════════════════════════
        // DrawTabs — Layout C: control bar inside tab above grid
        // ═══════════════════════════════════════════════════════════════
        private void DrawTabs(Rect rect)
        {
            var tabs = new List<TabRecord>
            {
                new TabRecord("EOTF_Decals_Armor".Translate(), () => _curTab = DecalSlot.Armor, _curTab == DecalSlot.Armor),
                new TabRecord("EOTF_Decals_Helmet".Translate(), () => _curTab = DecalSlot.Helmet, _curTab == DecalSlot.Helmet)
            };

            Widgets.DrawMenuSection(rect);
            TabDrawer.DrawTabs(rect, tabs);
            rect = rect.ContractedBy(TabMargin);

            bool isArmor = (_curTab == DecalSlot.Armor);

            // Control bar: Enable checkbox left, Random Symbol button right
            float controlBarH = 28f;
            Rect controlBar = new Rect(rect.x, rect.y, rect.width, controlBarH);

            bool active = isArmor ? _profileSet.Armor.Active : _profileSet.Helmet.Active;
            bool wasActive = active;
            Widgets.CheckboxLabeled(new Rect(controlBar.x, controlBar.y, controlBar.width * 0.5f, controlBarH),
                "EOTF_Decals_Enabled".Translate(), ref active);
            if (wasActive != active)
            {
                if (isArmor) _profileSet.Armor.Active = active;
                else _profileSet.Helmet.Active = active;
                PushLive();
            }

            if (Widgets.ButtonText(new Rect(controlBar.xMax - 140f, controlBar.y, 140f, controlBarH),
                "EOTF_Decals_RandomSymbol".Translate()) && _symbols.Count > 0)
            {
                if (isArmor) _selectedArmorIndex = Rand.Range(0, _symbols.Count);
                else _selectedHelmetIndex = Rand.Range(0, _symbols.Count);
                SyncSelection();
                PushLive();
            }

            // Advance past control bar
            rect.yMin = controlBar.yMax + 6f;

            // Shrink grid to leave room for colors — vanilla hair tab pattern
            rect.yMax -= _colorsHeight;

            // Draw the symbol grid — fills the remaining rect, scrollable
            Vector2 scrollPos = isArmor ? _armorScrollPosition : _helmetScrollPosition;
            DrawSymbolGrid(rect, ref scrollPos);
            if (isArmor) _armorScrollPosition = scrollPos;
            else _helmetScrollPosition = scrollPos;

            // Colors below grid — vanilla DrawHairColors position
            DrawColors(new Rect(rect.x, rect.yMax + 10f, rect.width, _colorsHeight));
        }

        // ═══════════════════════════════════════════════════════════════
        // DrawSymbolGrid — vanilla DrawStylingItemType pattern
        // ═══════════════════════════════════════════════════════════════
        private void DrawSymbolGrid(Rect rect, ref Vector2 scrollPosition)
        {
            bool isArmor = (_curTab == DecalSlot.Armor);
            int selectedIdx = isArmor ? _selectedArmorIndex : _selectedHelmetIndex;

            if (_symbols.Count == 0)
            {
                Widgets.NoneLabelCenteredVertically(rect);
                return;
            }

            Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, _viewRectHeight);
            int columns = Mathf.FloorToInt(viewRect.width / IconSize) - 1;
            if (columns < 1) columns = 1;
            float xPadding = (viewRect.width - columns * IconSize - (columns - 1) * 10f) / 2f;

            int itemIndex = 0;
            int col = 0;
            int row = 0;

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            for (int i = 0; i < _symbols.Count; i++)
            {
                if (col >= columns - 1)
                {
                    col = 0;
                    row++;
                }
                else if (itemIndex > 0)
                {
                    col++;
                }

                Rect iconRect = new Rect(
                    rect.x + xPadding + col * IconSize + col * 10f,
                    rect.y + row * IconSize + row * 10f,
                    IconSize, IconSize);

                Widgets.DrawHighlight(iconRect);

                if (Mouse.IsOver(iconRect))
                {
                    Widgets.DrawHighlight(iconRect);
                    TooltipHandler.TipRegion(iconRect, _symbols[i].LabelCap);
                }

                // Draw symbol thumbnail
                Texture2D? thumb = GetThumb(_symbols[i]);
                if (thumb != null)
                {
                    GUI.DrawTexture(iconRect.ContractedBy(4f), thumb, ScaleMode.ScaleToFit);
                }

                // Selected = DrawBox, same as vanilla
                if (i == selectedIdx)
                {
                    Widgets.DrawBox(iconRect, 2);
                }

                if (Widgets.ButtonInvisible(iconRect))
                {
                    if (isArmor) _selectedArmorIndex = i;
                    else _selectedHelmetIndex = i;
                    SyncSelection();
                    PushLive();
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }

                itemIndex++;
            }

            if (Event.current.type == EventType.Layout)
            {
                _viewRectHeight = (row + 1) * IconSize + row * 10f + 10f;
            }

            Widgets.EndScrollView();
        }

        // ═══════════════════════════════════════════════════════════════
        // DrawColors — vanilla DrawHairColors pattern with Favorite Colors incorporated
        // ═══════════════════════════════════════════════════════════════
        private void DrawColors(Rect rect)
        {
            bool isArmor = (_curTab == DecalSlot.Armor);
            float curY = rect.y;

            Color color = isArmor ? _profileSet.Armor.SymbolColor : _profileSet.Helmet.SymbolColor;
            Color original = color;

            Widgets.ColorSelector(
                new Rect(rect.x, curY, rect.width, _colorsHeight),
                ref color, AllColors(), out _colorsHeight);
            curY += _colorsHeight;

            // Spacer: Add a direct graphical layout gap between the selector matrix and the helper buttons
            float spacerGap = 12f;
            curY += spacerGap;

            // Row for Ideo, Random, and Favorite buttons balanced dynamically
            float totalGap = 12f;
            bool showIdeo = ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode;
            bool showFav = TryGetFavoriteColor(_pawn, out Color favColor);

            int activeButtons = 1; // Random is always active
            if (showIdeo) activeButtons++;
            if (showFav) activeButtons++;

            float btnW = (rect.width - (totalGap * (activeButtons - 1))) / activeButtons;
            float btnH = 24f;
            float btnX = rect.x;

            if (showIdeo)
            {
                if (Widgets.ButtonText(new Rect(btnX, curY, btnW, btnH), "EOTF_Decals_IdeoColor".Translate()))
                {
                    if (_pawn.Ideo != null) color = _pawn.Ideo.ApparelColor;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
                btnX += btnW + totalGap;
            }
            
            if (Widgets.ButtonText(new Rect(btnX, curY, btnW, btnH), "EOTF_Decals_RandomColor".Translate()))
            {
                var colors = AllColors();
                if (colors.Count > 0)
                {
                    color = colors[Rand.Range(0, colors.Count)];
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }
            btnX += btnW + totalGap;   
            
            if (showFav)
            {
                if (Widgets.ButtonText(new Rect(btnX, curY, btnW, btnH), "EOTF_Decals_FavColor".Translate()))
                {
                    color = favColor;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }            

            if (!original.IndistinguishableFrom(color))
            {
                if (isArmor) _profileSet.Armor.SymbolColor = color;
                else _profileSet.Helmet.SymbolColor = color;
                PushLive();
            }

            // Pad colorsHeight so next frame reserves enough space above (accounting for the new spacer)
            _colorsHeight += (Text.LineHeight * 2f) + spacerGap;
        }

        // ═══════════════════════════════════════════════════════════════
        // DrawBottomButtons — vanilla: Cancel left, Reset center, Accept right
        // ═══════════════════════════════════════════════════════════════
        private void DrawBottomButtons(Rect inRect)
        {
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "EOTF_Decals_Cancel".Translate()))
            {
                _committed = false;
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.xMin + inRect.width / 2f - ButSize.x / 2f, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "EOTF_Decals_Reset".Translate()))
            {
                _profileSet = _original;
                _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath);
                _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath);
                SyncSelection();
                PushLive();
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            if (Widgets.ButtonText(new Rect(inRect.xMax - ButSize.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "EOTF_Decals_Accept".Translate()))
            {
                _committed = true;
                Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════

        private Texture2D? GetThumb(DecalSymbolDef def)
        {
            if (def.Path.NullOrEmpty()) return null;
            if (_thumbCache.TryGetValue(def.Path, out Texture2D cached)) return cached;
            Texture2D? tex = ContentFinder<Texture2D>.Get(def.Path + "_south", false)
                          ?? ContentFinder<Texture2D>.Get(def.Path, false);
            if (tex != null) _thumbCache[def.Path] = tex;
            return tex;
        }

        private int FindSymbolIndex(string path)
        {
            if (path.NullOrEmpty() || _symbols.Count == 0) return 0;
            return Mathf.Max(0, _symbols.FindIndex(d => d.Path == path));
        }

        private void SyncSelection()
        {
            if (_symbols.Count == 0) return;
            _selectedHelmetIndex = Mathf.Clamp(_selectedHelmetIndex, 0, _symbols.Count - 1);
            _profileSet.Helmet.SymbolPath = _symbols[_selectedHelmetIndex].Path;
            _selectedArmorIndex = Mathf.Clamp(_selectedArmorIndex, 0, _symbols.Count - 1);
            _profileSet.Armor.SymbolPath = _symbols[_selectedArmorIndex].Path;
        }

        // Vanilla apparel color palette + OldDialog color types and HSV Sort
        private List<Color> AllColors()
        {
            if (_allColors != null) return _allColors;
            
            HashSet<Color> colorSet = new HashSet<Color>();

            if (ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode)
                colorSet.Add(_pawn.Ideo.ApparelColor);

            if (TryGetFavoriteColor(_pawn, out Color favColor))
                colorSet.Add(favColor);

            foreach (ColorDef def in DefDatabase<ColorDef>.AllDefs)
            {
                if (def.colorType == ColorType.Ideo || def.colorType == ColorType.Misc || def.colorType == ColorType.Structure)
                {
                    if (!colorSet.Any(c => c.IndistinguishableFrom(def.color)))
                        colorSet.Add(def.color);
                }
            }

            _allColors = new List<Color>(colorSet);
            
            // Sort by Hue & Saturation so the list looks visually coherent 
            _allColors.Sort((a, b) => 
            { 
                Color.RGBToHSV(a, out float hA, out float sA, out _); 
                Color.RGBToHSV(b, out float hB, out float sB, out _); 
                int c = hA.CompareTo(hB); 
                return (c != 0) ? c : sA.CompareTo(sB); 
            });

            return _allColors;
        }

        private static bool TryGetFavoriteColor(Pawn? pawn, out Color c) 
        { 
            c = Color.white; 
            if (!ModsConfig.IdeologyActive || pawn?.story == null || pawn.DevelopmentalStage.Baby()) return false; 
            ColorDef def = pawn.story.favoriteColor; 
            if (def == null) return false; 
            c = def.color; 
            return true; 
        }

        private void PushLive() => DecalUtil.SetLiveEditFull(_pawn, _profileSet);
    }
}