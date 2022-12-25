using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Zenject;
using System;

namespace Backend.Scripts.Components
{
    public class ConsoleRedirect : MonoBehaviour, IInitializable, IDisposable
    {
        [Inject(Id = "consoleOutputCaption")] private readonly TextMeshProUGUI consoleOutputText;

        private string currentOutput = "Copnsole output:";

        public void Initialize()
        {
            Application.logMessageReceived += RedirectLog;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= RedirectLog;
        }

        private void RedirectLog(string logString, string stackTrace, LogType type)
        {
            bool isCritical = (type == LogType.Error || type == LogType.Exception);
            string newLog = "\n <b>["+type+"]</b> <color=" + (isCritical ? "red" : "white")+ ">"+ logString + "</color>";
            currentOutput += newLog;
            consoleOutputText.text = currentOutput;
        }
    }
}
