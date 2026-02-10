using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace IwakenLabUnityStudy
{
    public class WeaponDrawSequencer : IDisposable
    {
        private CompositeDisposable _compositeDisposable = new();
        
        private readonly MouseInputObserver _mouseInput;
        private readonly DrawWeapon _weapon;
 
        public WeaponDrawSequencer(MouseInputObserver mouseInput, DrawWeapon weapon)
        {
            _mouseInput = mouseInput;
            _weapon = weapon;
        }

        public async UniTask<Vector3[]> DrawSequence(CancellationToken token)
        {
            await _mouseInput.LeftClick.Where(clicked => clicked == true).FirstAsync(token);

            var touchSubscription = Observable.EveryUpdate().Subscribe(_ =>
            {
                var deltaTime = Time.deltaTime;
                var pos = _mouseInput.MouseWorldPosition.CurrentValue;

                _weapon.Draw(pos, deltaTime);
            });

            await _mouseInput.LeftClick.Where(clicked => clicked == false).FirstAsync(token);
            touchSubscription.Dispose();

            var data = _weapon.DrawEnd();
            if (data == null) return await DrawSequence(token); // whileのほうがやりたいことがわかりやすいしバグも出づらそうだけど思いついたので書いてみる。
            return data;
        }

        public void Dispose()
        {
            _compositeDisposable?.Dispose();
            _compositeDisposable = null;
        }
    }
}
