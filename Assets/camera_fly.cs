using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;
using System.Text.RegularExpressions;

[System.Serializable]
struct Landmarks {
    public List<int> clicked;
    public List<int> route;
    public Landmarks(List<int> clicked, List<int> route) {
        this.clicked = clicked;
        this.route = route;
    }
}

public class camera_fly : MonoBehaviour {
 
    float mainSpeed = 10.0f; // alternative speed
    float altSpeed = 100.0f; // alternative speed
    // we remove shiftAdd and maxShift from 250 and 1000 to go slower instead
    //   float shiftAdd = 250.0f; // faster motion, scaled by duration of shift
    //   float maxShift = 1000.0f; // Maximum speed when holding shift
    //   float camSens = 0.25f; // how sensitive it with mouse
    // position in the middle of the screen, rather than at the top
    private Vector3 lastMouse = new Vector3(255, 255, 255); 
    private float totalRun= 1.0f;
    // these variables are for an alternate method
    public float speed = 3.5f;
    private float arrowRotateSens = 0.5f;
    private float X;
    private float Y;
    public float sensitivity = 10f;
    private float sphere_scale = 0.35f;
    Vector3 lastMousePosition;
    // receive the graph from the loader
    public Graph<int, string> graph;
    // store the output path
    public string source_path;
    // build a list of boundary conditions
    // dev: this could be a separate class later probably
    public Dictionary<string,List<int>> bounds;
    // hold the list of probe points
    public List<int> clicked = new List<int>();
    // fill in the shortest path between probe points
    public List<int> route = new List<int>();
    // hold the vertices and transform so other methods can see them
    private Vector3[] vertices;
    private Transform hitTransform;
    // name for the boundary
    string boundName = string.Empty;
    // error for the GUI
    string error = string.Empty;
    //! needs docs
    public float mouseSensitivity = 5.0f;
    //! private float rotationY = 0.0f;
    // trying to add collision
    public Rigidbody rb;
 
    void pathLink(uint ind0, uint ind1) {
        Debug.Log("[STATUS] computing path");
        ShortestPathResult result = graph.Dijkstra(ind0,ind1);
        var path = result.GetPath();
        var ec = path.GetEnumerator();
        var prev = ec.Current;
        var rp0 = hitTransform.TransformPoint(vertices[ec.Current]);
        var rp1 = hitTransform.TransformPoint(vertices[ec.Current]);
        while (ec.MoveNext())
        {
            route.Add((int)ec.Current);
            rp1 = hitTransform.TransformPoint(vertices[ec.Current]);
            // dev: the debug lines are not visible for some reason
            //   Debug.DrawLine(rp0, rp1, Color.red, 1000f);
            // increment the previous point
            rp0 = rp1;
            // draw spheres that mark the path
            GameObject sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere2.GetComponent<Renderer>().material.color = new Color(0,0,1,1);
            sphere2.transform.position = rp1;
            sphere2.transform.localScale = new Vector3(
                sphere_scale,sphere_scale,sphere_scale);
            sphere2.SetActive(true);
            sphere2.tag = "route_sphere";
        }
        string check_route = "[STATUS] current route: ";
        foreach (var item in route)
        {
            check_route += item.ToString() + " ";
        }
    }

    void OnGUI() {
        boundName = GUI.TextField(new Rect(0, 32, 256, 32), boundName);
        GUI.Label(new Rect(0, 32, 128, 32), "");
        if(GUI.Button(new Rect(256, 32, 128, 32), "SAVE"))
        {
            Regex r = new Regex("[^A-Za-z0-9_-]");
            if (r.IsMatch(boundName)) {
                error = "invalid characters";
            }
            if (clicked.Count==0) {
                error = "no selected vertices";
            }
          
            Landmarks dump = new Landmarks(clicked,route);
            string json = JsonUtility.ToJson(dump);
            var path_out = (
                Regex.Replace(source_path, @"^file://", "") + 
                    ".tagged." + boundName + ".json"
            );
            System.IO.File.WriteAllText(path_out, json);
            
            Debug.Log("[STATUS] saved");
            // testing only: UnityEditor.EditorUtility.RevealInFinder(path_out);
            // via: https://answers.unity.com/questions/24257/\
            //   how-do-i-find-all-game-objects-with-the-same-name.html
            // change the colors after saving so the spheres remain as a reminder
            foreach (GameObject this_sphere in GameObject.FindGameObjectsWithTag("route_sphere"))
            {
                this_sphere.GetComponent<Renderer>().material.color = new Color(1,1,0,1);
            }
            // clear the selections
            clicked.Clear();
            route.Clear();
        }
        if(GUI.Button(new Rect(256+128, 32, 128, 32), "RESET"))
        {
            // clear the clicked and route variables and remove spheres in case something goes wrong
            clicked.Clear();
            route.Clear();
            foreach (GameObject this_sphere in GameObject.FindGameObjectsWithTag("route_sphere"))
            {
                Destroy(this_sphere);
            }
            foreach (GameObject this_sphere in GameObject.FindGameObjectsWithTag("mark_sphere"))
            {
                Destroy(this_sphere);
            }

        }
        if(!string.IsNullOrWhiteSpace(error))
        {
            GUI.color = Color.red;
            GUI.Box(new Rect(0, 64, 256 + 64, 32), error);
            GUI.color = Color.white;
        }
    }

    void FixedUpdate () {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }
        else if(Input.GetMouseButton(0))
        {
            // via: https://answers.unity.com/questions/918884/using-quaternion-for-mouse-movement.html
            var r = transform.rotation;
            r = Quaternion.AngleAxis(-Input.GetAxis("Mouse X"), transform.up) * r;
            r = Quaternion.AngleAxis(Input.GetAxis("Mouse Y"), transform.right) * r;
            // not sure where the horizontal came from but it messes things up
            //   r = Quaternion.AngleAxis(Input.GetAxis("Horizontal"), transform.forward) * r;
            transform.rotation = r;
        }
        else if (Input.GetMouseButtonDown(1))
        {
            // via: https://answers.unity.com/questions/411793/\
            //    selecting-a-game-object-with-a-mouse-click-on-it.html
            RaycastHit hitInfo = new RaycastHit();
            bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
            if (hit) 
            {
                // via: https://forum.unity.com/threads/\
                //   unity-raycast-and-highlighting-mesh-triangle-of-intersection.917015/
                MeshCollider meshCollider = hitInfo.collider as MeshCollider;
                Mesh mesh = meshCollider.sharedMesh;
                vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Vector3 p0 = vertices[triangles[hitInfo.triangleIndex * 3 + 0]];
                Vector3 p1 = vertices[triangles[hitInfo.triangleIndex * 3 + 1]];
                Vector3 p2 = vertices[triangles[hitInfo.triangleIndex * 3 + 2]];
                hitTransform = hitInfo.collider.transform;
                p0 = hitTransform.TransformPoint(p0);
                p1 = hitTransform.TransformPoint(p1);
                p2 = hitTransform.TransformPoint(p2);
                Vector3[] nears = new Vector3[]{p0,p1,p2};
                Debug.DrawLine(p0, p1, Color.red, 1000f);
                Debug.DrawLine(p1, p2, Color.red, 1000f);
                Debug.DrawLine(p2, p0, Color.red, 1000f);
                // compute the closest vertex
                // via: https://docs.unity3d.com/ScriptReference/Collider.ClosestPoint.html
                // dev: is this clumsy? it has been a while!
                var dist = Mathf.Infinity;
                var close_point = p0;
                var index = 0;
                for (int i = 0; i < 3; i++)
                {
                    var dist_this = Vector3.Distance(hitInfo.point,nears[i]);
                    if (dist_this < dist){
                        close_point = nears[i];
                        dist = dist_this;
                        index = i;
                    }
                }
                var vertex_this = triangles[hitInfo.triangleIndex * 3 + 2];
                Debug.Log("hit vertex " + vertex_this);
                var vpt = close_point;
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.GetComponent<Renderer>().material.color = new Color(0,1,0,1);
                sphere.transform.position = vpt;
                sphere.transform.localScale = new Vector3(
                    sphere_scale,sphere_scale,sphere_scale);
                sphere.SetActive(true);
                sphere.tag = "mark_sphere";
                clicked.Add(vertex_this);
                if (clicked.Count > 1)
                {
                    var ind0 = (uint)clicked[clicked.Count-2];
                    var ind1 = (uint)clicked[clicked.Count-1];
                    pathLink(ind0, ind1);
                }
            }
        }

        Vector3 p = GetBaseInput();
        if (p.sqrMagnitude > 0){ 
            if (Input.GetKey (KeyCode.RightShift)){
                // previously included some time-scaling
                //   totalRun += Time.deltaTime;
                //   p  = p * totalRun * shiftAdd;
                //   p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
                //   p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
                //   p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
                totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                p = p * altSpeed;
            } else {
                totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                p = p * mainSpeed;
            }
            p = p * Time.deltaTime;
            // dev: attempt to intervene to handle collision
            // we generally fail to add force to a rigid body and sphere
            //   collider on the camera to prevent it from clipping
            // prevent stumbling sideways through a wall
            //   this looks foward: Ray ray = new Ray(transform.position, transform.forward);
            // original collider code is passable but not great so we abandon
            //   Ray ray = new Ray(transform.position, p);
            //   RaycastHit hit;
            //   if (!Physics.Raycast(ray,out hit,1.0f)) {
            //       transform.Translate(p);
            //   } else {
            //       p = p - Vector3.Normalize(p) * (1.0f - hit.distance);
            //       transform.Translate(p);
            //   }
            transform.Translate(p);
        }
        // hold the object in the center of the screen to sweep the camera
        if (Input.GetKey (KeyCode.L)){
            // ref: https://answers.unity.com/questions/32067/\
            //   rotating-an-object-to-face-the-camera-regardless-o.html#
            // this method works because we can use a second argument for LookAt which is really useful
            var origin = new Vector3(0,0,0);
            Transform camera_t = GameObject.Find("camera").transform;
            camera_t.LookAt(origin,camera_t.up);
        }
    }

    private Vector3 GetBaseInput() { 
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey (KeyCode.W)){
            p_Velocity += new Vector3(0, 0 , 1);
        }
        if (Input.GetKey (KeyCode.S)){
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey (KeyCode.A)){
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey (KeyCode.D)){
            p_Velocity += new Vector3(1, 0, 0);
        }
        if (Input.GetKey (KeyCode.LeftShift)){
            p_Velocity += new Vector3(0, 1, 0);
        }
        if (Input.GetKey (KeyCode.Space)){
            p_Velocity += new Vector3(0, -1, 0);
        }
        if (Input.GetKey (KeyCode.Return)){
            // connect the path to the start point on Return
            pathLink((uint)clicked[clicked.Count-1],(uint)clicked[0]);
        }

        // include arrow key controls here
        var r = transform.rotation;
        if (Input.GetKey (KeyCode.LeftArrow)) {
            r = Quaternion.AngleAxis(-arrowRotateSens, transform.up) * r;
            transform.rotation = r;
        }
        if (Input.GetKey (KeyCode.RightArrow)) {
            r = Quaternion.AngleAxis(arrowRotateSens, transform.up) * r;
            transform.rotation = r;
        }
        if (Input.GetKey (KeyCode.UpArrow)) {
            r = Quaternion.AngleAxis(arrowRotateSens, transform.right) * r;
            transform.rotation = r;
        }
        if (Input.GetKey (KeyCode.DownArrow)) {
            r = Quaternion.AngleAxis(-arrowRotateSens, transform.right) * r;
            transform.rotation = r;
        }
        return p_Velocity;
    }   
}