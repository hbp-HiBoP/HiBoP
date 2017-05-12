using System.Runtime.Serialization;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class RegionOfInterest : ICloneable
    {
        #region Properties
        string m_Name;
        [DataMember]
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
                OnChangeName.Invoke();
            }
        }
        [IgnoreDataMember]
        public UnityEvent OnChangeName { get; set; }

        List<Volume> m_Volumes;
        [DataMember]
        public ReadOnlyCollection<Volume> Volumes
        {
            get
            {
                return new ReadOnlyCollection<Volume>(m_Volumes);
            }
        }
        [IgnoreDataMember]
        public UnityEvent OnAddVolume { get; set; }
        [IgnoreDataMember]
        public UnityEvent OnRemoveVolume { get; set; }
        #endregion

        #region Constructors
        public RegionOfInterest(string name, IEnumerable<Volume> volumes)
        {
            OnChangeName = new UnityEvent();
            OnAddVolume = new UnityEvent();
            OnRemoveVolume = new UnityEvent();
            Name = name;
            AddVolume(volumes);
        }
        public RegionOfInterest(string name) : this(name, new Collection<Volume>())
        {
        }
        #endregion

        #region Public Methods
        public void AddVolume(IEnumerable<Volume> volumes)
        {
            foreach (var volume in volumes) AddVolume(volume);
        }
        public void AddVolume(Volume volume)
        {
            m_Volumes.Add(volume);
            OnAddVolume.Invoke();
        }
        public void RemoveVolume(IEnumerable<Volume> volumes)
        {
            foreach (var volume in volumes) RemoveVolume(volume);
        }
        public void RemoveVolume(Volume volume)
        {
            m_Volumes.Remove(volume);
            OnRemoveVolume.Invoke();
        }
        public object Clone()
        {
            IEnumerable<Volume> volumes = from volume in Volumes select volume.Clone() as Volume;
            return new RegionOfInterest(Name.Clone() as string, volumes);
        }
        #endregion
    }
}