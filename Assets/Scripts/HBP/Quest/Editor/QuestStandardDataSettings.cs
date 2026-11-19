using UnityEngine;

namespace HBP.Quest.Editor
{
    [CreateAssetMenu(menuName = "HiBoP/Quest scientific data", fileName = "QuestStandardData")]
    public sealed class QuestStandardDataSettings : ScriptableObject
    {
        [Tooltip("Include the installed localizer files in Android builds. Save this asset at Assets/Settings/QuestStandardData.asset.")] public bool IncludeLocalizers;
    }
}
