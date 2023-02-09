// (c) 2023 Eric BeCude-McWilliams

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;



public class singleVertexManipulator : MonoBehaviour
{

    /// <summary>
    /// This script is for grabbing individual vertices and moving them.
    /// </summary>

    [Tooltip("Max distance from click (in units, not pixels) to check for vertices.")]
    public float clickRadius = 0.02f;

    [Tooltip("Max modifiable distance from camera.")]
    public float maxDistance = 50.0f;

    [Tooltip("Only game objects on these layers will be modified.")]
    public LayerMask modifiableLayers;

    [Tooltip("(Optional) Only game objects with this tag will be modified.")]
    public string modifiableTag;

    [Tooltip("(Optional) Prefab used to higlight the selected vertex.")]
    public GameObject selectedHighlightPrefab;

    [Tooltip("(Optional) Prefab used to higlight the moused-over vertex.")]
    public GameObject mouseoverHighlightPrefab;

    [Tooltip("This tells the script how far to move the vertex forward.")]
    public float forwardAmount;

    [Tooltip("This tells the script how far to move the vertex backward.")]
    public int backwardAmount;

    //[SerializeField] private int lel;
    [Tooltip("If you'd like to limit the amount of deformation to the player can do than use this. It checks how far away the vertex is away from the meshs transform, and it's too far it will stop.")]
    public int deformationLimit;

    //[Tooltip("If the deformationLimit is reached, and the vector is released this will move the vector backward a tiny bit so it is still grabbable.")]
    public float deformationLimitRevert;

    private MeshFilter currMesh;
    private Rigidbody currRB;
    private List<int> currVertIndices; // Some objects (like cubes) store multiple copies of a vertex, so we need a list
    private GameObject sHighlight;
    private GameObject mHighlight;
    //private HashSet<Edge> uniqueEdges;
    //private HashSet<Face> uniqueEdgesforFaces;
    private Vector3 edgeAvg;

    public float radiusOfSphere, distance;
    Rigidbody selectedRigidbody;

    public LayerMask spherecastLayerMask;
    private Vector3 sphereRayOrigin;
    private Vector3 sphereRayDirection;
    public float sphereRadius;
    public float sphereMaxDistance;
    private float sphereCurrentHitDistance;
    public GameObject sphereCurrentHitObject;
    private Rigidbody sphereRB;

    // Use this for initialization
    void Start()
    {
        if (selectedHighlightPrefab != null)
        {
            sHighlight = Instantiate<GameObject>(selectedHighlightPrefab);
            sHighlight.SetActive(false);
        }

        if (mouseoverHighlightPrefab != null)
        {
            mHighlight = Instantiate<GameObject>(mouseoverHighlightPrefab);
            mHighlight.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.yellow;
        Debug.DrawLine(sphereRayOrigin, sphereRayOrigin + sphereRayDirection * sphereCurrentHitDistance);
        Gizmos.DrawWireSphere(sphereRayOrigin + sphereRayDirection * sphereCurrentHitDistance, sphereRadius);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            grabVertex(); //Finds the mesh and vertex indices.
            updateVertex(); //Updates the mesh based on player interaction.
        }
        else if (Input.GetMouseButton(0))
        {
            updateVertex();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            releaseVertex();
        }
        else
        {
            mouseoverVertex();
        }
        /////////////////////////////////////////////////
        //Raycasting Junk...

        if (Input.GetButtonDown("Fire1"))
        {
            sphereRayOrigin = Camera.main.transform.position;
            sphereRayDirection = Camera.main.transform.forward;

            RaycastHit hit;
            if(Physics.SphereCast(sphereRayOrigin, sphereRadius, sphereRayDirection, out hit, sphereMaxDistance, spherecastLayerMask, QueryTriggerInteraction.UseGlobal) && hit.transform.tag == "Pickable")
            {
                sphereCurrentHitObject = hit.transform.gameObject;
                sphereCurrentHitDistance = hit.distance;
                sphereRB = sphereCurrentHitObject.GetComponent<Rigidbody>();
                
                    sphereRB.constraints =
                        RigidbodyConstraints.FreezePositionX
                        | RigidbodyConstraints.FreezePositionZ
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationY
                        | RigidbodyConstraints.FreezeRotationZ;
            }
            else
            {
                if (sphereRB != null)
                {
                    //sphereRB.constraints = RigidbodyConstraints.None;
                    sphereCurrentHitObject = null;
                    sphereRB = null;
                }
            }
        }

        if (Input.GetButtonUp("Fire1")) //yay redundancy!
        {
            if (sphereRB != null)
            {
                sphereRB.constraints = RigidbodyConstraints.None;
                sphereCurrentHitObject = null;
                sphereRB = null;
            }
        }
    }

    public static float sqrDistanceToRay(Vector3 point, Ray ray)
    {
        return Vector3.Cross(ray.direction, point - ray.origin).sqrMagnitude;
    }

    public static Vector3 pointIntersectRay(Vector3 point, Ray ray)
    {
        return ray.origin + ray.direction * Vector3.Dot(ray.direction, point - ray.origin);
    }

    public Vector3 MoveVertexForward(Vector3 oldPos)
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0) //Mouse Wheel Up
        {
            return oldPos + (oldPos - Camera.main.transform.position).normalized * forwardAmount;
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0) //Mouse Wheel Down
        {
            return oldPos - (oldPos - Camera.main.transform.position).normalized * backwardAmount;
        }
        else
        {
            return oldPos;
        }

    }

    public void grabVertex()
    {

        if (mHighlight != null)
        {
            mHighlight.SetActive(false);
        }

        MeshFilter mesh;
        List<int> vertexIndices;
        List<int> vertexIndices2;
        Vector3 vertexPos;

        if (findVertex(out mesh, out vertexIndices, out vertexPos))
        {

            currMesh = mesh;
            currVertIndices = vertexIndices;
            currMesh.mesh.MarkDynamic(); // For optimization

            if (sHighlight != null)
            {
                sHighlight.SetActive(true);
            }

        }

        //if (multiVertexSelection == true && findEdge(out mesh, out vertexIndices2, out vertexPos))
        {
            //currMesh2 = mesh;
            //currVertIndices2 = vertexIndices2; 

            //if (sHighlight != null)
            {
                //    sHighlight.SetActive(true);
            }
        }

    }

    public void mouseoverVertex()
    {

        if (mHighlight == null)
        {
            return;
        }

        MeshFilter mesh;
        List<int> vertexIndices;
        Vector3 vertexPos;

        if (findVertex(out mesh, out vertexIndices, out vertexPos))
        {
            mHighlight.transform.position = vertexPos;
            mHighlight.SetActive(true);
        }
        else
        {
            mHighlight.SetActive(false);
        }
    }

    public void updateVertex()
    {
        if (currMesh != null)
        {

            Ray clickRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            float baseSpeed = 1f;

            Vector3[] vertices = currMesh.mesh.vertices;
            Vector3 oldPos = currMesh.transform.TransformPoint(vertices[currVertIndices[0]]);
            oldPos = MoveVertexForward(oldPos);
            float distanceBetweenVectorAndMesh = Vector3.Distance(oldPos, currMesh.transform.position);
            Vector3 newPos = pointIntersectRay(oldPos, clickRay);
            oldPos = newPos;
            //Vector3.MoveTowards(oldPos, newPos, baseSpeed * Time.deltaTime);
            Vector3 newVertex = currMesh.transform.InverseTransformPoint(newPos);

            //Debug.Log("Distance between vertex, and mesh is: " + distanceBetweenVectorAndMesh);

            MeshCollider collider = currMesh.GetComponent<MeshCollider>();
            if (collider != null)
            {
                oldPos = newPos;
                collider.sharedMesh = currMesh.mesh;
                collider.enabled = false; // Seems to be needed
                collider.enabled = true;
            }

            if (distanceBetweenVectorAndMesh >= deformationLimit)
            {
                Debug.Log("Deformation Limit reached. Vertex released");
                newPos = newPos - this.transform.forward * deformationLimitRevert; //* Time.deltaTime
                releaseVertex();

            }
            else
            {
                foreach (int i in currVertIndices)
                {
                    vertices[i] = newVertex;
                }
                currMesh.mesh.vertices = vertices;

                currMesh.mesh.RecalculateBounds();
                currMesh.mesh.RecalculateNormals();

                if (sHighlight != null)
                {
                    sHighlight.transform.position = newPos;
                }
            }
        }
    }

    public void releaseVertex() //Sets the collider right and cleans up some things.
    {
        if (currMesh != null)
        {

            var o_264_12_636598621749381692 = currMesh.mesh; //I have no idea why that var is all fucked up. I think it happened when I went over to a newer Unity version.

            MeshCollider collider = currMesh.GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = currMesh.mesh;
                collider.enabled = false; // Seems to be needed
                collider.enabled = true;
            }
            else
            {
                Debug.LogWarning("No MeshCollider Found!", currMesh.gameObject);
            }

            currMesh = null;
            currVertIndices.Clear();

            if (sHighlight != null)
            {
                sHighlight.SetActive(false);
            }
        }
    }

    public bool findVertex(out MeshFilter mesh, out List<int> vertexIndices, out Vector3 vertexPos)
    {
        vertexPos = Vector3.zero;

        RaycastHit hitInfo;
        Ray clickRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.SphereCast(clickRay, clickRadius, out hitInfo, maxDistance, modifiableLayers)
            && (modifiableTag.Length == 0 || hitInfo.transform.tag.Equals(modifiableTag)))
        {

            //Found modifiable gameObject.

            mesh = hitInfo.transform.GetComponent<MeshFilter>();
            if (mesh == null)
            {
                vertexIndices = null;
                return false;
            }

            Vector3[] vertices = mesh.mesh.vertices;

            //Vertex finding.

            float clickRadiusSquared = clickRadius * clickRadius;

            vertexIndices = new List<int>();
            float closestVertSqDist = float.MaxValue;
            Vector3 currVertPos;

            for (int i = 0; i < vertices.Length; i++) // Search for closest vertex in range
            {

                if (vertexIndices.Count != 0 && vertices[vertexIndices[0]].Equals(vertices[i])) // This vert is a copy of the selected one, add.
                {
                    vertexIndices.Add(i);
                }
                else
                {
                    currVertPos = hitInfo.transform.TransformPoint(vertices[i]);

                    float currSqrDist = sqrDistanceToRay(currVertPos, clickRay);

                    if (currSqrDist <= clickRadiusSquared && currSqrDist < closestVertSqDist)
                    {
                        vertexIndices.Clear(); //This vert is closer than the selected one, replace.
                        vertexIndices.Add(i);
                        vertexPos = currVertPos;
                        closestVertSqDist = currSqrDist;
                    }
                }
            }
            return vertexIndices.Count != 0;
        }

        mesh = null;
        vertexIndices = null;
        return false;
    }

}

