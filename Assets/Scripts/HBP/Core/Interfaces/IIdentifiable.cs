namespace HBP.Core.Interfaces
{
    public interface IIdentifiable
    {
        string ID { get; set; }
        void GenerateID();
    }
}