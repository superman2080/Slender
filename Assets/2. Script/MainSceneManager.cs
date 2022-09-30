using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainSceneManager : MonoBehaviour
{
    private GameObject[] paperPos;
    private bool[] isExistingPapers;
    public GameObject paper;
    public GameObject light;
    public Material[] paperMats;
    public Material clearSkybox;

    // Start is called before the first frame update
    void Start()
    {
        light.SetActive(false);
        int i = 0;
        paperPos = GameObject.FindGameObjectsWithTag("PAPERPOS");
        isExistingPapers = new bool[paperPos.Length];
        while (i < 8)
        {
            int r = Random.Range(0, paperPos.Length);
            if (isExistingPapers[r] == true)
                continue;
            else
            {
                GameObject temp = Instantiate(paper, paperPos[r].transform);
                temp.GetComponent<MeshRenderer>().material = paperMats[i];
                isExistingPapers[r] = true;
                i++;
            }
        }
    }

    public void ChangeSkyBox()
    {
        light.SetActive(true);
        RenderSettings.skybox = clearSkybox;
    }
}
