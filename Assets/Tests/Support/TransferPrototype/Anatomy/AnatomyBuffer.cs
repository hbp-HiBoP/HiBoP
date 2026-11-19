using System;

namespace HBP.Transfer.Anatomy
{
    /// <summary>
    /// GC-owned immutable numeric buffer. Adapted from RenderBuffer at eb26c323e;
    /// ownership transfer is internal so public callers cannot retain a mutable alias.
    /// </summary>
    public sealed class AnatomyBuffer<T> where T : unmanaged
    {
        private readonly T[] m_Values;

        internal AnatomyBuffer(T[] ownedValues)
        {
            m_Values = ownedValues;
        }

        public int Count => m_Values.Length;
        public T this[int index] => m_Values[index];
        public ReadOnlySpan<T> AsReadOnlySpan() => m_Values;
        public T[] ToArray() => (T[])m_Values.Clone();
    }
}
