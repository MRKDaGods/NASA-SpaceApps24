using UnityEngine;

public class Heatmap : MonoBehaviour
{
    public Vector4[] positions;
    public Vector4[] properties;

    public Material material;

    public int count = 150;

    void Start()
    {
        AllocateIfNeeded();
    }

    public void AllocateIfNeeded()
    {
        if (positions == null || positions.Length == 0)
        {
            positions = new Vector4[150];
            properties = new Vector4[150];

            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new Vector4(Random.Range(-0.4f, +0.4f), Random.Range(-0.4f, +0.4f), 0, 0);
                properties[i] = new Vector4(Random.Range(0f, 0.25f), Random.Range(-0.25f, 1f), 0, 0);
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < positions.Length; i++)
            positions[i] += new Vector4(Random.Range(-5f, +5f), Random.Range(-5f, +5f), 0, 0) * Time.deltaTime;

        material.SetInt("_Points_Length", count);
        material.SetVectorArray("_Points", positions);
        material.SetVectorArray("_Properties", properties);
    }
}