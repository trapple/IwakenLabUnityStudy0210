using UnityEngine;

namespace IwakenLabUnityStudy
{
    public class MainFlow : MonoBehaviour
    {
        [SerializeField] private TutorialFlow tutorialFlow;

        private async void OnEnable()
        {
            tutorialFlow.gameObject.SetActive(true);
            await tutorialFlow.RunTutorialAsync(destroyCancellationToken);
            tutorialFlow.gameObject.SetActive(false);
        }
    }
}
