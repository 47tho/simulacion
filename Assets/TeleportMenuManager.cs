using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class TeleportMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;

    [Header("Teleport Settings")]
    [SerializeField] private GameObject player;
    
    private InputAction menuAction;
    private bool isMenuOpen = false;
    private CharacterController playerController;

    [System.Serializable]
    public struct TeleportLocation
    {
        public string buildingName;
        public string spawnPointName;
    }

    [SerializeField] private List<TeleportLocation> locations = new List<TeleportLocation>();

    private void Awake()
    {
        menuAction = InputSystem.actions.FindAction("Menu");
        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
        }
        
        // Hide menu on start
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void Start()
    {
        PopulateMenu();
    }

    private void Update()
    {
        if (menuAction != null && menuAction.WasPressedThisFrame())
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        
        if (menuPanel != null)
        {
            menuPanel.SetActive(isMenuOpen);
        }

        if (isMenuOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void PopulateMenu()
    {
        Debug.Log("[TeleportMenu] PopulateMenu called. Locations: " + locations.Count);
        if (buttonContainer == null || buttonPrefab == null) 
        {
            Debug.LogError("[TeleportMenu] Missing container or prefab reference!");
            return;
        }

        // Clear existing buttons
        int childCount = buttonContainer.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Destroy(buttonContainer.GetChild(i).gameObject);
        }

        foreach (var location in locations)
        {
            Debug.Log("[TeleportMenu] Instantiating button for: " + location.buildingName);
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            btnObj.name = "TeleportButton_" + location.buildingName;
            
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = location.buildingName;
            }

            UnityEngine.UI.Button btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                string spawnName = location.spawnPointName;
                btn.onClick.AddListener(() => TeleportTo(spawnName));
            }
        }
    }

    public void TeleportTo(string spawnPointName)
    {
        Debug.Log("[TeleportMenu] Attempting to teleport to: " + spawnPointName);
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        if (spawnPoint != null && player != null)
        {
            Debug.Log("[TeleportMenu] Teleporting player to: " + spawnPoint.transform.position);
            // Disable CharacterController to teleport
            if (playerController != null) playerController.enabled = false;
            
            player.transform.position = spawnPoint.transform.position;
            player.transform.rotation = spawnPoint.transform.rotation;
            
            if (playerController != null) playerController.enabled = true;
            
            // Close menu after teleport
            ToggleMenu();
        }
        else
        {
            Debug.LogWarning("[TeleportMenu] Spawn point or player not found: " + spawnPointName + " (Player exists: " + (player != null) + ")");
        }
    }
}
