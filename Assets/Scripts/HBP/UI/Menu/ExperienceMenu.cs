﻿using UnityEngine;

namespace HBP.UI.Experience
{
    public class ExperienceMenu : MonoBehaviour
    {
        public void OpenProtocolGestion()
        {
            ApplicationState.WindowsManager.Open("Protocol gestion window", true);
        }

        public void OpenDatasetGestion()
        {
            ApplicationState.WindowsManager.Open("Dataset gestion window", true);
        }
    }
}