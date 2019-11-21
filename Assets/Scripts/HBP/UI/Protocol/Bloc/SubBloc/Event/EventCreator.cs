using p = HBP.Data.Experience.Protocol;
using Tools.Unity.Components;

namespace HBP.UI.Experience.Protocol
{
    public class EventCreator : ObjectCreator<p.Event>
    {
        public Data.Enums.MainSecondaryEnum Type;

        public override void CreateFromScratch()
        {
            OpenModifier(new p.Event(Type));
        }
    }
}