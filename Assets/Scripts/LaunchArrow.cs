using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchArrow : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Start(){
        lineRenderer = GetComponent<LineRenderer>();
        Hide();
    }

    public void Show(){
        lineRenderer.enabled = true;
    }

    public void Hide(){
        lineRenderer.enabled = false;
    }

    public void SetData(Vector2 start, Vector2 end){
        lineRenderer.SetPositions(new Vector3[]{start, end});

    }
}
