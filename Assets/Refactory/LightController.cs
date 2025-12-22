using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightController : MonoBehaviour
{
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
