using UnityEngine;

namespace SmartNPC
{
    [CreateAssetMenu(fileName = "SmartNPC Connection Config", menuName = "SmartNPC/Connection Configuration", order = 0)]
    public class SmartNPCConnectionConfig : ScriptableObject
    {
        [SerializeField] public SmartNPCAPIKey APIKey;
        [SerializeField] public SmartNPCPlayerSettings Player;
        [SerializeField] public SmartNPCVoiceSettings Voice;
        [SerializeField] public SmartNPCBehaviorsSettings Behaviors;
        [SerializeField] public SmartNPCAdvancedSettings Advanced;
    }
}
