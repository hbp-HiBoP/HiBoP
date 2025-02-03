using HBP.Core.Interfaces;
using Newtonsoft.Json;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BaseTag : BaseData, INameable
    {
        #region Properties
        /// <summary>
        /// BaseTag Name.
        /// </summary>
        [JsonProperty(Order = 0)] public string Name { get; set; }
        #endregion

        #region Constructors
        public BaseTag() : this("New BaseTag")
        {
        }
        public BaseTag(string name) : base()
        {
            Name = name;
        }
        public BaseTag(string name, string ID) : base(ID)
        {
            Name = name;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new BaseTag(Name, ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is BaseTag tag)
            {
                Name = tag.Name;
            }
        }
        #endregion
    }
}