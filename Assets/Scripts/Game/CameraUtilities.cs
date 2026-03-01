using UnityEngine;

namespace Game
{
    public static class CameraUtilities
    {
        public const float BaselineCameraSize = 50f;
        
        public static Vector3 SwitchCameraSpace(Vector3 worldPos, Camera fromCam, Camera toCam)
        {
            var screenPos = fromCam.WorldToScreenPoint(worldPos);
            screenPos.z = Mathf.Abs(toCam.transform.position.z);
            var newWorldPos = toCam.ScreenToWorldPoint(screenPos);
            newWorldPos.z = 0f; 
            return newWorldPos;
        }
    }
}

