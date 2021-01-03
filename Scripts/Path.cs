using System.Collections.Generic;
using UnityEngine;

namespace Spliny
{
    [System.Serializable]
    public class Path
    {
        [SerializeField, HideInInspector]
        private List<Vector3> points;
        [SerializeField, HideInInspector]
        private bool closed;
        [SerializeField, HideInInspector]
        private bool autoSetControlPoints;

        public int Segments => points.Count / 3;

        public int NumPoints => points.Count;
        public bool AutoSetControlPoints
        {
            get => autoSetControlPoints;
            set
            {
                if (autoSetControlPoints != value)
                {
                    autoSetControlPoints = value;
                    if (autoSetControlPoints)
                    {
                        AutoSetAllControlPoints();
                    }
                }
            }
        }

        public Vector3 this[int i]
        {
            get => points[i];
        }

        public bool Closed
        {
            get => closed;
            set 
            {
                closed = value;
                ToggleClosed();
            }
        }

        public Path(Vector3 center)
        {
            points = new List<Vector3>()
            {
                center + Vector3.left,
                center + (Vector3.left + Vector3.up) * .5f,
                center + (Vector3.right + Vector3.down) * .5f,
                center + Vector3.right
            };
        }

        public Path(List<Vector3> startPoints, bool state = false)
        {
            points = new List<Vector3>(startPoints);
            closed = state;
        }

        public void AddSegment(Vector3 anchor)
        {
            points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
            points.Add((points[points.Count - 1] + anchor) * .5f);
            points.Add(anchor);

            if (autoSetControlPoints)
            {
                AutosetAllaffectedControlPoints(points.Count - 1);
            }
        }

        public void SplitSegment(Vector3 anchorPos, int segmentIndex)
        {
            points.InsertRange(segmentIndex * 3 + 2, new Vector3[] {Vector3.zero, anchorPos, Vector3.zero});
            if(autoSetControlPoints)
            {
                AutosetAllaffectedControlPoints(segmentIndex * 3 + 3);
            }
            else
            {
                AutoSetAnchorControlPoint(segmentIndex * 3 + 3);
            }
        }

        public void RemoveSegment(int anchorIndex)
        {
            if (!(Segments > 2 || !closed && Segments > 1)) return;
            if (anchorIndex == 0)
            {
                if (closed)
                {
                    points[points.Count - 1] = points[2];
                }
                points.RemoveRange(0, 3);
            }
            else if (anchorIndex == points.Count - 1 && !closed)
            {
                points.RemoveRange(anchorIndex - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex - 1, 3);
            }
        }

        public Vector3[] GetPointsSegment(int i)
        {
            return new Vector3[]{points[i * 3],
                points[i * 3 + 1], 
                points[i * 3 + 2], 
                points[LoopIndex(i * 3 + 3)]};
        }

        public void MovePoint(int i, Vector3 pos)
        {
            Vector3 deltaMove = pos - points[i];

            if (i % 3 == 0 || !autoSetControlPoints)
            {
                points[i] = pos;
            }

            if (autoSetControlPoints)
            {
                AutoSetAnchorControlPoint(i);
            }
            else
            {
                // If its 0 we know we are moving an anchor point
                if (i % 3 == 0)
                {
                    // Add the offset to the control points
                    if (i + 1 < points.Count || closed)
                        points[LoopIndex(i + 1)] += deltaMove;
                    if (i - 1 >= 0 || closed)
                        points[LoopIndex(i - 1)] += deltaMove;
                }
                else
                {
                    bool nextPointIsAnchor = (i + 1) % 3 == 0;
                    int controlIndex = nextPointIsAnchor ? i + 2 : i - 2;
                    int anchorIndex = nextPointIsAnchor ? i + 1 : i - 1;

                    if (controlIndex >= 0 && controlIndex < points.Count || closed)
                    {
                        float dst = (points[LoopIndex(anchorIndex)] - points[LoopIndex(controlIndex)]).magnitude;
                        Vector3 dir = (points[LoopIndex(anchorIndex)] - pos).normalized;
                        points[LoopIndex(controlIndex)] = points[LoopIndex(anchorIndex)] + dir * dst;
                    }
                }
            }
        }

        public Vector3 EvaluatePath(float t)
        {
            // which segment and position to evaluate from
            float pathT = t * Segments;

            // Get segment to which evaluate from with a floor function to get the integer of the number
            int segment = Mathf.FloorToInt(pathT);
            // Get the decimal part of the number to use it as a value from 0 to 1 in our segment
            float segmentFractionToEvaluateFrom = Frac(pathT);

            if (segment == Segments)
            {
                segmentFractionToEvaluateFrom = 1.0f;
                segment = Segments - 1;
            }

            Vector3[] p = GetPointsSegment(segment);

            return Belzier.EvaluateCubic(p[0], p[1], p[2], p[3], segmentFractionToEvaluateFrom);
        }

        public Vector3[] EvenlySpacedPoints(float spacing, float resolution = 2)
        {
            List<Vector3> evenlySpacedPoints = new List<Vector3>();
            evenlySpacedPoints.Add(points[0]);
            Vector3 previousPoint = points[0];
            float dstSincelastPoint = 0f;

            for (int i = 0; i < Segments; i++)
            {
                Vector3[] p =  GetPointsSegment(i);
                float controlNetLength = Vector3.Distance(p[0], p[1]) + Vector3.Distance(p[1], p[2]) + Vector3.Distance(p[2], p[3]);
                float estimatedLength = Vector2.Distance(p[0], p[3]) + controlNetLength / 2;
                int divisions = Mathf.CeilToInt(estimatedLength * resolution * 10f);
                float t = 0;
                while(t <= 1)
                {
                    t += .1f / divisions;
                    Vector3 pointOnCurve =  Belzier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
                    dstSincelastPoint += Vector3.Distance(previousPoint, pointOnCurve);
                    
                    while (dstSincelastPoint >= spacing)
                    {
                        float overshootDst = dstSincelastPoint - spacing;
                        Vector3 newEvenPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDst;
                        evenlySpacedPoints.Add(newEvenPoint);
                        dstSincelastPoint = overshootDst;

                        previousPoint = newEvenPoint;
                    }

                    previousPoint = pointOnCurve;
                }
            }

            return evenlySpacedPoints.ToArray();
        }

        private void ToggleClosed()
        {
            if (closed)
            {
                points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
                points.Add(points[0] * 2 - points[1]);
                if (autoSetControlPoints)
                {
                    AutoSetAnchorControlPoint(0);
                    AutoSetAnchorControlPoint(points.Count - 3);
                }
            }
            else
            {
                points.RemoveRange(points.Count - 2, 2);
                if (autoSetControlPoints)
                {
                    AutoSetStartEndControls();
                }
            }
        }

        private void AutosetAllaffectedControlPoints(int updateControlIndex)
        {
            for (int i = updateControlIndex - 3; i <= updateControlIndex + 3; i += 3)
            {
                if (i >= 0 && i < points.Count || closed)
                {
                    AutoSetAnchorControlPoint(LoopIndex(i));
                }
            }

            AutoSetStartEndControls();
        }

        private void AutoSetAllControlPoints()
        {
            for (int i = 0; i < points.Count; i += 3)
            {
                AutoSetAnchorControlPoint(i);
            }

            AutoSetStartEndControls();
        }

        private void AutoSetAnchorControlPoint(int anchorIndex)
        {
            Vector3 anchorPos = points[anchorIndex];
            Vector3 dir = Vector3.zero;
            float[] neighborDst = new float[2];

            if (anchorIndex - 3 >= 0 || closed)
            {
                Vector3 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
                dir += offset.normalized;
                neighborDst[0] = offset.magnitude;
            }
            if (anchorIndex + 3 >= 0 || closed)
            {
                Vector3 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
                dir -= offset.normalized;
                neighborDst[1] = -offset.magnitude;
            }

            dir.Normalize();

            for (int i = 0; i < 2; i++)
            {
                int controlIndex = anchorIndex + i * 2 - 1;
                if (controlIndex >= 0 && controlIndex < points.Count || closed)
                {
                    points[LoopIndex(controlIndex)] = anchorPos + dir * neighborDst[i] * .5f;
                }
            }
        }

        private void AutoSetStartEndControls()
        {
            if (!closed)
            {
                points[1] = (points[0] + points[2]) * .5f;
                points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * .5f;
            }
        }

        private int LoopIndex(int i)
        {
            return (i + points.Count) % points.Count;
        }

        public static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }
    }
}
