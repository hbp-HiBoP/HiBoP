using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    [DataContract, DisplayName("Anatomic")]
    public class AnatomicColumn : Column
    {
        #region Properties
        [DataMember] public AnatomicConfiguration AnatomicConfiguration { get; set; }
        #endregion

        #region Constructors
        public AnatomicColumn(string name, BaseConfiguration baseConfiguration, AnatomicConfiguration anantomicConfiguration, string ID) : base(name, baseConfiguration, ID)
        {
            AnatomicConfiguration = anantomicConfiguration;
        }
        public AnatomicColumn(string name, BaseConfiguration baseConfiguration, AnatomicConfiguration anantomicConfiguration) : base(name, baseConfiguration)
        {
            AnatomicConfiguration = anantomicConfiguration;
        }
        public AnatomicColumn(string name, BaseConfiguration baseConfiguration) : this(name, baseConfiguration, new AnatomicConfiguration())
        {
        }
        public AnatomicColumn() : this("", new BaseConfiguration())
        {
        }
        #endregion

        #region Public Methods
        public override void GenerateID()
        {
            base.GenerateID();
            AnatomicConfiguration.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            IDs.AddRange(AnatomicConfiguration.GetAllIdentifiable());
            return IDs;
        }
        public override object Clone()
        {
            return new AnatomicColumn(Name, BaseConfiguration.Clone() as BaseConfiguration, AnatomicConfiguration.Clone() as AnatomicConfiguration, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is AnatomicColumn anatomicColumn)
            {
                AnatomicConfiguration = anatomicColumn.AnatomicConfiguration;
            }
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