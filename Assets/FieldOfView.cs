using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float viewRadius;
    [Range(0,360)]
    public float viewAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    public float meshResolution;
    public MeshFilter meshFilter;
    Mesh mesh;

    public int edgeResolveIterations;
    public float edgeDistanceThreshold;

    [HideInInspector]
    public List<Transform> visibleTargets = new List<Transform>();

    private void Start() {
        mesh = new Mesh();
        mesh.name = "View Mesh";
        meshFilter.mesh = mesh;
        StartCoroutine("FindTargetsWithDelay", .2f);
    }

    IEnumerator FindTargetsWithDelay(float delay) {
        while(true) {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void LateUpdate() {
        DrawFieldOfView();
    }

    void FindVisibleTargets() {
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        for(int i = 0; i < targetsInViewRadius.Length; i++) {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 direction = (target.position - transform.position).normalized;
            if(Vector3.Angle(transform.forward, direction) < viewAngle/2) {
                float distance = Vector3.Distance(transform.position, target.position);
                if(!Physics.Raycast(transform.position, direction, distance, obstacleMask)) {
                    visibleTargets.Add(target);
                    target.gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                }
            } else {
                target.gameObject.GetComponent<Renderer>().material = Resources.Load("EnemyNotSeenMat", typeof(Material)) as Material;
            }
        }
    }

    void DrawFieldOfView() {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        ViewCastInfo oldViewCast = new ViewCastInfo();
        List<Vector3> viewPoints = new List<Vector3>();
        for(int i = 0; i < stepCount; i++) {
            float angle = transform.eulerAngles.y - viewAngle/2 + stepAngleSize * i;
            ViewCastInfo viewCast = ViewCast(angle);
            if(i > 0) {
                bool exceededEdgeDistanceThreshold = Mathf.Abs(oldViewCast.distance - viewCast.distance) > edgeDistanceThreshold;
                if(oldViewCast.hit != viewCast.hit || (oldViewCast.hit && viewCast.hit && exceededEdgeDistanceThreshold)) {
                    // Find the edge
                    EdgeInfo edge = FindEdge(oldViewCast, viewCast);
                    if(edge.pointA != Vector3.zero) {
                        viewPoints.Add(edge.pointA);
                    } 
                    if(edge.pointB != Vector3.zero) {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }
            viewPoints.Add(viewCast.point);
            oldViewCast = viewCast;

            // Debug.DrawLine(transform.position, viewCast.point, Color.black);
        }

        int numVertices = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[numVertices];

        //Stores the indices of each vertex in the triangle.
        int[] triangles = new int[(numVertices - 2) * 3];

        vertices[0] = Vector3.zero;
        for(int i = 0; i < numVertices - 1; i++) {
            vertices[i+1] = transform.InverseTransformPoint(viewPoints[i]);

            if(i < numVertices - 2) {
                triangles[i*3] = 0;
                triangles[i*3 + 1] = i + 1;
                triangles[i*3 + 2] = i + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    EdgeInfo FindEdge(ViewCastInfo min, ViewCastInfo max) {
        float minAngle = min.angle;
        float maxAngle = max.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for(int i = 0; i < edgeResolveIterations; i++) {
            float newAngle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(newAngle);
            bool exceededEdgeDistanceThreshold = Mathf.Abs(min.distance - newViewCast.distance) > edgeDistanceThreshold;

            if(newViewCast.hit == min.hit && !exceededEdgeDistanceThreshold) {
                minAngle = newAngle;
                minPoint = newViewCast.point;
            } else {
                maxAngle = newAngle;
                maxPoint = newViewCast.point;
            }
        }
        return new EdgeInfo(minPoint, maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle) {
        Vector3 direction = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        if(Physics.Raycast(transform.position, direction, out hit, viewRadius, obstacleMask)) {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        return new ViewCastInfo(false, transform.position + direction * viewRadius, viewRadius, globalAngle);
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal) {
        if(!angleIsGlobal) {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public struct ViewCastInfo {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;

        public ViewCastInfo(bool hit, Vector3 point, float distance, float angle) {
            this.hit = hit;
            this.point = point;
            this.distance = distance;
            this.angle = angle;
        }

    }

    public struct EdgeInfo {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 pointA, Vector3 pointB) {
            this.pointA = pointA;
            this.pointB = pointB;
        }
    }
}
