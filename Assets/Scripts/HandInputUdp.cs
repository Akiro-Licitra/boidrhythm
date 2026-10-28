using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Receives hand data from Tools/hand_tracker.py (MediaPipe Hands) over UDP.
// Packet: {"present":true,"x":0.62,"y":0.48,"open":true}  (x,y normalised, mirrored like a mirror)
// To use MediaPipe's Unity plugin instead, keep this same TryGet() signature.
public class HandInputUdp : MonoBehaviour
{
    [Serializable] class Packet { public bool present; public float x; public float y; public bool open; }

    UdpClient client;
    Thread thread;
    volatile bool running;
    readonly object lk = new object();
    string raw;
    long lastMs = -100000;
    readonly Stopwatch sw = Stopwatch.StartNew();

    void Start()
    {
        try
        {
            client = new UdpClient(GameConfig.UdpPort);
            running = true;
            thread = new Thread(Loop) { IsBackground = true };
            thread.Start();
        }
        catch (Exception e) { UnityEngine.Debug.LogWarning("Hand UDP disabled: " + e.Message); }
    }

    void Loop()
    {
        var ep = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] data = client.Receive(ref ep);
                lock (lk) { raw = Encoding.UTF8.GetString(data); lastMs = sw.ElapsedMilliseconds; }
            }
            catch (Exception) { /* socket closed or transient error */ }
        }
    }

    // Returns true if a fresh packet says a hand is visible.
    public bool TryGet(out float x, out float y, out bool open)
    {
        x = 0.5f; y = 0.5f; open = false;
        string s; long ms;
        lock (lk) { s = raw; ms = lastMs; }
        if (s == null || sw.ElapsedMilliseconds - ms > 300) return false;
        Packet p;
        try { p = JsonUtility.FromJson<Packet>(s); } catch { return false; }
        if (p == null || !p.present) return false;
        x = p.x; y = p.y; open = p.open;
        return true;
    }

    void OnDestroy()
    {
        running = false;
        try { client?.Close(); } catch { }
    }
}
