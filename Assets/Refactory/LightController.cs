using CharacterSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightController : MonoBehaviour
{
    public Color Mage, Balrog, Tree, Yeti, Pupperfish, Litch, WhiteMage;
    [Header("Light Fields")]
    [SerializeField] private GameObject fireField;
    [SerializeField] private GameObject grassField;
    [SerializeField] private GameObject waterField;
    [SerializeField] private GameObject iceField;

    private AudioSource audioSource;
    private Animator animator;
    private Light2D light2D;
    [SerializeField] private LightFieldType currentLightField = LightFieldType.None;

    public int lightIntensity = 0;
    public LightFieldType CurrentLightField => currentLightField;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        light2D = GetComponent<Light2D>();
        audioSource = GetComponent<AudioSource>();
        RefreshLightFields();

    }
    public void ChangeLightColor(Color c)
    {
        light2D.color = c;
    }
    public void ChangeLightColor(CharacterType character)
    {
        switch (character)
        {
            case CharacterType.Mage:
                light2D.color = Mage;
                break;
            case CharacterType.Balrog:
                light2D.color = Balrog;
                break;
            case CharacterType.Tree:
                light2D.color = Tree;
                break;
            case CharacterType.Yeti:
                light2D.color = Yeti;
                break;
            case CharacterType.PupperFish:
                light2D.color = Pupperfish;
                break;
            case CharacterType.Litch:
                light2D.color = Litch;
                break;
            case CharacterType.WhiteMage:
                light2D.color = WhiteMage;
                break;
        }
    }



    public void SetLightField(LightFieldType fieldType)
    {
        currentLightField = fieldType;
        RefreshLightFields();
    }

    public void ClearLightField()
    {
        currentLightField = LightFieldType.None;
        RefreshLightFields();
    }

    public void ToggleLightField(LightFieldType fieldType)
    {
        if (currentLightField == fieldType)
        {
            ClearLightField();
            return;
        }

        SetLightField(fieldType);
    }

    public bool IsLightFieldActive(LightFieldType fieldType)
    {
        return currentLightField == fieldType;
    }

    public bool IsPoweredFor(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.Mage:
                return lightIntensity == 3;
            case CharacterType.Balrog:
                return IsLightFieldActive(LightFieldType.Fire);
            case CharacterType.Tree:
                return IsLightFieldActive(LightFieldType.Grass);
            case CharacterType.PupperFish:
                return IsLightFieldActive(LightFieldType.Water);
            case CharacterType.Yeti:
                return IsLightFieldActive(LightFieldType.Ice);
            default:
                Debug.LogWarning($"IsPoweredFor: {characterType} not implemented yet. Returning false.");
                return false;
        }
    }

    private void RefreshLightFields()
    {
        SetFieldActive(fireField, currentLightField == LightFieldType.Fire);
        SetFieldActive(grassField, currentLightField == LightFieldType.Grass);
        SetFieldActive(waterField, currentLightField == LightFieldType.Water);
        SetFieldActive(iceField, currentLightField == LightFieldType.Ice);
    }

    private void SetFieldActive(GameObject field, bool active)
    {
        if (field == null)
        {
            return;
        }

        field.SetActive(active);
    }
    public void IncreaseLightLevel()
    {
        if (lightIntensity < 3)
        {
            lightIntensity++;
            //CompileUILevel();
            animator.SetTrigger("lightOn");
            animator.SetInteger("lightIntesity", lightIntensity);
            PlayAudio();
        }
        if (GameMan.Instance.isProceduralMode)
        {
            GameMan.Instance.spawnerManager.timer = 0;
        }

    }

    public void PlayAudio()
    {
        if (audioSource.clip != null) audioSource.Play();
    }

    public void StartLight()
    {

        //lancia l'animazione della magia di luce determinando il livello di intensità stabilito per questo livello
        animator.SetTrigger("lightOn");
        animator.SetInteger("lightIntesity", lightIntensity);
        PlayAudio();
    }
}
