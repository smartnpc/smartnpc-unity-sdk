using UnityEngine;

namespace SmartNPC
{
    [CreateAssetMenu(fileName = "Lip Sync Config", menuName = "SmartNPC/Lip Sync Configuration", order = 1)]
    public class SmartNPCLipSyncConfig : ScriptableObject
    {
        [SerializeField] [Range(1, 100)] public int VisemeBlendRange = 1;
        [SerializeField] public SmartNPCVisemes VisemeBlendShapes = new SmartNPCVisemes();
    }
}
