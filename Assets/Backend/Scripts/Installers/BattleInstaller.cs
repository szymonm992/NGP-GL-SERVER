using Backend.Scripts.Signals;
using Frontend.Scripts;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using GLShared.General.ScriptableObjects;
using GLShared.General.Signals;
using GLShared.Networking.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Backend.Scripts
{
    public class BattleInstaller : MonoInstaller
    {
        [SerializeField] private RandomBattleParameters randomBattleParameters;
        [SerializeField] private VehiclesDatabase vehiclesDatabase;
        [SerializeField] private GameParameters gameParameters;

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
        }
        private void InstallNetworkComponents()
        {
            Container.Bind<GameParameters>().FromInstance(gameParameters).AsSingle();
            Container.BindInterfacesAndSelfTo<IVehiclesDatabase>().FromInstance(vehiclesDatabase).AsCached();
            Container.BindInterfacesAndSelfTo<ISyncManager>().FromComponentInHierarchy().AsCached();
        }

        private void InstallSignals()
        {
            SignalBusInstaller.Install(Container);

            //shared signals
            Container.DeclareSignal<PlayerSignals.OnPlayerInitialized>();
            Container.DeclareSignal<PlayerSignals.OnPlayerSpawned>();
            Container.DeclareSignal<PlayerSignals.OnAllPlayersInputLockUpdate>();

            //backend signals
            Container.DeclareSignal<SyncSignals.OnPlayerSpawned>();
        }
    }
}

