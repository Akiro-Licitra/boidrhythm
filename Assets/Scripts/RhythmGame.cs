using System.Collections.Generic;
using UnityEngine;

public class RhythmGame : MonoBehaviour
{
    class Note
    {
        public Transform t; public int lane;
        public float spawnTime, hitTime, prevHit = -999f; public Vector3 start; public bool missed;
    }

    const float HitY = 0.14f, Depth = 9f;
    static readonly float[] LaneX = { 0.42f, 0.58f };           // lane 0 = Z, lane 1 = X
    // Saturated blue / pink: far from the dull slate flock colour.
    static readonly Color[] LaneCol = { new Color(0.10f, 0.60f, 1f), new Color(1f, 0.22f, 0.65f) };

    public CameraRig rig; public HandInputUdp hand;
    Camera cam; BoidFlock flock; AudioSource src; AudioClip clip;
    GameObject hudRoot; Transform lineT; Transform[] markers = new Transform[2];
    Material[] laneMat = new Material[2]; Material lineMat;

    readonly List<Note> notes = new List<Note>();
    readonly List<float> respawns = new List<float>();
    System.Random rng;
    float[] orbit = new float[64];
    float[] spectrum = new float[256];
    float[] flash = new float[2];
    float[] lastHit = { -999f, -999f };   // hit time of the most recently spawned note per lane

    double startDsp; float songTime, songLength, bass;
    int nextHalfBeat, lastBeat;
    int score, combo, maxCombo, perfect, great, good, miss;
    string judge = ""; float judgeT = -10f; Color judgeCol = Color.white;
    bool finished;
    GUIStyle st;

    void Start()
    {
        cam = rig.GetComponent<Camera>();
        src = gameObject.AddComponent<AudioSource>();
        clip = Resources.Load<AudioClip>(GameConfig.SongResourceName);
        if (clip == null) Debug.LogWarning("No Assets/Resources/" + GameConfig.SongResourceName + ".mp3 found - running silent.");
        Begin();
    }

    void Begin()
    {
        rng = new System.Random(1234);
        orbit[0] = 0;
        for (int i = 1; i < orbit.Length; i++)
            orbit[i] = orbit[i - 1] + (rng.Next(2) == 0 ? -1 : 1) *
                       Mathf.Lerp(GameConfig.OrbitMinDeg, GameConfig.OrbitMaxDeg, (float)rng.NextDouble());

        var fg = new GameObject("Flock");
        flock = fg.AddComponent<BoidFlock>();
        flock.Init(GameConfig.FlockSize);

        hudRoot = new GameObject("HUD3D");
        hudRoot.transform.SetParent(cam.transform, false);
        for (int i = 0; i < 2; i++) laneMat[i] = MaterialUtil.Make(LaneCol[i]);
        lineMat = MaterialUtil.Make(Color.white);
        lineT = MakeCube(lineMat);
        for (int i = 0; i < 2; i++) markers[i] = MakeSphere(laneMat[i]);

        songLength = clip != null ? clip.length : 90f;
        nextHalfBeat = 8; lastBeat = -1;
        score = combo = maxCombo = perfect = great = good = miss = 0;
        finished = false; judge = "";
        lastHit[0] = lastHit[1] = -999f;

        startDsp = AudioSettings.dspTime + GameConfig.LeadIn;
        if (clip != null) { src.clip = clip; src.PlayScheduled(startDsp); }
    }

    Transform MakeCube(Material m) { return Prim(PrimitiveType.Cube, m); }
    Transform MakeSphere(Material m) { return Prim(PrimitiveType.Sphere, m); }
    Transform Prim(PrimitiveType p, Material m)
    {
        var g = GameObject.CreatePrimitive(p);
        Destroy(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().sharedMaterial = m;
        g.transform.SetParent(hudRoot.transform, false);
        return g.transform;
    }

    void Restart()
    {
        src.Stop();
        foreach (var n in notes) if (n.t) Destroy(n.t.gameObject);
        notes.Clear(); respawns.Clear();
        Destroy(flock.gameObject); Destroy(hudRoot);
        rig.Yaw = 0;
        Begin();
    }

    // ---------- helpers ----------
    Vector3 ViewportToLocal(float vx, float vy)
    {
        float hh = Depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), hw = hh * cam.aspect;
        return new Vector3((vx - 0.5f) * 2f * hw, (vy - 0.5f) * 2f * hh, Depth);
    }
    Vector3 HitPoint(int lane) { return cam.transform.TransformPoint(ViewportToLocal(LaneX[lane], HitY)); }

    float OrbitAngle(float t)
    {
        if (t < GameConfig.OrbitStart) return 0f;
        float u = (t - GameConfig.OrbitStart) / GameConfig.OrbitCycle;
        int k = Mathf.Min((int)u, orbit.Length - 2);
        float f = (u - k) * GameConfig.OrbitCycle;
        float m = Mathf.Clamp01((f - GameConfig.OrbitHold) / (GameConfig.OrbitCycle - GameConfig.OrbitHold));
        return Mathf.Lerp(orbit[k], orbit[k + 1], Mathf.SmoothStep(0, 1, m));
    }

    float HalfBeatTime(int h) { return GameConfig.FirstBeatOffset + h * 30f / GameConfig.Bpm; }

    // ---------- main loop ----------
    void Update()
    {
        if (InputShim.RDown) { Restart(); return; }

        songTime = (float)(AudioSettings.dspTime - startDsp) - GameConfig.AudioOffset;

        if (src.isPlaying)
        {
            src.GetSpectrumData(spectrum, 0, FFTWindow.Blackman);
            float s = 0; for (int i = 0; i < 6; i++) s += spectrum[i];
            bass = Mathf.Lerp(bass, Mathf.Clamp01(s * 6f), 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
        flock.Bass = bass;

        // flock orbits the player
        float ang = OrbitAngle(songTime);
        Vector3 dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
        flock.Center = rig.transform.position + dir * 14f + Vector3.up * 1f;

        // beat clock -> flock pulse
        int beat = Mathf.FloorToInt((songTime - GameConfig.FirstBeatOffset) * GameConfig.Bpm / 60f);
        if (songTime >= GameConfig.FirstBeatOffset && beat > lastBeat) { flock.Beat(); lastBeat = beat; }

        SpawnNotes();
        UpdateNotes();
        UpdateHud();

        if (InputShim.ZDown) Press(0);
        if (InputShim.XDown) Press(1);

        for (int i = respawns.Count - 1; i >= 0; i--)
            if (songTime >= respawns[i]) { flock.Spawn(false); respawns.RemoveAt(i); }

        if (songTime > songLength + 1f && notes.Count == 0) finished = true;
    }

    void SpawnNotes()
    {
        while (HalfBeatTime(nextHalfBeat) - GameConfig.NoteTravelTime <= songTime)
        {
            int h = nextHalfBeat++;
            float hitTime = HalfBeatTime(h);
            if (hitTime > songLength - 1.5f || flock.Boids.Count < 40) continue;
            float chance = (h % 2 == 0) ? GameConfig.OnBeatChance : GameConfig.OffBeatChance;
            if (rng.NextDouble() > chance) continue;

            var b = flock.Detach();
            if (b == null) continue;
            int lane = rng.Next(2);
            b.t.GetComponent<Renderer>().sharedMaterial = laneMat[lane];
            notes.Add(new Note
            {
                t = b.t, lane = lane, start = b.t.position,
                spawnTime = hitTime - GameConfig.NoteTravelTime, hitTime = hitTime,
                prevHit = lastHit[lane]
            });
            lastHit[lane] = hitTime;
            respawns.Add(songTime + 1.2f);
        }
    }

    void UpdateNotes()
    {
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            var n = notes[i];
            float t = (songTime - n.spawnTime) / (n.hitTime - n.spawnTime);
            // accelerating "fall" from where it left the flock to the line (in camera space, so it
            // always lands on the line no matter where the camera is looking)
            n.t.position = Vector3.LerpUnclamped(n.start, HitPoint(n.lane), t * t);
            float sc = Mathf.Lerp(0.35f, 0.70f, Mathf.Clamp01(t));   // bigger notes read better against the flock
            if (n.missed) sc *= Mathf.Clamp01(1f - (t - 1.1f) * 2.5f);
            n.t.localScale = Vector3.one * sc;

            if (!n.missed && songTime - n.hitTime > GameConfig.HitWindow)
            {
                n.missed = true; miss++; combo = 0; Judge("Miss", new Color(1f, 0.35f, 0.35f));
            }
            if (t > 1.5f) { Destroy(n.t.gameObject); notes.RemoveAt(i); }
        }
    }

    void Press(int lane)
    {
        flash[lane] = 1f;

        // Only the earliest unresolved note in this lane can react (notes are stored in hit-time order).
        Note n = null;
        foreach (var c in notes) if (c.lane == lane && !c.missed) { n = c; break; }
        if (n == null) return;

        float dt = songTime - n.hitTime;      // negative = early
        float d = Mathf.Abs(dt);

        if (d <= GameConfig.HitWindow)
        {
            int pts; string j; Color col;
            if (d <= 0.05f)      { pts = 300; j = "Perfect"; col = Color.yellow; perfect++; }
            else if (d <= 0.10f) { pts = 200; j = "Great";   col = Color.green;  great++; }
            else                 { pts = 100; j = "Good";    col = Color.cyan;   good++; }
            combo++; maxCombo = Mathf.Max(maxCombo, combo);
            score += pts * (1 + combo / 10);
            Judge(j, col);
            Destroy(n.t.gameObject);
            notes.Remove(n);
            return;
        }

        // Too early: counts as a miss and consumes the note, but only inside the early zone, and never
        // overlapping the previous same-lane note's hit window (so close notes can't punish each other).
        float earlyStart = Mathf.Max(n.hitTime - GameConfig.EarlyMissWindow, n.prevHit + GameConfig.HitWindow);
        if (dt < -GameConfig.HitWindow && songTime >= earlyStart)
        {
            n.missed = true; miss++; combo = 0;
            Judge("Too early", new Color(1f, 0.55f, 0.1f));
        }
    }

    void Judge(string s, Color c) { judge = s; judgeCol = c; judgeT = Time.time; }

    void UpdateHud()
    {
        float x0 = ViewportToLocal(0.3f, HitY).x, x1 = ViewportToLocal(0.7f, HitY).x;
        lineT.localPosition = ViewportToLocal(0.5f, HitY);
        lineT.localScale = new Vector3(x1 - x0, 0.04f, 0.04f);
        for (int i = 0; i < 2; i++)
        {
            flash[i] = Mathf.Max(0, flash[i] - Time.deltaTime * 6f);
            markers[i].localPosition = ViewportToLocal(LaneX[i], HitY);
            markers[i].localScale = Vector3.one * (0.55f + 0.35f * flash[i]);
        }
    }

    // ---------- UI ----------
    // Every label gets a translucent dark panel + a black outline, so text stays readable over anything.
    void Lbl(string s, float x, float y, float w, TextAnchor a, Color c, float sizeMul = 1f)
    {
        if (string.IsNullOrEmpty(s)) return;
        st.wordWrap = false; st.clipping = TextClipping.Overflow; st.alignment = TextAnchor.UpperLeft;
        st.fontSize = Mathf.RoundToInt(Screen.height / 28f * sizeMul);
        Vector2 sz = st.CalcSize(new GUIContent(s));          // exact single-line size, any resolution

        // x/w define the area the text is anchored in; the box is always sized to the text itself
        float pad = st.fontSize * 0.3f;
        float bx = x;
        if (a == TextAnchor.UpperCenter) bx = x + (w - sz.x) * 0.5f;
        else if (a == TextAnchor.UpperRight) bx = x + w - sz.x;
        bx = Mathf.Clamp(bx, pad + 4f, Mathf.Max(pad + 4f, Screen.width - sz.x - pad - 4f));   // keep on screen

        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(bx - pad, y - pad * 0.3f, sz.x + pad * 2f, sz.y + pad * 0.6f), Texture2D.whiteTexture);
        GUI.color = prev;

        float o = Mathf.Max(1.5f, st.fontSize / 16f), rw = sz.x + 4f;
        st.normal.textColor = Color.black;
        GUI.Label(new Rect(bx - o, y - o, rw, sz.y), s, st); GUI.Label(new Rect(bx + o, y - o, rw, sz.y), s, st);
        GUI.Label(new Rect(bx - o, y + o, rw, sz.y), s, st); GUI.Label(new Rect(bx + o, y + o, rw, sz.y), s, st);
        st.normal.textColor = c;
        GUI.Label(new Rect(bx, y, rw, sz.y), s, st);
    }

    void OnGUI()
    {
        if (st == null) st = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        float W = Screen.width, H = Screen.height;

        Lbl("Score " + score, 20, 10, 400, TextAnchor.UpperLeft, Color.white);
        Lbl(combo > 1 ? combo + " combo" : "", 20, 10 + H / 24f, 400, TextAnchor.UpperLeft, Color.yellow, 0.8f);
        string hs = rig.HandTracked ? (rig.PalmOpen ? "Hand: OPEN palm - steering" : "Hand: fist - holding")
                                    : "Hand: not tracked (A/D or arrows work)";
        Lbl(hs, 0, 10, W - 20, TextAnchor.UpperRight,
            rig.HandTracked ? new Color(0.4f, 1f, 0.4f) : new Color(0.8f, 0.8f, 0.8f), 0.7f);

        // lane labels
        string[] keys = { "Z", "X" };
        for (int i = 0; i < 2; i++)
        {
            Vector3 sp = cam.WorldToScreenPoint(markers[i].position);
            Lbl(keys[i], sp.x - 50, H - sp.y + H * 0.03f, 100, TextAnchor.UpperCenter, Color.Lerp(LaneCol[i], Color.white, 0.35f));
        }

        // judgement popup
        if (Time.time - judgeT < 0.6f)
            Lbl(judge, W / 2 - 200, H * 0.62f, 400, TextAnchor.UpperCenter, judgeCol, 1.4f);

        // off-screen flock arrow
        float diff = Mathf.DeltaAngle(rig.Yaw, OrbitAngle(songTime));
        float hfov = 2f * Mathf.Atan(Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect) * Mathf.Rad2Deg;
        if (Mathf.Abs(diff) > hfov * 0.42f)
            Lbl(diff > 0 ? "flock  >>>" : "<<<  flock", diff > 0 ? W - 320 : 20, H * 0.4f, 300,
                diff > 0 ? TextAnchor.UpperRight : TextAnchor.UpperLeft, Color.white, 1.2f);

        if (songTime < 0) Lbl("Get ready...  " + Mathf.CeilToInt(-songTime), 0, H * 0.3f, W, TextAnchor.UpperCenter, Color.white, 1.5f);
        if (clip == null) Lbl("Put your mp3 at Assets/Resources/song.mp3", 0, H * 0.45f, W, TextAnchor.UpperCenter, new Color(1f, 0.35f, 0.35f), 0.9f);

        if (finished)
        {
            Lbl("RESULTS", 0, H * 0.18f, W, TextAnchor.UpperCenter, Color.white, 1.6f);
            Lbl("Score " + score + "   Max combo " + maxCombo, 0, H * 0.3f, W, TextAnchor.UpperCenter, Color.yellow);
            Lbl("Perfect " + perfect + "  Great " + great + "  Good " + good + "  Miss " + miss, 0, H * 0.38f, W, TextAnchor.UpperCenter, Color.white);
            Lbl("Press R to restart", 0, H * 0.48f, W, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.85f));
        }
    }
}