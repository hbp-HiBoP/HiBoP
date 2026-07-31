using System.Collections.Generic;
using System.Linq;

namespace HBP.UI.Main
{
    public class BlocImporterData
    {
        #region Properties

        public Dictionary<int, int> OccurencesByCode { get; } = new();
        public Dictionary<int, string> BlocNamesByCode { get; } = new();
        public List<int> SelectedMainCodes { get; } = new();
        public List<int> SelectedResponseCodes { get; } = new();
        public Dictionary<int, List<int>> ResponseCodesByMainCode { get; } = new();

        public List<BlocCreationData> CreatedBlocs { get; } = new();

        #endregion

        #region Public Methods

        public void Clear()
        {
            OccurencesByCode.Clear();
            BlocNamesByCode.Clear();
            SelectedMainCodes.Clear();
            SelectedResponseCodes.Clear();
            ResponseCodesByMainCode.Clear();
            CreatedBlocs.Clear();
        }

        public void ProcessBlocNames()
        {
            CreatedBlocs.Clear();

            Dictionary<string, List<int>> codesByBlocName = new();

            foreach (var kvp in BlocNamesByCode)
            {
                int code = kvp.Key;
                string namesInput = kvp.Value;

                if (string.IsNullOrEmpty(namesInput)) continue;

                string[] names = namesInput.Split(',');
                foreach (string name in names)
                {
                    string trimmedName = name.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                    {
                        if (!codesByBlocName.ContainsKey(trimmedName))
                            codesByBlocName[trimmedName] = new List<int>();

                        codesByBlocName[trimmedName].Add(code);
                    }
                }
            }

            foreach (var kvp in codesByBlocName)
            {
                string blocName = kvp.Key;
                List<int> codes = kvp.Value;

                List<int> responseCodes = new();
                foreach (int mainCode in codes)
                {
                    if (ResponseCodesByMainCode.ContainsKey(mainCode))
                    {
                        responseCodes.AddRange(ResponseCodesByMainCode[mainCode]);
                    }
                }

                CreatedBlocs.Add(new BlocCreationData
                {
                    Name = blocName,
                    MainCodes = codes,
                    ResponseCodes = responseCodes.Distinct().ToList()
                });
            }
        }

        #endregion
    }

    public class BlocCreationData
    {
        public string Name { get; set; }
        public List<int> MainCodes { get; set; } = new();
        public List<int> ResponseCodes { get; set; } = new();
    }
}
