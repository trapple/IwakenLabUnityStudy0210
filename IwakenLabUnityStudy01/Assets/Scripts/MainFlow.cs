using System.Threading;
using UnityEngine;

namespace IwakenLabUnityStudy
{
    public class MainFlow : MonoBehaviour
    {
        [SerializeField] private TutorialFlow tutorialFlow;

        private CancellationTokenSource _cts;

        private async void OnEnable()
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            tutorialFlow.gameObject.SetActive(true);
            await tutorialFlow.RunTutorialAsync(_cts.Token);
            tutorialFlow.gameObject.SetActive(false);
        }

        [ContextMenu("キャンセル")]
        private void Cancel()
        {
            _cts?.Cancel();
        }
    }
}
