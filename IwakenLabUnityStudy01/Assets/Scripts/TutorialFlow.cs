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
            [SerializeField] private Vector3 ballSpawnPosition = new Vector3(0, 5, 0);

            private IDisposable _ballSpawnSubscription;
            private CancellationTokenSource _cts;

            public async UniTask ExecTutorial(CancellationToken token)
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                using var ovrWeaponDraw = new WeaponDrawSequencer(mouseInputObserver, drawWeapon);
                
                var nodes = await ovrWeaponDraw.DrawSequence(_cts.Token);
                
                text.text = "Spaceキーで剣を振ってぶった斬れ！";
                drawWeapon.gameObject.SetActive(false);

                var battleWeaponInstance = Instantiate(battleWeaponPrefab);
                battleWeaponInstance.Initialize(nodes);

                // ボールを定期的に生成
                var spawnBallSubscription = StartBallSpawning();
                
                try
                {
                    var col = await battleWeaponInstance.OnHit.FirstAsync(_cts.Token).AddTo(this);
                    Destroy(col.gameObject);
                }
                finally
                {
                    Destroy(battleWeaponInstance.gameObject);
                    spawnBallSubscription.Dispose();
                }

                text.text = "チュートリアルクリア！";
                _cts.Cancel();
            }

            private void OnEnable()
            {
                text.text = "一筆書きで剣を描け！";
            }

            private IDisposable StartBallSpawning()
            {
                _ballSpawnSubscription?.Dispose();
                var disposable = _ballSpawnSubscription = Observable
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

                var instance = Instantiate(fallingBallPrefab, spawnPos, Quaternion.identity);
                // シンプルでこっちのが一旦良い気がする。終了時全削除とか想定場合はtaskのほうが良さそう、ただ真面目にやるならObjectPoolとかにはなりそう。
                Destroy(instance, ballSpawnInterval * 2);
            }

            private void OnDisable()
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _ballSpawnSubscription?.Dispose();
            }
    }
}
