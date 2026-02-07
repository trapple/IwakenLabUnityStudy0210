using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace IwakenLabUnityStudy
{
    public class WeaponDrawSequencer : IDisposable
    {
        private readonly MouseInputObserver _mouseInput;
        private readonly DrawWeapon _weapon;

        public WeaponDrawSequencer(MouseInputObserver mouseInput, DrawWeapon weapon)
        {
            _mouseInput = mouseInput;
            _weapon = weapon;
        }

        public async UniTask<Vector3[]> WaitForDrawEndAsync(CancellationToken cancellation)
        {
            while (true)
            {
                // マウス左クリックで描画
                await _mouseInput.LeftClick
                    .Where(pressed => pressed)
                    .FirstAsync(cancellation);

                var mousePosition = _mouseInput.MouseWorldPosition.CurrentValue;
                _weapon.DrawStart(mousePosition);

                using var drowSubscription = Observable.EveryUpdate().Subscribe(_ =>
                {
                    var deltaTime = Time.deltaTime;
                    var pos = _mouseInput.MouseWorldPosition.CurrentValue;

                    _weapon.Draw(pos, deltaTime);
                });

                await _mouseInput.LeftClick
                    .Where(pressed => !pressed)
                    .FirstAsync(cancellation);

                var data = _weapon.DrawEnd();
                if (data != null)
                {
                    return data;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
