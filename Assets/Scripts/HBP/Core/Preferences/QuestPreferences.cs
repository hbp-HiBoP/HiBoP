using Newtonsoft.Json;
using System;
using UnityEngine.Scripting;

namespace HBP.Core.Preferences
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class QuestPreferences : ICloneable
    {
        [JsonProperty] public bool AutomaticallySynchronizeScenes { get; set; }
        [JsonProperty] public bool AutomaticallyConnect { get; set; }

        public QuestPreferences() : this(true, true)
        {
        }

        public QuestPreferences(bool automaticallySynchronizeScenes, bool automaticallyConnect)
        {
            AutomaticallySynchronizeScenes = automaticallySynchronizeScenes;
            AutomaticallyConnect = automaticallyConnect;
        }

        public object Clone()
        {
            return new QuestPreferences(AutomaticallySynchronizeScenes, AutomaticallyConnect);
        }
    }
}
