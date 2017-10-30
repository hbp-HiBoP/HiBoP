using HBP.Data.Experience.Protocol;
using Tools.CSharp;

namespace HBP.UI.Experience.Protocol
{
	public class BlocGrid : Tools.Unity.Lists.CustomGrid<Bloc>
	{
        protected override Position GetPosition(Bloc obj)
        {
            return obj.Position;
        }
        protected override void SetPosition(Bloc obj, Position position)
        {
            obj.Position = position;
        }
        protected override Bloc CreateObjAtPosition(Position position)
        {
            return new Bloc(position);
        }
    }
}
