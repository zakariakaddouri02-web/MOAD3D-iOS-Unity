using System.Runtime.InteropServices;
using UnityEngine;

public class FlashToggle : MonoBehaviour
{
    private bool isEnabled;

    public void Toggle()
    {
        isEnabled = !isEnabled;
#if UNITY_IOS && !UNITY_EDITOR
        MOADSetTorch(isEnabled ? 1 : 0);
#else
        Debug.Log("Torch control is available in an iOS device build.");
#endif
    }

    private void OnDisable()
    {
        if (isEnabled)
        {
            isEnabled = false;
#if UNITY_IOS && !UNITY_EDITOR
            MOADSetTorch(0);
#endif
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void MOADSetTorch(int enabled);
#endif
}
