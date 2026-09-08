using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject nameInputField;
    public Text welcomeBackText;
    public Text t_version;
    public Text advancedScores;
    public Text potionDrunked, totalDeath, totalMutation;
    public List<LoadLevelButton> loadLevel_buttons;
    public Animation titleScreenAnimation;
    public Animator lightAnimator;
    public Animator menuMovement;

    public Animator[] buttonAnimator;

    public GameObject arcadeLevelCanvas, advanceLevelCanvas, recordLevelCanvas, pausePanel, updateLogPanel;
    private AudioSource audioSource;
    public GameObject coffeMage;

    public EndlessManager endlessManager;

    public bool blockMenu = false;
    private void Awake()
    {
        if (PlayerPrefs.HasKey("Version"))
        {
            string version = PlayerPrefs.GetString("Version");
            if (version.Equals(t_version.text))
            {
                //Nessun update
                updateLogPanel.SetActive(false);
            }
            else
            {
                PlayerPrefs.SetString("Version", t_version.text);
                updateLogPanel.SetActive(true);
            }
        }
        else
        {
            PlayerPrefs.SetString("Version", t_version.text);
            updateLogPanel.SetActive(true);
        }
    }
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        Time.timeScale = 1;
    }



    public void StartScene()
    {
        titleScreenAnimation.Play();
        lightAnimator.SetTrigger("Start");
        StartCoroutine(StartButtonsAnimation());
        audioSource.Play();
    }

    public IEnumerator StartButtonsAnimation()
    {

        yield return new WaitForSeconds(1f);



        foreach (Animator a in buttonAnimator)
        {
            a.SetTrigger("pop");
            yield return new WaitForSeconds(0.2f);
        }


    }

    public void SetLoadButtons()
    {
        for (int i = 0; i < DataSaver.instance.scores.Count; i++)
        {
            loadLevel_buttons[i].Config(DataSaver.instance.scores[i], i < DataSaver.instance.maxLevelReached);
        }
    }

    public void ButtonArcade()
    {
        SetLoadButtons();
        audioSource.Play();
        if (arcadeLevelCanvas.activeSelf)
        {
            StartCoroutine(CloseArcadeGame());
        }
        else
        {
            if (!blockMenu)
            {
                StartCoroutine(StartArcadeGame());
                blockMenu = true;
            }

        }
    }

    public void ButtonAdvance()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            nameInputField.SetActive(false);
            welcomeBackText.text = "Welcome back\n <color=yellow>" + PlayerPrefs.GetString("PlayerName") + "</color>";
            welcomeBackText.gameObject.SetActive(true);
        }
        else
        {
            nameInputField.SetActive(true);
            welcomeBackText.gameObject.SetActive(false);
        }

        advancedScores.text = "Best Score: " + DataSaver.instance.bestScore + "\n\nLast Score: " + DataSaver.instance.lastScore;
        audioSource.Play();

        if (advanceLevelCanvas.activeSelf)
        {
            StartCoroutine(CloseAdvanceGame());

        }
        else
        {
            if (!blockMenu)
            {
                StartCoroutine(StartAdvanceGame());
                blockMenu = true;
            }

        }
    }

    public void PausePanel()
    {
        pausePanel.SetActive(!pausePanel.activeSelf);
    }

    public void ButtonRecord()
    {
        totalDeath.text = DataSaver.instance.totalDeath + "";
        potionDrunked.text = DataSaver.instance.totalDrunkedPotion + "";
        totalMutation.text = DataSaver.instance.totalTransformation + "";


        audioSource.Play();
        if (recordLevelCanvas.activeSelf)
        {
            StartCoroutine(CloseRecordTab());
            coffeMage.SetActive(false);
        }
        else
        {
            if (!blockMenu)
            {
                StartCoroutine(StartRecordTab());
                blockMenu = true;

            }

        }
    }


    public IEnumerator StartArcadeGame()
    {


        menuMovement.SetTrigger("right");
        StartCoroutine(StartButtonsAnimation());

        yield return new WaitForSeconds(.8f);
        lightAnimator.SetInteger("menu", 1);

        yield return new WaitForSeconds(2);
        arcadeLevelCanvas.SetActive(true);

    }

    public IEnumerator CloseArcadeGame()
    {

        arcadeLevelCanvas.SetActive(false);
        StartCoroutine(StartButtonsAnimation());
        yield return new WaitForSeconds(.5f);
        lightAnimator.SetInteger("menu", 0);
        menuMovement.SetTrigger("right");
        yield return new WaitForSeconds(2);
        blockMenu = false;


    }

    public IEnumerator StartAdvanceGame()
    {

        menuMovement.SetTrigger("down");

        StartCoroutine(StartButtonsAnimation());

        yield return new WaitForSeconds(.8f);


        yield return new WaitForSeconds(1);
        endlessManager.ShowPotions();

        advanceLevelCanvas.SetActive(true);

    }

    public IEnumerator CloseAdvanceGame()
    {

        advanceLevelCanvas.SetActive(false);
        endlessManager.ShowPotions();
        StartCoroutine(StartButtonsAnimation());
        yield return new WaitForSeconds(.5f);
        lightAnimator.SetInteger("menu", 0);
        menuMovement.SetTrigger("down");
        yield return new WaitForSeconds(2);

        blockMenu = false;


    }

    public IEnumerator StartRecordTab()
    {

        menuMovement.SetTrigger("left");
        StartCoroutine(StartButtonsAnimation());

        yield return new WaitForSeconds(.8f);
        lightAnimator.SetInteger("menu", 1);

        yield return new WaitForSeconds(2);
        coffeMage.SetActive(true);
        recordLevelCanvas.SetActive(true);

    }

    public IEnumerator CloseRecordTab()
    {

        recordLevelCanvas.SetActive(false);
        StartCoroutine(StartButtonsAnimation());
        yield return new WaitForSeconds(.5f);
        lightAnimator.SetInteger("menu", 0);
        menuMovement.SetTrigger("left");
        yield return new WaitForSeconds(2);
        blockMenu = false;


    }

    public void StartLevel(int i)
    {
        GameMan.currentLevel = i;
        SceneManager.LoadScene(GameMan.currentLevel);
    }

    public void OpenDiscord()
    {
        Application.OpenURL("https://discord.gg/M9CJxvkeFr");
    }

    public void OpenItch()
    {
        Application.OpenURL("https://creative-lizards.itch.io/goodnight-potion");
    }

    public void OpenBuyMeACoffe()
    {
        Application.OpenURL("https://www.buymeacoffee.com/creativelizards");
    }

    public void CloseUpdateLog(bool reset)
    {
        updateLogPanel.SetActive(false);
        if (reset)
        {
            DataSaver.instance.ResetDataSave();
        }
    }
}
