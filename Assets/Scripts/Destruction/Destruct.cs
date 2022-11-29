using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Destruct
{
    enum Side : uint
    {
        AllBelow = 0,
        AllAbove = 7
    }

    public class SplitResult
    {
        public List<Vector3> vertices;
        public List<int> triangles;

        public SplitResult() { }

        public SplitResult(List<Vector3> vertices)
        {
            this.vertices = new(vertices);
            triangles = new();
        }

        public SplitResult(List<Vector3> vertices, List<int> triangles)
        {
            this.vertices = new(vertices);
            this.triangles = new(triangles);
        }
    }

    public class Loop
    {
        public Dictionary<int, int> forwardLinks;
        public Dictionary<int, int> backwardLinks;
        public int first;
        public int last;

        public Loop()
        {
            this.forwardLinks = new();
            this.backwardLinks = new();
            this.first = -1;
            this.last = -1;
        }

        public Loop(Dictionary<int, int> forwardLinks, Dictionary<int, int> backwardLinks, int first, int last)
        {
            this.forwardLinks = forwardLinks;
            this.backwardLinks = backwardLinks;
            this.first = first;
            this.last = last;
        }

        public int Next(int index)
        {
            if (index == last) return first;
            return forwardLinks[index];
        }

        public int Prev(int index)
        {
            if (index == first) return last;
            return backwardLinks[index];
        }

        public void AddLink(int start, int end)
        {
            forwardLinks.Add(start, end);
            backwardLinks.Add(end, start);
            if (!forwardLinks.ContainsKey(end)) last = end;
            if (!backwardLinks.ContainsKey(start)) first = start;
        }

        public void SetLast(int newLast)
        {
            forwardLinks[backwardLinks[last]] = newLast;
            backwardLinks.Add(newLast, backwardLinks[last]);
            backwardLinks.Remove(last);
            last = newLast;
        }

        public void RemoveLink(int index)
        {
            if (index == last)
            {
                forwardLinks.Remove(backwardLinks[index]);
                last = backwardLinks[index];
                backwardLinks.Remove(index);
            }
            else if (index == first)
            {
                backwardLinks.Remove(forwardLinks[index]);
                first = forwardLinks[index];
                forwardLinks.Remove(index);
            }
            else
            {
                forwardLinks[backwardLinks[index]] = forwardLinks[index];
                backwardLinks[forwardLinks[index]] = backwardLinks[index];
                forwardLinks.Remove(index);
                backwardLinks.Remove(index);
            }
        }

        public List<int> ToList()
        {
            List<int> result = new(forwardLinks.Count + 1);
            int index = first;
            while (index != last)
            {
                result.Add(index);
                index = Next(index);
            }
            result.Add(index);
            return result;
        }

        public void Clear()
        {
            forwardLinks.Clear();
            backwardLinks.Clear();
            first = -1;
            last = -1;
        }
    }

    public static class Static
    {
        public static readonly Vector3[] cubeNormals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        public static readonly Vector3[] regularDodecahedron = {
            Vector3.down,
            Quaternion.Euler(116.56505f, 0f     , 0f) * Vector3.down,
            Quaternion.Euler(116.56505f, 72f    , 0f) * Vector3.down,
            Quaternion.Euler(116.56505f, 72f*2  , 0f) * Vector3.down,
            Quaternion.Euler(116.56505f, 72f*3  , 0f) * Vector3.down,
            Quaternion.Euler(116.56505f, 72f*4  , 0f) * Vector3.down,
            Vector3.up,
            Quaternion.Euler(116.56505f, 0f     , 0f) * Vector3.up,
            Quaternion.Euler(116.56505f, 72f    , 0f) * Vector3.up,
            Quaternion.Euler(116.56505f, 72f*2  , 0f) * Vector3.up,
            Quaternion.Euler(116.56505f, 72f*3  , 0f) * Vector3.up,
            Quaternion.Euler(116.56505f, 72f*4  , 0f) * Vector3.up,
        };

        public static void Destruct(IDestructible script, Vector3 origin, float size)
        {
            script.PreDestruct();
            MeshFilter mFilter = script.GetMeshFilter();
            List<Vector3> v = new();
            List<int> t = new();
            List<Vector2> u = new();
            mFilter.mesh.GetVertices(v);
            mFilter.mesh.GetTriangles(t, 0);

            var cut = CutOutShape(v, t, new Pose(origin, Quaternion.identity), size, Static.regularDodecahedron);

            List<SplitResult> result = Fracture(cut.Item2.vertices, cut.Item2.triangles, new Bounds(origin, Vector3.one * size / 2f));

            RemoveLooseVertices(ref cut.Item1.vertices, ref cut.Item1.triangles);
            RemoveLooseVertices(ref result);

            List<GameObject> objects = InstantiateObjectsFromSplitResults(result, new Pose(mFilter.transform.position, mFilter.transform.rotation), mFilter.GetComponent<MeshRenderer>().material);

            script.PostDestruct((cut.Item1, objects));
        }

        public static List<SplitResult> Fracture(List<Vector3> vertices, List<int> triangles, Bounds bounds)
        {
            List<SplitResult> result = new();
            result.Add(new SplitResult(vertices, triangles));

            List<Plane> planes = new();
            for (int i = 0; i < 8; i++)
            {
                planes.Add(
                    new Plane(Random.onUnitSphere,
                    new Vector3(
                        UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                        UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                        UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                    )));
            }

            foreach (Plane pl in planes)
            {
                List<SplitResult> newResult = new();
                foreach (SplitResult r in result)
                {
                    var (up, down) = SplitMeshAlongPlane(r.vertices, r.triangles, pl);
                    if (up.triangles.Count > 0) newResult.Add(up);
                    if (down.triangles.Count > 0) newResult.Add(down);
                }
                result = newResult;
            }

            return result;
        }

        public static (SplitResult, SplitResult) CutOutShape(List<Vector3> vertices, List<int> triangles, Pose pose, float sideLenght, Vector3[] normals)
        {
            SplitResult cube = new(vertices, triangles);
            SplitResult notCube = new(vertices, new());

            List<Plane> planes = new(6);

            foreach (Vector3 normal in normals)
            {
                planes.Add(new Plane(pose.rotation * normal, normal * sideLenght / 2f + pose.position));
            }

            foreach (Plane plane in planes)
            {
                Debug.DrawLine(pose.position, pose.position + plane.normal, Color.red, 120f);
                var result = SplitMeshAlongPlane(cube.vertices, cube.triangles, plane);
                cube = result.Item2;
                notCube.vertices.AddRange(result.Item1.vertices.GetRange(notCube.vertices.Count, result.Item1.vertices.Count - notCube.vertices.Count));
                notCube.triangles.AddRange(result.Item1.triangles);
            }

            return (notCube, cube);
        }

        public static (SplitResult, SplitResult) SplitMeshAlongPlane(List<Vector3> vertices, List<int> triangles, Plane plane, bool fill = true) // 1st mesh is above plane
        {
            (SplitResult, SplitResult) result = (new(vertices), new(vertices));
            (Dictionary<int, int>, Dictionary<int, int>) adjacentVertices = (new(vertices.Count), new(vertices.Count));
            Dictionary<(int, int), (int, int)> splitEdges = new(new EdgeComparer());
            BitArray vSides = new BitArray(vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
            {
                vSides[i] = plane.GetSide(vertices[i]);
            }
            List<(int, int)> newPoints = new();

            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3[] vPos = { vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]] };

                /*
                 * 000 = 0  All vertices below plane
                 * 
                 * 001 = 1
                 * 010 = 2
                 * 011 = 3
                 * 
                 * 100 = 4
                 * 101 = 5
                 * 110 = 6
                 * 
                 * 111 = 7  All vertices above plane
                 * 
                 * I am really not sure if this is necessary, but better than an array and a bunch of if statements
                 */
                uint vSide = 0;
                vSide |= Convert.ToUInt32(vSides[triangles[i]]);
                vSide |= Convert.ToUInt32(vSides[triangles[i + 1]]) << 1;
                vSide |= Convert.ToUInt32(vSides[triangles[i + 2]]) << 2;

                switch ((Side)vSide)
                {
                    case Side.AllBelow:
                        result.Item2.triangles.Add(triangles[i]);
                        result.Item2.triangles.Add(triangles[i + 1]);
                        result.Item2.triangles.Add(triangles[i + 2]);
                        break;

                    case Side.AllAbove:
                        result.Item1.triangles.Add(triangles[i]);
                        result.Item1.triangles.Add(triangles[i + 1]);
                        result.Item1.triangles.Add(triangles[i + 2]);
                        break;

                    default:
                        /*
                        *          Lone(0)
                        *          /  \         This side can be above or below the plane
                        *         0    2        loneIsAbove tell us where
                        *    ----------------
                        *       1        3
                        *      /          \
                        * Previous(2)------Next(1)
                        *   
                        *   SplitEdges go:
                        *   Vertice A
                        *   Vertice B
                        *   New Vertice to A
                        *   New Vertice to B
                        */

                        // Gives position of the lone vertice
                        int loneV = (int)vSide - 1;
                        if (loneV > 2) loneV = 5 - loneV;

                        //              Lone                    Next                           Previous
                        int[] t = { triangles[i + loneV], triangles[i + (loneV + 1) % 3], triangles[i + (loneV + 2) % 3] };
                        int[] newV = { -1, -1, -1, -1 };

                        bool loneIsAbove = Convert.ToBoolean(vSide & (0x001 << loneV));

                        (int, int) split;
                        if (!splitEdges.TryGetValue((t[0], t[2]), out split))
                        {
                            result.Item1.vertices.Add(LinePlaneIntersection(vPos[loneV], vPos[(loneV + 2) % 3], plane));
                            newV[0] = result.Item1.vertices.Count - 1;
                            result.Item2.vertices.Add(result.Item1.vertices[^1]);
                            newV[1] = result.Item2.vertices.Count - 1;


                            splitEdges.Add((t[0], t[2]), (newV[0], newV[1]));

                        }
                        else
                        {
                            newV[0] = split.Item1;
                            newV[1] = split.Item2;
                        }

                        if (!splitEdges.TryGetValue((t[0], t[1]), out split))
                        {

                            result.Item1.vertices.Add(LinePlaneIntersection(vPos[loneV], vPos[(loneV + 1) % 3], plane));
                            newV[2] = result.Item1.vertices.Count - 1;
                            result.Item2.vertices.Add(result.Item1.vertices[^1]);
                            newV[3] = result.Item2.vertices.Count - 1;



                            splitEdges.Add((t[0], t[1]), (newV[2], newV[3]));
                        }
                        else
                        {
                            newV[2] = split.Item1;
                            newV[3] = split.Item2;
                        }

                        if (loneIsAbove)
                        {
                            result.Item1.triangles.Add(t[0]);
                            result.Item1.triangles.Add(newV[2]);
                            result.Item1.triangles.Add(newV[0]);

                            result.Item2.triangles.Add(t[1]);
                            result.Item2.triangles.Add(newV[1]);
                            result.Item2.triangles.Add(newV[3]);

                            result.Item2.triangles.Add(t[2]);
                            result.Item2.triangles.Add(newV[1]);
                            result.Item2.triangles.Add(t[1]);

                            adjacentVertices.Item1.Add(newV[0], newV[2]);
                            adjacentVertices.Item2.Add(newV[3], newV[1]);
                        }
                        else
                        {
                            result.Item2.triangles.Add(t[0]);
                            result.Item2.triangles.Add(newV[2]);
                            result.Item2.triangles.Add(newV[0]);

                            result.Item1.triangles.Add(t[1]);
                            result.Item1.triangles.Add(newV[1]);
                            result.Item1.triangles.Add(newV[3]);

                            result.Item1.triangles.Add(t[2]);
                            result.Item1.triangles.Add(newV[1]);
                            result.Item1.triangles.Add(t[1]);

                            adjacentVertices.Item1.Add(newV[3], newV[1]);
                            adjacentVertices.Item2.Add(newV[0], newV[2]);
                        }

                        newPoints.Add((newV[0], newV[1]));
                        newPoints.Add((newV[2], newV[3]));
                        break;
                }

            }

            List<Loop> loopsUp = EdgesToLoops(result.Item1.vertices, adjacentVertices.Item1);

            foreach (Loop loop in loopsUp)
            {
                List<int> filler = EarClippingTriangulation(Vector3ontoPlane(result.Item1.vertices, loop.ToList(), plane), loop);
                foreach (int v in filler)
                {
                    result.Item1.vertices.Add(result.Item1.vertices[v]);
                    result.Item1.triangles.Add(result.Item1.vertices.Count - 1);
                }
                RevertTriangles(ref filler);
                foreach (int v in filler)
                {
                    result.Item2.vertices.Add(result.Item2.vertices[v]);
                    result.Item2.triangles.Add(result.Item2.vertices.Count - 1);
                }
            }

            return result;
        }

        public static List<GameObject> InstantiateObjectsFromSplitResults(List<SplitResult> results, Pose pose, Material mat, bool withRigidBody = true)
        {
            List<GameObject> newObjects = new(results.Count);
            foreach (SplitResult res in results)
            {
                var obj = CreateGameObjectFromMeshData("Fragment", pose, res.vertices, res.triangles, mat);
                if (withRigidBody)
                {
                    var col = obj.AddComponent<MeshCollider>();
                    col.convex = true;
                    obj.AddComponent<Rigidbody>();
                }
                newObjects.Add(obj);
            }
            return newObjects;
        }

        public static Vector3 LinePlaneIntersection(Vector3 linePointA, Vector3 linePointB, Plane plane)
        {
            Vector3 lineNormal = (linePointB - linePointA).normalized;

            if (linePointA == linePointB)
                return linePointB;

            return linePointA
                - lineNormal
                * (Vector3.Dot(linePointA + plane.normal * plane.distance, plane.normal)
                / Vector3.Dot(lineNormal, plane.normal));
        }

        public static List<Vector2> Vector3ontoPlane(List<Vector3> vertices, List<int> points, Plane plane)
        {
            Quaternion planeRotation = Quaternion.FromToRotation(plane.normal, Vector3.forward);

            List<Vector2> projectedVertices = new(vertices.Count);
            for (int i = 0; i < vertices.Count; i++) projectedVertices.Add(new Vector2(float.NaN, float.NaN));

            foreach (int idx in points)
            {
                Vector3 temp = planeRotation * vertices[idx];
                projectedVertices[idx] = new Vector2(temp.x, temp.y);
            }

            return projectedVertices;
        }

        public static void RevertTriangles(ref List<int> triangles)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                (triangles[i], triangles[i + 1]) = (triangles[i + 1], triangles[i]);
            }
        }

        public static List<Loop> EdgesToLoops(List<Vector3> vertices, Dictionary<int, int> adjacentVertices)
        {
            List<Loop> result = new();

            while (adjacentVertices.Count > 0)
            {
                Loop loop = new();
                var iter = adjacentVertices.GetEnumerator();
                iter.MoveNext();
                loop.AddLink(iter.Current.Key, iter.Current.Value);
                adjacentVertices.Remove(iter.Current.Key);

                while (adjacentVertices.ContainsKey(loop.last) && adjacentVertices[loop.last] != loop.first)
                {
                    int previousLast = loop.last;
                    if (vertices[loop.last] == vertices[adjacentVertices[loop.last]])
                    {
                        loop.SetLast(adjacentVertices[loop.last]);
                    }
                    else
                    {
                        loop.AddLink(loop.last, adjacentVertices[loop.last]);
                    }
                    adjacentVertices.Remove(previousLast);
                }

                if (adjacentVertices.ContainsKey(loop.last)) adjacentVertices.Remove(loop.last);

                result.Add(loop);
            }

            HashSet<int> unevaluatedLoops = new(result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                if (vertices[result[i].first] != vertices[result[i].last])
                {
                    unevaluatedLoops.Add(i);
                }
                else
                {
                    result[i].Clear();
                }
            }

            for (int i = 0; i < result.Count; i++)
            {
                if (!unevaluatedLoops.Contains(i)) continue;
                unevaluatedLoops.Remove(i);

                int matchingLoop = -1;

                foreach (int loop in unevaluatedLoops)
                {
                    if ((vertices[result[loop].first] == vertices[result[i].last])
                        || (vertices[result[loop].last] == vertices[result[i].first]))
                    {
                        matchingLoop = loop;
                        break;
                    }
                }

                while (matchingLoop != -1)
                {

                    result[matchingLoop].forwardLinks.ToList().ForEach(x => result[i].forwardLinks.Add(x.Key, x.Value));
                    result[matchingLoop].backwardLinks.ToList().ForEach(x => result[i].backwardLinks.Add(x.Key, x.Value));

                    if (vertices[result[matchingLoop].last] == vertices[result[i].first])
                    {
                        if (result[matchingLoop].last != result[i].first) result[i].AddLink(result[matchingLoop].last, result[i].first);
                        result[i].first = result[matchingLoop].first;
                    }
                    else
                    {
                        if (result[matchingLoop].first != result[i].last) result[i].AddLink(result[i].last, result[matchingLoop].first);
                        result[i].last = result[matchingLoop].last;
                    }

                    result[matchingLoop].Clear();
                    unevaluatedLoops.Remove(matchingLoop);

                    matchingLoop = -1;

                    foreach (int loop in unevaluatedLoops)
                    {
                        if ((vertices[result[loop].first] == vertices[result[i].last])
                            || (vertices[result[loop].last] == vertices[result[i].first]))
                        {
                            matchingLoop = loop;
                            break;
                        }
                    }
                }
            }

            result.RemoveAll(loop => loop.first == -1);

            foreach (Loop loop in result)
            {
                int index = loop.Next(loop.first);
                do
                {
                    while (vertices[index] == vertices[loop.Prev(index)]) loop.RemoveLink(loop.Prev(index));
                    index = loop.Next(index);
                } while (index != loop.Next(loop.first));
            }

            return result;
        }

        public static List<int> DumbFillLoops(List<Loop> loops)
        {
            List<int> result = new(4096);
            foreach (var loop in loops)
            {
                int previousIndex = loop.forwardLinks[loop.first];
                int currentIndex = loop.forwardLinks[previousIndex];
                while (currentIndex != loop.first && currentIndex != loop.last)
                {
                    result.Add(loop.first);
                    result.Add(previousIndex);
                    result.Add(currentIndex);
                    previousIndex = currentIndex;
                    currentIndex = loop.forwardLinks[currentIndex];
                }
            }
            return result;
        }

        public static List<int> EarClippingTriangulation(List<Vector2> vertices, Loop loop)
        {
            List<int> result = new();
            int index = loop.first;

#if DEBUG
            int notRemovedCounter = 0;
#endif

            while (loop.forwardLinks.Count > 1)
            {
                int next = loop.Next(index);
                int prev = loop.Prev(index);
                Vector2 a = vertices[prev] - vertices[index];
                Vector2 b = vertices[next] - vertices[index];

                float crossZ = a.x * b.y - a.y * b.x;

                if (crossZ > 0)
                {
                    bool isValid = true;
                    int it = loop.Next(next);
                    while (it != prev)
                    {
                        if (PointInTriangle(vertices[it], vertices[next], vertices[index], vertices[prev]))
                        {
                            isValid = false;
                            break;
                        }
                        it = loop.Next(it);
                    }
                    if (isValid)
                    {
                        result.Add(index);
                        result.Add(next);
                        result.Add(prev);
                        loop.RemoveLink(index);
#if DEBUG
                        notRemovedCounter = 0;
#endif
                    }
                }
                else if (Mathf.Approximately(crossZ, 0f))
                {
                    loop.RemoveLink(index);
#if DEBUG
                    notRemovedCounter = 0;
#endif
                }

#if DEBUG
                else
                {
                    notRemovedCounter++;
                }
                if (notRemovedCounter > loop.forwardLinks.Count)
                {
                    throw new Exception();
                }
#endif

                index = next;
            }

            return result;
        }

        public static GameObject CreateGameObjectFromMeshData(string name, Pose pose, List<Vector3> vertices, List<int> triangles, Material mat)
        {

            GameObject obj = new GameObject(name);

            obj.transform.position = pose.position;
            obj.transform.rotation = pose.rotation;

            MeshFilter mFilter = obj.AddComponent<MeshFilter>();
            mFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            MeshRenderer mRenderer = obj.AddComponent<MeshRenderer>();

            mFilter.mesh.SetVertices(vertices);
            mFilter.mesh.MarkDynamic();
            Material[] materials = mRenderer.materials;
            materials[0] = mat;
            mRenderer.materials = materials;
            mFilter.mesh.SetTriangles(triangles, 0);
            mFilter.mesh.RecalculateNormals();
            mFilter.mesh.RecalculateTangents();
            return obj;
        }

        public static void RemoveLooseVertices(ref List<Vector3> vertices, ref List<int> triangles)
        {
            List<Vector3> newVertices = new(vertices.Count);
            List<int> newVerticePositions = Enumerable.Repeat(0, vertices.Count).ToList();

            HashSet<int> usedVertices = new(triangles);

            for (int i = 0; i < vertices.Count; i++)
            {
                if (usedVertices.Contains(i))
                {
                    newVertices.Add(vertices[i]);
                    newVerticePositions[i] = newVertices.Count - 1;
                }
            }

            for (int i = 0; i < triangles.Count; i++)
            {
                triangles[i] = newVerticePositions[triangles[i]];
            }

            vertices = newVertices;
        }

        public static void RemoveLooseVertices(ref List<SplitResult> results)
        {
            foreach (SplitResult res in results)
            {
                RemoveLooseVertices(ref res.vertices, ref res.triangles);
            }
        }

        public static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        public static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1, d2, d3;
            bool has_neg, has_pos;

            d1 = Sign(pt, v1, v2);
            d2 = Sign(pt, v2, v3);
            d3 = Sign(pt, v3, v1);

            has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }


        #region Stuff

        public static void RemoveLooseVerticesFromList(ref List<SplitResult> splitResults, int index)
        {
            List<Vector3> newVertices = new(splitResults[index].vertices.Count);
            List<int> newVerticePositions = Enumerable.Repeat(0, splitResults[index].vertices.Count).ToList();

            HashSet<int> usedVertices = new(splitResults[index].triangles);

            for (int i = 0; i < splitResults[index].vertices.Count; i++)
            {
                if (usedVertices.Contains(i))
                {
                    newVertices.Add(splitResults[index].vertices[i]);
                    newVerticePositions[i] = newVertices.Count - 1;
                }
            }

            for (int i = 0; i < splitResults[index].triangles.Count; i++)
            {
                splitResults[index].triangles[i] = newVerticePositions[splitResults[index].triangles[i]];
            }
            splitResults[index].vertices.Clear();
            splitResults[index].vertices.AddRange(newVertices);
        }


        public static List<SplitResult> LinearFracture(List<Vector3> vertices, List<int> triangles, int linesNumber, int minSectionsCount, int maxSectionsCount, float heightError, float lengthError, float XSizeMultiplier, float YSizeMultiplier, int seed,
            float patternRotation, Vector3 patternOrientation, Vector3 patternOffSet)
        {
            List<SplitResult> result = new List<SplitResult>();

            for (int i = 0; i < linesNumber + 1; i++)
            {
                result.Add(new SplitResult
                {
                    triangles = new List<int>(),
                    vertices = new List<Vector3>()
                });
            }

            List<List<Vector3>> pattern = new List<List<Vector3>>();
            GeneratePattern(ref pattern, linesNumber, minSectionsCount, maxSectionsCount, heightError, lengthError, XSizeMultiplier, YSizeMultiplier, seed);


            SplitResult sparePartWorkObj;
            SplitResult top;
            SplitResult bottom;
            SplitResult restWorkObj = new SplitResult { vertices = vertices, triangles = triangles };

            // place pattern in right position
            Vector3 patternDirection;
            Vector3 linesDirection;
            RotateAndTranslatePattern(ref pattern, patternRotation, patternOrientation, patternOffSet, out patternDirection, out linesDirection);

            Debug.DrawLine(Vector3.zero, patternDirection, Color.red, duration: 100f);

            // iterate by lines
            for (int lineNum = 1; lineNum <= linesNumber; lineNum++)
            {
                sparePartWorkObj = new SplitResult { vertices = new List<Vector3>(), triangles = new List<int>() };
                //iterate by sections
                for (int pointNum = 1; pointNum <= pattern[lineNum - 1].Count - 1; pointNum++)
                {
                    Vector3 chainSegmentVector = pattern[lineNum - 1][pointNum - 1] - pattern[lineNum - 1][pointNum];
                    // devide plane by chain
                    Vector3 buffer = Vector3.Cross(linesDirection, chainSegmentVector);
                    buffer = Vector3.Cross(chainSegmentVector, buffer);
                    (top, bottom) = SplitMeshAlongPlane(restWorkObj.vertices, restWorkObj.triangles,
                        new Plane(buffer, pattern[lineNum - 1][pointNum - 1]));

                    // see the direction of plane
                    // if it goes up, use bottom part for further use
                    if ((linesDirection + chainSegmentVector).magnitude <= linesDirection.magnitude)
                    {
                        // add old part to the rest of mesh
                        AddSplitResultToSplitResult(ref sparePartWorkObj, ref top, false);
                        restWorkObj = bottom;

                        if (lineNum == linesNumber)
                            AddSplitResultOutListToSplitResultInList(ref result, lineNum - 1, ref restWorkObj, false);

                    }
                    // if goes down, use top half for further use
                    else
                    {
                        AddSplitResultOutListToSplitResultInList(ref result, lineNum - 1, ref bottom, false);
                        restWorkObj = top;

                        if (lineNum == linesNumber)
                            AddSplitResultToSplitResult(ref sparePartWorkObj, ref restWorkObj, false);
                    }
                }
                restWorkObj = sparePartWorkObj;
            }


            AddSplitResultOutListToSplitResultInList(ref result, linesNumber, ref restWorkObj, false);
            return result;
        }


        public static (SplitResult, SplitResult) CutOutSphereFromMesh(List<Vector3> vertices, List<int> triangles, Vector3 offSet, int xRotations, int yRotations, float midPerpenLen, bool isSmooth = false)
        {

            Vector3 pencilVector = Vector3.up;
            Vector3 pencilVectorBuffer = pencilVector;

            Quaternion rotateStuffX = Quaternion.Euler(360f / xRotations, 0, 0);
            Quaternion rotateStuffY = Quaternion.AngleAxis(360f / yRotations, pencilVector);

            List<Plane> cutPlanes = new List<Plane>();

            List<Vector3> centerVectors = new List<Vector3>();

            centerVectors.Add(pencilVector);
            if (xRotations % 2 == 0)
                centerVectors.Add(-pencilVector);

            List<Vector3> sideVectors = new List<Vector3>();

            if (xRotations % 2 == 0)
            {
                for (int i = 0; i < xRotations / 2 - 1; i++)
                {
                    pencilVector = rotateStuffX * pencilVector;
                    sideVectors.Add(pencilVector);
                }

                sideVectors.AddRange(sideVectors.Select(o => -o).ToList());
            }
            else
            {
                for (int i = 0; i < (xRotations - 1) / 2; i++)
                {
                    pencilVector = rotateStuffX * pencilVector;
                    sideVectors.Add(pencilVector);
                }
            }

            if (yRotations % 2 == 0)
            {
                List<Vector3> sideBuffer = sideVectors;
                for (int i = 0; i < yRotations / 2 - 1; i++)
                {
                    sideBuffer = sideVectors.Select(o => rotateStuffY * o).ToList();
                    sideVectors.AddRange(sideBuffer);
                }
            }
            else
            {
                List<Vector3> sideBuffer = sideVectors;
                for (int i = 0; i < (xRotations - 1); i++)
                {
                    sideBuffer = sideBuffer.Select(o => rotateStuffY * o).ToList();
                    sideVectors.AddRange(sideBuffer);
                }
            }

            sideVectors.AddRange(centerVectors);

            sideVectors = sideVectors.Select(o => o.normalized).ToList();

            foreach (Vector3 sideVector in sideVectors)
            {
                Plane plane = new Plane(sideVector, midPerpenLen);
                plane.Translate(-offSet);

                Debug.DrawLine(Vector3.zero, sideVector * midPerpenLen, Color.red, duration: 100f);

                if (!cutPlanes.Contains(plane))
                    cutPlanes.Add(plane);
            }

            (SplitResult hole, SplitResult theRest) = SplitMeshAlongPlane(vertices, triangles, cutPlanes[0]);

            SplitResult buffer = new SplitResult { vertices = new List<Vector3>(), triangles = new List<int>() };


            for (int i = 1; i < cutPlanes.Count; i++)
            {
                (hole, buffer) = SplitMeshAlongPlane(hole.vertices, hole.triangles, cutPlanes[i]);
                AddSplitResultToSplitResult(ref theRest, ref buffer, isSmooth);
            }

            return (theRest, hole);
        }

        public static (SplitResult, SplitResult) CutOutCylinderFromMesh(List<Vector3> vertices, List<int> triangles, Vector3 offSet, Vector3 direction, int sideCount, float midPerpenLen, float frontLength = 0f, float rearLength = 0f, bool isSmooth = false)
        {

            Vector3 virtualXaxis = direction.normalized;
            Vector3 virtualYaxis = Vector3.Cross(Vector3.up, virtualXaxis).normalized;

            Quaternion rotateStuff = Quaternion.AngleAxis(360f / sideCount, virtualXaxis);

            List<Plane> cutPlanes = new List<Plane>();

            for (int i = 0; i < sideCount; i++)
            {
                Plane plane = new Plane(virtualYaxis, midPerpenLen);
                plane.Translate(-offSet);
                cutPlanes.Add(plane);
                cutPlanes[i].Translate(-offSet);
                virtualYaxis = rotateStuff * virtualYaxis;
            }


            Plane planeForward = new Plane();
            Plane planeBackward = new Plane();

            if (frontLength + rearLength > 0)
            {
                planeForward = new Plane(virtualXaxis, frontLength);
                planeBackward = new Plane(-virtualXaxis, rearLength);
                planeForward.Translate(-offSet);
                planeBackward.Translate(-offSet);
            }


            if (frontLength + rearLength > 0)
            {

                (SplitResult buffer, SplitResult restFront) = SplitMeshAlongPlane(vertices, triangles, planeForward);
                (SplitResult hole, SplitResult theRest) = SplitMeshAlongPlane(buffer.vertices, buffer.triangles, planeBackward);

                AddSplitResultToSplitResult(ref theRest, ref restFront, isSmooth);

                for (int i = 0; i < sideCount; i++)
                {
                    (hole, buffer) = SplitMeshAlongPlane(hole.vertices, hole.triangles, cutPlanes[i]);
                    AddSplitResultToSplitResult(ref theRest, ref buffer, isSmooth);
                }

                return (theRest, hole);
            }
            else
            {
                (SplitResult hole, SplitResult theRest) = SplitMeshAlongPlane(vertices, triangles, cutPlanes[0]);

                SplitResult buffer = new SplitResult { vertices = new List<Vector3>(), triangles = new List<int>() };

                for (int i = 1; i < sideCount; i++)
                {
                    (hole, buffer) = SplitMeshAlongPlane(hole.vertices, hole.triangles, cutPlanes[i]);
                    AddSplitResultToSplitResult(ref theRest, ref buffer, isSmooth);
                }

                return (theRest, hole);
            }
        }

        // Split result operations
        public static void AddSplitResultToSplitResult(ref SplitResult result1, ref SplitResult result2, bool isSmooth)
        {
            List<int> triangles = new List<int>();
            List<Vector3> vertecies = result2.vertices;

            Dictionary<Vector3, int> indexes = new Dictionary<Vector3, int>();

            if (isSmooth)
            {
                for (int i = 0; i < result1.vertices.Count; i++)
                {
                    if (!indexes.ContainsKey(result1.vertices[i]))
                        indexes.Add(result1.vertices[i], i);
                }
            }

            for (int i = 0; i < result2.triangles.Count; i++)
            {
                if (isSmooth)
                {
                    Vector3 point = result2.vertices[result2.triangles[i]];
                    int index;
                    if (indexes.TryGetValue(point, out index))
                    {
                        triangles.Add(index);
                    }
                    else
                        triangles.Add(result2.triangles[i] + result1.vertices.Count);
                }
                else
                    triangles.Add(result2.triangles[i] + result1.vertices.Count);
            }

            result1.vertices.AddRange(result2.vertices);
            result1.triangles.AddRange(triangles);
        }

        public static void AddSplitResultOutListToSplitResultInList(ref List<SplitResult> list1, int index1, ref SplitResult split, bool isSmooth)
        {

            List<int> triangles = new List<int>();

            Dictionary<Vector3, int> indexes = new Dictionary<Vector3, int>();

            if (isSmooth)
            {
                for (int i = 0; i < list1[index1].vertices.Count; i++)
                {
                    if (!indexes.ContainsKey(list1[index1].vertices[i]))
                        indexes.Add(list1[index1].vertices[i], i);
                }
            }

            for (int i = 0; i < split.triangles.Count; i++)
            {
                if (isSmooth)
                {
                    Vector3 point = split.vertices[split.triangles[i]];
                    int index;
                    if (indexes.TryGetValue(point, out index))
                    {
                        triangles.Add(index);
                    }
                    else
                        triangles.Add(split.triangles[i] + list1[index1].vertices.Count);
                }
                else
                    triangles.Add(split.triangles[i] + list1[index1].vertices.Count);
            }

            list1[index1].vertices.AddRange(split.vertices);
            list1[index1].triangles.AddRange(triangles);


        }

        public static void AddSplitResultInListToSplitResultOutList(ref SplitResult split, ref List<SplitResult> list, int index, bool isSmooth)
        {


            List<int> triangles = new List<int>();

            Dictionary<Vector3, int> indexes = new Dictionary<Vector3, int>();

            if (isSmooth)
            {
                for (int i = 0; i < split.vertices.Count; i++)
                {
                    if (!indexes.ContainsKey(split.vertices[i]))
                        indexes.Add(split.vertices[i], i);
                }
            }

            for (int i = 0; i < list[index].triangles.Count; i++)
            {
                if (isSmooth)
                {
                    Vector3 point = list[index].vertices[split.triangles[i]];
                    int id;
                    if (indexes.TryGetValue(point, out id))
                    {
                        triangles.Add(id);
                    }
                    else
                        triangles.Add(list[index].triangles[i] + split.vertices.Count);
                }
                else
                    triangles.Add(list[index].triangles[i] + split.vertices.Count);
            }

            split.vertices.AddRange(list[index].vertices);
            split.triangles.AddRange(triangles);


        }

        public static void AddSplitResultInListToSplitResultInList(ref List<SplitResult> list1, int index1, ref List<SplitResult> list2, int index2, bool isSmooth)
        {

            List<int> triangles = new List<int>();

            Dictionary<Vector3, int> indexes = new Dictionary<Vector3, int>();

            if (isSmooth)
            {
                for (int i = 0; i < list1[index1].vertices.Count; i++)
                {
                    if (!indexes.ContainsKey(list1[index1].vertices[i]))
                        indexes.Add(list1[index1].vertices[i], i);
                }
            }

            for (int i = 0; i < list2[index2].triangles.Count; i++)
            {
                if (isSmooth)
                {
                    Vector3 point = list2[index2].vertices[list2[index2].triangles[i]];
                    int index;
                    if (indexes.TryGetValue(point, out index))
                    {
                        triangles.Add(index);
                    }
                    else
                        triangles.Add(list2[index2].triangles[i] + list1[index1].vertices.Count);
                }
                else
                    triangles.Add(list2[index2].triangles[i] + list1[index1].vertices.Count);
            }

            list1[index1].vertices.AddRange(list2[index2].vertices);
            list1[index1].triangles.AddRange(triangles);

        }

        public static void GlueSplitResultToSplitResult(ref SplitResult result1, ref SplitResult result2, ref SplitResult original)
        {
            List<int> triangles = new List<int>();
            List<Vector3> vertecies = result2.vertices;

            Dictionary<Vector3, int> indexes = new Dictionary<Vector3, int>();


            for (int i = 0; i < result2.triangles.Count; i++)
            {
                triangles.Add(result2.triangles[i] + result1.vertices.Count);
            }

            result1.vertices.AddRange(result2.vertices);
            result1.triangles.AddRange(triangles);
        }

        // Pattern operations
        public static void RotateAndTranslatePattern(ref List<List<Vector3>> pattern, float patternRotation, Vector3 patternOrientation, Vector3 patternOffSet, out Vector3 patternDirection, out Vector3 linesDirection)
        {
            float xDegree = Vector3.Angle(new Vector3(patternOrientation.x, patternOrientation.y, 0), Vector3.left);
            float yDegree = Vector3.Angle(new Vector3(patternOrientation.x, 0, patternOrientation.z), Vector3.forward);

            Quaternion rotateStuff = Quaternion.Euler(xDegree, yDegree, patternRotation);

            patternDirection = rotateStuff * Vector3.right;
            linesDirection = rotateStuff * Vector3.up;

            for (int i = 0; i < pattern.Count; i++)
            {
                for (int j = 0; j < pattern[i].Count; j++)
                {
                    pattern[i][j] = rotateStuff * pattern[i][j];
                    pattern[i][j] = pattern[i][j] + patternOffSet;
                }
            }
        }

        public static void GeneratePattern(ref List<List<Vector3>> targetArray, int linesNumber, int minSectionsCount, int maxSectionsCount, float heightError, float lengthError, float XSizeMultiplier, float YSizeMultiplier, int seed)
        {
            if (heightError < 0 || heightError >= 1f)
                throw new Exception("heightError is less than 0 of not lower than 1");


            if (lengthError < 0 || lengthError >= 1f)
                throw new Exception("lengthError is less than 0 of not lower than 1");

            if (minSectionsCount < 1)
                throw new Exception("Minumum number of min sections is 1");

            if (maxSectionsCount < 1)
                throw new Exception("Minumum number of max sections is 1");

            // initialise seed
            UnityEngine.Random.InitState(seed);

            // make array to store things
            targetArray = new List<List<Vector3>>();

            // length is 2, but coords are [-1, 1]
            //base height of the chain
            float currentHeight = 2f / (linesNumber + 1); // length by number of parts by two, to get the middle of the section

            // max value by which the line can move in height, quater of section by def
            float currentHeightOffset = (2f / (linesNumber + 1)) * heightError;

            // add chains
            for (int i = 0; i < linesNumber; i++)
            {
                // add list for chain points
                targetArray.Add(new List<Vector3>());

                // generated number of sections in chain
                int sectionNumber = UnityEngine.Random.Range(minSectionsCount, maxSectionsCount);

                //base length of the chain part
                float currentLength = 0;

                //max value by which the line can move in length, quater of section by def
                float currentLengthOffset = (2 / sectionNumber) * lengthError;

                // Add starting point
                targetArray[i].Add(new Vector2((currentLength - 1) * XSizeMultiplier, (currentHeight + UnityEngine.Random.Range(-currentHeightOffset, currentHeightOffset) - 1) * YSizeMultiplier)); // -1 to shift from [0, 2] to [-1 , 1]

                // Add new point for current chain
                if (sectionNumber > 1)
                    for (int j = 0; j < sectionNumber - 1; j++)
                    {
                        currentLengthOffset = ((2 - currentLength) / (sectionNumber - j)) * lengthError;
                        currentLength = currentLength + (2 - currentLength) / (sectionNumber - j) + UnityEngine.Random.Range(-currentLengthOffset, currentLengthOffset);
                        targetArray[i].Add(new Vector2((currentLength - 1) * XSizeMultiplier, ((currentHeight + UnityEngine.Random.Range(-currentHeightOffset, currentHeightOffset) - 1) * YSizeMultiplier)));
                    }

                // Add end point
                targetArray[i].Add(new Vector2(1 * XSizeMultiplier, (currentHeight + UnityEngine.Random.Range(-currentHeightOffset, currentHeightOffset) - 1) * YSizeMultiplier));

                currentHeight = currentHeight + (2 - currentHeight) / (linesNumber - i);
                currentHeightOffset = ((2 - currentHeight) / (linesNumber - i)) * heightError;

            }

        }

        #endregion

    }

    public class EdgeComparer : IEqualityComparer<(int, int)>
    {
        public bool Equals((int, int) edgeOne, (int, int) edgeTwo)
        {
            return (edgeOne.Item1 == edgeTwo.Item1 && edgeOne.Item2 == edgeTwo.Item2)
                || (edgeOne.Item1 == edgeTwo.Item2 && edgeOne.Item2 == edgeTwo.Item1);
        }

        public int GetHashCode((int, int) edge)
        {
            Debug.Assert(edge.Item1 >= 0 && edge.Item2 >= 0);
            return (edge.Item1 + edge.Item2).GetHashCode();
        }
    }
}