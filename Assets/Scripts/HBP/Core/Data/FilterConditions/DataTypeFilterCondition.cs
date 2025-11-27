using HBP.Core.Tools;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("Data type"), SortingOrder(2), FilterCondition(typeof(DataInfo))]
    public class DataTypeFilterCondition : BaseFilterCondition
    {
        #region Properties
        [JsonProperty("DataType")] public Type Type { get; set; }

        public override string Description
        {
            get
            {
                return $"Data is{(IsNot ? " not " : " ")}of type {Type.GetDisplayName()}";
            }
        }
        #endregion

        #region Constructors
        public DataTypeFilterCondition() : this(null, false) { }
        public DataTypeFilterCondition(Type type, bool isNot) : base(isNot)
        {
            Type = type;
        }
        public DataTypeFilterCondition(Type type, bool isNot, string ID) : base(isNot, ID)
        {
            Type = type;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new DataTypeFilterCondition(Type, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is DataTypeFilterCondition dataTypeFilterCondition)
            {
                Type = dataTypeFilterCondition.Type;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(object obj)
        {
            if (obj is DataInfo dataInfo)
            {
                if (Type == null)
                    return false;
                else
                    return (dataInfo.GetType() == Type) != IsNot;
            }
            return false;
        }
        #endregion
    }
}