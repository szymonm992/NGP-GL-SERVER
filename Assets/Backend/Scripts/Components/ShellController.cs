using GLShared.General.Interfaces;
using GLShared.Networking.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Components
{
    public class ShellController : MonoBehaviour, IShellController
    {
        [Inject] private readonly ShellEntity shellEntity;

        public string OwnerUsername => shellEntity.Properties.Username;

        public void Initialize()
        {
            Debug.Log("shell initialized");
        }

       
    }
}
