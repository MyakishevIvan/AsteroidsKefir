using System;
using Asteroids.Configs;
using Asteroids.Enemies;
using Asteroids.Enums;
using Asteroids.Player;
using Asteroids.Player.ShootSystem;
using Asteroids.Windows;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;

namespace Asteroids.BaseLogic
{
    public class SceneInstaller : MonoInstaller
    {
        [SerializeField] private Transform horizontalBounds;
        [SerializeField] private Transform verticalBounds;
        [Inject] private BalanceStorage _balanceStorage;
        [Inject] private WindowsManager _windowsManager;
        private static bool isFirstPlay = true;

        public override void InstallBindings()
        {
            InstantiateScene();
        }


        private void Awake()
        {
            if (isFirstPlay)
                OpenPromptWindow();
        }

        private void InstantiateScene()
        {
            var playerViewInstance = Container.InstantiatePrefabForComponent<PlayerView>(
                _balanceStorage.ObjectViewConfig.PlayerView,
                Vector3.zero, quaternion.identity, null);
            Container.BindInterfacesAndSelfTo<PlayerInputAction>().AsSingle().NonLazy();
            Container.Bind<PlayerView>().FromInstance(playerViewInstance).AsSingle().NonLazy();
            Container.Bind<EnemiesControlSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PlayerShootSystem>().AsSingle();
            Container.BindInterfacesAndSelfTo<BulletShootCreator>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<RayShootCreator>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PlayerMovementSystem>().AsSingle()
                .WithArguments(horizontalBounds, verticalBounds);
            InstantiatePools();
            Container.BindInterfacesAndSelfTo<EnemySpawner>().AsSingle().NonLazy();

            var playerController = Container.InstantiateComponent<PlayerController>(playerViewInstance.gameObject);
            Container.Bind<PlayerController>().FromInstance(playerController).AsSingle().NonLazy();

            _windowsManager.Open<Hud, WindowSetup.Empty>(null);
        }

        private void InstantiatePools()
        {
            Container.BindFactory<EnemyView, EnemyView.AsteroidFactory>()
                // We could just use FromMonoPoolableMemoryPool here instead, but
                // for IL2CPP to work we need our pool class to be used explicitly here
                .FromPoolableMemoryPool<EnemyView, EnemyViewPool>(poolBinder => poolBinder
                    .WithInitialSize(5)
                    .FromComponentInNewPrefab(_balanceStorage.ObjectViewConfig.GetEnemy(EnemyType.Asteroid))
                    .UnderTransformGroup("Enemies"));
            
            Container.BindFactory<EnemyView, EnemyView.AsteroidParticleFactory>()
                // We could just use FromMonoPoolableMemoryPool here instead, but
                // for IL2CPP to work we need our pool class to be used explicitly here
                .FromPoolableMemoryPool<EnemyView, EnemyViewPool>(poolBinder => poolBinder
                    .WithInitialSize(5)
                    .FromComponentInNewPrefab(_balanceStorage.ObjectViewConfig.GetEnemy(EnemyType.AsteroidParticle))
                    .UnderTransformGroup("Enemies"));
            
            Container.BindFactory<EnemyView, EnemyView.SaucerFactory>()
                // We could just use FromMonoPoolableMemoryPool here instead, but
                // for IL2CPP to work we need our pool class to be used explicitly here
                .FromPoolableMemoryPool<EnemyView, EnemyViewPool>(poolBinder => poolBinder
                    .WithInitialSize(5)
                    .FromComponentInNewPrefab(_balanceStorage.ObjectViewConfig.GetEnemy(EnemyType.Saucer))
                    .UnderTransformGroup("Enemies"));
        }

        private void OpenPromptWindow()
        {
            _windowsManager.Open<PromptWindow, PromptWindowSetup>(new PromptWindowSetup()
            {
                onOkButtonClick = () =>
                {
                    _windowsManager.Close<PromptWindow>();
                    isFirstPlay = false;
                },
                promptText = "Hold the lef mouse button to shoot bullets\nHold the right mouse button to shoot ray"
            });
        }
    }

    // We could just use FromMonoPoolableMemoryPool above, but we have to use these instead
    // for IL2CPP to work
    class EnemyViewPool : MonoPoolableMemoryPool<IMemoryPool, EnemyView>
    {
    }
}