using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    
    public static Vector3 GetNewSpawnPoint(int Xwidth, int Ywidth)
    {
        //Debug.Log("Started getting sp");
        
        return new Vector3(Random.Range(-Xwidth, Xwidth), 3, Random.Range(-Ywidth, Ywidth));

    }


}
