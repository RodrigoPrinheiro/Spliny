using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spliny
{
    public class PathData : ScriptableObject
    {
        [SerializeField, ReadOnly]
        private List<Vector3> _points;
        public bool Closed { get; set; }
        public List<Vector3> Points
        {
            get
            {
                if (_points == null) _points = new List<Vector3>();
                return _points;
            }
            set => _points = value;
        }
    }
}
