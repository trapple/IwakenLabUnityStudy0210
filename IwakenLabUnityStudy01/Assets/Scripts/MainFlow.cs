using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace IwakenLabUnityStudy
{
    public class MainFlow : MonoBehaviour
    {
        [SerializeField] private TutorialFlow tutorialFlow;
        private readonly CancellationTokenSource _cts = new ();
        
        void OnEnable()
        {
            Debug.Log("TutorialFlow Enable");
            StartFlow(_cts.Token).Forget();
        }

        private async UniTaskVoid StartFlow(CancellationToken token)
        {
            tutorialFlow.gameObject.SetActive(true);
            
            await tutorialFlow.ExecTutorial(token);

            tutorialFlow.gameObject.SetActive(false);
        }
        
        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
