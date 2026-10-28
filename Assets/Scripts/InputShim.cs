using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Works with either the old Input Manager or the new Input System package.
public static class InputShim
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    static Keyboard K => Keyboard.current;
    public static bool ZDown => K != null && K.zKey.wasPressedThisFrame;
    public static bool XDown => K != null && K.xKey.wasPressedThisFrame;
    public static bool RDown => K != null && K.rKey.wasPressedThisFrame;
    public static float Horizontal
    {
        get
        {
            if (K == null) return 0;
            float h = 0;
            if (K.leftArrowKey.isPressed || K.aKey.isPressed) h -= 1;
            if (K.rightArrowKey.isPressed || K.dKey.isPressed) h += 1;
            return h;
        }
    }
#else
    public static bool ZDown => Input.GetKeyDown(KeyCode.Z);
    public static bool XDown => Input.GetKeyDown(KeyCode.X);
    public static bool RDown => Input.GetKeyDown(KeyCode.R);
    public static float Horizontal
    {
        get
        {
            float h = 0;
            if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) h -= 1;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h += 1;
            return h;
        }
    }
#endif
}
