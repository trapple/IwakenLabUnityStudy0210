using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;

namespace IwakenLabUnityStudy
{
    public class TutorialFlow : MonoBehaviour
    {
        [SerializeField] private MouseInputObserver mouseInputObserver;
        [SerializeField] private DrawWeapon drawWeapon;
        [SerializeField] private TMP_Text text;
        [SerializeField] private BattleWeapon battleWeaponPrefab;
        [SerializeField] private FallingBall fallingBallPrefab;
        [SerializeField] private float ballSpawnInterval = 6f;
        [SerializeField] private Vector3 ballSpawnPosition = new(0, 5, 0);

        private BattleWeapon _battleWeaponInstance;
        private IDisposable _ballSpawnSubscription;

        public async UniTask RunTutorialAsync(CancellationToken token)
        {
            text.text = "一筆書きで剣を描け！";

            using var weaponDrawSequencer = new WeaponDrawSequencer(mouseInputObserver, drawWeapon);

            var nodes = await weaponDrawSequencer.WaitForDrawEndAsync(token);

            text.text = "Spaceキーで剣を振ってぶった斬れ！";
            drawWeapon.gameObject.SetActive(false);

            _battleWeaponInstance = Instantiate(battleWeaponPrefab);
            _battleWeaponInstance.Initialize(nodes);

            // ボールを定期的に生成
            StartBallSpawning();

            var col = await _battleWeaponInstance.OnHit
                .Where(col => col.gameObject.CompareTag("StartObject"))
                .FirstAsync(token);

            _ballSpawnSubscription?.Dispose();
            Destroy(_battleWeaponInstance.gameObject);
            text.text = "チュートリアルクリア！";
            Destroy(col.gameObject);
        }

        private void StartBallSpawning()
        {
            _ballSpawnSubscription?.Dispose();
            _ballSpawnSubscription = Observable
                .Interval(TimeSpan.FromSeconds(ballSpawnInterval))
                .Subscribe(_ => SpawnBall())
                .AddTo(this);

            // 最初のボールをすぐに生成
            SpawnBall();
        }

        private void SpawnBall()
        {
            if (fallingBallPrefab == null) return;

            // ランダムなX位置で生成
            var spawnPos = ballSpawnPosition;
            spawnPos.x += UnityEngine.Random.Range(-3f, 3f);

            Instantiate(fallingBallPrefab, spawnPos, Quaternion.identity);
        }

        private void OnDisable()
        {
            _ballSpawnSubscription?.Dispose();
        }
    }
}
