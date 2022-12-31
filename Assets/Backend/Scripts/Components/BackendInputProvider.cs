using Backend.Scripts.Models;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using GLShared.General.Signals;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using GLShared.General.Utilities;

namespace Backend.Scripts.Components
{
    public class BackendInputProvider : MonoBehaviour, IInitializable, IPlayerInputProvider, IMouseActionsProvider
    {
        [Inject] private readonly SignalBus signalBus;

        private PlayerInput currentInput = null;
        private float lastVertical = 0;
        private bool lockPlayerInput = true;

        public float Vertical => currentInput.Vertical;
        public float Horizontal => currentInput.Horizontal;
        public bool Brake => currentInput.Brake;
        public float CombinedInput => AbsoluteHorizontal + AbsoluteHorizontal;
        public float SignedVertical => Vertical != 0 ? Mathf.Sign(Vertical) : 0f;
        public float SignedHorizontal => Horizontal != 0 ? Mathf.Sign(Horizontal): 0f;
        public float RawVertical => currentInput.RawVertical;
        public float AbsoluteVertical => Mathf.Abs(Vertical);
        public float AbsoluteHorizontal => Mathf.Abs(Horizontal);
        public float LastVerticalInput => lastVertical;
        public bool SnipingKey => false;
        public bool TurretLockKey => currentInput.TurretLockKey;
        public bool LockPlayerInput => lockPlayerInput;

        public Vector3 CameraTargetingPosition => currentInput.CameraTargetingPosition;

        public void Initialize()
        {
            this.currentInput = currentInput.EmptyPlayerInput();
            signalBus.Subscribe<PlayerSignals.OnAllPlayersInputLockUpdate>(OnAllPlayersInputLockUpdate);
        }

        public void SetInput(PlayerInput input)
        {
            if (currentInput != null)
            { 
                this.lastVertical = currentInput.Vertical;
            }

            this.currentInput = lockPlayerInput ? currentInput.EmptyPlayerInput() : input;
        }

        private void OnAllPlayersInputLockUpdate(PlayerSignals.OnAllPlayersInputLockUpdate OnAllPlayersInputLockUpdate)
        {
            this.lockPlayerInput = OnAllPlayersInputLockUpdate.LockPlayersInput;
        }
    }
}
