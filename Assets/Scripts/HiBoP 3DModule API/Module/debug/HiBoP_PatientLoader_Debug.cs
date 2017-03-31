
/**
 * \file    HiBoP_PatientLoader_Debug.cs
 * \author  Lance Florian
 * \date    04/05/2016
 * \brief   Define the HiBoP_PatientLoader_Debug class
 */

// system
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

// unity
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HBP.VISU3D
{
    public struct PatientPaths
    {
        public string IRM, IRMPost;
        public string imp_PRE_SB_PTS, imp_MNI_PTS, imp_POST_SB_PTS;
        public string transfo_T1Pre_to_SB, transfo_T1Post_to_SB, transfo_T1Post_to_T1Pre;
        public string head, Lhemi, Rhemi, Lwhite, Rwhite;
    }


    /// <summary>
    /// Debug editor paths structure
    /// </summary>
    [System.Serializable]
    public struct PathsEditor
    {
        public string ALL_NIFTI;
        public string ALL_GIFTI;
        public string ALL_TRANSFO;
        public string ALL_ELECTRODE;
        public string IMAGE_ICONES;
        public string MNI_NIFTI;
        public string MNI_GIFTI;
        public string MNI_TRANSFO;
    }

    /// <summary>
    /// Debug editor patient structure
    /// </summary>
    [System.Serializable]
    public struct PatientEditor { public bool pre; public string name; public string place; public int date; public string datePre; public string datePost; }

    /// <summary>
    /// Column editor data structure
    /// </summary>
    [System.Serializable]
    public struct ColumnDataEditor { public float minValue; public float maxValue; public int startLimit; public int endLimit; }

#if UNITY_EDITOR
    [CustomEditor(typeof(HiBoP_PatientLoader_Debug))]
    public class HiBoP_PatientLoader_Debug_Editor : Editor
    {
        #region DLLImport

        #endregion DLLImport

        SerializedProperty SPColumnData;
        SerializedProperty MPColumnData;
        SerializedProperty SPSceneSizeColumnData;
        SerializedProperty MPSceneSizeColumnData;
        SerializedProperty SPColumndDataActiveNb;
        SerializedProperty MPColumndDataActiveNb;

        bool showColumnsData = false;
        bool showSPColumnData = false;
        bool showMPColumnData = false;

        void OnEnable()
        {
            SPColumnData = serializedObject.FindProperty("m_SPColumnDataListEditor");
            MPColumnData = serializedObject.FindProperty("m_MPColumnDataListEditor");
            SPSceneSizeColumnData = serializedObject.FindProperty("m_SPSceneSizeColumnData");
            MPSceneSizeColumnData = serializedObject.FindProperty("m_MPSceneSizeColumnData");
            SPColumndDataActiveNb = serializedObject.FindProperty("m_SPColumndDataActiveNb");
            MPColumndDataActiveNb = serializedObject.FindProperty("m_MPColumndDataActiveNb");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

            showColumnsData = EditorGUILayout.Foldout(showColumnsData, "Column data");
            if (showColumnsData)
            {
                EditorGUI.indentLevel++;
                showSPColumnData = EditorGUILayout.Foldout(showSPColumnData, "Single Patient");
                if (showSPColumnData)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add column", new GUILayoutOption[] { GUILayout.Width(80), GUILayout.Height(20) }))
                    {
                        SPColumnData.InsertArrayElementAtIndex(SPColumnData.arraySize);
                    }
                    if (GUILayout.Button("Remove last column", new GUILayoutOption[] { GUILayout.Width(150), GUILayout.Height(20) }))
                    {
                        SPColumnData.DeleteArrayElementAtIndex(SPColumnData.arraySize - 1);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(SPColumndDataActiveNb, new GUIContent("Active columns nb  "), false);
                    EditorGUILayout.PropertyField(SPSceneSizeColumnData, new GUIContent("T "), false);

                    for (int ii = 0; ii < SPColumnData.arraySize; ++ii)
                    {
                        EditorGUILayout.PropertyField(SPColumnData.GetArrayElementAtIndex(ii), new GUIContent("Column " + ii), true);
                    }
                    EditorGUI.indentLevel--;
                }

                showMPColumnData = EditorGUILayout.Foldout(showMPColumnData, "Multi patients");
                if (showMPColumnData)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add column", new GUILayoutOption[] { GUILayout.Width(80), GUILayout.Height(20) }))
                    {
                        MPColumnData.InsertArrayElementAtIndex(MPColumnData.arraySize);
                    }
                    if (GUILayout.Button("Remove last column", new GUILayoutOption[] { GUILayout.Width(150), GUILayout.Height(20) }))
                    {
                        MPColumnData.DeleteArrayElementAtIndex(MPColumnData.arraySize - 1);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(MPColumndDataActiveNb, new GUIContent("Active columns nb  "), false);
                    EditorGUILayout.PropertyField(MPSceneSizeColumnData, new GUIContent("T "), false);

                    for (int ii = 0; ii < MPColumnData.arraySize; ++ii)
                    {
                        EditorGUILayout.PropertyField(MPColumnData.GetArrayElementAtIndex(ii), new GUIContent("Column " + ii), true);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
    }
#endif

    /// <summary>
    /// Debug module which use editor parameters to generate scenes
    /// </summary>
    public class HiBoP_PatientLoader_Debug : MonoBehaviour
    {
        public int m_columnsNb = 0; /**< number of columns */

        private HiBoP_3DModule_API m_command = null; /**< command main module */

        public List<PatientEditor> m_PatientsListEditor = null;

        public int m_SPColumndDataActiveNb = 1; 
        public int m_SPSceneSizeColumnData = 100;
        public List<ColumnDataEditor> m_SPColumnDataListEditor = null;

        public int m_MPColumndDataActiveNb = 1;
        public int m_MPSceneSizeColumnData = 100;
        public List<ColumnDataEditor> m_MPColumnDataListEditor = null;

        void Start()
        {
            m_command = transform.parent.gameObject.GetComponent<HiBoP_3DModule_API>();

            // read arguments
            string cmd = System.Environment.CommandLine;
            if (cmd.Length > 0)
            {
                //Debug.LogError(cmd);
                string[] split = cmd.Split('?');
                if (split.Length > 1)
                {
                    //Debug.LogError(split[0]);
                    //Debug.LogError(split[1]);
                    if (split[0].Contains("-debug_load"))
                        load_debug_config_file(split[1]);
                }
            }
        }

        /// <summary>
        /// Load the config debug file containing patients paths and load the scene
        /// </summary>
        public void load_debug_config_file(string path)
        {
            string fileText = File.ReadAllText(path, Encoding.UTF8);
            string[] lines = fileText.Split('\n');
            if (lines.Length < 3)
            {
                Debug.LogError("Can't load config debug config file.");
                return;
            }

            int nbPatientsToBeLoaded = int.Parse(lines[1]);
            if (nbPatientsToBeLoaded == 0)
                return;

            bool spScene = nbPatientsToBeLoaded == 1;

            List<PatientPaths> patientsPaths = new List<PatientPaths>(nbPatientsToBeLoaded);
            List<string> names = new List<string>(nbPatientsToBeLoaded);
            for(int ii = 2; ii < lines.Length; ++ii)
            {
                PatientPaths paths = new PatientPaths();
                string[] elementsLine = lines[ii].Split('|');
                names.Add(elementsLine[0]);
                paths.IRM = elementsLine[1];
                paths.imp_PRE_SB_PTS = elementsLine[2];
                paths.imp_MNI_PTS = elementsLine[3];
                paths.transfo_T1Pre_to_SB = elementsLine[4];
                paths.head = elementsLine[5];
                paths.Lhemi = elementsLine[6];
                paths.Rhemi = elementsLine[7];
                paths.Lwhite = elementsLine[8];
                paths.Rwhite = elementsLine[9];
                patientsPaths.Add(paths);                
            }

            if (spScene)
            {
                load_debug_SP_scene(names[0], patientsPaths[0], true);
                m_command.set_scenes_visibility(true, false);
            }
            else
            {
                load_debug_MP_scene(names, patientsPaths);
                m_command.set_scenes_visibility(false, true);
            }
        }

        /// <summary>
        /// Create a debug patient 
        /// </summary>
        /// <param name="patientName"></param>
        /// <param name="paths"></param>
        /// <param name="spScene"></param>
        /// <param name="pre"></param>
        /// <param name="ptsFiles"></param>
        /// <param name="id"></param>
        /// <param name="epilepsy"></param>
        /// <returns></returns>
        HBP.Data.Patient create_patient(string patientName,  PatientPaths paths, bool spScene, bool pre, List<string> ptsFiles, string id, string epilepsy)
        {            
            HBP.Data.Patient p1 = new HBP.Data.Patient();
            HBP.Data.Anatomy.Brain b1 = new HBP.Data.Anatomy.Brain();

            // dummy connectivity
            b1.PlotsConnectivity = "dummyPath";

            if (pre)
                b1.PatientReferenceFrameImplantation = paths.imp_PRE_SB_PTS;
            else
                b1.PatientReferenceFrameImplantation = paths.imp_PRE_SB_PTS; // paths.imp_POST_SB_PTS;

            b1.MNIReferenceFrameImplantation = paths.imp_MNI_PTS;

            if (spScene)
                ptsFiles.Add(b1.PatientReferenceFrameImplantation);
            else
                ptsFiles.Add(b1.MNIReferenceFrameImplantation);

            if (pre)
                b1.PreOperationMRI = paths.IRM;
            else
                b1.PreOperationMRI = paths.IRMPost;

            b1.LeftCerebralHemisphereMesh = paths.Lhemi;
            b1.RightCerebralHemisphereMesh = paths.Rhemi;

            if (pre)
                b1.PreOperationReferenceFrameToScannerReferenceFrameTransformation = paths.transfo_T1Pre_to_SB;
            else
                b1.PreOperationReferenceFrameToScannerReferenceFrameTransformation = paths.transfo_T1Post_to_T1Pre; // !!!!

            string[] split = patientName.Split('_');

            p1.Brain = b1;
            p1.ID = id;
            p1.Place = split[0];
            p1.Date = Int32.Parse(split[1]);
            p1.Name = split[2];

            return p1;
        }


        /// <summary>
        /// Create a debug column data from a scene
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="size"></param>
        /// <param name="startLimit"></param>
        /// <param name="endLimit"></param>
        /// <param name="posStart"></param>
        /// <param name="posEnd"></param>
        /// <param name="plotsNb"></param>
        /// <returns></returns>
        HBP.Data.Visualisation.ColumnData create_column_data(float minValue, float maxValue, int size, int startLimit, int endLimit, int posStart, int posEnd, DLL.PatientElectrodesList patientsSites)
        {
            HBP.Data.Visualisation.ColumnData c1 = new HBP.Data.Visualisation.ColumnData();
            c1.Label = "Dummy column data " + (m_columnsNb++);

            HBP.Data.Visualisation.Event em1 = new HBP.Data.Visualisation.Event("mainEvent", 50);
            List<HBP.Data.Visualisation.Event> es1 = new List<HBP.Data.Visualisation.Event>();
            es1.Add(new HBP.Data.Visualisation.Event("secEvent1", (int)(size * 0.3f)));
            es1.Add(new HBP.Data.Visualisation.Event("secEvent2", (int)(size * 0.4f)));
            es1.Add(new HBP.Data.Visualisation.Event("secEvent3", (int)(size * 0.7f)));

            // check min
            HBP.Data.Visualisation.Limit minLimit = new Data.Visualisation.Limit(minValue, minValue, "ms", posStart);
            HBP.Data.Visualisation.Limit maxLimit = new Data.Visualisation.Limit(maxValue, maxValue, "ms", posEnd);
            c1.TimeLine = new HBP.Data.Visualisation.TimeLine(minLimit, maxLimit, size, (maxValue - minValue) / size, em1, es1.ToArray());

            c1.PlotMask = new bool[patientsSites.total_sites_nb()];
            for (int ii = 0; ii < c1.PlotMask.Length; ++ii)
            {
                c1.PlotMask[ii] = false; // mask from the main UI
            }            

            string baseIConesDir = GlobalPaths.Data + "debug/dummy_icones/";
            c1.IconicScenario = new HBP.Data.Visualisation.IconicScenario();
            List<HBP.Data.Visualisation.Icon> ic1 = new List<HBP.Data.Visualisation.Icon>();
            ic1.Add(new HBP.Data.Visualisation.Icon());
            ic1[0].StartPosition = 0;
            ic1[0].EndPosition = 6;
            ic1[0].Label = "Car";
            ic1[0].IllustrationPath = baseIConesDir + "car.png";
            ic1.Add(new HBP.Data.Visualisation.Icon());
            ic1[1].StartPosition = 7;
            ic1[1].EndPosition = 8;
            ic1[1].Label = "Computer";
            ic1[1].IllustrationPath = baseIConesDir + "computer.png";
            ic1.Add(new HBP.Data.Visualisation.Icon());
            ic1[2].StartPosition = 9;
            ic1[2].EndPosition = 11;
            ic1[2].Label = "Test label";
            ic1[2].IllustrationPath = "";
            c1.IconicScenario.SetIcon(ic1.ToArray());

            Vector3 point00 = transform.TransformPoint(new Vector3(minValue, minValue));
            Vector3 point10 = transform.TransformPoint(new Vector3(maxValue, minValue));
            Vector3 point01 = transform.TransformPoint(new Vector3(minValue, maxValue));
            Vector3 point11 = transform.TransformPoint(new Vector3(maxValue, maxValue));

            //Gradient coloring;
            float frequency = 1f;
            int dimensions = 2;
            int octaves = 1;
            float lacunarity = 2f;
            float persistence = 0.5f;

            NoiseMethod method = Noise.methods[(int)NoiseMethodType.Perlin][dimensions - 1];

            c1.Values = new float[c1.PlotMask.Length][];
            int resolution1 = c1.Values.Length;
            float stepSize1 = 1f / resolution1;
            for (int ii = 0; ii < c1.Values.Length; ++ii)
            {
                Vector3 point0 = Vector3.Lerp(point00, point01, (ii + 0.5f) * stepSize1);
                Vector3 point1 = Vector3.Lerp(point10, point11, (ii + 0.5f) * stepSize1);

                c1.Values[ii] = new float[size];
                for (int jj = 0; jj < c1.Values[ii].Length; ++jj)
                {
                    int resolition2 = c1.Values[ii].Length;
                    float stepSize2 = 1f / resolition2;

                    Vector3 point = Vector3.Lerp(point0, point1, (jj + 0.5f) * stepSize2);
                    float sample = Noise.Sum(method, point, frequency, octaves, lacunarity, persistence).value;
                    sample = sample * 0.5f + 0.5f;
                    c1.Values[ii][jj] = sample;
                }
            }

            return c1;
        }

        /// <summary>
        /// Load a debug SP scene
        /// </summary>
        /// <param name="patientName"></param>
        /// <param name="paths"></param>
        /// <param name="pre"></param>
        void load_debug_SP_scene(string patientName, PatientPaths paths, bool pre)
        {
            // create patient
            List<string> ptsFilesSP = new List<string>(), namePatient = new List<string>();

            HBP.Data.Patient patient = create_patient(patientName, paths, true, pre, ptsFilesSP,  "0", "IGE");
            namePatient.Add(patient.Name);

            // count plots 
            DLL.PatientElectrodesList plotsSP = new DLL.PatientElectrodesList();
            plotsSP.load_pts_files(ptsFilesSP, namePatient, GlobalGOPreloaded.MarsAtlasIndex);

            // create columns data
            List<HBP.Data.Visualisation.ColumnData> columnsDataSP = new List<HBP.Data.Visualisation.ColumnData>();

            float min = float.MaxValue;
            float max = float.MinValue;
            for (int ii = 0; ii < m_SPColumnDataListEditor.Count; ++ii)
            {
                if (min > m_SPColumnDataListEditor[ii].startLimit)
                    min = m_SPColumnDataListEditor[ii].startLimit;
                if (max < m_SPColumnDataListEditor[ii].endLimit)
                    max = m_SPColumnDataListEditor[ii].endLimit;
            }
            float diff = max - min;

            for (int ii = 0; ii < m_SPColumndDataActiveNb; ++ii)
            {
                int posStart = (int)((m_SPColumnDataListEditor[ii].startLimit - min) / diff * m_SPSceneSizeColumnData);
                int posEnd = (int)((m_SPColumnDataListEditor[ii].endLimit - min) / diff * m_SPSceneSizeColumnData);

                columnsDataSP.Add(create_column_data(m_SPColumnDataListEditor[ii].minValue, m_SPColumnDataListEditor[ii].maxValue, m_SPSceneSizeColumnData,
                    m_SPColumnDataListEditor[ii].startLimit, m_SPColumnDataListEditor[ii].endLimit, posStart, posEnd, plotsSP));
            }

            // load multi patients scene
            m_command.set_scene_data(new HBP.Data.Visualisation.SinglePatientVisualisationData(patient, columnsDataSP.ToArray()));
        }

        /// <summary>
        /// Load a debug MP scene
        /// </summary>
        /// <param name="patientsNames"></param>
        /// <param name="patientsPaths"></param>
        void load_debug_MP_scene(List<string> patientsNames, List<PatientPaths> patientsPaths)
        {
            // create patients list
            List<HBP.Data.Patient> patients = new List<HBP.Data.Patient>(patientsNames.Count);
            List<string> ptsFilesMP = new List<string>(), namePatients = new List<string>(patientsNames.Count);
            for (int ii = 0; ii < patientsNames.Count; ++ii)
                patients.Add(create_patient(patientsNames[ii], patientsPaths[ii], false, true, ptsFilesMP, "0", "IGE"));

            // count plots
            DLL.PatientElectrodesList plotsMP = new DLL.PatientElectrodesList();
            plotsMP.load_pts_files(ptsFilesMP, namePatients, GlobalGOPreloaded.MarsAtlasIndex);

            // create columns data
            List<HBP.Data.Visualisation.ColumnData> columsnDataMP = new List<HBP.Data.Visualisation.ColumnData>();

            float min = float.MaxValue;
            float max = float.MinValue;
            for (int ii = 0; ii < m_MPColumnDataListEditor.Count; ++ii)
            {
                if (min > m_MPColumnDataListEditor[ii].startLimit)
                    min = m_MPColumnDataListEditor[ii].startLimit;
                if (max < m_MPColumnDataListEditor[ii].endLimit)
                    max = m_MPColumnDataListEditor[ii].endLimit;
            }
            float diff = max - min;

            for (int ii = 0; ii < m_MPColumndDataActiveNb; ++ii)
            {
                int posStart = (int)((m_MPColumnDataListEditor[ii].startLimit - min) / diff * m_MPSceneSizeColumnData);
                int posEnd = (int)((m_MPColumnDataListEditor[ii].endLimit - min) / diff * m_MPSceneSizeColumnData);
                columsnDataMP.Add(create_column_data(m_MPColumnDataListEditor[ii].minValue, m_MPColumnDataListEditor[ii].maxValue, m_MPSceneSizeColumnData,
                    m_MPColumnDataListEditor[ii].startLimit, m_MPColumnDataListEditor[ii].endLimit, posStart, posEnd, plotsMP));
            }

            // load multi patients scene
            m_command.set_scene_data(new HBP.Data.Visualisation.MultiPatientsVisualisationData(patients.ToArray(), columsnDataMP.ToArray()));

        } 
    }    
}