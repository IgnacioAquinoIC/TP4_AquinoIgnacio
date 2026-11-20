using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RpgBattle
{
    public class MenuEntry
    {
        public string label, right, description; public bool enabled;
        public MenuEntry(string l, string r, bool e, string d) { label = l; right = r; enabled = e; description = d; }
    }
    public class MenuResult { public int index = -1; }

    /// Presentation only: reads combat state, reports menu choices. Contains no combat rules.
    public class BattleUI : MonoBehaviour
    {
        class Bar { public RectTransform fill; public Text text; }
        class Panel { public Combatant c; public Bar hp, ep, sp, atb; public Text status; public Image border; }
        class Overlay { public Combatant c; public RectTransform root; public Bar hp, atb; public Text label; }

        BattleManager m;
        Font font;
        RectTransform hudRoot, fxRoot, screenRoot;
        Canvas fxCanvas;
        CanvasGroup hudGroup;
        readonly List<Panel> panels = new List<Panel>();
        readonly List<Overlay> overlays = new List<Overlay>();
        class Portrait { public Combatant unit; public Camera cam; public RenderTexture rt; }
        readonly List<Portrait> portraits = new List<Portrait>();
        RectTransform menuRoot;
        Text menuTitle, descText, infoText, bannerText, prompt, endTitle, endPrompt;
        readonly List<Image> rowBg = new List<Image>();
        readonly List<Text> rowL = new List<Text>(), rowR = new List<Text>();
        Image bannerBg, flashImg, dimImg;
        Coroutine bannerCo, flashCo;
        GameObject startRoot, endRoot;
        readonly RectTransform[] sliderRect = new RectTransform[2];
        readonly RectTransform[] sliderFill = new RectTransform[2];
        readonly RectTransform[] sliderRow = new RectTransform[2];
        readonly Text[] sliderVal = new Text[2];
        readonly Image[] sliderRowBg = new Image[2];
        int sliderSel, dragging = -1;
        bool begun;
        float dimTarget;

        // ---------- construction helpers ----------
        Canvas MakeCanvas(string name, int order, out RectTransform root)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = 0.5f;
            root = go.GetComponent<RectTransform>();
            return c;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        Image Img(Transform p, Color c, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Img", typeof(RectTransform));
            go.transform.SetParent(p, false);
            var i = go.AddComponent<Image>(); i.color = c; i.raycastTarget = false;
            Place(i.rectTransform, anchor, pivot, pos, size);
            return i;
        }

        Text Txt(Transform p, string s, int size, TextAnchor al, Color c, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sz, bool outline = false)
        {
            var go = new GameObject("Txt", typeof(RectTransform));
            go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            t.font = font; t.text = s; t.fontSize = size; t.alignment = al; t.color = c; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            Place(t.rectTransform, anchor, pivot, pos, sz);
            if (outline) { var o = go.AddComponent<Outline>(); o.effectColor = Color.black; o.effectDistance = new Vector2(2, -2); }
            return t;
        }

        static readonly Vector2 TL = new Vector2(0, 1), TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0), MC = new Vector2(0.5f, 0.5f), BL = Vector2.zero;

        Bar MakeBar(Transform p, Color col, Vector2 pos, Vector2 size, bool text = true)
        {
            var bg = Img(p, new Color(0, 0, 0, 0.65f), TL, TL, pos, size);
            var fill = Img(bg.transform, col, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(fill.rectTransform);
            Text t = null;
            if (text) t = Txt(bg.transform, "", 18, TextAnchor.MiddleCenter, Color.white, MC, MC, Vector2.zero, size, true);
            return new Bar { fill = fill.rectTransform, text = t };
        }

        static void SetBar(Bar b, float r, string label = null)
        {
            b.fill.anchorMax = new Vector2(Mathf.Clamp01(r), 1f);
            if (b.text != null && label != null) b.text.text = label;
        }

        // ---------- build ----------
        public void Build(BattleManager mgr)
        {
            m = mgr;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var hudC = MakeCanvas("HUD", 10, out hudRoot);
            hudGroup = hudC.gameObject.AddComponent<CanvasGroup>();
            fxCanvas = MakeCanvas("FX", 20, out fxRoot);
            MakeCanvas("Screens", 30, out screenRoot);
            BuildOverlays(); BuildPanels(); BuildMenu(); BuildFx(); BuildStart(); BuildEnd();
            menuRoot.gameObject.SetActive(false);
            endRoot.SetActive(false);
        }

        void BuildOverlays()
        {
            foreach (var u in m.Units)
            {
                var root = new GameObject("Overlay", typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(hudRoot, false);
                Place(root, BL, new Vector2(0.5f, 0), Vector2.zero, new Vector2(120, 40));
                var o = new Overlay { c = u, root = root };
                o.label = Txt(root, u.displayName, 18, TextAnchor.LowerCenter, Color.white, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(240, 24), true);
                o.hp = MakeBar(root, u.IsPlayer ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.9f, 0.25f, 0.2f), new Vector2(10, -16), new Vector2(100, 9), false);
                o.atb = MakeBar(root, new Color(1f, 0.85f, 0.2f), new Vector2(10, -27), new Vector2(100, 5), false);
                overlays.Add(o);
            }
        }

        void BuildPanels()
        {
            int i = 0;
            foreach (var u in m.Units)
            {
                if (!u.IsPlayer) continue;
                float x = (i == 0 ? -290f : 290f); i++;
                var bg = Img(hudRoot, new Color(0.05f, 0.08f, 0.18f, 0.88f), BC, BC, new Vector2(x, 20), new Vector2(560, 175));
                var p = new Panel { c = u };
                p.border = Img(bg.transform, Color.clear, MC, MC, Vector2.zero, new Vector2(572, 187));
                p.border.transform.SetAsFirstSibling();
                var frame = Img(bg.transform, new Color(0.95f, 0.8f, 0.4f), TL, TL, new Vector2(8, -8), new Vector2(78, 78));
                var rawGo = new GameObject("Portrait", typeof(RectTransform));
                rawGo.transform.SetParent(frame.transform, false);
                var raw = rawGo.AddComponent<RawImage>(); raw.raycastTarget = false;
                Place(raw.rectTransform, MC, MC, Vector2.zero, new Vector2(72, 72));
                var rt = new RenderTexture(192, 192, 16) { name = "Portrait_" + u.displayName };
                raw.texture = rt;
                var camGo = new GameObject("PortraitCam_" + u.displayName);
                camGo.transform.SetParent(transform, false);
                var pc = camGo.AddComponent<Camera>();
                pc.orthographic = true; pc.orthographicSize = u.data.portraitSize; pc.targetTexture = rt;
                pc.clearFlags = CameraClearFlags.SolidColor; pc.backgroundColor = new Color(0.1f, 0.15f, 0.3f, 1f);
                pc.nearClipPlane = 0.2f; pc.farClipPlane = 1.6f; pc.depth = -20;
                portraits.Add(new Portrait { unit = u, cam = pc, rt = rt });
                Txt(bg.transform, u.displayName, 30, TextAnchor.UpperLeft, Color.white, TL, TL, new Vector2(95, -8), new Vector2(200, 36), true);
                p.status = Txt(bg.transform, "", 18, TextAnchor.UpperRight, Color.white, TL, TL, new Vector2(300, -14), new Vector2(250, 30));
                string[] labels = { "HP", "EP", "SP" };
                Color[] cols = { new Color(0.9f, 0.75f, 0.15f), new Color(0.2f, 0.55f, 0.95f), new Color(0.3f, 0.8f, 0.3f) };
                for (int k = 0; k < 3; k++)
                {
                    Txt(bg.transform, labels[k], 20, TextAnchor.MiddleLeft, Color.white, TL, TL, new Vector2(95, -56 - k * 30), new Vector2(50, 24), true);
                    var b = MakeBar(bg.transform, cols[k], new Vector2(145, -56 - k * 30), new Vector2(395, 24));
                    if (k == 0) p.hp = b; else if (k == 1) p.ep = b; else p.sp = b;
                }
                Txt(bg.transform, "ATB", 16, TextAnchor.MiddleLeft, Color.white, TL, TL, new Vector2(95, -150), new Vector2(50, 16), true);
                p.atb = MakeBar(bg.transform, new Color(1f, 0.6f, 0.1f), new Vector2(145, -152), new Vector2(395, 12), false);
                panels.Add(p);
            }
        }

        void BuildMenu()
        {
            var bg = Img(hudRoot, new Color(0.05f, 0.08f, 0.18f, 0.9f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 40), new Vector2(460, 560));
            menuRoot = bg.rectTransform;
            menuTitle = Txt(bg.transform, "", 34, TextAnchor.UpperCenter, new Color(0.95f, 0.8f, 0.4f), TC, TC, new Vector2(0, -14), new Vector2(440, 44), true);
            for (int i = 0; i < 8; i++)
            {
                var row = Img(bg.transform, Color.clear, TL, TL, new Vector2(15, -70 - i * 48), new Vector2(430, 44));
                rowBg.Add(row);
                rowL.Add(Txt(row.transform, "", 26, TextAnchor.MiddleLeft, Color.white, TL, TL, new Vector2(12, 0), new Vector2(280, 44)));
                rowR.Add(Txt(row.transform, "", 24, TextAnchor.MiddleRight, Color.white, TL, TL, new Vector2(290, 0), new Vector2(130, 44)));
            }
            descText = Txt(bg.transform, "", 22, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 14), new Vector2(420, 100));
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        void BuildFx()
        {
            dimImg = Img(fxRoot, Color.clear, MC, MC, Vector2.zero, Vector2.zero); Stretch(dimImg.rectTransform);
            flashImg = Img(fxRoot, Color.clear, MC, MC, Vector2.zero, Vector2.zero); Stretch(flashImg.rectTransform);
            bannerBg = Img(fxRoot, new Color(0.05f, 0.08f, 0.18f, 0.85f), TC, TC, new Vector2(0, -30), new Vector2(620, 60));
            bannerText = Txt(bannerBg.transform, "", 34, TextAnchor.MiddleCenter, Color.white, MC, MC, Vector2.zero, new Vector2(620, 60), true);
            bannerBg.gameObject.SetActive(false);
            infoText = Txt(hudRoot, "", 28, TextAnchor.UpperLeft, Color.white, TL, TL, new Vector2(30, -30), new Vector2(900, 40), true);
        }

        void BuildStart()
        {
            var bg = Img(screenRoot, new Color(0.03f, 0.04f, 0.1f, 0.94f), MC, MC, Vector2.zero, Vector2.zero);
            Stretch(bg.rectTransform);
            startRoot = bg.gameObject;
            Txt(bg.transform, "ISOMETRIC RPG COMBAT PROTOTYPE", 56, TextAnchor.MiddleCenter, new Color(0.95f, 0.8f, 0.4f), TC, TC, new Vector2(0, -70), new Vector2(1400, 80), true);
            Txt(bg.transform, "Fang & Xiangling  vs  10 enemies", 28, TextAnchor.MiddleCenter, Color.white, TC, TC, new Vector2(0, -150), new Vector2(1000, 40));
            string controls =
                "CONTROLS\n\n" +
                "Controller (DualSense / Xbox):\n" +
                "D-pad or Left Stick - navigate menus, squares and targets\n" +
                "X (PlayStation) / A (Xbox) - confirm        Circle (PlayStation) / B (Xbox) - go back\n" +
                "L1 / R1  (LB / RB) - cycle targets\n\n" +
                "Keyboard & Mouse:\n" +
                "Mouse - select options, squares and targets        WASD / Arrow keys - navigate\n" +
                "Left Click - confirm        Right Click - go back        Q / E - cycle targets";
            Txt(bg.transform, controls, 26, TextAnchor.UpperCenter, Color.white, TC, TC, new Vector2(0, -230), new Vector2(1500, 330));
            string[] names = { "Music Volume", "Sound Effects Volume" };
            for (int i = 0; i < 2; i++)
            {
                var row = Img(bg.transform, new Color(1, 1, 1, 0.06f), MC, MC, new Vector2(0, -80 - i * 80), new Vector2(1000, 64));
                sliderRowBg[i] = row; sliderRow[i] = row.rectTransform;
                Txt(row.transform, names[i], 28, TextAnchor.MiddleLeft, Color.white, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(320, 40));
                var track = Img(row.transform, new Color(0, 0, 0, 0.7f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(350, 0), new Vector2(520, 26));
                sliderRect[i] = track.rectTransform;
                var fill = Img(track.transform, new Color(0.95f, 0.75f, 0.2f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                Stretch(fill.rectTransform); sliderFill[i] = fill.rectTransform;
                sliderVal[i] = Txt(row.transform, "", 28, TextAnchor.MiddleRight, Color.white, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(100, 40));
            }
            prompt = Txt(bg.transform, "Press X (PlayStation) / A (Xbox) or Left-Click to begin", 36, TextAnchor.MiddleCenter, Color.white, BC, BC, new Vector2(0, 80), new Vector2(1500, 60), true);
            RefreshSliders();
        }

        void BuildEnd()
        {
            var bg = Img(screenRoot, new Color(0, 0, 0, 0.7f), MC, MC, Vector2.zero, Vector2.zero);
            Stretch(bg.rectTransform);
            endRoot = bg.gameObject;
            endTitle = Txt(bg.transform, "", 120, TextAnchor.MiddleCenter, Color.white, MC, MC, new Vector2(0, 40), new Vector2(1400, 160), true);
            endPrompt = Txt(bg.transform, "Press X / A or Left-Click to restart", 34, TextAnchor.MiddleCenter, Color.white, MC, MC, new Vector2(0, -80), new Vector2(1200, 50), true);
        }

        // ---------- public API ----------
        public void ShowStart() { startRoot.SetActive(true); hudGroup.alpha = 0f; }
        public void HideStart() { startRoot.SetActive(false); hudGroup.alpha = 1f; }
        public void SetHidden(bool hidden) { hudGroup.alpha = hidden ? 0f : 1f; }
        public void SetDim(float a) { dimTarget = a; }
        public void SetTargetInfo(string s) { if (infoText != null) infoText.text = s; }
        public void SetActive(Combatant c) { activeUnit = c; }
        Combatant activeUnit;

        public void ShowEnd(bool win)
        {
            endTitle.text = win ? "VICTORY" : "DEFEAT";
            endTitle.color = win ? new Color(1f, 0.85f, 0.35f) : new Color(0.9f, 0.3f, 0.3f);
            endRoot.SetActive(true);
            hudGroup.alpha = 1f;
        }

        public void Flash(Color c, float dur)
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(Util.Tween(dur, t => flashImg.color = Util.WithAlpha(c, 1f - t)));
        }

        public void Banner(string s, bool small)
        {
            if (bannerCo != null) StopCoroutine(bannerCo);
            bannerCo = StartCoroutine(BannerRoutine(s, small));
        }

        IEnumerator BannerRoutine(string s, bool small)
        {
            bannerBg.gameObject.SetActive(true);
            bannerText.text = s; bannerText.fontSize = small ? 28 : 36;
            bannerBg.rectTransform.anchoredPosition = new Vector2(0, small ? -100 : -30);
            yield return new WaitForSeconds(1.3f);
            bannerBg.gameObject.SetActive(false);
        }

        Vector2 ToUi(Vector3 world)
        {
            var sp = m.cam.cam.WorldToScreenPoint(world);
            return new Vector2(sp.x, sp.y) / fxCanvas.scaleFactor;
        }

        public void Popup(Vector3 world, string s, Color c, int size = 34)
        {
            var t = Txt(fxRoot, s, size, TextAnchor.MiddleCenter, c, BL, new Vector2(0.5f, 0.5f), ToUi(world), new Vector2(300, 50), true);
            StartCoroutine(PopRoutine(t, ToUi(world) + new Vector2(Random.Range(-25f, 25f), 0)));
        }

        IEnumerator PopRoutine(Text t, Vector2 start)
        {
            yield return Util.Tween(1.1f, k =>
            {
                if (t == null) return;
                t.rectTransform.anchoredPosition = start + new Vector2(0, 90f * k);
                t.color = Util.WithAlpha(t.color, k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
            });
            if (t != null) Destroy(t.gameObject);
        }

        public IEnumerator RunMenu(string title, List<MenuEntry> entries, MenuResult r)
        {
            r.index = -1;
            yield return null;
            menuRoot.gameObject.SetActive(true);
            menuTitle.text = title;
            int index = 0;
            for (int i = 0; i < entries.Count; i++) if (entries[i].enabled) { index = i; break; }
            while (m.state != BattleManager.State.Ended)
            {
                var nav = m.input.RawNav;
                if (nav.y != 0) { index = (index - nav.y + entries.Count) % entries.Count; m.audio.Play("ui_move"); }
                int hover = -1;
                for (int i = 0; i < entries.Count; i++)
                    if (RectTransformUtility.RectangleContainsScreenPoint(rowBg[i].rectTransform, m.input.PointerPos, null)) hover = i;
                if (m.input.PointerMoved && hover >= 0 && hover != index) { index = hover; m.audio.Play("ui_move"); }
                for (int i = 0; i < rowBg.Count; i++)
                {
                    bool on = i < entries.Count;
                    rowBg[i].gameObject.SetActive(on);
                    if (!on) continue;
                    bool sel = i == index;
                    rowBg[i].color = sel ? new Color(0.95f, 0.75f, 0.2f, 0.95f) : new Color(1, 1, 1, 0.05f);
                    Color tc = !entries[i].enabled ? new Color(0.5f, 0.5f, 0.55f) : (sel ? Color.black : Color.white);
                    rowL[i].text = entries[i].label; rowR[i].text = entries[i].right;
                    rowL[i].color = rowR[i].color = tc;
                }
                descText.text = entries[index].description;
                if (m.input.Cancel) { r.index = -1; m.audio.Play("ui_back"); break; }
                if (m.input.Confirm && !(m.input.ConfirmByPointer && hover < 0))
                {
                    if (entries[index].enabled) { r.index = index; m.audio.Play("ui_confirm"); break; }
                    m.audio.Play("ui_back");
                }
                yield return null;
            }
            menuRoot.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            foreach (var p in portraits)
            {
                if (p.unit == null || p.unit.view == null) continue;
                Vector3 focus = p.unit.view.transform.position + Vector3.up * (p.unit.view.Head.y - p.unit.view.transform.position.y) * p.unit.data.portraitHeightFraction;
                Vector3 fwd = p.unit.view.transform.forward;
                p.cam.transform.position = focus + fwd * 1.0f;
                p.cam.transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
            }
        }

        void OnDestroy()
        {
            foreach (var p in portraits) if (p.rt != null) p.rt.Release();
        }

        // ---------- per-frame ----------
        void Update()
        {
            if (m == null) return;
            dimImg.color = Color.Lerp(dimImg.color, new Color(0, 0, 0, dimTarget), 8f * Time.deltaTime);
            if (m.state == BattleManager.State.Intro && startRoot.activeSelf) HandleStart();
            else if (endRoot.activeSelf && m.input.Confirm) { endRoot.SetActive(false); m.RequestRestart(); }
            if (!m.Units.Exists(u => u != null)) return;
            RefreshPanels();
            RefreshOverlays();
        }

        void RefreshPanels()
        {
            foreach (var p in panels)
            {
                var c = p.c;
                SetBar(p.hp, c.hp / (float)c.MaxHp, c.hp + " / " + c.MaxHp);
                SetBar(p.ep, c.MaxEp > 0 ? c.ep / (float)c.MaxEp : 0, c.ep + " / " + c.MaxEp);
                SetBar(p.sp, c.sp / (float)c.MaxSp, c.sp + " / " + c.MaxSp);
                SetBar(p.atb, Mathf.Clamp01(c.atb / m.config.atbMax));
                p.status.text = StatusText(c);
                p.border.color = activeUnit == c && !c.isDead ? new Color(1f, 0.85f, 0.2f, 1f) : (c.isDead ? new Color(0.5f, 0.1f, 0.1f, 0.8f) : Color.clear);
            }
        }

        void RefreshOverlays()
        {
            foreach (var o in overlays)
            {
                bool show = !o.c.isDead && o.c.view != null;
                o.root.gameObject.SetActive(show);
                if (!show) continue;
                o.root.anchoredPosition = ToUi(o.c.view.Head + Vector3.up * 0.55f);
                SetBar(o.hp, o.c.hp / (float)o.c.MaxHp);
                SetBar(o.atb, Mathf.Clamp01(o.c.atb / m.config.atbMax));
                string st = StatusText(o.c);
                o.label.text = o.c.displayName + (st.Length > 0 ? "\n" + st : "");
                o.label.fontSize = st.Length > 0 ? 15 : 18;
            }
        }

        string StatusText(Combatant c)
        {
            var sb = new StringBuilder();
            foreach (var e in c.effects) sb.Append("<color=#").Append(Util.Hex(e.color)).Append(">").Append(e.label).Append(' ').Append(e.turns).Append("</color>  ");
            if (c.shield > 0) sb.Append("<color=#E6B266>Guard</color>");
            return sb.ToString();
        }

        // ---------- start screen ----------
        void RefreshSliders()
        {
            if (m == null) return;
            float[] v = { m.audio.MusicVolume, m.audio.SfxVolume };
            for (int i = 0; i < 2; i++)
            {
                sliderFill[i].anchorMax = new Vector2(v[i], 1f);
                sliderVal[i].text = Mathf.RoundToInt(v[i] * 100f) + "%";
                sliderRowBg[i].color = i == sliderSel ? new Color(1f, 0.85f, 0.3f, 0.18f) : new Color(1, 1, 1, 0.06f);
            }
        }

        void SetSlider(int i, float v)
        {
            v = Mathf.Clamp01(v);
            if (i == 0) m.audio.SetMusicVolume(v); else { m.audio.SetSfxVolume(v); m.audio.Play("ui_confirm"); }
            RefreshSliders();
        }

        void HandleStart()
        {
            prompt.color = Util.WithAlpha(Color.white, 0.6f + 0.4f * Mathf.Sin(Time.time * 4f));
            var inp = m.input;
            if (inp.RawNav.y != 0) { sliderSel = Mathf.Clamp(sliderSel - inp.RawNav.y, 0, 1); m.audio.Play("ui_move"); RefreshSliders(); }
            if (inp.RawNav.x != 0) SetSlider(sliderSel, (sliderSel == 0 ? m.audio.MusicVolume : m.audio.SfxVolume) + 0.05f * inp.RawNav.x);
            if (inp.LeftPressed)
            {
                dragging = -1;
                for (int i = 0; i < 2; i++)
                    if (RectTransformUtility.RectangleContainsScreenPoint(sliderRow[i], inp.PointerPos, null)) dragging = i;
                if (dragging >= 0) { sliderSel = dragging; RefreshSliders(); }
                else { Begin(); return; }
            }
            if (dragging >= 0)
            {
                if (inp.LeftHeld)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderRect[dragging], inp.PointerPos, null, out var local);
                    var rect = sliderRect[dragging].rect;
                    float v = Mathf.Clamp01((local.x - rect.xMin) / rect.width);
                    if (dragging == 0) m.audio.SetMusicVolume(v); else m.audio.SetSfxVolume(v);
                    RefreshSliders();
                }
                else { if (dragging == 1) m.audio.Play("ui_confirm"); dragging = -1; }
            }
            if (inp.Confirm && !inp.ConfirmByPointer) Begin();
        }

        void Begin()
        {
            if (begun) return;
            begun = true;
            m.Begin();
        }
    }
}
