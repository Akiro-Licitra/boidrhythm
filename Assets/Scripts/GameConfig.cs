public static class GameConfig
{
    // General config options
    public static string SongResourceName = "song";   // Assets/Resources/song.mp3
    public static float  Bpm              = 205f;     // tempo of your mp3
    public static float  FirstBeatOffset  = 0.0f;     // seconds from start of mp3 to the first beat
    public static float  AudioOffset      = 0.0f;     // tweak for output latency (+ = notes judged later)

    public static float  LeadIn           = 2f;       // seconds of silence before the song starts
    public static float  NoteTravelTime   = 1.6f;     // seconds a sphere takes to fall to the line
    public static float  HitWindow        = 0.15f;    // seconds either side of the beat
    public static float  EarlyMissWindow  = 0.30f;    // pressing this far before a note (but outside HitWindow) = miss
    public static int    FlockSize        = 300;
    public static float  OnBeatChance     = 0.65f;    // chance a beat spawns a note
    public static float  OffBeatChance    = 0.10f;    // chance an 8th-note between beats spawns one

    // Orbit schedule: flock holds still, then swings to a new angle around the player.
    public static float  OrbitStart       = 15f;      // seconds before the first swing cycle
    public static float  OrbitCycle       = 20f;      // seconds per cycle
    public static float  OrbitHold        = 12f;      // seconds of each cycle spent stationary
    public static float  OrbitMinDeg      = 70f;
    public static float  OrbitMaxDeg      = 130f;

    public static int    UdpPort          = 5005;    
}
