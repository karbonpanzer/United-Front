using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EOTF.Core.DecalSystem
{
    // Layout modeled after vanilla Dialog_StylingStation:
    // Left ~30% is pawn portrait, right ~70% is tabbed content, footer spans full width
    public sealed class DialogEditDecals : Window
    {
        private readonly Pawn _pawn;
        private DecalProfileSet _profileSet;
        private readonly DecalProfileSet _original;
        private readonly List<DecalSymbolDef> _symbols;

        private int _selectedHelmetIndex, _selectedArmorIndex;

        private bool _committed;
        private List<Color>? _allColors;

        // Per-slot scroll positions for symbol grids
        private Vector2 _armorGridScroll, _helmetGridScroll;
        // Per-tab scroll for the entire tab content
        private Vector2 _armorTabScroll, _helmetTabScroll;

        // Texture cache for symbol thumbnails
        private Dictionary<string, Texture2D>? _thumbCache;

        // Which tab we're looking at right now
        private DecalSlot _activeTab = DecalSlot.Armor;
        private List<TabRecord>? _tabs;

        // Layout — matching vanilla styling station proportions
        private const float LeftRectPercent = 0.35f;
        private const float TileSize = 48f;
        private const float TilePad = 4f;

        // Pawn preview
        private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);
        private const float PortraitZoom = 1.3f;
        private Rot4 _previewRot = Rot4.South;
        private bool _showClothes = true;

        // Button sizes matching vanilla
        private static readonly Vector2 ButSize = new Vector2(200f, 40f);

        public override Vector2 InitialSize => new Vector2(750f, 760f);

        public DialogEditDecals(Pawn pawn)
        {
            _pawn = pawn;
            forcePause = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;
            preventCameraMotion = false;
            doCloseX = true;

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

            // Footer buttons at the very bottom
            float footerY = inRect.yMax - ButSize.y;
            Rect footerRect = new Rect(inRect.x, footerY, inRect.width, ButSize.y);

            // Main content area above footer
            Rect contentRect = new Rect(inRect.x, inRect.y, inRect.width, footerY - inRect.y - 10f);

            // Split into left (portrait) and right (tabs) — same as vanilla styling station
            Rect leftRect = new Rect(contentRect.x, contentRect.y, 
                contentRect.width * LeftRectPercent, contentRect.height);
            Rect rightRect = new Rect(leftRect.xMax + 10f, contentRect.y, 
                contentRect.width - leftRect.width - 10f, contentRect.height);

            DrawPawnPreview(leftRect);
            DrawRightPanel(rightRect);
            DrawFooterButtons(footerRect);
        }

        // ═══════════════════════════════════════════════════════════════
        // LEFT — pawn portrait with rotation and clothes toggle
        // Vanilla styling station style: portrait fills most of the column
        // ═══════════════════════════════════════════════════════════════
        private void DrawPawnPreview(Rect rect)
        {
            // Portrait — as tall as width to keep it square
            float portraitSize = rect.width;
            Rect portraitOuter = new Rect(rect.x, rect.y, rect.width, portraitSize);
            Widgets.DrawMenuSection(portraitOuter);
            Rect portraitInner = portraitOuter.ContractedBy(6f);

            RenderTexture portrait = PortraitsCache.Get(
                _pawn,
                new Vector2(portraitInner.width, portraitInner.height),
                _previewRot,
                PortraitOffset,
                PortraitZoom,
                renderClothes: _showClothes,
                renderHeadgear: _showClothes);
            GUI.DrawTexture(portraitInner, portrait);

            float curY = portraitOuter.yMax + 8f;

            // Rotation: < [Reset] >
            float rotBtnW = 36f;
            float rotGap = 4f;
            float rotCenterW = rect.width - rotBtnW * 2f - rotGap * 2f;

            if (Widgets.ButtonText(new Rect(rect.x, curY, rotBtnW, 28f), "<"))
            {
                _previewRot.Rotate(RotationDirection.Counterclockwise);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            if (Widgets.ButtonText(new Rect(rect.x + rotBtnW + rotGap, curY, rotCenterW, 28f),
                "EOTF_Decals_Rotate".Translate()))
            {
                _previewRot = Rot4.South;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            if (Widgets.ButtonText(new Rect(rect.xMax - rotBtnW, curY, rotBtnW, 28f), ">"))
            {
                _previewRot.Rotate(RotationDirection.Clockwise);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            curY += 34f;

            // Show clothes
            Widgets.CheckboxLabeled(new Rect(rect.x, curY, rect.width, 24f),
                "EOTF_Decals_ShowClothes".Translate(), ref _showClothes);
        }

        // ═══════════════════════════════════════════════════════════════
        // RIGHT — tabbed panel, each tab is self-contained
        // ═══════════════════════════════════════════════════════════════
        private void DrawRightPanel(Rect rect)
        {
            float tabH = 32f;
            Rect tabContentRect = new Rect(rect.x, rect.y + tabH, rect.width, rect.height - tabH);
            Widgets.DrawMenuSection(tabContentRect);

            _tabs ??= new List<TabRecord>
            {
                new TabRecord("EOTF_Decals_Armor".Translate(), () => _activeTab = DecalSlot.Armor, _activeTab == DecalSlot.Armor),
                new TabRecord("EOTF_Decals_Helmet".Translate(), () => _activeTab = DecalSlot.Helmet, _activeTab == DecalSlot.Helmet)
            };
            _tabs[0].selected = (_activeTab == DecalSlot.Armor);
            _tabs[1].selected = (_activeTab == DecalSlot.Helmet);
            TabDrawer.DrawTabs(tabContentRect, _tabs);

            Rect innerRect = tabContentRect.ContractedBy(10f);
            DrawTabContent(innerRect, _activeTab);
        }

        // Each tab contains everything for that slot: enabled, symbol grid, color palette, color buttons
        private void DrawTabContent(Rect rect, DecalSlot slot)
        {
            bool isArmor = (slot == DecalSlot.Armor);
            Vector2 tabScroll = isArmor ? _armorTabScroll : _helmetTabScroll;

            // Calculate how tall the content actually is so we know if we need to scroll
            // Enabled row (28) + gap (6) + grid section + gap (8) + symbol label (22) + gap (12) 
            // + color label (22) + gap (4) + palette (~80) + gap (8) + color buttons (28)
            float gridH = CalcGridHeight(rect.width);
            float totalH = 28f + 6f + gridH + 8f + 22f + 12f + 22f + 4f + 80f + 8f + 28f;

            Rect viewRect = new Rect(0f, 0f, rect.width - (totalH > rect.height ? 16f : 0f), Mathf.Max(totalH, rect.height));
            Widgets.BeginScrollView(rect, ref tabScroll, viewRect);

            if (isArmor) _armorTabScroll = tabScroll;
            else _helmetTabScroll = tabScroll;

            float curY = 0f;
            float contentW = viewRect.width;

            // ── Enabled + Random Symbol ──
            bool active = isArmor ? _profileSet.Armor.Active : _profileSet.Helmet.Active;
            bool wasActive = active;
            Widgets.CheckboxLabeled(new Rect(0f, curY, 160f, 28f), "Enabled".Translate(), ref active);

            if (wasActive != active)
            {
                if (isArmor) _profileSet.Armor.Active = active;
                else _profileSet.Helmet.Active = active;
                PushLive();
            }

            if (Widgets.ButtonText(new Rect(contentW - 130f, curY, 130f, 28f),
                "EOTF_Decals_RandomSymbol".Translate()) && _symbols.Count > 0)
            {
                if (isArmor) _selectedArmorIndex = Rand.Range(0, _symbols.Count);
                else _selectedHelmetIndex = Rand.Range(0, _symbols.Count);
                SyncSelection();
                PushLive();
            }
            curY += 34f;

            // ── Symbol grid ──
            Rect gridOuter = new Rect(0f, curY, contentW, gridH);
            Widgets.DrawMenuSection(gridOuter);
            Rect gridInner = gridOuter.ContractedBy(4f);
            DrawSymbolGrid(gridInner, slot);
            curY = gridOuter.yMax + 8f;

            // ── Selected symbol name ──
            int selectedIdx = isArmor ? _selectedArmorIndex : _selectedHelmetIndex;
            string symName = (_symbols.Count > 0 && selectedIdx >= 0 && selectedIdx < _symbols.Count)
                ? _symbols[selectedIdx].LabelCap.ToString() : "-";
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(0f, curY, contentW, 22f), symName);
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 34f;

            // ── Color section ──
            Widgets.Label(new Rect(0f, curY, contentW, 22f), "EOTF_Decals_Color".Translate());
            curY += 24f;

            Color color = isArmor ? _profileSet.Armor.SymbolColor : _profileSet.Helmet.SymbolColor;
            Color original = color;

            Widgets.ColorSelector(new Rect(0f, curY, contentW, 1000f),
                ref color, AllColors(), out float usedHeight, null, 22, 2);
            curY += usedHeight + 8f;

            // Color shortcut buttons
            float btnH = 28f;
            float gap = 6f;
            float btnW = (contentW - gap * 2f) / 3f;

            if (Widgets.ButtonText(new Rect(0f, curY, btnW, btnH), "EOTF_Decals_IdeoColor".Translate()))
            {
                if (TryGetIdeoColor(_pawn, out Color c))
                {
                    color = c;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }
            if (Widgets.ButtonText(new Rect(btnW + gap, curY, btnW, btnH), "EOTF_Decals_FavColor".Translate()))
            {
                if (TryGetFavoriteColor(_pawn, out Color c))
                {
                    color = c;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }
            if (Widgets.ButtonText(new Rect((btnW + gap) * 2f, curY, btnW, btnH), "EOTF_Decals_RandomColor".Translate()))
            {
                var p = AllColors();
                if (p.Count > 0)
                {
                    color = p[Rand.Range(0, p.Count)];
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }

            if (!original.IndistinguishableFrom(color))
            {
                if (isArmor) _profileSet.Armor.SymbolColor = color;
                else _profileSet.Helmet.SymbolColor = color;
                PushLive();
            }

            Widgets.EndScrollView();
        }

        // Calculate grid height based on available width and symbol count
        private float CalcGridHeight(float availableWidth)
        {
            float innerW = availableWidth - 8f; // ContractedBy(4f) on each side
            int columns = Mathf.Max(1, Mathf.FloorToInt((innerW + TilePad) / (TileSize + TilePad)));
            int rows = (_symbols.Count > 0) ? Mathf.CeilToInt((float)_symbols.Count / columns) : 1;
            // Cap grid height so it doesn't dominate the tab
            float naturalH = rows * (TileSize + TilePad) - TilePad + 8f;
            return Mathf.Min(naturalH, 280f);
        }

        // Scrollable symbol grid — renders inside whatever rect it's given
        private void DrawSymbolGrid(Rect innerRect, DecalSlot slot)
        {
            bool isArmor = (slot == DecalSlot.Armor);
            int selectedIdx = isArmor ? _selectedArmorIndex : _selectedHelmetIndex;
            Vector2 scrollPos = isArmor ? _armorGridScroll : _helmetGridScroll;

            float fullW = innerRect.width;
            int colsFull = Mathf.Max(1, Mathf.FloorToInt((fullW + TilePad) / (TileSize + TilePad)));
            int rowsFull = (_symbols.Count > 0) ? Mathf.CeilToInt((float)_symbols.Count / colsFull) : 1;
            float gridHFull = rowsFull * (TileSize + TilePad) - TilePad;
            bool needsScroll = gridHFull > innerRect.height;

            float usableW = needsScroll ? innerRect.width - 16f : innerRect.width;
            int columns = Mathf.Max(1, Mathf.FloorToInt((usableW + TilePad) / (TileSize + TilePad)));
            int rowCount = (_symbols.Count > 0) ? Mathf.CeilToInt((float)_symbols.Count / columns) : 1;
            float gridH = rowCount * (TileSize + TilePad) - TilePad;

            float strideX = (columns > 1) ? (usableW - TileSize) / (columns - 1) : 0f;
            float strideY = TileSize + TilePad;

            Rect viewRect = new Rect(0f, 0f, usableW, gridH);
            Widgets.BeginScrollView(innerRect, ref scrollPos, viewRect);

            if (isArmor) _armorGridScroll = scrollPos;
            else _helmetGridScroll = scrollPos;

            for (int i = 0; i < _symbols.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Rect tileRect = new Rect(col * strideX, row * strideY, TileSize, TileSize);

                bool isSelected = (i == selectedIdx);

                Widgets.DrawBoxSolid(tileRect, new Color(0.12f, 0.12f, 0.12f, 0.6f));

                if (isSelected)
                {
                    Widgets.DrawHighlight(tileRect);
                    Widgets.DrawHighlight(tileRect);
                }
                else if (Mouse.IsOver(tileRect))
                {
                    Widgets.DrawHighlight(tileRect);
                }

                Texture2D? thumb = GetThumb(_symbols[i]);
                if (thumb != null)
                {
                    GUI.DrawTexture(tileRect.ContractedBy(3f), thumb, ScaleMode.ScaleToFit);
                }

                if (Widgets.ButtonInvisible(tileRect))
                {
                    if (isArmor) _selectedArmorIndex = i;
                    else _selectedHelmetIndex = i;
                    SyncSelection();
                    PushLive();
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }

                if (Mouse.IsOver(tileRect))
                    TooltipHandler.TipRegion(tileRect, _symbols[i].LabelCap);
            }

            Widgets.EndScrollView();
        }

        // ═══════════════════════════════════════════════════════════════
        // FOOTER — Reset / Apply / Close, same layout as vanilla
        // ═══════════════════════════════════════════════════════════════
        private void DrawFooterButtons(Rect rect)
        {
            // Three buttons right-aligned, matching vanilla button sizing
            float w = ButSize.x * 0.55f; // ~110px
            float h = ButSize.y * 0.8f;  // ~32px
            float gap = 10f;
            float totalW = w * 3f + gap * 2f;
            float startX = rect.x + (rect.width - totalW) / 2f; // Center the button group

            if (Widgets.ButtonText(new Rect(startX, rect.y, w, h), "EOTF_Decals_Reset".Translate()))
            {
                _profileSet = _original;
                _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath);
                _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath);
                SyncSelection();
                PushLive();
            }
            if (Widgets.ButtonText(new Rect(startX + w + gap, rect.y, w, h), "EOTF_Decals_Apply".Translate()))
            {
                _committed = true;
                Close();
            }
            if (Widgets.ButtonText(new Rect(startX + (w + gap) * 2f, rect.y, w, h), "Close".Translate()))
            {
                _committed = false;
                Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════

        private Texture2D? GetThumb(DecalSymbolDef def)
        {
            if (def.Path.NullOrEmpty()) return null;
            _thumbCache ??= new Dictionary<string, Texture2D>();
            if (_thumbCache.TryGetValue(def.Path, out Texture2D cached)) return cached;

            Texture2D tex = ContentFinder<Texture2D>.Get(def.Path + "_south", false);
            if (tex == null) tex = ContentFinder<Texture2D>.Get(def.Path, false);
            _thumbCache[def.Path] = tex;
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

        // Sorts by hue so it doesn't look like ass
        private List<Color> AllColors()
        {
            if (_allColors != null) return _allColors;
            HashSet<Color> colorSet = new HashSet<Color>();
            if (TryGetIdeoColor(_pawn, out Color ideo)) colorSet.Add(ideo);
            if (TryGetFavoriteColor(_pawn, out Color fav)) colorSet.Add(fav);
            foreach (var def in DefDatabase<ColorDef>.AllDefsListForReading)
                if (def.colorType == ColorType.Ideo || def.colorType == ColorType.Misc || def.colorType == ColorType.Structure)
                    colorSet.Add(def.color);
            _allColors = new List<Color>(colorSet);
            _allColors.Sort((a, b) =>
            {
                Color.RGBToHSV(a, out float hA, out float sA, out _);
                Color.RGBToHSV(b, out float hB, out float sB, out _);
                int c = hA.CompareTo(hB);
                return (c != 0) ? c : sA.CompareTo(sB);
            });
            return _allColors;
        }

        private void PushLive() => DecalUtil.SetLiveEditFull(_pawn, _profileSet);

        // These break if Ideology isn't active obviously
        private static bool TryGetIdeoColor(Pawn? pawn, out Color c)
        {
            c = Color.white;
            if (!ModsConfig.IdeologyActive || pawn?.Ideo == null || Find.IdeoManager.classicMode) return false;
            c = pawn.Ideo.ApparelColor;
            return true;
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
    }
}
