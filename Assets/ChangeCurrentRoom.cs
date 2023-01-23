using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeCurrentRoom : MonoBehaviour
{
    public CurrentRoom currentRoom;
    public string roomName;
    public ChangeMaterial changeMaterial;
    public ChangeMaterialFromUI ChangeMaterialFromUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag=="Player")
        {
            currentRoom.currentRoom = roomName;
            ChangeMaterialFromUI.changeMaterial = changeMaterial;
        }
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
