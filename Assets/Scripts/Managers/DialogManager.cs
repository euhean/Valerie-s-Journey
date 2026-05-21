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
    private bool dialogueActive = false;
    private bool eventBound = false;

    #region Lifecycle Overrides
    public override void Configure(GameManager gm)
    {
        base.Configure(gm);
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

        GameManager.Instance?.ChangeState(GameManager.GameState.Dialog);
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
        else portraitImage.gameObject.SetActive(false);
    }

    private void SubscribeToInput()
    {
        if (eventBound) return;
        EventBus.Instance.Subscribe<SubmitPressedEvent>(OnSubmitEvent);
        eventBound = true;
        Debug.Log("DialogManager: Suscrito al evento de submit.");
    }

    private void UnsubscribeFromInput()
    {
        if (!eventBound) return;
        EventBus.Instance.Unsubscribe<SubmitPressedEvent>(OnSubmitEvent);
        eventBound = false;
        Debug.Log("DialogManager: Unsubscribed from SubmitPressedEvent");
    }

    private void OnSubmitEvent(SubmitPressedEvent _) => NextNode();

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
        GameManager.Instance?.ChangeState(GameManager.ChangeState.Gameplay);
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
