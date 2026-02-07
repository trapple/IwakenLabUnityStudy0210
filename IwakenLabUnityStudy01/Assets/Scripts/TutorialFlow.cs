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

        private CancellationTokenSource _cts;
        private BattleWeapon _battleWeaponInstance;

        public async UniTask RunTutorialAsync(CancellationToken token)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            text.text = "一筆書きで剣を描け！";

            using var weaponDrawSequencer = new WeaponDrawSequencer(mouseInputObserver, drawWeapon);

            var nodes = await weaponDrawSequencer.WaitForDrawEndAsync(_cts.Token);

            text.text = "Spaceキーで剣を振ってぶった斬れ！";
            drawWeapon.gameObject.SetActive(false);

            _battleWeaponInstance = Instantiate(battleWeaponPrefab);
            _battleWeaponInstance.Initialize(nodes);

            // ボールを定期的に生成
            using var ballSpawning = StartBallSpawning();

            try
            {
                var col = await _battleWeaponInstance.OnHit
                    .Where(col => col.gameObject.CompareTag("StartObject"))
                    .FirstAsync(_cts.Token);
                Destroy(col.gameObject);
            }
            finally
            {
                Destroy(_battleWeaponInstance.gameObject);
            }
            text.text = "チュートリアルクリア！";
        }

        private IDisposable StartBallSpawning()
        {
            var disposable = Observable
                .Interval(TimeSpan.FromSeconds(ballSpawnInterval))
                .Subscribe(_ => SpawnBall())
                .AddTo(this);

            // 最初のボールをすぐに生成
            SpawnBall();
            return disposable;
        }

        private void SpawnBall()
        {
            if (fallingBallPrefab == null) return;

            // ランダムなX位置で生成
            var spawnPos = ballSpawnPosition;
            spawnPos.x += UnityEngine.Random.Range(-3f, 3f);

            Instantiate(fallingBallPrefab, spawnPos, Quaternion.identity);
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
            if (_battleWeaponInstance != null)
            {
                Destroy(_battleWeaponInstance.gameObject);
            }
        }
    }
}
