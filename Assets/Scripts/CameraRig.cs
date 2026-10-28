using UnityEngine;

// Yaw-only camera at the origin (the player). Open palm held left/right of
// the webcam centre = rotate that way. Fist or no hand = stop.
// Keyboard fallback: A/D or arrow keys.
public class CameraRig : MonoBehaviour
{
    public float Yaw;
    public float MaxRate = 110f;      // deg/sec at full deflection
    public float DeadZone = 0.08f;    // fraction of frame width around centre
    public HandInputUdp Hand;
    public bool HandTracked;          // for UI
    public bool PalmOpen;

    float rate;

    void Update()
    {
        float target = 0f;
        HandTracked = false; PalmOpen = false;

        if (Hand != null && Hand.TryGet(out float x, out float y, out bool open))
        {
            HandTracked = true; PalmOpen = open;
            if (open)
            {
                float dx = x - 0.5f, a = Mathf.Abs(dx);
                if (a > DeadZone) target = Mathf.Sign(dx) * (a - DeadZone) / (0.5f - DeadZone);
            }
        }
        float kb = InputShim.Horizontal;
        if (kb != 0) target = kb;

        rate = Mathf.Lerp(rate, target * MaxRate, 1f - Mathf.Exp(-8f * Time.deltaTime));
        Yaw += rate * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, Yaw, 0f);
    }
}
