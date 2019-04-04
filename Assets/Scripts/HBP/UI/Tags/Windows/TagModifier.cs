using HBP.Data.Tags;
using System;
using System.Globalization;
using System.Text;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class TagModifier : ItemModifier<Tag>
    {
        #region Properties
        enum Type { Empty, Boolean, Enumeration, Decimal, Integer, Text }

        // General.
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropDown;

        // EmptyTag.
        [SerializeField] GameObject m_EmptyTagOptions;

        // BoolTag.
        [SerializeField] GameObject m_BoolTagOptions;

        // EnumTag.
        [SerializeField] GameObject m_EnumTagOptions;
        [SerializeField] InputField m_EnumValuesInputField;
        string[] m_LastEnumValues = new string[0];

        // FloatTag.
        [SerializeField] GameObject m_FloatTagOptions;
        [SerializeField] Toggle m_FloatLimitedToggle;
        [SerializeField] InputField m_FloatMinInputField;
        [SerializeField] InputField m_FloatMaxInputField;
        bool m_LastFloatLimited = false;
        float m_LastFloatMin = 0.0f;
        float m_LastFloatMax = 0.0f;

        // IntTag.
        [SerializeField] GameObject m_IntTagOptions;
        [SerializeField] Toggle m_IntLimitedToggle;
        [SerializeField] InputField m_IntMinInputField;
        [SerializeField] InputField m_IntMaxInputField;
        bool m_LastIntLimited = false;
        int m_LastIntMin = 0;
        int m_LastIntMax = 0;

        // StringTag.
        [SerializeField] GameObject m_StringTagOptions;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_TypeDropDown.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            Item = ItemTemp;
            OnSave.Invoke();
            base.Close();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            // General.
            m_NameInputField.onValueChanged.AddListener(OnChangeNameHandler);
            m_TypeDropDown.onValueChanged.AddListener(OnChangeTypeHandler);

            // Enum.
            m_EnumValuesInputField.onValueChanged.AddListener(OnChangeEnumHandler);

            // Decimal.
            m_FloatLimitedToggle.onValueChanged.AddListener(OnChangeFloatLimitedHandler);
            m_FloatMinInputField.onValueChanged.AddListener(OnChangeFloatMinHandler);
            m_FloatMaxInputField.onValueChanged.AddListener(OnChangeFloatMaxHandler);

            // Integer.
            m_IntLimitedToggle.onValueChanged.AddListener(OnChangeIntLimitedHandler);
            m_IntMinInputField.onValueChanged.AddListener(OnChangeIntMinHandler);
            m_IntMaxInputField.onValueChanged.AddListener(OnChangeIntMaxHandler);
        }

        protected override void SetFields(Tag objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;

            int value = 0;
            if (objectToDisplay is EmptyTag)
            {
                value = 0;
            }
            else if (objectToDisplay is BoolTag)
            {
                value = 1;
            }
            else if (objectToDisplay is EnumTag)
            {
                value = 2;
                EnumTag enumTag = objectToDisplay as EnumTag;
                string possibleValues = string.Join(",", enumTag.PossibleValues);
                m_EnumValuesInputField.text = possibleValues;
            }
            else if (objectToDisplay is FloatTag)
            {
                FloatTag floatTag = objectToDisplay as FloatTag;
                m_FloatLimitedToggle.isOn = floatTag.Limited;
                m_FloatMinInputField.text = floatTag.Min.ToString("G", CultureInfo.InvariantCulture);
                m_FloatMaxInputField.text = floatTag.Max.ToString("G", CultureInfo.InvariantCulture);
                value = 3;
            }
            else if (objectToDisplay is IntTag)
            {
                IntTag intTag = objectToDisplay as IntTag;
                m_IntLimitedToggle.isOn = intTag.Limited;
                m_IntMinInputField.text = intTag.Min.ToString("G", CultureInfo.InvariantCulture);
                m_IntMaxInputField.text = intTag.Max.ToString("G", CultureInfo.InvariantCulture);
                value = 4;
            }
            else if (objectToDisplay is StringTag)
            {
                value = 5;
            }
            DropdownExtension.Set(m_TypeDropDown, typeof(Type), value);
        }

        protected void OnChangeNameHandler(string value)
        {
            ItemTemp.Name = value;
        }
        protected void OnChangeTypeHandler(int value)
        {
            Type type = (Type)value;
            switch (type)
            {
                case Type.Empty:
                    if (ItemTemp is BoolTag)
                    {
                        m_BoolTagOptions.SetActive(false);
                        ItemTemp = new EmptyTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is EnumTag)
                    {
                        m_EnumTagOptions.SetActive(false);
                        ItemTemp = new EmptyTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is FloatTag)
                    {
                        m_FloatTagOptions.SetActive(false);
                        ItemTemp = new EmptyTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is IntTag)
                    {
                        m_IntTagOptions.SetActive(false);
                        ItemTemp = new EmptyTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is StringTag)
                    {
                        m_StringTagOptions.SetActive(false);
                        ItemTemp = new EmptyTag(ItemTemp.Name);
                    }
                    m_EmptyTagOptions.SetActive(true);
                    break;
                case Type.Boolean:
                    if (ItemTemp is EmptyTag)
                    {
                        m_EmptyTagOptions.SetActive(false);
                        ItemTemp = new BoolTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is EnumTag)
                    {
                        m_EnumTagOptions.SetActive(false);
                        ItemTemp = new BoolTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is FloatTag)
                    {
                        m_FloatTagOptions.SetActive(false);
                        ItemTemp = new BoolTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is IntTag)
                    {
                        m_IntTagOptions.SetActive(false);
                        ItemTemp = new BoolTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is StringTag)
                    {
                        m_StringTagOptions.SetActive(false);
                        ItemTemp = new BoolTag(ItemTemp.Name);
                    }
                    m_BoolTagOptions.SetActive(true);
                    break;
                case Type.Enumeration:
                    if (ItemTemp is EmptyTag)
                    {
                        m_EmptyTagOptions.SetActive(false);
                        ItemTemp = new EnumTag(ItemTemp.Name, m_LastEnumValues);
                    }
                    else if (ItemTemp is BoolTag)
                    {
                        m_BoolTagOptions.SetActive(false);
                        ItemTemp = new EnumTag(ItemTemp.Name, m_LastEnumValues);
                    }
                    else if (ItemTemp is FloatTag)
                    {
                        m_FloatTagOptions.SetActive(false);
                        ItemTemp = new EnumTag(ItemTemp.Name, m_LastEnumValues);
                    }
                    else if (ItemTemp is IntTag)
                    {
                        m_IntTagOptions.SetActive(false);
                        ItemTemp = new EnumTag(ItemTemp.Name, m_LastEnumValues);
                    }
                    else if (ItemTemp is StringTag)
                    {
                        m_StringTagOptions.SetActive(false);
                        ItemTemp = new EnumTag(ItemTemp.Name, m_LastEnumValues);
                    }
                    m_EnumTagOptions.SetActive(true);
                    break;
                case Type.Decimal:
                    if (ItemTemp is EmptyTag)
                    {
                        m_EmptyTagOptions.SetActive(false);
                        ItemTemp = new FloatTag(ItemTemp.Name, m_LastFloatLimited, m_LastFloatMin, m_LastFloatMax);
                    }
                    else if (ItemTemp is BoolTag)
                    {
                        m_BoolTagOptions.SetActive(false);
                        ItemTemp = new FloatTag(ItemTemp.Name, m_LastFloatLimited, m_LastFloatMin, m_LastFloatMax);
                    }
                    else if (ItemTemp is EnumTag)
                    {
                        m_EnumTagOptions.SetActive(false);
                        ItemTemp = new FloatTag(ItemTemp.Name, m_LastFloatLimited, m_LastFloatMin, m_LastFloatMax);
                    }
                    else if (ItemTemp is IntTag)
                    {
                        m_IntTagOptions.SetActive(false);
                        IntTag intTag = ItemTemp as IntTag;
                        ItemTemp = new FloatTag(ItemTemp.Name, intTag.Limited, intTag.Min, intTag.Max);
                    }
                    else if (ItemTemp is StringTag)
                    {
                        m_StringTagOptions.SetActive(false);
                        ItemTemp = new FloatTag(ItemTemp.Name, m_LastFloatLimited, m_LastFloatMin, m_LastFloatMax);
                    }
                    m_FloatTagOptions.SetActive(true);
                    break;
                case Type.Integer:
                    if (ItemTemp is EmptyTag)
                    {
                        m_EmptyTagOptions.SetActive(false);
                        ItemTemp = new IntTag(ItemTemp.Name, m_LastIntLimited, m_LastIntMin, m_LastIntMax);
                    }
                    else if (ItemTemp is BoolTag)
                    {
                        m_BoolTagOptions.SetActive(false);
                        ItemTemp = new IntTag(ItemTemp.Name, m_LastIntLimited, m_LastIntMin, m_LastIntMax);
                    }
                    else if (ItemTemp is EnumTag)
                    {
                        m_EnumTagOptions.SetActive(false);
                        ItemTemp = new IntTag(ItemTemp.Name, m_LastIntLimited, m_LastIntMin, m_LastIntMax);
                    }
                    else if (ItemTemp is FloatTag)
                    {
                        m_FloatTagOptions.SetActive(false);
                        FloatTag floatTag = ItemTemp as FloatTag;
                        ItemTemp = new IntTag(ItemTemp.Name, floatTag.Limited, Convert.ToInt32(floatTag.Min), Convert.ToInt32(floatTag.Max));
                    }
                    else if (ItemTemp is StringTag)
                    {
                        m_StringTagOptions.SetActive(false);
                        ItemTemp = new IntTag(ItemTemp.Name, m_LastIntLimited, m_LastIntMin, m_LastIntMax);
                    }
                    m_IntTagOptions.SetActive(true);
                    break;
                case Type.Text:
                    if (ItemTemp is EmptyTag)
                    {
                        m_EmptyTagOptions.SetActive(false);
                        ItemTemp = new StringTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is BoolTag)
                    {
                        m_BoolTagOptions.SetActive(false);
                        ItemTemp = new StringTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is EnumTag)
                    {
                        m_EnumTagOptions.SetActive(false);
                        ItemTemp = new StringTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is FloatTag)
                    {
                        m_FloatTagOptions.SetActive(false);
                        ItemTemp = new StringTag(ItemTemp.Name);
                    }
                    else if (ItemTemp is IntTag)
                    {
                        m_IntTagOptions.SetActive(false);
                        ItemTemp = new StringTag(ItemTemp.Name);
                    }
                    m_StringTagOptions.SetActive(true);
                    break;
                default:
                    break;
            }
        }
        protected void OnChangeEnumHandler(string value)
        {
            EnumTag enumTag = ItemTemp as EnumTag;
            string[] enumValues = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            m_LastEnumValues = enumValues;
            enumTag.PossibleValues = enumValues;
        }
        protected void OnChangeFloatLimitedHandler(bool value)
        {
            FloatTag floatTag = ItemTemp as FloatTag;
            m_LastFloatLimited = value;
            floatTag.Limited = value;
        }
        protected void OnChangeFloatMinHandler(string value)
        {
            FloatTag floatTag = ItemTemp as FloatTag;
            if (NumberExtension.TryParseFloat(value, out float min))
            {
                m_LastFloatMin = min;
                floatTag.Min = min;
            }
        }
        protected void OnChangeFloatMaxHandler(string value)
        {
            FloatTag floatTag = ItemTemp as FloatTag;
            if (NumberExtension.TryParseFloat(value, out float max))
            {
                m_LastFloatMax = max;
                floatTag.Max = max;
            }
        }
        protected void OnChangeIntLimitedHandler(bool value)
        {
            IntTag intTag = ItemTemp as IntTag;
            m_LastIntLimited = value;
            intTag.Limited = value;
        }
        protected void OnChangeIntMinHandler(string value)
        {
            IntTag intTag = ItemTemp as IntTag;
            if (int.TryParse(value, out int min))
            {
                m_LastIntMin = min;
                intTag.Min = min;
            }
        }
        protected void OnChangeIntMaxHandler(string value)
        {
            IntTag intTag = ItemTemp as IntTag;
            if (int.TryParse(value, out int max))
            {
                m_LastIntMax = max;
                intTag.Max = max;
            }
        }
        #endregion
    }
}

