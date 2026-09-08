using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventListener : MonoBehaviour
{
    public SurfaceEffector2D bottomPlatformNear, bottomPlatformFar, upperPlatform;
    public Animator lightAnimator;
    public AudioSource powerDown;

    public GameObject[] obstacle;
    public GameObject currentObstacle;

    public GameObject[] familiars;


    public SpriteRenderer imageSpriteUp, imageSpriteDown;
    public Sprite[] velocityImage;

    public void SetSceneLightsLevel(float light)
    {
        int intensity =(int)light;
        if(intensity < 0)
        {
            intensity = 0;
            GameMan.Instance.PopDialog("I need my power back! HURRY!");
        } else if( intensity == 0 && GameMan.Instance.isProceduralMode)
        {
            AchievementManager.instance.Achive("Not a waster");
        }
        GameMan.Instance.lightIntensity = intensity;
        lightAnimator.SetInteger("lightIntesity", intensity);
        lightAnimator.SetTrigger("lightAdvance");
        powerDown.Play();
        GameMan.Instance.CompileUILevel();
        GameMan.Instance.PopDialog("My power is low!", 2f);

    }

    public void SetUpperPlatformSpeed(float speed)
    {
        //TODO: CAMBIA LA VELOCITA DELLA PIATTAFORMA SOTTO LO SPAWNER AUTOMATICO
        upperPlatform.speed = speed;

        if(speed < 0)
        {
            imageSpriteUp.flipX = true;
            switch (speed)
            {
                case -3.0f:
                    imageSpriteUp.sprite = velocityImage[0];
                    break;
                case -6.0f:
                    imageSpriteUp.sprite = velocityImage[1];
                    break;
                case -9.0f:
                    imageSpriteUp.sprite = velocityImage[2];
                    break;

            }
        } else
        {
            imageSpriteUp.flipX = false;
            switch (speed)
            {
                case 3.0f:
                    imageSpriteUp.sprite = velocityImage[0];
                    break;
                case 6.0f:
                    imageSpriteUp.sprite = velocityImage[1];
                    break;
                case 9.0f:
                    imageSpriteUp.sprite = velocityImage[2];
                    break;
            }
        }
        
    }

    public void SetBottomPlatformSpeed(float speed)
    {
        //TODO: CAMBIA LA VELOCITA DELLA PIATTAFORMA SULLA PARTE BASSA DELLO SCHERMO
        bottomPlatformFar.speed = speed;
        bottomPlatformNear.speed = speed;

        if (speed < 0)
        {
            imageSpriteDown.flipX = false;
            switch (speed)
            {
                case -3.0f:
                    imageSpriteDown.sprite = velocityImage[0];
                    break;
                case -6.0f:
                    imageSpriteDown.sprite = velocityImage[1];
                    break;
                case -9.0f:
                    imageSpriteDown.sprite = velocityImage[2];
                    break;

            }
        }
        else
        {
            imageSpriteDown.flipX = true;
            switch (speed)
            {
                case 3.0f:
                    imageSpriteDown.sprite = velocityImage[0];
                    break;
                case 6.0f:
                    imageSpriteDown.sprite = velocityImage[1];
                    break;
                case 9.0f:
                    imageSpriteDown.sprite = velocityImage[2];
                    break;
            }
        }
    }

    public void MoveUpperPlatform(Transform dest)
    {
        Vector2 destination = new Vector2(dest.position.x, dest.position.y);
        MoveUpperPlatform(destination);
    }
    public void MoveUpperPlatform(Vector2 destination)
    {
        //TODO: SPOSTA LA PIATTAFORMA DELLO SPAWNER IN UN ALTRA POSIZIONE, RICHIEDE UNA COROUTINE
    }

    public void EnableEnvironmentElement(GameObject env_element)
    {
        if (env_element.activeSelf)
            return;
        //TODO: ATTIVA UN ELEMENTO DISATTIVO DELLA SCENA
    }

    public void DisableEnvironmentElement(GameObject env_element)
    {
        if (!env_element.activeSelf)
            return;
        //TODO: DISATTIVA UN ELEMENTO ATTIVO DELLA SCENA
    }

    public void RandomObstacle()
    {
        if (currentObstacle!=null)
        {
            currentObstacle.SetActive(false);
        } 
        int i = UnityEngine.Random.Range(0, obstacle.Length - 1);
        currentObstacle = obstacle[i];
        obstacle[i].SetActive(true);
    }

    public void SpawnFamiliar()
    {
        //Spawna o attiva un famiglio casuale;
        int random = UnityEngine.Random.Range(0, familiars.Length);
        //Mi assicuro che ci sia un solo famiglio attivo alla volta
        for(int i=0; i < familiars.Length; i++)
        {
            if (i != random)
                familiars[i].SetActive(false);
            else
                familiars[i].SetActive(true);
        }
    }

    public void SpawnFamiliarBomb()
    {
        familiars[1].SetActive(true);
    }

}
