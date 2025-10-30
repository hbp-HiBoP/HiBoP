using HBP.Core.Exceptions;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which contains all the data about a experience protocol used to epoch, and visualize data.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Name</b></term> 
    /// <description>Name of the protocol.</description>
    /// </item>
    /// <item>
    /// <term><b>Blocs</b></term> 
    /// <description>Blocs of the protocol.</description>
    /// </item>
    /// </list>
    /// </remarks>
	[JsonObject(MemberSerialization.OptIn), Preserve]
    public class Protocol : BaseData, ILoadable<Protocol>, INameable
    {
        #region Properties
        /// <summary>
        /// Protocol file extension.
        /// </summary>
        public const string EXTENSION = ".prov";
        /// <summary>
        /// Name of the protocol.
        /// </summary>
        [JsonProperty] public string Name { get; set; }
        /// <summary>
        /// Blocs of the protocol.
        /// </summary>
        [JsonProperty] public List<Bloc> Blocs { get; set; }
        /// <summary>
        /// Blocs ordered by Bloc.Order.
        /// </summary>
        public IOrderedEnumerable<Bloc> OrderedBlocs
        {
            get
            {
                return Blocs.OrderBy(s => s.Order).ThenBy(s => s.Name);
            }
        }
        /// <summary>
        /// Return True if the protocol is visualizable, False otherwise.
        /// </summary>
        public bool IsVisualizable
        {
            get
            {
                return Blocs.Count > 0 && Blocs.All(b => b.IsVisualizable);
            }
        }
        public bool IsAdvanced
        {
            get
            {
                if (Blocs.Count == 0) return false;
                if (Blocs.Any(b => b.MainSubBloc == null)) return true;
                if (Blocs.Any(b => b.SubBlocs.Count > 1)) return true;
                if (Blocs.Any(b => b.MainSubBloc.MainEvent == null)) return true;
                if (Blocs.Any(b => b.MainSubBloc.SecondaryEvents.Count > 1)) return true;
                if (Blocs.Any(b => b.MainSubBloc.Window != Blocs[0].MainSubBloc.Window)) return true;
                if (Blocs.Any(b => b.MainSubBloc.Baseline != Blocs[0].MainSubBloc.Baseline)) return true;
                if (Blocs.Where(b => b.MainSubBloc.SecondaryEvents.Count == 1).Any(b => b.Sort != $"{b.MainSubBloc.Name}_{b.MainSubBloc.SecondaryEvents[0].Name}_LATENCY;{b.MainSubBloc.Name}_{b.MainSubBloc.MainEvent.Name}_CODE")) return true;
                if (Blocs.Where(b => b.MainSubBloc.SecondaryEvents.Count == 0).Any(b => b.Sort != $"{b.MainSubBloc.Name}_{b.MainSubBloc.MainEvent.Name}_CODE")) return true;
                return false;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new protocol instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="blocs">Blocs</param>
        /// <param name="ID">Unique identifier</param>
        public Protocol(string name,IEnumerable<Bloc> blocs,string ID) : base(ID)
        {
            Name = name;
            Blocs = blocs.ToList();
        }
        /// <summary>
        /// Create a new protocol.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="blocs">Blocs</param>
        public Protocol(string name, IEnumerable<Bloc> blocs) : base()
        {
            Name = name;
            Blocs = blocs.ToList();
        }
        /// <summary>
        /// Create a new protocol instance with default values.
        /// </summary>
        public Protocol() : this("New protocol",new List<Bloc>())
		{
        }
        #endregion

        #region Public Methods
        public void SetBasicProtocolFeatures()
        {
            foreach (var bloc in Blocs)
            {
                if (bloc.MainSubBloc == null)
                    bloc.SubBlocs.Add(new SubBloc() { Name = "Main" });

                if (bloc.MainSubBloc.MainEvent == null)
                    bloc.MainSubBloc.Events.Add(new Event(Enums.MainSecondaryEnum.Main) { Name = bloc.Name });

                if (bloc.MainSubBloc.SecondaryEvents.Count > 1)
                    bloc.MainSubBloc.Events.RemoveAll(e => e.Type == Enums.MainSecondaryEnum.Secondary && e != bloc.MainSubBloc.SecondaryEvents[0]);

                if (bloc.SecondarySubBlocs.Count > 0)
                    foreach (var subBloc in bloc.SecondarySubBlocs)
                        bloc.SubBlocs.Remove(subBloc);

                if (bloc.MainSubBloc.SecondaryEvents.Count == 1)
                    bloc.Sort = $"{bloc.MainSubBloc.Name}_{bloc.MainSubBloc.SecondaryEvents[0].Name}_LATENCY;{bloc.MainSubBloc.Name}_{bloc.MainSubBloc.MainEvent.Name}_CODE";
                else
                    bloc.Sort = $"{bloc.MainSubBloc.Name}_{bloc.MainSubBloc.MainEvent.Name}_CODE";
            }
        }
        /// <summary>
        /// Generates new unique identifier recursively.
        /// </summary>
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var bloc in Blocs) bloc.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var bloc in Blocs) IDs.AddRange(bloc.GetAllIdentifiable());
            return IDs;
        }
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Get all the protocol file extensions.
        /// </summary>
        /// <returns>Array of extensions</returns>
        public static string[] GetExtensions()
        {
            return new string[] { EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION };
        }
        /// <summary>
        /// Load a protocol from a protocol file.
        /// </summary>
        /// <param name="path">Path to the protocol file</param>
        /// <param name="result">Protocol loaded</param>
        /// <returns>True if is ok, False otherwise</returns>
        public static bool LoadFromFile(string path, out Protocol result)
        {
            result = null;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Protocol>(path);
                if (result != null) return true;
                else return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadProtocolFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        #endregion

        #region Operator
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Instance cloned.</returns>
        public override object Clone()
        {
            return new Protocol(Name, Blocs.DeepClone(), ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">Instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is Protocol protocol)
            {
                Name = protocol.Name;
                Blocs = protocol.Blocs;
            }
        }
        #endregion

        #region Interfaces
        /// <summary>
        /// Get all the protocol file extensions.
        /// </summary>
        /// <returns>Array of extensions</returns>
        string[] ILoadable<Protocol>.GetExtensions()
        {
            return GetExtensions();
        }
        /// <summary>
        /// Load a protocol from a protocol file.
        /// </summary>
        /// <param name="path">Path to the protocol file</param>
        /// <param name="result">Protocol loaded</param>
        /// <returns>True if is ok, False otherwise</returns>
        bool ILoadable<Protocol>.LoadFromFile(string path, out Protocol[] result)
        {
            bool success = LoadFromFile(path, out Protocol protocol);
            result = new Protocol[] { protocol };
            return success;
        }
        #endregion
    }
}