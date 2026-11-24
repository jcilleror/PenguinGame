using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkButtonsUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartHost();
                gameObject.SetActive(false);            // Hide the buttons when connecting as a host
            });
        }

        if (clientButton != null)
        {
            clientButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartClient();
                gameObject.SetActive(false);            // Hide the buttons when connecting as a client
            });
        }
    }
}