using Automachine.Scripts.Components;
using Automachine.Scripts.Signals;
using Backend.Scripts.Signals;
using GLShared.General.Components;
using GLShared.General.Enums;
using GLShared.General.Interfaces;
using GLShared.General.ScriptableObjects;
using GLShared.General.Signals;

using UnityEngine;
using Zenject;

namespace Backend.Scripts.Components
{
    public class RandomBattleManager : AutomachineEntity<BattleStage>, IBattleManager
    {
        [Inject] private readonly ISyncManager syncManager;
        [Inject] private readonly RandomBattleParameters battleParameters;

        [SerializeField] private bool allPlayersConnectionsEstablished = true;

        private BattleCountdownStage countdownState;
        private bool trackCountdown;
        private BattleStage currentBattleStage;

        public BattleStage CurrentBattleStage => currentBattleStage;

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
            var newState = OnStateEnter.signalStateStarted;
            currentBattleStage = newState;

            var lockPlayerInput = newState != BattleStage.InProgress;
            trackCountdown = newState == BattleStage.Countdown;

            signalBus.Fire(new PlayerSignals.OnAllPlayersInputLockUpdate()
            {
                LockPlayersInput = lockPlayerInput
            });

            signalBus.Fire(new SyncSignals.OnGameStateChanged()
            {
                CurrentGameStateIndex = (int)OnStateEnter.signalStateStarted,
            });

            
        }

        protected override void Update()
        {
            if(trackCountdown)
            {
                var currentTimer = countdownState.CurrentIntegerTimer;

                if(countdownState.PreviousIntegerTimer != currentTimer)
                {
                    signalBus.Fire(new SyncSignals.OnGameCountdownUpdate()
                    {
                        CurrentCountdownValue = currentTimer,
                    });
                }
            }
        }

    }
}
