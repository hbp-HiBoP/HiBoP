namespace Tools.CSharp
{
    public struct BlocPosition
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public BlocPosition(int row,int column)
        {
            Row = row;
            Column = column;
        }
        public override bool Equals(object obj)
        {
            if(obj is BlocPosition)
            {
                BlocPosition bloc = (BlocPosition) obj;
                if(bloc.Column == Column && bloc.Row == Row)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return Column.GetHashCode() * Row.GetHashCode();
        }
        public static bool operator ==(BlocPosition position1,BlocPosition position2)
        {
            return position1.Equals(position2);
        }
        public static bool operator !=(BlocPosition position1, BlocPosition position2)
        {
            return !position1.Equals(position2);
        }
    }
}