using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class AnatomicColumn : BaseColumn
    {
        #region Properties
        [DataMember] public AnantomicConfiguration AnantomicConfiguration { get; set; }
        #endregion

        #region Constructors
        public AnatomicColumn(string name, BaseConfiguration baseConfiguration) : this(name,baseConfiguration, new AnantomicConfiguration())
        {
        }
        public AnatomicColumn(string name, BaseConfiguration BaseConfiguration, AnantomicConfiguration anantomicConfiguration) : base(name, BaseConfiguration)
        {
            AnantomicConfiguration = anantomicConfiguration;
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
        #endregion  
    }
}