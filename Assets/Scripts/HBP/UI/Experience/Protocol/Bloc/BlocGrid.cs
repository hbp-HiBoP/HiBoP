using HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class BlocGrid : Tools.Unity.Lists.CustomGrid<Bloc>
	{
        protected override Position GetPosition(Bloc obj)
        {
            Position l_position;
            l_position.Row = obj.DisplayInformations.Row;
            l_position.Col = obj.DisplayInformations.Column;
            return l_position;
        }

        protected override void SetPosition(Bloc obj, Position position)
        {
            obj.DisplayInformations.Column = position.Col;
            obj.DisplayInformations.Row = position.Row;
        }

        protected override Bloc CreateObjAtPosition(Position position)
        {
            return new Bloc(new DisplayInformations(position.Row, position.Col));
        }
    }
}
