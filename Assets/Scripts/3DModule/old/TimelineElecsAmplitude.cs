//using UnityEngine;
//using System.Collections;


//using System.Collections.Generic;


//public class TimelineColumnsAmplitudes
//{
//    public float[] m_amplitudes = new float[1];
//    public int[] m_dims = new int[3];

//    public TimelineColumnsAmplitudes() { }


//    public int size() { if (m_amplitudes.Length == 1) { return 0; } else return m_amplitudes.Length; }

//    public void generateDummyValues(int lengthTimeline, int nbColumns, int nbElectrodes, float min, float max)
//    {
//        //m_dims[0] = lengthTimeline;
//        //m_dims[1] = nbColumns;
//        //m_dims[2] = nbElectrodes;

//        //m_amplitudes = new float[m_dims[0] * m_dims[1] * m_dims[2]];
//        //for(int ii = 0; ii < lengthTimeline*nbColumns*nbElectrodes; ++ii)
//        //{
//        //    //m_amplitudes[ii] = 50;
//        //    m_amplitudes[ii] = (float)Random.Range((int)min, (int)max);
//        //}

//        m_dims[0] = lengthTimeline;// ;
//        m_dims[1] = 1;// nbColumns;// Random.Range((int)1, (int)5); // nbColumns;// 
//        m_dims[2] = nbElectrodes;

//        //Debug.Log("-> plots : " + nbElectrodes);
//        //Debug.Log(" (float)Random.Range((int)min, (int)max) " + (float)Random.Range((int)min, (int)max));
//        //Debug.Log(" (float)Random.Range((int)min, (int)max) " + (float)Random.Range((int)min, (int)max));
//        //Debug.Log(" (float)Random.Range((int)min, (int)max) " + (float)Random.Range((int)min, (int)max));
//        //Debug.Log(" (float)Random.Range((int)min, (int)max) " + (float)Random.Range((int)min, (int)max));
//        //Debug.Log(" (float)Random.Range((int)min, (int)max) " + (float)Random.Range((int)min, (int)max));

//        m_amplitudes = new float[m_dims[0] * m_dims[1] * m_dims[2]];

//        for (int ii = 0; ii < m_dims[0]; ++ii)
//        {
//            for(int jj = 0; jj < m_dims[1]; ++jj)
//            {
//                for(int kk = 0; kk < m_dims[2]; ++kk)
//                {
//                    //float value = 100f;
//                    //if (jj == 0)
//                    //    value = 0f;
//                    //if (jj % 2 == 0)
//                    //    value = 50;
//                    //else
//                    //    value = 0;

//                    //if (ii < 50 && value != 0)
//                    //    value = 10;

//                    m_amplitudes[ii * m_dims[1] * m_dims[2] + jj * m_dims[2] + kk] =  (float)Random.Range((int)min, (int)max); // 10;// 


//                }
//            }
//        }
//    }

//    public int timelineLength() { return m_dims[0]; }

//    public int columnsNb() { return m_dims[1]; }

//    public int[] dimensions() { return m_dims; }

//    public float[] amplitudes() { return m_amplitudes; }

//}
