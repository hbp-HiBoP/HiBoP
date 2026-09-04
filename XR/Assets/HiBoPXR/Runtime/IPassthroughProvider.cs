namespace CRNL.HiBoP.XR.Bootstrap
{
    public interface IPassthroughProvider
    {
        bool IsAvailable { get; }

        bool IsPassthroughActive { get; }

        bool TrySetPassthrough(bool enabled);
    }
}
