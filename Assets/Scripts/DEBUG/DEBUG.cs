using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HBP.Data;

public class DEBUG : MonoBehaviour
{
    Patient[] patients;

    void Start()
    {
        string[] patientDirectories = Patient.GetPatientsDirectories("Y:/BrainVisaDB/Epilepsy");
        patients = new Patient[patientDirectories.Length];
        for (int i = 0; i < patientDirectories.Length; i++)
        {
            patients[i] = new Patient(patientDirectories[i]);
        }

    }

    void LoadAll()
    {
        foreach (var patient in patients)
        {
            patient.Brain.LoadImplantation(HBP.Data.Anatomy.Implantation.ReferenceFrameType.Patient, true);
        }
    }

    void DisposeAll()
    {

    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.L))
        {
            LoadAll();
        }
        else if(Input.GetKey(KeyCode.D))
        {
            DisposeAll();
        }
    }
}
