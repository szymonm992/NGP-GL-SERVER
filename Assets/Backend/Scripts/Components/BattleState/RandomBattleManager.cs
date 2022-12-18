using Automachine.Scripts.Components;
using Automachine.Scripts.Signals;
using GLShared.General.Components;
using GLShared.General.Enums;
using GLShared.General.Interfaces;
using GLShared.General.ScriptableObjects;
using GLShared.General.Signals;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Components
{
    public class RandomBattleManager : AutomachineEntity<BattleStage>
    {
        [Inject] private readonly ISyncManager syncManager;
        [Inject] private readonly RandomBattleParameters battleParameters;

        [SerializeField] private bool allPlayersConnectionsEstablished = true;

        private BattleCountdownStage countdownState;

        public override void OnStateMachineInitialized(OnStateMachineInitialized<BattleStage> OnStateMachineInitialized)
        {
            countdownState = (BattleCountdownStage)stateMachine.GetState(BattleStage.Countdown);

            base.OnStateMachineInitialized(OnStateMachineInitialized);

            stateMachine.AddTransition(BattleStage.Beginning, BattleStage.Countdown
                , () => battleParameters.AreAllPlayersSpawned.Invoke(syncManager.SpawnedPlayersAmount)
                && allPlayersConnectionsEstablished, 2f);

            stateMachine.AddTransition(BattleStage.Countdown, BattleStage.InProgress,
                () => countdownState.FinishedCountdown);

            signalBus.Subscribe<OnStateEnter<BattleStage>>(OnStateEnter);
        }

        public void OnStateEnter(OnStateEnter<BattleStage> OnStateEnter)
        {
            bool lockPlayerInput = OnStateEnter.signalStateStarted != BattleStage.InProgress;
            signalBus.Fire(new PlayerSignals.OnAllPlayersInputLockUpdate()
            {
                LockPlayersInput = lockPlayerInput
            });
        }
    }
}
