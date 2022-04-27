using Dummiesman;
using System.IO;
using System.Text;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;

public class ObjFromStream : MonoBehaviour {
    IEnumerator LoadOBJ() {
        // var www = new WWW("https://people.sc.fsu.edu/~jburkardt/data/obj/lamp.obj");
        // var www = new WWW("file:///Users/rpb/stage/magnolia.obj");
        // var www = new WWW("file:///Users/rpb/stage/testFilesForInterp/fibers-review-epi-LA.obj");
        // var www = new WWW("file:///Users/rpb/stage/tmp-godot-example/P13_reod_marked.alt.obj");
        // var www = new WWW("file:///Users/rpb/stage/tmp-godot-example/P13_reod_marked.obj");
        // var url = "file:///Users/rpb/stage/tmp-godot-example/P13_reod_marked.alt.obj";
        var url = "file:///Users/rpb/stage/magnolia.obj";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        }
        else {
            // dev: this code is replicated in ObjFromFile.cs for production
            var textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.downloadHandler.text));
            var loadedObj = new OBJLoader().Load(textStream);
            // dev: can we wait on objloader to avoid the error?
            GameObject.Find("camera").transform.position = new Vector3(200,0,0);
            var origin = new Vector3(0,0,0);
            GameObject.Find("camera").transform.LookAt(origin);
            GameObject.Find("camera").GetComponent<camera_fly>().source_path = url;
            // we expect only one child
            if (loadedObj.transform.childCount > 1) {
                Debug.LogError("more than one child: "+loadedObj.transform.childCount);
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
    void Start () {
        StartCoroutine(LoadOBJ());
    }
}
