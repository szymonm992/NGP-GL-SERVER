using Backend.Scripts.Models;
using GLShared.General.Components;
using GLShared.General.Models;
using GLShared.Networking.Components;
using Zenject;

namespace Backend.Scripts
{
    public class PlayerInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInitializableExecutionOrder<PlayerEntity>(+10);
            Container.BindInitializableExecutionOrder<UTVehicleController>(+20);

            Container.BindInterfacesAndSelfTo<UTAxleBase>().FromComponentsInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<UTPhysicWheelBase>().FromComponentsInHierarchy().AsCached().NonLazy();
        }
    }
}
