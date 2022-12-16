using Frontend.Scripts;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using GLShared.General.ScriptableObjects;
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

        public override void InstallBindings()
        {
            InstallMain();
            InstallNetworkComponents();
        }

        private void InstallMain()
        {
            SignalBusInstaller.Install(Container);

            Container.BindInterfacesAndSelfTo<PlayerSpawner>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
            Container.Bind<PlayerProperties>().FromInstance(new PlayerProperties()).AsCached();
            Container.BindFactory<PlayerEntity, PlayerProperties, PlayerEntity, PlayerSpawner.Factory>().FromSubContainerResolve().ByInstaller<PlayerSpawner.PlayerInstaller>();

            Container.BindInterfacesAndSelfTo<RandomBattleParameters>().FromInstance(randomBattleParameters).AsSingle();
        }
        private void InstallNetworkComponents()
        {
            Container.BindInterfacesAndSelfTo<IVehiclesDatabase>().FromInstance(vehiclesDatabase).AsCached();
            Container.BindInterfacesAndSelfTo<ISyncManager>().FromComponentInHierarchy().AsCached();
        }
    }
}

