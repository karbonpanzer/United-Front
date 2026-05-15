using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EOTF.Core.DecalSystem
{
    public sealed class DialogEditDecals : Window
    {
        private readonly Pawn _pawn;
        private DecalProfileSet _profileSet;
        private readonly DecalProfileSet _original;
        private readonly List<DecalSymbolDef> _symbols;

        private int _selectedHelmetIndex;
        private int _selectedArmorIndex;
        private DecalSymbolDef? _selectedHelmetSymbol;
        private DecalSymbolDef? _selectedArmorSymbol;
        
        private bool _committed;
        private Vector2 _scrollPos;
        private List<Color>? _allColors;
        
        private Action? _helmetLeft, _helmetCenter, _helmetRight;
        private Action? _armorLeft, _armorCenter, _armorRight;
        
        private float _viewRectHeight = 750f;
        private readonly Listing_Standard _listing = new Listing_Standard(); 

        public override Vector2 InitialSize => new Vector2(600f, 750f);

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

            _helmetLeft   = () => { UpdateIndex(DecalSlot.Helmet, -1); SyncSelection(); PushLive(); };
            _helmetCenter = () => FloatMenuUtility.MakeMenu(_symbols, e => e.LabelCap, v => () => { SetIndex(DecalSlot.Helmet, _symbols.IndexOf(v)); SyncSelection(); PushLive(); });
            _helmetRight  = () => { UpdateIndex(DecalSlot.Helmet,  1); SyncSelection(); PushLive(); };
            _armorLeft    = () => { UpdateIndex(DecalSlot.Armor,  -1); SyncSelection(); PushLive(); };
            _armorCenter  = () => FloatMenuUtility.MakeMenu(_symbols, e => e.LabelCap, v => () => { SetIndex(DecalSlot.Armor,  _symbols.IndexOf(v)); SyncSelection(); PushLive(); });
            _armorRight   = () => { UpdateIndex(DecalSlot.Armor,   1); SyncSelection(); PushLive(); };
        }

        // Main window drawing, this whole thing needs a rewrite honestly
        public override void Close(bool doCloseSound = true)
        {

            DecalUtil.EndLiveEdit(_pawn, _committed, _original);
            base.Close(doCloseSound);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (_pawn.Destroyed) { Close(false); return; }

            float outerPad = 12f;
            float footerH = 58f;
            
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x + outerPad, inRect.y, inRect.width - outerPad * 2f, 35f);
            Widgets.Label(titleRect, "EOTF_StyleDecalsTitle".Translate(_pawn.Name.ToStringShort));
            Text.Font = GameFont.Small;
            
            Rect mainRect = new Rect(inRect.x + outerPad, titleRect.yMax + 8f, inRect.width - outerPad * 2f, inRect.height - titleRect.height - footerH - 20f);
            Widgets.DrawMenuSection(mainRect);
            
            Rect inner = mainRect.ContractedBy(12f);
            Rect viewRect = new Rect(0, 0, inner.width - 16f, _viewRectHeight);
            
            Widgets.BeginScrollView(inner, ref _scrollPos, viewRect);
            _listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            _listing.Label("EOTF_Decals_Armor".Translate());
            Text.Font = GameFont.Small;
            _listing.GapLine(6f);
            DrawSlotEditor(_listing, DecalSlot.Armor);
            
            _listing.Gap(24f); 

            Text.Font = GameFont.Medium;
            _listing.Label("EOTF_Decals_Helmet".Translate());
            Text.Font = GameFont.Small;
            _listing.GapLine(6f);
            DrawSlotEditor(_listing, DecalSlot.Helmet);
            
            if (Event.current.type == EventType.Layout)
            {
                _viewRectHeight = _listing.CurHeight + 20f;
            }

            _listing.End();
            Widgets.EndScrollView();
            
            Rect footer = new Rect(inRect.x + outerPad, inRect.yMax - footerH, inRect.width - outerPad * 2f, footerH);
            DrawBottomButtons(footer);
        }

        // All the helmet and armor editing shit lives here
        private void DrawSlotEditor(Listing_Standard listing, DecalSlot slot)
        {
            bool isArmor = (slot == DecalSlot.Armor);
            bool active = isArmor ? _profileSet.Armor.Active : _profileSet.Helmet.Active;
            DecalSymbolDef? symbol = isArmor ? _selectedArmorSymbol : _selectedHelmetSymbol;
            
            Rect row1 = listing.GetRect(28f);
            bool wasActive = active;
            Widgets.CheckboxLabeled(new Rect(row1.x, row1.y, row1.width * 0.45f, row1.height), "Enabled".Translate(), ref active);
            
            if (wasActive != active)
            {
                if (isArmor) _profileSet.Armor.Active = active;
                else _profileSet.Helmet.Active = active;
                PushLive();
            }

            if (Widgets.ButtonText(new Rect(row1.xMax - 130f, row1.y, 130f, row1.height), "EOTF_Decals_RandomSymbol".Translate()) && _symbols.Count > 0)
            {
                if (isArmor) _selectedArmorIndex = Rand.Range(0, _symbols.Count);
                else _selectedHelmetIndex = Rand.Range(0, _symbols.Count);
                SyncSelection();
                PushLive();
            }

            _listing.Gap(8f);
            
            Rect row2 = listing.GetRect(32f);
            string label = symbol?.LabelCap ?? "EOTF_Decals_NoSelection".Translate();

            bool isArmorForButtons = slot == DecalSlot.Armor;
            MakeFloatOptionButtons(row2,
                isArmorForButtons ? _armorLeft!   : _helmetLeft!,
                isArmorForButtons ? _armorCenter! : _helmetCenter!,
                label,
                isArmorForButtons ? _armorRight!  : _helmetRight!);

            _listing.Gap(12f);
            
            _listing.Label("EOTF_Decals_Color".Translate());
            
            Color color = isArmor ? _profileSet.Armor.SymbolColor : _profileSet.Helmet.SymbolColor;
            Color original = color;
            
            Rect posRect = listing.GetRect(0f);
            Widgets.ColorSelector(new Rect(posRect.x, posRect.y, listing.ColumnWidth, 1000f), ref color, AllColors(), out float usedHeight, null, 22, 2);
            _listing.Gap(usedHeight + 8f);
            
            Rect btnRow = listing.GetRect(28f);
            float btnW = (btnRow.width - 12f) / 3f;

            if (Widgets.ButtonText(new Rect(btnRow.x, btnRow.y, btnW, btnRow.height), "EOTF_Decals_IdeoColor".Translate()))
            {
                if (TryGetIdeoColor(_pawn, out Color c)) { color = c; SoundDefOf.Tick_Low.PlayOneShotOnCamera(); }
            }
            if (Widgets.ButtonText(new Rect(btnRow.x + btnW + 6f, btnRow.y, btnW, btnRow.height), "EOTF_Decals_RandomColor".Translate()))
            {
                var p = AllColors(); if (p.Count > 0) { color = p[Rand.Range(0, p.Count)]; SoundDefOf.Tick_Low.PlayOneShotOnCamera(); }
            }
            if (Widgets.ButtonText(new Rect(btnRow.xMax - btnW, btnRow.y, btnW, btnRow.height), "EOTF_Decals_FavColor".Translate()))
            {
                if (TryGetFavoriteColor(_pawn, out Color c)) { color = c; SoundDefOf.Tick_Low.PlayOneShotOnCamera(); }
            }

            if (!original.IndistinguishableFrom(color))
            {
                if (isArmor) _profileSet.Armor.SymbolColor = color;
                else _profileSet.Helmet.SymbolColor = color;
                PushLive();
            }
        }

        // Apply, Reset, Close - the usual bottom bar crap
        private void DrawBottomButtons(Rect rect)
        {
            float w = 110f;
            float btnY = rect.yMax - 32f - 12f;
            float x = rect.xMax - (w * 3 + 20f);

            if (Widgets.ButtonText(new Rect(x, btnY, w, 32f), "EOTF_Decals_Reset".Translate())) 
            { 
                _profileSet = _original; 
                _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath); 
                _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath); 
                SyncSelection(); PushLive(); 
            }
            if (Widgets.ButtonText(new Rect(x + w + 10f, btnY, w, 32f), "EOTF_Decals_Apply".Translate())) { _committed = true; Close(); }
            if (Widgets.ButtonText(new Rect(rect.xMax - w, btnY, w, 32f), "Close".Translate())) { _committed = false; Close(); }
        }

        // Left/right arrows just blindly cycle through symbols for now
        private void UpdateIndex(DecalSlot slot, int delta)
        {
            if (_symbols.Count == 0) return;
            if (slot == DecalSlot.Armor) _selectedArmorIndex = (_selectedArmorIndex + delta + _symbols.Count) % _symbols.Count;
            else _selectedHelmetIndex = (_selectedHelmetIndex + delta + _symbols.Count) % _symbols.Count;
        }

        private void SetIndex(DecalSlot slot, int val)
        {
            if (slot == DecalSlot.Armor) _selectedArmorIndex = val;
            else _selectedHelmetIndex = val;
        }

        private int FindSymbolIndex(string path) => (path.NullOrEmpty() || _symbols.Count == 0) ? 0 : Mathf.Max(0, _symbols.FindIndex(d => d?.Path == path));

        private void SyncSelection()
        {
            if (_symbols.Count == 0) return;
            _selectedHelmetIndex = Mathf.Clamp(_selectedHelmetIndex, 0, _symbols.Count - 1);
            _selectedHelmetSymbol = _symbols[_selectedHelmetIndex];
            _profileSet.Helmet.SymbolPath = _selectedHelmetSymbol?.Path ?? "";

            _selectedArmorIndex = Mathf.Clamp(_selectedArmorIndex, 0, _symbols.Count - 1);
            _selectedArmorSymbol = _symbols[_selectedArmorIndex];
            _profileSet.Armor.SymbolPath = _selectedArmorSymbol?.Path ?? "";
        }

        // Color palette crap, sorts by hue so it doesn't look like ass
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

        // Ideo and fav color button helpers, these break if Ideology isn't active obviously
        private void PushLive() => DecalUtil.SetLiveEditFull(_pawn, _profileSet);
        private static bool TryGetIdeoColor(Pawn? pawn, out Color c) { c = Color.white; if (!ModsConfig.IdeologyActive || pawn?.Ideo == null || Find.IdeoManager.classicMode) return false; c = pawn.Ideo.ApparelColor; return true; }
        private static bool TryGetFavoriteColor(Pawn? pawn, out Color c) { c = Color.white; if (!ModsConfig.IdeologyActive || pawn?.story == null || pawn.DevelopmentalStage.Baby()) return false; ColorDef def = pawn.story.favoriteColor; if (def == null) return false; c = def.color; return true; }

        private static void MakeFloatOptionButtons(Rect rect, Action leftAction, Action centerAction, string centerButtonName, Action rightAction)
        {
            float sideW = 40f;
            Rect l = new Rect(rect.x, rect.y, sideW, rect.height);
            Rect r = new Rect(rect.xMax - sideW, rect.y, sideW, rect.height);
            Rect m = new Rect(l.xMax + 6f, rect.y, rect.width - (sideW * 2f + 12f), rect.height);
            if (Widgets.ButtonText(l, "<")) leftAction();
            if (Widgets.ButtonText(m, centerButtonName)) centerAction();
            if (Widgets.ButtonText(r, ">")) rightAction();
        }
    }
}