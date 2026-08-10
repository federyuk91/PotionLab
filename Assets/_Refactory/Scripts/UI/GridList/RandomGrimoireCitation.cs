using InspectorValidation;
using TMPro;
using UnityEngine;

namespace Refactory.UI.GridList
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class RandomGrimoireCitation : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private TMP_Text citationText;

        [Header("Grimoire Curiosities")]
        [SerializeField, TextArea(2, 4)] private string[] citations =
        {
            "A potion never forgets the first moonlight that touched its bottle.",
            "Mandrakes sing more softly when planted beside sleeping cauldrons.",
            "A witch's familiar can smell spoiled mana long before its master can see it.",
            "Tree bark shields keep the memory of every flame and frost that broke them.",
            "Liches do not fear the cold. They fear remembering what warmth felt like.",
            "Grounded magic grows stronger when roots can hear running water nearby.",
            "Venom brewed under a new moon is said to dream of becoming medicine.",
            "A cauldron stirred anticlockwise may reveal a potion's oldest secret.",
            "Fire and ice are not opposites in alchemy; they are impatient siblings.",
            "The rarest ingredient in healing magic is an honest intention.",
            "Some grimoires rearrange their pages when nobody is watching.",
            "A blessed spell shines brightest after surviving a curse.",
            "Never trust a silent bubbling potion. It is usually listening.",
            "Ancient witches marked dangerous recipes with flowers, not skulls.",
            "Every familiar knows one spell it will never teach its witch.",
            "You do not need to be the Chosen One. Sometimes reading the potion label is enough.",
            "If the grimoire starts talking, do not answer. Especially if it asks who is the fairest of them all.",
            "A red potion does not always restore health. This is not that kind of game.",
            "The real treasure was not the gold, but all the wrong potions we drank along the way.",
            "Never accept candy from a witch. Potions, however, are perfectly trustworthy."
        };

        private int lastCitationIndex = -1;

        private void Reset()
        {
            citationText = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            if (citationText == null && !TryGetComponent(out citationText))
            {
                Debug.LogError($"{name}: Random Grimoire Citation requires a TMP_Text reference on the same GameObject.", this);
            }
        }

        private void OnEnable()
        {
            ShowRandomCitation();
        }

        public void ShowRandomCitation()
        {
            if (citationText == null)
            {
                Debug.LogError($"{name}: Cannot show a grimoire curiosity because Citation Text is missing.", this);
                return;
            }

            if (citations == null || citations.Length == 0)
            {
                Debug.LogWarning($"{name}: No grimoire curiosities are configured.", this);
                citationText.text = string.Empty;
                return;
            }

            int citationIndex = Random.Range(0, citations.Length);
            if (citations.Length > 1 && citationIndex == lastCitationIndex)
            {
                int offset = Random.Range(1, citations.Length);
                citationIndex = (citationIndex + offset) % citations.Length;
            }

            lastCitationIndex = citationIndex;
            citationText.text = citations[citationIndex];
        }
    }
}
