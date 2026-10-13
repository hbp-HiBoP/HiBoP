using Newtonsoft.Json;
using System;
using UnityEngine.Scripting;

namespace HBP.Core.Preferences
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class QuestPreferences : ICloneable
    {
        [JsonProperty] public bool AutomaticallySynchronizeScenes { get; set; }

        public QuestPreferences() : this(true)
        {
        }

        public QuestPreferences(bool automaticallySynchronizeScenes)
        {
            AutomaticallySynchronizeScenes = automaticallySynchronizeScenes;
        }

        public object Clone()
        {
            return new QuestPreferences(AutomaticallySynchronizeScenes);
        }
    }
}
