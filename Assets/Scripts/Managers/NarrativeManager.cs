using TMPro;
using UnityEngine;

public class NarrativeManager : BaseManager
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Dialogue Sequence")]
    [SerializeField] private DialogueNode[] dialogueNodes;
}
