using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("문 속성")]
    public bool isOpened = false;
    private Vector3 originRot;
    private bool isPlaying;


    // Start is called before the first frame update
    void Start()
    {
        originRot = transform.rotation.eulerAngles;
    }

    public void OpenDoor()
    {
        if (!isPlaying && !isOpened)
            StartCoroutine(OpenDoorCor());
    }

    public void CloseDoor()
    {
        if(!isPlaying && isOpened)
        {
            StartCoroutine(CloseDoorCor());
        }
    }

    IEnumerator OpenDoorCor()
    {
        isPlaying = true;
        Vector3 rot = originRot;
        rot.y += 90 % 360;
        float i = originRot.y;
        while (i <= rot.y)
        {
            i += (Time.deltaTime / 0.5f) * (rot.y - originRot.y);                                       //0.5초만에 문을 염
            transform.localRotation = Quaternion.Euler(originRot.x, i, originRot.z);
            yield return null;
        }
        transform.localRotation = Quaternion.Euler(rot);
        isOpened = true;
        isPlaying = false;
    }

    IEnumerator CloseDoorCor()
    {
        isPlaying = true;
        Vector3 rot = originRot;
        rot.y += 90 % 360;
        float i = rot.y;
        while(i >= originRot.y)
        {
            i -= (Time.deltaTime / 0.5f) * (rot.y - originRot.y);
            transform.localRotation = Quaternion.Euler(originRot.x, i, originRot.z);
            yield return null;
        }
        transform.localRotation = Quaternion.Euler(originRot);
        isOpened = false;
        isPlaying = false;
    }
}
