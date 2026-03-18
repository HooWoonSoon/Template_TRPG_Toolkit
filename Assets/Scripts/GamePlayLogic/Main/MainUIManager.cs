using UnityEngine;

public class MainUIManager : MonoBehaviour
{
    public GameObject mainUIPanel;

    private bool isDeploymentPhase = false;

    private void Start()
    {
        mainUIPanel.SetActive(false);
        GameEvent.onDeploymentStart += () => isDeploymentPhase = true;
        GameEvent.onDeploymentEnd += () => isDeploymentPhase = false;
        GameEvent.onEnterDeployMap += () => isDeploymentPhase = true;
        GameEvent.onEnterMap += () => isDeploymentPhase = false;
    }

    private void Update()
    {
        if (isDeploymentPhase) { return; }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            mainUIPanel.SetActive(!mainUIPanel.activeSelf);
        }
    }
}
