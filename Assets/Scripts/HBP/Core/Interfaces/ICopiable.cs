namespace HBP.Core.Interfaces
{
    public interface ICopiable
    {
        /// <summary>
        /// Copy an instance to this instance.
        /// </summary>
        /// <param name="copy">instance to copy.</param>
        void Copy(object copy);
    }
}