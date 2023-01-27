using Automachine.Scripts.Components;
using Automachine.Scripts.Signals;
using Backend.Scripts.Signals;
using GLShared.General.Components;
using GLShared.General.Enums;
using GLShared.General.Interfaces;
using GLShared.General.ScriptableObjects;
using GLShared.General.Signals;
using TMPro;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Components
{
    public class RandomBattleManager : AutomachineEntity<BattleStage>, IBattleManager
    {
        [Inject] private readonly ISyncManager syncManager;
        [Inject] private readonly RandomBattleParameters battleParameters;
        [Inject (Id = "battleTimer")] private readonly TextMeshProUGUI countdownText;

        [SerializeField] private bool allPlayersConnectionsEstablished = true;

        private BattleCountdownStage countdownState;
        private BattleInProgressStage inProgressState;

        private bool trackCountdown = false;
        private BattleStage currentBattleStage;

        public BattleStage CurrentBattleStage => currentBattleStage;

        public override void OnStateMachineInitialized(OnStateMachineInitialized<BattleStage> OnStateMachineInitialized)
        {
            base.OnStateMachineInitialized(OnStateMachineInitialized);

            countdownState = (BattleCountdownStage)stateMachine.GetState(BattleStage.Countdown);
            inProgressState = (BattleInProgressStage)stateMachine.GetState(BattleStage.InProgress);

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
            trackCountdown = (newState == BattleStage.Countdown || newState == BattleStage.InProgress);

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
            if (trackCountdown)
            {
                if(currentBattleStage == BattleStage.Countdown)
                {
                    var currentTimer = countdownState.CurrentIntegerTimer;

                    if (countdownState.PreviousIntegerTimer != currentTimer)
                    {
                        signalBus.Fire(new SyncSignals.OnGameCountdownUpdate()
                        {
                            CurrentCountdownValue = currentTimer,
                        });
                    }
                }
                else if(currentBattleStage == BattleStage.InProgress)
                {
                    if (inProgressState.HaveSecondsChanged)
                    {
                        signalBus.Fire(new PlayerSignals.OnBattleTimeChanged()
                        {
                            CurrentMinutesLeft = inProgressState.MinutesLeft,
                            CurrentSecondsLeft = inProgressState.SecondsLeft,
                        });

                        var seconds = inProgressState.SecondsLeft.ToString();
                        countdownText.text = $"{inProgressState.MinutesLeft}:{seconds}";
                    }
                }
            }
        }
    }
}
