using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChaserCtrl : MonoBehaviour
{
    public enum PROPERTY
    {
        IDLE,
        ALERT,
        CHASE,
    }

    [Header("추격자 속성")]
    [Range(0f, 10f)]
    public float speed;
    public Transform camPos;
    public PROPERTY chaserProp = PROPERTY.IDLE;
    private NavMeshAgent nav;
    private bool isPlayingAlert;
    private float idleDelta;

    [Header("추격 상태 관련 변수")]
    public float chaseTime;             //시야에 있을 때 
    public float degree;
    public float recogDist;
    [SerializeField()]
    private float chaseDelta;           //추격
    private RaycastHit hit;

    [Header("플레이어")]
    public Transform target;
    public GameObject cam;

    [Header("플레이어 쳐다보는 변수")]
    public Transform head;
    private Animator anim;
    private Vector3 lookAtTargetPosition;
    private Vector3 lookAtPosition;
    public float blendTime = 0.4f;
    public float towards = 5.0f;
    public float weightMul = 1;
    public float clampWeight = 0.5f;
    public Vector3 weight = new Vector3(0.4f, 0.8f, 0.9f);
    public bool yTargetHeadSynk;
    float lookAtWeight;

    [Header("Audio")]
    public AudioSource audio;

    // Start is called before the first frame update
    void Start()
    {
        nav = GetComponent<NavMeshAgent>();
        nav.speed = speed;
        anim = GetComponent<Animator>();

        audio = GetComponent<AudioSource>();
        audio.loop = true;
        lookAtTargetPosition = target.position + transform.forward;
        lookAtPosition = head.position + transform.forward;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        lookAtTargetPosition = target.position + transform.forward;
        ChaserMove();
    }

    void ChaserMove()
    {
        Vector3 watchDir = Vector3.zero;
        watchDir.y = GetAngle(transform.position, target.position);
        transform.localRotation = Quaternion.Euler(watchDir);
        switch (chaserProp)
        {
            case PROPERTY.IDLE:
                Idle();
                break;
            case PROPERTY.ALERT:
                Alert();
                break;
            case PROPERTY.CHASE:
                Chase();
                break;
            default:
                break;
        }
    }



    private void Idle()
    {
        nav.isStopped = true;
        audio.volume = 0;
        audio.Stop();
        idleDelta += Time.deltaTime;

        
        if (idleDelta > 5f && !target.GetComponent<PlayerCtrl>().isDied)
        {
            IdleRandomPos(5f, -120, 120);
            idleDelta = 0;
        }
    }

    private void Alert()
    {
        chaseDelta = chaseTime;
        if (!isPlayingAlert && !target.GetComponent<PlayerCtrl>().isDied)
            StartCoroutine(AlertToChaseAnim());
    }

    private void Chase()
    {
        nav.isStopped = false;
        Vector3 navPos;

        NavMeshPath path = new NavMeshPath();
        nav.CalculatePath(target.position, path);

        if (path.status == NavMeshPathStatus.PathPartial && FindNearDoor(20f) != null)
        {
            navPos = FindNearDoor(20f).position;
        }
        else
        {
            navPos = target.position;
        }
        nav.SetDestination(navPos);
        if (IsInSight(transform, target.transform, degree, recogDist))
        {
            chaseDelta = chaseTime;
        }
        else
        {
            chaseDelta -= Time.deltaTime;
            if (chaseDelta <= 0)
            {
                ToIdle();
            }
        }
    }

    private float GetAngle(Vector3 start, Vector3 end)
    {
        Vector3 v = end - start;
        return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
    }

    private bool IsInSight(Transform origin, Transform target, float degree, float recogDist)
    {
        Vector3 dir = (target.position - origin.position).normalized;

        float dot = Vector3.Dot(origin.forward, dir);
        float theta = Mathf.Acos(dot) * Mathf.Rad2Deg;
        float nowDist = Vector3.Distance(origin.position, target.position);


        if (theta <= degree && nowDist <= recogDist && Physics.Raycast(transform.position, dir, out hit, recogDist))
        {
            Debug.DrawRay(origin.position, dir * nowDist, Color.red);
            if (hit.transform.CompareTag("PLAYER"))
            {
                return true;
            }

            else
                return false;
        }
        else
            return false;
    }

    void OnAnimatorIK()
    {
        //--------------
        if (yTargetHeadSynk == false)
            lookAtTargetPosition.y = head.position.y;
        Vector3 curDir = lookAtPosition - head.position;
        curDir = Vector3.RotateTowards(curDir, lookAtTargetPosition - head.position, towards * Time.deltaTime, float.PositiveInfinity);
        lookAtPosition = head.position + curDir;
        lookAtWeight = Mathf.MoveTowards(lookAtWeight, 1, Time.deltaTime / blendTime);
        anim.SetLookAtWeight(lookAtWeight * weightMul, weight.x, weight.y, weight.z, clampWeight);
        anim.SetLookAtPosition(lookAtPosition);

        //--------------
    }

    public void ToIdle()
    {
        chaserProp = PROPERTY.IDLE;
        anim.SetBool("isIdle", true);
        anim.SetBool("isScream", false);
        anim.SetBool("isRun", false);
    }

    public Transform FindNearDoor(float radius)
    {
        Collider[] doors = Physics.OverlapSphere(target.position, radius, 1 << 7);
        if (doors.Length < 1)
            return null;
        else
        {
            Transform door = doors[0].transform;
            float doorDist = Vector3.Distance(target.position, doors[0].transform.position);
            for (int i = 0; i < doors.Length; i++)
            {
                float temp = Vector3.Distance(target.position, doors[i].transform.position);
                if (temp < doorDist)
                {
                    door = doors[i].transform;
                    doorDist = temp;
                }
            }
            return door;
        }
    }

    public void IdleRandomPos(float dist, float minDeg, float maxDeg)
    {
        float rand = Random.Range(minDeg, maxDeg);
        float rad = (-1 * cam.transform.rotation.eulerAngles.y + 270 + rand) % 360 * Mathf.Deg2Rad;
        nav.Warp(target.position + new Vector3(dist * Mathf.Cos(rad), target.position.y, dist * Mathf.Sin(rad)));
    }

    IEnumerator AlertToChaseAnim()
    {
        isPlayingAlert = true;
        idleDelta = 0;
        anim.SetBool("isIdle", false);
        anim.SetBool("isScream", true);
        anim.SetBool("isRun", false);

        target.GetComponent<PlayerCtrl>().PlaySFX(target.GetComponent<PlayerCtrl>().jumpScare);
        yield return new WaitForSeconds(1f);
        anim.SetBool("isIdle", false);
        anim.SetBool("isScream", false);
        anim.SetBool("isRun", true);
        audio.volume = PlayerPrefs.GetFloat("SFX");
        audio.Play();


        chaserProp = PROPERTY.CHASE;
        isPlayingAlert = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("PLAYER") && !other.GetComponent<PlayerCtrl>().isDied)
        {
            ToIdle();
            other.GetComponent<PlayerCtrl>().PlaySFX(other.GetComponent<PlayerCtrl>().deadSFX);
            other.GetComponent<PlayerCtrl>().isDied = true;
        }
    }
}
