using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMaterial : MonoBehaviour
{
    public string roomName;
    public Material[] materials;
    public GameObject[] grounds;


    public void ChangeMaterialsOfGrounds(int index)
    {
        for(int i = 0; i < grounds.Length; i++)
        {
            grounds[i].GetComponent<Renderer>().material = materials[index];
        }
    }

    private void Start()
    {
        //ChangeMaterialsOfGrounds(3);
    }
}