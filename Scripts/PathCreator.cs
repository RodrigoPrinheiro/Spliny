using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spliny
{
    public class PathCreator : MonoBehaviour
    {
        [HideInInspector]
        public Path path;

        public void CreatePath()
        {
            path = new Path(transform.position);
        }

        public void CreatePath(List<Vector3> points, bool state = false)
        {
            path = new Path(points, state);
        }
        private void Reset()
        {
            CreatePath();
        }

        public void LoadPath(PathData data)
        {
            CreatePath(data.Points, data.Closed);
        }
    }
}