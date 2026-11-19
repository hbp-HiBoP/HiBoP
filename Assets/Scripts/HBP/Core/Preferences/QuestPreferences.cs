using HBP.Core.Enums;
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
        [JsonProperty] public QuestEnvironment Environment { get; set; }

        public QuestPreferences() : this(true, true, QuestEnvironment.Passthrough)
        {
        }

        public QuestPreferences(bool automaticallySynchronizeScenes, bool automaticallyConnect, QuestEnvironment environment)
        {
            AutomaticallySynchronizeScenes = automaticallySynchronizeScenes;
            AutomaticallyConnect = automaticallyConnect;
            Environment = environment;
        }

        public object Clone()
        {
            return new QuestPreferences(AutomaticallySynchronizeScenes, AutomaticallyConnect, Environment);
        }
    }
}
