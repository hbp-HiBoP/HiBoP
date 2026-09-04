using System;

namespace CRNL.HiBoP.RenderModel
{
    /// <summary>
    /// Read-only buffer whose creation states explicitly whether the source is copied or transferred.
    /// V1 buffers are GC-owned and are never returned to a pool.
    /// </summary>
    public sealed class RenderBuffer<T> where T : struct
    {
        private readonly T[] m_Values;

        private RenderBuffer(T[] values)
        {
            m_Values = values;
        }

        public int Count => m_Values.Length;

        public T this[int index] => m_Values[index];

        public static RenderBuffer<T> CopyFrom(T[] source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new RenderBuffer<T>((T[])source.Clone());
        }

        public static RenderBuffer<T> CopyFrom(ReadOnlySpan<T> source)
        {
            return new RenderBuffer<T>(source.ToArray());
        }

        /// <summary>
        /// Transfers a freshly-created array to the buffer without copying it. The caller must not retain
        /// or mutate the array after this call.
        /// </summary>
        public static RenderBuffer<T> TakeOwnership(T[] source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new RenderBuffer<T>(source);
        }

        public T[] ToArray()
        {
            return (T[])m_Values.Clone();
        }
    }
}
