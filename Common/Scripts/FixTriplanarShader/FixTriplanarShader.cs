using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FixTriplanarShader : MonoBehaviour
{

    void OnEnable()
    {
        SkinnedMeshRenderer sR;
        if (TryGetComponent<SkinnedMeshRenderer>(out sR))
        {
            Mesh mesh = sR.sharedMesh;
            mesh.SetUVs(3, mesh.vertices);
            mesh.SetUVs(2, mesh.normals);
        }
        else
        {
            MeshFilter mF;
            if (TryGetComponent<MeshFilter>(out mF))
            {
                Mesh mesh = mF.sharedMesh;
                mesh.SetUVs(3, mesh.vertices);
                mesh.SetUVs(2, mesh.normals);
            }
        }
        //mesh.SetUVs(1, mesh.tangents);
        //mesh.SetUVs(2, mesh.normals);
    }
}
