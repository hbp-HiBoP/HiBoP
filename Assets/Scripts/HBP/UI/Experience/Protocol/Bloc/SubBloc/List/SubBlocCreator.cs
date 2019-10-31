using HBP.Data.Experience.Protocol;
using Tools.Unity.Components;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocCreator : ObjectCreator<SubBloc>
    {
        public Data.Enums.MainSecondaryEnum Type;

        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            //SubBloc item = m_Objects.Any((e) => e.Type == Data.Enums.MainSecondaryEnum.Main) ? new SubBloc(Data.Enums.MainSecondaryEnum.Secondary) : new SubBloc(Data.Enums.MainSecondaryEnum.Main);
            SubBloc item = new SubBloc(Type);
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item);
                    break;
                case Data.Enums.CreationType.FromExistingItem:
                    OpenSelector(ExistingItems);
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item))
                    {
                        OpenModifier(item);
                    }
                    break;
            }
        }
    }
}