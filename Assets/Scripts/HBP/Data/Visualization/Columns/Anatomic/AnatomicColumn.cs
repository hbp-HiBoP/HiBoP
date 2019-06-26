using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract, DisplayName("Anatomic")]
    public class AnatomicColumn : Column
    {
        #region Properties
        [DataMember] public AnantomicConfiguration AnantomicConfiguration { get; set; }
        #endregion

        #region Constructors
        public AnatomicColumn(string name, BaseConfiguration baseConfiguration) : this(name,baseConfiguration, new AnantomicConfiguration())
        {
        }
        public AnatomicColumn(string name, BaseConfiguration baseConfiguration, AnantomicConfiguration anantomicConfiguration) : base(name, baseConfiguration)
        {
            AnantomicConfiguration = anantomicConfiguration;
        }
        public AnatomicColumn() : this("", new BaseConfiguration())
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new AnatomicColumn(Name.Clone() as string, BaseConfiguration.Clone() as BaseConfiguration, AnantomicConfiguration.Clone() as AnantomicConfiguration);
        }
        public override bool IsCompatible(IEnumerable<Patient> patients)
        {
            return true;
        }
        public override void Unload()
        {
        }
        #endregion  
    }
}