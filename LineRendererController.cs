using System;
using UnityEngine;

namespace BladeAndTitan;

public class LineRendererController : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Transform target;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, target.position);
    }
}