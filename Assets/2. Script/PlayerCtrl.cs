using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.WSA;
using UnityEngine.UI;

public class PlayerCtrl : MonoBehaviour
{
    [Header("카메라")]
    public GameObject mainCam;
    public GameObject glitchImg;

    [Header("UI")]
    public GameObject paperText;
    public UnityEngine.UI.Image fadeImg;
    private bool isFadePlaying;
    
    [Header("플레이어 속성")]
    public GameObject flashLight;
    private bool isFlashOn = true;
    [SerializeField]
    private int paperCnt = 0;
    private bool isClear;

    [Header("이동 관련")]
    [SerializeField]
    private float speed;
    private float runningSpeed;
    private float walkingSpeed;
    [Range(10, 500)]
    public float mSensitivity;      //마우스 감도
    public float jumpPower = 10;
    private float yVelocity;        //y가속도(떨어지는 속도)
    private bool canJump = false;
    CharacterController cc;
    private float mx, my, rx, ry;
    [Range(0, 100)]
    public float maxStamina;
    public float stamina;
    public float staminaDelta;
    private float runningDelta;
    private bool isRunning;

    [Header("죽음")]
    public bool isDied;
    [Range(0f, 1f)]
    public float firstGlitchValue;
    [Range(0f, 1f)]
    public float endGlitchValue;
    private float glitchDelta;
    private bool isDeadScenePlaying;

    [Header("레이캐스트")]
    private RaycastHit hit;
    [Range(0, 10)]
    public float dist = 5f;

    [Header("추격자")]
    public GameObject chaser;
    public GameObject chaserRay;
    public SkinnedMeshRenderer chaserBody;
    [Range(0f, 90f)]
    public float degree;            //추격자를 인식하는 시야각
    [Range(0f, 30f)]
    public float recogDist;         //추격자를 인식하는 거리

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip jumpScare;
    public AudioClip deadSFX;
    public AudioClip BGM;
    public AudioClip walk;
    public float walkSoundInterval;
    private float maxWalkSoundInterval;
    private float minWalkSoundInterval;
    private float walkTime;
    [Range(0f, 1f)]
    public float volume;

    void Awake()
    {
        audioSource.volume = PlayerPrefs.GetFloat("BGM");
        mSensitivity = PlayerPrefs.GetFloat("MS");
    }

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        cc = GetComponent<CharacterController>();
        audioSource.volume = volume;
        audioSource.loop = true;
        PlayBGM(BGM);
        paperText.SetActive(false);

        stamina = maxStamina;
        runningSpeed = speed * 1.5f;
        walkingSpeed = speed;
        maxWalkSoundInterval = walkSoundInterval;
        minWalkSoundInterval = walkSoundInterval / 1.5f;
        if(!isFadePlaying)
            StartCoroutine(FadeIn(1f));
    }

    // Update is called once per frame
    void Update()
    {
        glitchImg.GetComponent<Renderer>().material.SetFloat("_GlitchAmount", 0);

        if (!isClear)
        {
            if (IsInSight(mainCam.transform, chaserRay.transform, degree, recogDist))
            {
                glitchImg.GetComponent<Renderer>().material.SetFloat("_GlitchAmount", 0.02f);
                if(chaser.GetComponent<ChaserCtrl>().chaserProp == ChaserCtrl.PROPERTY.IDLE)
                    chaser.GetComponent<ChaserCtrl>().chaserProp = ChaserCtrl.PROPERTY.ALERT;
            }
        }
        if (!isDied)
            PlayerMove();
        else
            DeadScene();
        Ray();
    }

    private void PlayerMove()
    {
        ////////////////////////////////////////////////////////////////////////////////////////
        ///플레이어 점프 체크
        if (!cc.isGrounded)
            yVelocity += -9.81f * Time.deltaTime;

        if (Input.GetButtonDown("Jump") && canJump)
        {
            yVelocity = jumpPower;
            canJump = false;
        }
        ////////////////////////////////////////////////////////////////////////////////////////
        ///마우스
        mx = Input.GetAxis("Mouse X");
        my = Input.GetAxis("Mouse Y");

        rx += mx * mSensitivity * Time.deltaTime;
        ry += my * mSensitivity * Time.deltaTime;

        ry = Mathf.Clamp(ry, -80, 80);

        mainCam.transform.eulerAngles = new Vector3(-ry, rx, 0);
        ////////////////////////////////////////////////////////////////////////////////////////
        ///키보드
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 dir = Vector3.forward * v + Vector3.right * h;
        dir = Camera.main.transform.TransformDirection(dir);

        dir.Normalize();

        dir.y = yVelocity;

        ////////////////////////////////////////////////////////////////////////////////////////
        ///손전등
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isFlashOn = !isFlashOn;
        }
        flashLight.SetActive(isFlashOn);

        ////////////////////////////////////////////////////////////////////////////////////////
        ///대쉬
        if (Input.GetKeyDown(KeyCode.LeftShift))            
            isRunning = true;
        else if (Input.GetKeyUp(KeyCode.LeftShift) || stamina < 0)
            isRunning = false;

        if (!isRunning)
        {
            speed = walkingSpeed;
            walkSoundInterval = maxWalkSoundInterval;
            runningDelta += Time.deltaTime;
            if (stamina < maxStamina && runningDelta >= 0.1f)
            {
                runningDelta = 0;
                stamina += staminaDelta;
            }
        }
        else
        {
            speed = runningSpeed;
            walkSoundInterval = minWalkSoundInterval;
            runningDelta += Time.deltaTime;
            if(runningDelta >= 0.1f)
            {
                runningDelta = 0;
                stamina -= staminaDelta;
            }
        }

        cc.Move(dir * speed * Time.deltaTime);
        ////////////////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////
        //걷는 사운드
        if (h != 0 || v != 0)
            walkTime += Time.deltaTime;
        if(walkTime >= walkSoundInterval)
        {
            PlaySFX(walk);
            walkTime = 0;
        }
        //////////////////////////////////////////////////////////////////
    }

    private void Ray()
    {
        if(paperCnt == 8)
        {
            if (!isClear)
                StartCoroutine(ClearScene());
        }
        if(Physics.Raycast(mainCam.transform.position, mainCam.transform.TransformDirection(Vector3.forward), out hit, dist))       
        {
            Debug.DrawRay(mainCam.transform.position, mainCam.transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);      //레이캐스트로 종이에 닿았는지
            if (Input.GetMouseButtonDown(0) && hit.transform.CompareTag("PAPER"))
            {
                Destroy(hit.transform.gameObject);
                paperCnt++;
                StartCoroutine(PaperCor());
            }

            if (Input.GetMouseButtonDown(0) && hit.transform.CompareTag("DOOR"))                                                               //레이캐스트로 문에 닿았는지
            {
                if (hit.transform.gameObject.GetComponent<Door>().isOpened)
                    hit.transform.gameObject.GetComponent<Door>().CloseDoor();
                else
                    hit.transform.gameObject.GetComponent<Door>().OpenDoor();

            }
        }
    }

    private bool IsInSight(Transform origin, Transform target, float degree, float recogDist)
    {
        Vector3 dir = (target.position - origin.position).normalized;

        float dot = Vector3.Dot(origin.forward, dir);
        float theta = Mathf.Acos(dot) * Mathf.Rad2Deg;
        float nowDist = Vector3.Distance(origin.position, target.position);

        if (theta <= degree && nowDist <= recogDist && Physics.Raycast(mainCam.transform.position, dir, out hit, recogDist))
        {
            Debug.DrawRay(mainCam.transform.position, dir * hit.distance, Color.green);
            if (hit.transform.CompareTag("CHASER"))
                return true;
            else
                return false;
        }
        else
            return false;
    }

    public void DeadScene()
    {
        if(!isDeadScenePlaying)
            StartCoroutine(DeadSceneJumpScare());
    }

    IEnumerator DeadSceneJumpScare()
    {
        isDeadScenePlaying = true;
        glitchDelta = 0;
        glitchImg.GetComponent<Renderer>().material.SetFloat("_GlitchAmount", endGlitchValue);
        yield return new WaitForSeconds(0.1f);
        glitchImg.GetComponent<Renderer>().material.SetFloat("_GlitchAmount", firstGlitchValue);
        yield return new WaitForSeconds(1f);

        while (glitchDelta < 5)
        {
            transform.position = chaser.GetComponent<ChaserCtrl>().camPos.position;

            glitchDelta += Time.deltaTime;
            mainCam.transform.rotation = Quaternion.Slerp(mainCam.transform.rotation, chaser.GetComponent<ChaserCtrl>().camPos.rotation, glitchDelta);
            float f = Mathf.Lerp(firstGlitchValue, endGlitchValue, glitchDelta * (1 / 5f));
            glitchImg.GetComponent<Renderer>().material.SetFloat("_GlitchAmount", f);
            chaserBody.SetBlendShapeWeight(0, glitchDelta * 20);
            yield return null;
        }
        StartCoroutine(FadeOut(0.1f));
    }

    IEnumerator ClearScene()
    {
        isClear = true;
        Destroy(chaser);
        glitchDelta = 0;
        while(glitchDelta < 5f)
        {
            glitchDelta += Time.deltaTime;
            glitchImg.GetComponent<Renderer>().material.SetFloat("_GlitchAmount", glitchDelta * 0.02f);
            yield return null;
        }
        GameObject.Find("MainSceneManager").GetComponent<MainSceneManager>().ChangeSkyBox();
        glitchDelta = 2;
        while(glitchDelta > 0f)
        {
            glitchDelta -= Time.deltaTime;
            glitchImg.GetComponent<Renderer>().material.SetFloat("_GlitchAmount", glitchDelta * 0.02f);
            yield return null;
        }
        glitchImg.GetComponent<Renderer>().material.SetFloat("_GlitchAmount", 0f);
    }

    IEnumerator PaperCor()
    {
        paperText.SetActive(true);
        paperText.GetComponent<Text>().text = "Paper : " + paperCnt + " / 8";
        yield return new WaitForSeconds(2f);
        paperText.SetActive(false);
    }

    IEnumerator FadeIn(float time)
    {
        isFadePlaying = true;
        Color c = fadeImg.color;
        c.a = 1;

        while(c.a > 0)
        {
            c.a -= Time.deltaTime / time;
            fadeImg.color = c;
            yield return null;
        }
        fadeImg.color = new Color(0, 0, 0, 0);

        isFadePlaying = false;
    }

    IEnumerator FadeOut(float time)
    {
        isFadePlaying = true;
        Color c = fadeImg.color;
        c.a = 0;

        while(c.a < 1)
        {
            c.a += Time.deltaTime / time;
            fadeImg.color = c;
            yield return null;
        }
        fadeImg.color = new Color(0, 0, 0, 1);

        isFadePlaying = false;
    }

    public void PlaySFX(AudioClip clip)
    {
        audioSource.PlayOneShot(clip, PlayerPrefs.GetFloat("SFX"));
    }

    public void PlayBGM(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)     //점프 체크
    {
        if (hit.transform.CompareTag("GROUND"))
            canJump = true;
    }
}
