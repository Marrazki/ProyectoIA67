using UnityEngine;

public class CamaraFollow : MonoBehaviour
{
    public Transform target; // El objeto que la cámara seguirá
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(target.position.x,transform.position.y,target.position.z);
    }
}
