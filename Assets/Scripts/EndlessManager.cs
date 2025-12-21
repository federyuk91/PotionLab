using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessManager : MonoBehaviour
{

    public GameObject advancePotions;
    public GameObject hyperModeCheck, hyperHyperModeCheck, failureModeCheck;
    public DataBetweenLevel endlessModifier;
    

    // Start is called before the first frame update
    void Start()
    {
        endlessModifier = GameObject.FindGameObjectWithTag("music").GetComponent<DataBetweenLevel>();
        CheckAllMode();
    }

    public void ShowPotions()
    {
        //advancePotions.SetActive(!advancePotions.activeSelf);
    }

    private void CheckAllMode()
    {
        if (endlessModifier.hyperMode) { hyperModeCheck.SetActive(true); } else { hyperModeCheck.SetActive(false); }
        if (endlessModifier.hyperHyperMode) { hyperHyperModeCheck.SetActive(true); } else { hyperHyperModeCheck.SetActive(false); }
        if (endlessModifier.failureMode) { failureModeCheck.SetActive(true); } else { failureModeCheck.SetActive(false); }
    }

    public void ActivateHyperMode()
    {
        endlessModifier.hyperMode = !endlessModifier.hyperMode;
        if (endlessModifier.hyperHyperMode)
        {
            endlessModifier.hyperHyperMode = false;
        }
        CheckAllMode();
    }

    public void ActivateHyperHyperMode()
    {
        endlessModifier.hyperHyperMode = !endlessModifier.hyperHyperMode;

        if (endlessModifier.hyperMode)
        {
            endlessModifier.hyperMode = false;
        }
        CheckAllMode();
    }

    public void ActivateFailureMode()
    {
        endlessModifier.failureMode = !endlessModifier.failureMode;

        
        CheckAllMode();
    }
}
