namespace Elan
{
    public class Track
    {
        public int Measure;
        public int Channel;
        public int Frequency;

        public Track(int measure,int channel)
        {
            Measure = measure;
            Channel = channel;
        }

        public Track(int measure,int channel,int frequency) : this(measure,channel)
        {
            Frequency = frequency;
        }
    }
}
