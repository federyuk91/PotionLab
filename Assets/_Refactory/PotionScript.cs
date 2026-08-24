using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionScript : MonoBehaviour
{
    private static readonly System.Random ActivationPitchRandom = new System.Random();

    private Rigidbody2D _rb;
    private Sprite _potionSprite;
    private AudioSource _audio;
    private AudioSource _activationAudio;
    private float _clickPitch = 1f;

    [Header("Activation Audio")]
    [SerializeField, Range(0.1f, 3f)] private float activationPitchMin = 0.85f;
    [SerializeField, Range(0.1f, 3f)] private float activationPitchMax = 1.2f;

    public PotionScriptable potion;
    public GameObject whiteSquare;
    public bool isActive = false;
    public bool isStackable = true;

    public List<PotionScript> stock = new List<PotionScript>();

    private void Awake()
    {
        EnsureRuntimeReferences();
        stock.Add(this);
    }

    public void ResetRuntimeStateForPool()
    {
        EnsureRuntimeReferences();

        isActive = false;
        stock.Clear();
        stock.Add(this);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.mass = 1f;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (whiteSquare != null)
        {
            whiteSquare.SetActive(false);
        }
    }

    private void EnsureRuntimeReferences()
    {
        if (_audio == null)
        {
            _audio = GetComponent<AudioSource>();
            if (_audio != null)
            {
                _clickPitch = _audio.pitch;
            }
        }

        if (_activationAudio == null && _audio != null)
        {
            CreateActivationAudioSource();
        }

        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        if (_potionSprite == null)
        {
            _potionSprite = GetComponent<Sprite>();
        }
    }

    public void ActivateBox()
    {
        if(_rb==null)
            return;
        if (_rb.bodyType.Equals(RigidbodyType2D.Kinematic))
        {
            whiteSquare.SetActive(true);
            isActive = true;
            PlayActivationAudio();
        }
    }

    private void CreateActivationAudioSource()
    {
        _activationAudio = gameObject.AddComponent<AudioSource>();
        _activationAudio.playOnAwake = false;
        _activationAudio.loop = false;
        _activationAudio.outputAudioMixerGroup = _audio.outputAudioMixerGroup;
        _activationAudio.volume = _audio.volume;
        _activationAudio.priority = _audio.priority;
        _activationAudio.spatialBlend = _audio.spatialBlend;
        _activationAudio.panStereo = _audio.panStereo;
        _activationAudio.dopplerLevel = _audio.dopplerLevel;
        _activationAudio.spread = _audio.spread;
        _activationAudio.rolloffMode = _audio.rolloffMode;
        _activationAudio.minDistance = _audio.minDistance;
        _activationAudio.maxDistance = _audio.maxDistance;
    }

    private void PlayActivationAudio()
    {
        if (_audio == null || _audio.clip == null || _activationAudio == null)
        {
            return;
        }

        float minimumPitch = Mathf.Min(activationPitchMin, activationPitchMax);
        float maximumPitch = Mathf.Max(activationPitchMin, activationPitchMax);
        float randomFactor = (float)ActivationPitchRandom.NextDouble();

        _activationAudio.clip = _audio.clip;
        _activationAudio.volume = _audio.volume;
        _activationAudio.pitch = Mathf.Lerp(minimumPitch, maximumPitch, randomFactor);
        _activationAudio.Play();
    }

    public void DropPotion()
    {
        DropPotion(true);
    }

    public void DropPotion(bool clickEvent)
    {
        if (isActive)
        {
            if(clickEvent)
                ClickLightEvents.RaiseTargetClicked(transform);
            //Debug.Log("Drop Potion: " + this.name);
            //Debug.Log(_audio);
            if (_audio != null)
            {
                _audio.pitch = _clickPitch;
                _audio.Play();
            }
            _rb.bodyType = RigidbodyType2D.Dynamic;
            whiteSquare.SetActive(false);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isStackable || !collision.collider.CompareTag("Potion"))
        {
            return;
        }

        PotionScript collisionPotion = collision.collider.GetComponent<PotionScript>();
        if (collisionPotion == null || !collisionPotion.isStackable)
        {
            return;
        }

        foreach (PotionScript pot in collisionPotion.stock)
        {
            if (!stock.Contains(pot))
                stock.Add(pot);
        }
        _rb.mass = 1f / stock.Count;
    }


    private void OnCollisionExit2D(Collision2D collision)
    {
        PotionScript collisionPotion = collision.collider.GetComponent<PotionScript>();
        stock.Remove(collisionPotion);
        _rb.mass = 1f / stock.Count;
    }

}

