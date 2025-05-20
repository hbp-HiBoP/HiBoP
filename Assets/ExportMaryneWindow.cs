using HBP.Core.Tools;
using HBP.UI.Informations.TrialMatrix;
using HBP.UI.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Module3D;
using HBP.UI.Informations.Graphs;

namespace HBP.UI.Module3D
{
    public class ExportMaryneWindow : DialogWindow
    {
        [SerializeField] GameObject m_ExportMaryneCurveFieldPrefab;
        [SerializeField] Transform m_CurveFieldParent;
        List<ExportMaryneCurveField> m_Fields = new();
        [SerializeField] Text m_TotalNumberOfChar;
        [SerializeField] InputField m_TrialMatrixInputField;
        [SerializeField] Text m_TrialMatrixNumberOfChar;
        int m_ExportDirectoryLength;

        protected override void Initialize()
        {
            m_ExportDirectoryLength = Module3DMain.SelectedScene.GenerateExportDirectory().Length + 1;
            base.Initialize();
        }
        protected override void SetFields()
        {
            base.SetFields();
            var graph = Module3DUI.Scenes[Module3DMain.SelectedScene].GetComponentInChildren<Informations.ChannelInformations>().transform.GetComponentInChildren<Graph>();
            var curves = graph.GetDisplayedCurves();
            foreach (var curve in curves)
            {
                var field = Instantiate(m_ExportMaryneCurveFieldPrefab, m_CurveFieldParent).GetComponent<ExportMaryneCurveField>();
                field.Index = curves.IndexOf(curve);
                field.Curve = curve;
                field.BaseLength = m_ExportDirectoryLength;
                m_Fields.Add(field);
            }
            m_TrialMatrixInputField.text = Module3DUI.Scenes[Module3DMain.SelectedScene].GetComponentInChildren<TrialMatrixGrid>().ExportName;
            m_TrialMatrixInputField.onValueChanged.AddListener((value) =>
            {
                Module3DUI.Scenes[Module3DMain.SelectedScene].GetComponentInChildren<TrialMatrixGrid>().ExportName = value;
                m_TrialMatrixNumberOfChar.text = $"{m_ExportDirectoryLength + value.Length} chars";
            });
            m_TrialMatrixNumberOfChar.text = $"{m_ExportDirectoryLength + m_TrialMatrixInputField.text.Length} chars";
        }
        public override void OK()
        {
            CoroutineManager.StartSync(c_ExportMaryne());
            base.OK();
        }
        private void Update()
        {
            m_TotalNumberOfChar.text = $"{m_Fields.Sum(f => f.Curve.ExportName.Length) + m_Fields.Count - 1 + m_ExportDirectoryLength} chars";
        }


        private IEnumerator c_ExportMaryne()
        {
            yield return new WaitForEndOfFrame();

            string directory = Module3DMain.SelectedScene.GenerateExportDirectory();

            // Graph and Trial Matrix
            Informations.InformationsWrapper informations = Module3DUI.Scenes[Module3DMain.SelectedScene].GetComponentInChildren<Informations.InformationsWrapper>();
            Informations.ChannelInformations channelInformations = informations.GetComponentInChildren<Informations.ChannelInformations>();
            Informations.GridInformations gridInformations = informations.GetComponentInChildren<Informations.GridInformations>();
            if (!informations.Minimized)
            {
                if (channelInformations != null && channelInformations.isActiveAndEnabled)
                {
                    if (!Mathf.Approximately(channelInformations.GetComponent<ZoneResizer>().Ratio, 1.0f))
                    {
                        Informations.Graphs.Graph graph = channelInformations.transform.GetComponentInChildren<Informations.Graphs.Graph>();

                        List<Graph.Curve> curves = new List<Graph.Curve>();
                        List<Graph.Curve> getCurves(Graph.Curve curve)
                        {
                            List<Graph.Curve> curves = new List<Graph.Curve>();
                            if (!curve.Enabled)
                                return curves;

                            if (curve.Data != null && curve.Enabled)
                            {
                                curves.Add(curve);
                            }
                            foreach (var subCurve in curve.SubCurves)
                            {
                                curves.AddRange(getCurves(subCurve));
                            }
                            return curves;
                        }
                        foreach (var curve in graph.Curves)
                        {
                            curves.AddRange(getCurves(curve));
                        }
                        string fileName = string.Join("#", curves.Select(c => c.ExportName));

                        Texture2D graphTexture = Texture2DExtension.ScreenRectToTexture(graph.GetComponent<RectTransform>().ToScreenSpace());
                        var curvesName = graph.GetEnabledCurvesName();
                        try
                        {
                            string graphFilePath = Path.Combine(directory, $"{fileName}.png");
                            graphTexture.SaveToPNG(graphFilePath);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        try
                        {
                            string graphFilePath = Path.Combine(directory, $"{fileName}.svg");
                            using StreamWriter sw = new StreamWriter(graphFilePath);
                            sw.Write(graph.ToSVG());
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        Dictionary<string, string> curveValues = graph.ToCSV();
                        try
                        {
                            foreach (var curve in curveValues)
                            {
                                string curveFilePath = Path.Combine(directory, $"{curve.Key}.csv");
                                using StreamWriter sw = new StreamWriter(curveFilePath);
                                sw.Write(curve.Value);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                    }
                    if (!Mathf.Approximately(channelInformations.GetComponent<ZoneResizer>().Ratio, 0.0f))
                    {
                        var grid = channelInformations.GetComponentInChildren<TrialMatrixGrid>();
                        ScrollRect trialMatrixScrollRect = grid.GetComponent<ScrollRect>();
                        Sprite mask = trialMatrixScrollRect.viewport.GetComponent<Image>().sprite;
                        trialMatrixScrollRect.viewport.GetComponent<Image>().sprite = null;
                        Texture2D trialMatrixTexture;
                        if (trialMatrixScrollRect.content.rect.height > trialMatrixScrollRect.viewport.rect.height)
                        {
                            CanvasScalerHandler canvasScalerHandler = Module3DUI.Scenes[Module3DMain.SelectedScene].GetComponentInParent<CanvasScalerHandler>();
                            float scale = canvasScalerHandler.Scale;
                            trialMatrixTexture = new Texture2D((int)(trialMatrixScrollRect.content.rect.width / scale), (int)(trialMatrixScrollRect.content.rect.height / scale));
                            float step = trialMatrixScrollRect.viewport.rect.height / trialMatrixScrollRect.content.rect.height;
                            float position = 0.0f;
                            bool isFinished = false;
                            while (!isFinished)
                            {
                                if (position > 1.0f)
                                {
                                    position = 1.0f;
                                    isFinished = true;
                                }
                                trialMatrixScrollRect.verticalNormalizedPosition = position;
                                yield return new WaitForEndOfFrame();
                                Texture2D trialMatrixTextureFragment = Texture2DExtension.ScreenRectToTexture(trialMatrixScrollRect.viewport.ToScreenSpace());
                                trialMatrixTexture.SetPixels(0, (int)(position * trialMatrixTexture.height - position * trialMatrixTextureFragment.height), trialMatrixTextureFragment.width, trialMatrixTextureFragment.height, trialMatrixTextureFragment.GetPixels());
                                position += step;
                            }
                        }
                        else
                        {
                            trialMatrixTexture = Texture2DExtension.ScreenRectToTexture(trialMatrixScrollRect.content.ToScreenSpace());
                        }
                        try
                        {
                            string trialMatrixFilePath = Path.Combine(directory, $"{grid.ExportName}.png");
                            trialMatrixTexture.SaveToPNG(trialMatrixFilePath);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Screenshots could not be saved", "Please verify your rights");
                            yield break;
                        }
                        trialMatrixScrollRect.viewport.GetComponent<Image>().sprite = mask;
                    }
                }
            }
            DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Graph exported", "Graphs have been saved in " + directory);
        }
    }
}