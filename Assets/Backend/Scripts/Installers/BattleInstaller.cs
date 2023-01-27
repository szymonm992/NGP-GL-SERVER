using Backend.Scripts.Components;
using Backend.Scripts.Models;
using Backend.Scripts.Signals;
using Frontend.Scripts;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using GLShared.General.ScriptableObjects;
using GLShared.General.Signals;
using GLShared.Networking.Components;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;

namespace Backend.Scripts
{
    public class BattleInstaller : MonoInstaller
    {
        [SerializeField] private RandomBattleParameters randomBattleParameters;
        [SerializeField] private VehiclesDatabase vehiclesDatabase;
        [SerializeField] private GameParameters gameParameters;
        [SerializeField] private TextMeshProUGUI battleTimer;

        public override void InstallBindings()
        {
            InstallSignals();
            InstallMain();
            InstallNetworkComponents();
        }

        private void InstallMain()
        {
            //PLAYER SPAWNING SHARED LOGIC========
            Container.BindInterfacesAndSelfTo<PlayerSpawner>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
            Container.Bind<PlayerProperties>().FromInstance(new PlayerProperties()).AsCached();
            Container.BindFactory<PlayerEntity, PlayerProperties, PlayerEntity, PlayerSpawner.Factory>().FromSubContainerResolve()
                .ByInstaller<PlayerSpawner.PlayerInstaller>();
            //=======================

            Container.BindInterfacesAndSelfTo<RandomBattleParameters>().FromInstance(randomBattleParameters).AsSingle();
            Container.Bind<TextMeshProUGUI>().WithId("battleTimer").FromInstance(battleTimer).AsSingle();
        }
        private void InstallNetworkComponents()
        {
            Container.Bind<SmartFoxConnection>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<GameParameters>().FromInstance(gameParameters).AsSingle();
            Container.BindInterfacesAndSelfTo<RoomManager>().FromNew().AsSingle();
            Container.BindInterfacesAndSelfTo<DetectionManager>().FromNew().AsSingle();
            Container.BindInterfacesAndSelfTo<MapManager>().FromComponentInHierarchy().AsSingle();
        }

        private void InstallSignals()
        {
            SignalBusInstaller.Install(Container);

            //shared signals
            Container.DeclareSignal<PlayerSignals.OnPlayerInitialized>();
            Container.DeclareSignal<PlayerSignals.OnPlayerSpawned>();
            Container.DeclareSignal<PlayerSignals.OnAllPlayersInputLockUpdate>();
            Container.DeclareSignal<PlayerSignals.OnPlayerDetectionStatusUpdate>();
            Container.DeclareSignal<PlayerSignals.OnPlayerShot>();
            Container.DeclareSignal<PlayerSignals.OnBattleTimeChanged>();

            //backend signals
            Container.DeclareSignal<SyncSignals.OnPlayerSpawned>();
            Container.DeclareSignal<SyncSignals.OnGameStateChanged>();
            Container.DeclareSignal<SyncSignals.OnGameCountdownUpdate>();
        }
    }
}

