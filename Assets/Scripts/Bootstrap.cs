using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the whole game at startup. Just press Play in any scene (an empty one is fine).
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindFirstObjectByType<RhythmGame>() != null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.transform.position = Vector3.zero;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.06f);
        cam.fieldOfView = 60f;

        var rig = cam.GetComponent<CameraRig>();
        if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();

        var g = new GameObject("RhythmGame");
        var hand = g.AddComponent<HandInputUdp>();
        rig.Hand = hand;
        var game = g.AddComponent<RhythmGame>();
        game.rig = rig; game.hand = hand;
    }
}