using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : BaseManager
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image portraitImage;

    [Header("Dialogue JSON")]
    [SerializeField] private TextAsset dialogueJSON;

    private DialogueNode[] dialogueNodes;
    private int currentNodeIndex = 0;
    public InputManager inputManager;
    private bool dialogueActive = false;
    private bool eventBound = false;

    #region Lifecycle Overrides
    public override void Configure(GameManager gm)
    {
        base.Configure(gm);
        inputManager ??= GetComponent<InputManager>();
        Debug.Log("DialogManager: Configured.");
    }

    public override void Initialize()
    {
        LoadDialogueFromJSON();
        dialoguePanel.SetActive(false);

        if (dialogueNodes.Length > 0)
            ShowDialogueNode(0);
    }

    public override void BindEvents()
    {
        // No se suscribe aquí - espera a que el diálogo se active
    }

    public override void StartRuntime() { }
    public override void StopRuntime() { }

    public override void UnbindEvents()
    {
        UnsubscribeFromInput();
    }

    public override void Teardown()
    {
        EndDialogue();
    }

    private void OnDisable()
    {
        Teardown();
    }
    #endregion

    #region Dialogue Methods
    private void LoadDialogueFromJSON()
    {
        if (dialogueJSON != null)
            dialogueNodes = JsonHelper.FromJson<DialogueNode>(dialogueJSON.text);
        else
        {
            Debug.LogError("No se asignó ningún JSON en el Inspector.");
            dialogueNodes = new DialogueNode[0];
        }
    }

    private void ShowDialogueNode(int index)
    {
        if (index < 0 || index >= dialogueNodes.Length) return;

        var node = dialogueNodes[index];
        dialoguePanel.SetActive(true);
        dialogueActive = true;

        // Suscribirse al input solo cuando el diálogo se activa
        SubscribeToInput();

        speakerText.text = node.speaker;
        dialogueText.text = node.text;

        if (!string.IsNullOrEmpty(node.portraitName))
        {
            Sprite sprite = Resources.Load<Sprite>("DialogCharacters/" + node.portraitName);
            portraitImage.sprite = sprite;
            portraitImage.gameObject.SetActive(sprite != null);
            if (sprite == null) Debug.LogWarning("No se encontró el sprite: " + node.portraitName);
        }
        else
        {
            portraitImage.gameObject.SetActive(false);
        }
    }

    private void SubscribeToInput()
    {
        if (eventBound) return; // Evita doble suscripción

        if (inputManager == null)
            inputManager = GameManager.Instance.inputManager;

        if (inputManager != null)
        {
            inputManager.OnSubmitPressed += NextNode;
            eventBound = true;
            Debug.Log("DialogManager: Suscrito al evento de submit.");
        }
    }

    private void UnsubscribeFromInput()
    {
        if (!eventBound) return;

        if (inputManager != null)
        {
            inputManager.OnSubmitPressed -= NextNode;
            eventBound = false;
            Debug.Log("DialogManager: Desuscrito del evento de submit.");
        }
    }

    public void NextNode()
    {
        if (!dialogueActive || dialogueNodes.Length == 0)
            return;

        currentNodeIndex++;

        if (currentNodeIndex < dialogueNodes.Length)
        {
            ShowDialogueNode(currentNodeIndex);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        UnsubscribeFromInput();
        dialoguePanel.SetActive(false);
        speakerText.text = "";
        dialogueText.text = "";
        portraitImage.gameObject.SetActive(false);
        dialogueActive = false;
        currentNodeIndex = 0;
    }
    #endregion

    #region DialogueNode Class
    [System.Serializable]
    private class DialogueNode
    {
        public string speaker;
        public string text;
        public string portraitName;
    }
    #endregion
}