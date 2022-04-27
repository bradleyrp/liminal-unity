using Dummiesman;
using System.IO;
using UnityEngine;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;

public class ObjFromFile : MonoBehaviour
{
    string objPath = string.Empty;
    string error = string.Empty;
    GameObject loadedObj;

    void OnGUI() {
        objPath = GUI.TextField(new Rect(0, 0, 256, 32), objPath);

        if(GUI.Button(new Rect(256+128, 0, 128, 32), "QUIT"))
        {
            Application.Quit();
        }
        if(GUI.Button(new Rect(256, 0, 128, 32), "LOAD"))
        {
            //file path
            if (!File.Exists(objPath))
            {
                error = "File doesn't exist.";
            } else {
                if(loadedObj != null)            
                    Destroy(loadedObj);
                loadedObj = new OBJLoader().Load(objPath);
                error = string.Empty;

                var url = objPath;
                // dev: this code is replicated in ObjFromStream.cs so it needs consolidated
                // dev: can we wait on OBJLoader to avoid the error?
                GameObject.Find("camera").transform.position = new Vector3(200,0,0);
                var origin = new Vector3(0,0,0);
                GameObject.Find("camera").transform.LookAt(origin);
                GameObject.Find("camera").GetComponent<camera_fly>().source_path = url;
                // we expect only one child
                if (loadedObj.transform.childCount > 1) {
                    Debug.Log("[WARNING] more than one child: "+loadedObj.transform.childCount);
                }
                for (int i = 0; i < loadedObj.transform.childCount; i++)
                {
                    var x = loadedObj.transform.GetChild(i);
                    var mc = x.GetComponent<MeshCollider>();
                    Mesh mesh = mc.sharedMesh;
                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    
                    var graph = new Graph<int, string>();
                    // pattern matching a way to attach the graph to the object
                    GameObject.Find("camera").GetComponent<camera_fly>().graph = graph;
                    
                    for (var j = 0; j < vertices.Length; j++) {
                        graph.AddNode(j);
                    }
                    int ind0;
                    int ind1;
                    int ind2;
                    for (var k = 0; k < triangles.Length/3; k++) {
                        ind0 = triangles[k*3+0];
                        ind1 = triangles[k*3+1];
                        ind2 = triangles[k*3+2];
                        graph.Connect((uint)ind0,(uint)ind1,1,"");
                        graph.Connect((uint)ind1,(uint)ind2,1,"");
                        graph.Connect((uint)ind2,(uint)ind0,1,"");
                    }
                }




            }
        }

        if(!string.IsNullOrWhiteSpace(error))
        {
            GUI.color = Color.red;
            GUI.Box(new Rect(0, 64, 256 + 64, 32), error);
            GUI.color = Color.white;
        }
    }
}
