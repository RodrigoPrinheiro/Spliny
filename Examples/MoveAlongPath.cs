using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spliny;

[ExecuteInEditMode]
public class MoveAlongPath : MonoBehaviour
{
    [SerializeField, Range(0f,1f)] private float _lerpAmount = 0.0f;
    [SerializeField] private PathCreator _curve = default;

    private void Update() 
    {
        if (_curve == null) return;
        transform.position = _curve.path.EvaluatePath(_lerpAmount);
    }
}
