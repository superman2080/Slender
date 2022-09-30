using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    public GameObject flashLight;
    public AudioSource audioSource;
    public AudioClip BGM;

    public GameObject option;

    public Slider sliderBGM;
    public Slider sliderSFX;
    public Slider sliderMS;

    private float deltaTimer;
    private bool isPlaying;

    // Start is called before the first frame update
    void Start()
    {

        if (!PlayerPrefs.HasKey("BGM"))
            PlayerPrefs.SetFloat("BGM", 0.5f);
        if (!PlayerPrefs.HasKey("SFX"))
            PlayerPrefs.SetFloat("SFX", 0.5f);
        if (!PlayerPrefs.HasKey("MS"))
            PlayerPrefs.SetFloat("MS", 200);


        sliderBGM.value = PlayerPrefs.GetFloat("BGM");
        sliderSFX.value = PlayerPrefs.GetFloat("SFX");
        sliderMS.value = PlayerPrefs.GetFloat("MS");

        option.SetActive(false);

        audioSource.volume = PlayerPrefs.GetFloat("BGM");
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.clip = BGM;
        audioSource.Play();

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (deltaTimer > 3f)
        {
            if(!isPlaying)
                StartCoroutine(FlashLightCor());
        }
        else
            deltaTimer += Time.deltaTime;

        audioSource.volume = PlayerPrefs.GetFloat("BGM");

    }

    public void SetBGMVol()
    {
        PlayerPrefs.SetFloat("BGM", sliderBGM.value);
    }
    public void SetSFXVol()
    {
        PlayerPrefs.SetFloat("SFX", sliderSFX.value);
    }
    public void SetMSensitivity()
    {
        PlayerPrefs.SetFloat("MS", sliderMS.value);
    }
    public void OptionBtn()
    {
        option.SetActive(true);
    }
    public void BackgroundBtn()
    {
        option.SetActive(false);
    }

    public void StartSceneLoad()
    {
        SceneManager.LoadScene("MainScene");
    }

    IEnumerator FlashLightCor()
    {
        isPlaying = true;
        flashLight.SetActive(false);
        yield return new WaitForSeconds(0.05f);
        flashLight.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        flashLight.SetActive(false);
        yield return new WaitForSeconds(0.05f);
        flashLight.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        flashLight.SetActive(false);
        yield return new WaitForSeconds(0.05f);
        flashLight.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        flashLight.SetActive(false);
        yield return new WaitForSeconds(0.05f);
        flashLight.SetActive(true);

        yield return new WaitForSeconds(2.5f);
        flashLight.SetActive(false);
        yield return new WaitForSeconds(0.05f);
        flashLight.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        flashLight.SetActive(false);
        yield return new WaitForSeconds(0.05f);
        flashLight.SetActive(true);

        deltaTimer = 0;
        isPlaying = false;
    }
}
