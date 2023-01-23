using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMaterialFromUI : MonoBehaviour
{
    public ChangeMaterial changeMaterial;

    public void ChangeMaterials(int index)
    {
        changeMaterial.ChangeMaterialsOfGrounds(index);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
