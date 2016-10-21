namespace HBP.Data.Patient
{
    public class PlotID
    {
        public string Name;
        public Patient Patient;

        public PlotID(string name,Patient patient)
        {
            Name = name;
            Patient = patient;
        }
    }
}