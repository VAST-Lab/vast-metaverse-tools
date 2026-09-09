using UnityEngine;

namespace VastMetaverseTools.Runtime.Networking
{
    public class NetworkGameManager : MonoBehaviour
    {
        public static bool IsNetworkingReady => true; // If networking is connected
        public static PlatformType GetPlatform() => PlatformType.PC;
    }

    public enum PlatformType
    {
        Unknown,
        PC,
        Web,
        Mobile,
        Quest,
        OpenXR
    }
}