using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StringBuilder sb = new StringBuilder("fk", 2);
        
        Debug.Log(sb.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
