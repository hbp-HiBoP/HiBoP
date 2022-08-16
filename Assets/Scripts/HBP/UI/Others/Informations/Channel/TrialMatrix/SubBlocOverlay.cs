using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using data = HBP.Display.Informations.TrialMatrix;
using HBP.Core.Enums;
using HBP.Core.Tools;

namespace HBP.UI.TrialMatrix.Grid
{
    public class SubBlocOverlay : MonoBehaviour
    {
        #region Properties
        [SerializeField] Data m_Data;

        [SerializeField] Text m_DataText;
        [SerializeField] Text m_BlocText;
        [SerializeField] Text m_ChannelText;
        [SerializeField] Text m_SubBlocText;

        [SerializeField] Text m_TrialText;
        [SerializeField] Text m_WindowText;
        [SerializeField] Text m_ValueText;
        [SerializeField] Text m_LatencyText;
        [SerializeField] Text m_EventText;
        [SerializeField] Text m_EventPositionText;

        Bloc m_Bloc;
        ChannelBloc m_ChannelBloc;
        SubBloc m_SubBloc;

        RectTransform m_SubBlocRectTransform;
        const int EVENT_WIDTH = 5;
        #endregion

        #region Private Methods
        void Update()
        {
            Display();
        }
        void Awake()
        {
            m_Data.OnChangeIsHovered.AddListener((b) => UpdateHovered());
            UpdateHovered();
        }
        void UpdateHovered()
        {
            m_Bloc = m_Data.BlocHovered;
            m_ChannelBloc = m_Bloc?.ChannelBlocHovered;
            m_SubBloc = m_ChannelBloc?.SubBlocHovered;
            m_SubBlocRectTransform = m_SubBloc?.transform.GetChild(1).GetComponent<RectTransform>();
        }
        void Display()
        {
            if(m_Data != null)
            {
                m_DataText.transform.parent.gameObject.SetActive(true);
                m_DataText.text = m_Data.Title;
                if (m_Bloc != null)
                {
                    data.Grid.Bloc bloc = m_Bloc.Data;
                    m_BlocText.transform.parent.gameObject.SetActive(true);
                    m_BlocText.text = bloc.Data.Name;
                    if(m_ChannelBloc != null)
                    {
                        data.Grid.ChannelBloc channelBloc = m_ChannelBloc.Data;
                        m_ChannelText.transform.parent.gameObject.SetActive(true);
                        m_ChannelText.text = string.Format("{0} ({1})", channelBloc.Channel.Channel, channelBloc.Channel.Patient.Name);
                        if (channelBloc.IsFound && m_SubBloc != null)
                        {    
                            data.SubBloc subBloc = m_SubBloc.Data;
                            if(!subBloc.IsFiller)
                            {
                                m_SubBlocText.transform.parent.gameObject.SetActive(true);
                                m_SubBlocText.text = subBloc.SubBlocProtocol.Name;

                                data.SubTrial[] subTrials = subBloc.SubTrials;
                                Vector2 ratio = m_SubBlocRectTransform.GetRatioPosition(Input.mousePosition);
                                int trial = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(1 - ratio.y) * subTrials.Length),0, subTrials.Length - 1);                         
                                data.SubTrial subTrial = subTrials[trial];
                                m_WindowText.transform.parent.gameObject.SetActive(true);
                                Core.Data.EventInformation.EventOccurence mainEventOccurence = subTrial.Data.InformationsByEvent[subTrial.Data.InformationsByEvent.Keys.First(k => k.Type == MainSecondaryEnum.Main)].Occurences.First();
                                int startIndex = mainEventOccurence.Index - mainEventOccurence.IndexFromStart;
                                int endIndex = startIndex + subTrial.Data.Values.Length;
                                m_WindowText.text = string.Format("({0};{1})",startIndex,endIndex);

                                float[] values = subTrial.Data.Values;
                                int sample = Mathf.FloorToInt(ratio.x * values.Length);
                                float eventSemiWidth = EVENT_WIDTH / m_SubBlocRectTransform.rect.width / 2.0f; 
                                if (sample >= 0 && sample < values.Length)
                                {
                                    float value = values[sample];

                                    bool found = false;
                                    foreach (var pair in subTrial.Data.InformationsByEvent)
                                    {
                                        foreach (var occurence in pair.Value.Occurences)
                                        {
                                            float xMidle = (occurence.IndexFromStart + 0.5f) / values.Length;
                                            float xMin = xMidle - eventSemiWidth;
                                            float xMax = xMidle + eventSemiWidth;
                                            if(ratio.x >= xMin && ratio.x <= xMax)
                                            {
                                                found = true;
                                                m_EventText.text = string.Format("{0} ({1})", pair.Key.Name, occurence.Code);
                                                m_EventPositionText.text = string.Format("{0}", occurence.Index);
                                                break;
                                            }
                                        }
                                    }
                                    if(!found)
                                    {
                                        m_EventText.transform.parent.gameObject.SetActive(false);
                                        m_EventPositionText.transform.parent.gameObject.SetActive(false);
                                    }
                                    else
                                    {
                                        m_EventText.transform.parent.gameObject.SetActive(true);
                                        m_EventPositionText.transform.parent.gameObject.SetActive(true);
                                    }

                                    int percentageActivation = Mathf.RoundToInt(((value - m_Data.Limits.x)/ m_Data.Limits.Range() - 0.5f) * 200.0f);
                                    float latency = subBloc.SubBlocProtocol.Window.Start + ratio.x * subBloc.SubBlocProtocol.Window.Lenght;

                                    m_TrialText.transform.parent.gameObject.SetActive(true);
                                    m_TrialText.text = string.Format("{0}/{1}", trial + 1, subTrials.Length);

                                    m_ValueText.transform.parent.gameObject.SetActive(true);
                                    m_ValueText.text = string.Format("{0} {1} ({2}%)", value.ToString("N2"), subTrial.Data.Unit, percentageActivation.ToString());

                                    m_LatencyText.transform.parent.gameObject.SetActive(true);
                                    m_LatencyText.text = string.Format("{0} ms ({1}/{2})", latency.ToString("N2"),(sample +1),values.Length);
                                }
                                else
                                {
                                    m_TrialText.transform.parent.gameObject.SetActive(false);
                                    m_WindowText.transform.parent.gameObject.SetActive(false);
                                    m_ValueText.transform.parent.gameObject.SetActive(false);
                                    m_LatencyText.transform.parent.gameObject.SetActive(false);
                                    m_EventText.transform.parent.gameObject.SetActive(false);
                                    m_EventPositionText.transform.parent.gameObject.SetActive(false);

                                }
                            }
                            else
                            {
                                m_SubBlocText.transform.parent.gameObject.SetActive(false);
                                m_TrialText.transform.parent.gameObject.SetActive(false);
                                m_WindowText.transform.parent.gameObject.SetActive(false);
                                m_ValueText.transform.parent.gameObject.SetActive(false);
                                m_LatencyText.transform.parent.gameObject.SetActive(false);
                                m_EventText.transform.parent.gameObject.SetActive(false);
                                m_EventPositionText.transform.parent.gameObject.SetActive(false);

                            }
                        }
                        else
                        {
                            m_SubBlocText.transform.parent.gameObject.SetActive(false);
                            m_TrialText.transform.parent.gameObject.SetActive(false);
                            m_WindowText.transform.parent.gameObject.SetActive(false);
                            m_ValueText.transform.parent.gameObject.SetActive(false);
                            m_LatencyText.transform.parent.gameObject.SetActive(false);
                            m_EventText.transform.parent.gameObject.SetActive(false);
                            m_EventPositionText.transform.parent.gameObject.SetActive(false);

                        }
                    }
                    else
                    {
                        m_ChannelText.transform.parent.gameObject.SetActive(false);
                        m_SubBlocText.transform.parent.gameObject.SetActive(false);
                        m_TrialText.transform.parent.gameObject.SetActive(false);
                        m_WindowText.transform.parent.gameObject.SetActive(false);
                        m_ValueText.transform.parent.gameObject.SetActive(false);
                        m_LatencyText.transform.parent.gameObject.SetActive(false);
                        m_EventText.transform.parent.gameObject.SetActive(false);
                        m_EventPositionText.transform.parent.gameObject.SetActive(false);

                    }
                }
                else
                {
                    m_BlocText.transform.parent.gameObject.SetActive(false);
                    m_ChannelText.transform.parent.gameObject.SetActive(false);
                    m_SubBlocText.transform.parent.gameObject.SetActive(false);
                    m_TrialText.transform.parent.gameObject.SetActive(false);
                    m_WindowText.transform.parent.gameObject.SetActive(false);
                    m_ValueText.transform.parent.gameObject.SetActive(false);
                    m_LatencyText.transform.parent.gameObject.SetActive(false);
                    m_EventText.transform.parent.gameObject.SetActive(false);
                    m_EventPositionText.transform.parent.gameObject.SetActive(false);

                }
            }
            else
            {
                m_DataText.transform.parent.gameObject.SetActive(false);
                m_BlocText.transform.parent.gameObject.SetActive(false);
                m_ChannelText.transform.parent.gameObject.SetActive(false);
                m_SubBlocText.transform.parent.gameObject.SetActive(false);
                m_TrialText.transform.parent.gameObject.SetActive(false);
                m_WindowText.transform.parent.gameObject.SetActive(false);
                m_ValueText.transform.parent.gameObject.SetActive(false);
                m_LatencyText.transform.parent.gameObject.SetActive(false);
                m_EventText.transform.parent.gameObject.SetActive(false);
                m_EventPositionText.transform.parent.gameObject.SetActive(false);

            }
        }
        #endregion
    }
}