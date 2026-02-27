using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SaveSpot : MonoBehaviour {
    [SerializeField] private GameManager manager;
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private string spotID;

    [SerializeField] private AudioClip saveSound;
    [SerializeField] private Light2D lampLight;
    private AudioSource audioSource;
    private bool playerInRange = false;
    public string SpotID {get {return spotID;}}

    private void Start() {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        interactPrompt.SetActive(true);
    }

    public void OnTriggerExit2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        interactPrompt.SetActive(false);
    }

    private void Update() {
        if (playerInRange && Input.GetKeyDown(KeyCode.R)) {
            Interact();
        }
    }

    private void Interact() {
        player.Health = 100;
        player.UpdateHealthText();
        manager.SaveGame(spotID);
        interactPrompt.SetActive(false);
        audioSource.PlayOneShot(saveSound);
        lampLight.enabled = true;
    }
}