using CharacterSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightController : MonoBehaviour
{
    public Color Mage, Balrog, Tree, Yeti, Pupperfish, Litch, WhiteMage;
    private AudioSource audioSource;
    private Animator animator;
    private Light2D light2D;
    public int lightIntensity = 0;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        light2D = GetComponent<Light2D>();
        audioSource = GetComponent<AudioSource>();

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
}
