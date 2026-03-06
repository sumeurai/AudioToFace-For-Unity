using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SumeruAI.Editor
{
    public class MainWindow : UnityEditor.Editor
    {

        [MenuItem("SumeruAI/API Settings", false, 100)]
        public static void OpenAPISettingsWindow()
        {
            SettingsService.OpenProjectSettings("Project/SumeruAI/APISettingsProvider");
        }

    }
}
