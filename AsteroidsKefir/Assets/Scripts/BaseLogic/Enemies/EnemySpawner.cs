using System.Collections;
using System.Collections.Generic;
using Asteroids.Configs;
using Asteroids.Enemies;
using Asteroids.Enums;
using Asteroids.Helper;
using Asteroids.Signals;
using UnityEngine;
using Zenject;

public class EnemySpawner : IInitializable
{
    [Inject] private BalanceStorage _balanceStorage;
    [Inject] private DiContainer _diContainer;
    [Inject] private SignalBus _signalBus;
    [Inject] private EnemyView.AsteroidFactory _asteroidFactory;
    [Inject] private EnemyView.AsteroidParticleFactory _asteroidParticleFactory;
    [Inject] private EnemyView.SaucerFactory _saucerFactory;
    private Transform _playerTransform;


    public void Initialize()
    {
        _signalBus.Subscribe<AsteroidBlowSignal>(InstantiateAsteroidParticle);
        CoroutinesManager.StartRoutine(EnemySpawnRoutine());
    }

    private IEnumerator EnemySpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_balanceStorage.EnemiesConfig.EnemySpawnDelay);
            SpawnEnemies();
        }
    }

    private void SpawnEnemies()
    {
        for (var i = 0; i < _balanceStorage.EnemiesConfig.EnemySpawnCount; i++)
        {
            var enemyType = Random.value <= _balanceStorage.EnemiesConfig.SaucerSpawnChance
                ? EnemyType.Saucer
                : EnemyType.Asteroid;

            GetEnemyInstantiateParams(out var spawnDirection, out var spawnPoint, out var rotation);
            InstantiateEnemy(spawnPoint, rotation, spawnDirection, enemyType);
        }
    }

    private void InstantiateEnemy(Vector2 spawnPoint, Quaternion rotation, Vector2 spawnDirection,
        EnemyType enemyType)
    {
        var enemyObj = CreatEnemy(enemyType);
        enemyObj.transform.position = spawnPoint;

        var direction = rotation * -spawnDirection;
        var enemyController = _diContainer.InstantiateComponent<EnemyController>(enemyObj.gameObject);

        if (enemyType == EnemyType.Asteroid || enemyType == EnemyType.AsteroidParticle)
            enemyController.SetDirection(direction);
    }

    private EnemyView CreatEnemy(EnemyType enemyType)
    {
        EnemyView enemyView = null;
        switch (enemyType)
        {
            case EnemyType.Asteroid:
                enemyView = _asteroidFactory.Create();
                break;
            case EnemyType.AsteroidParticle:
                enemyView = _asteroidParticleFactory.Create();
                break;
            case EnemyType.Saucer:
                enemyView = _saucerFactory.Create();
                break;
        }

        return enemyView;
    }

    private void GetEnemyInstantiateParams( out Vector2 spawnDirection, out Vector2 spawnPoint,
        out Quaternion rotation)
    {
        spawnDirection = Random.insideUnitCircle.normalized;
        spawnPoint = spawnDirection * _balanceStorage.EnemiesConfig.EnemySpawnRadius;
        var variance = Random.Range(-_balanceStorage.EnemiesConfig.TrajectoryVariance,
            _balanceStorage.EnemiesConfig.TrajectoryVariance);
        rotation = Quaternion.AngleAxis(variance, Vector3.forward);
    }

    private void InstantiateAsteroidParticle(AsteroidBlowSignal signal)
    {
        for (int i = 0; i < _balanceStorage.EnemiesConfig.EnemyParticlesCount; i++)
        {
            var asteroidParticle = CreatEnemy(EnemyType.AsteroidParticle);
            Vector2 position = signal.AsteroidTransform.position;
            position += Random.insideUnitCircle;
            asteroidParticle.transform.position = position;

            var enemyController = _diContainer.InstantiateComponent<EnemyController>(asteroidParticle.gameObject);
            enemyController.SetDirection(Random.insideUnitCircle.normalized);
        }
    }
}