using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Destruct;

public interface IDestructible
{
    void PreDestruct();

    void PostDestruct((SplitResult, List<GameObject>) destructionResults);

    MeshFilter GetMeshFilter();
}