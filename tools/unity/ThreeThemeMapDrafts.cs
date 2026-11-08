#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Matto.PC.Editor
{
    // Draft layout tool only. Not final architectural art, gameplay, or a tested scene.
    public static class ThreeThemeMapDrafts
    {
        const string Root = "Assets/MattoPC";
        const string Models = "art/Environment/ThirdParty/KayKitFurnitureBits/Models";

        [MenuItem("Tools/Matto PC/1 Import CC0 OBJ props")]
        public static void ImportProps()
        {
            var project = Directory.GetParent(Application.dataPath);
            var repository = project?.Parent?.Parent;
            if (repository == null) { Debug.LogError("Repository root not found"); return; }
            string source = Path.Combine(repository.FullName, Models);
            if (!Directory.Exists(source)) { Debug.LogWarning("No staged OBJ models in " + source); return; }
            Folder("Assets/MattoPC/ImportedModels");
            int count = 0;
            foreach (string file in Directory.GetFiles(source, "*.obj"))
            {
                string destination = "Assets/MattoPC/ImportedModels/" + Path.GetFileName(file);
                if (File.Exists(destination)) continue;
                File.Copy(file, destination);
                count++;
            }
            AssetDatabase.Refresh();
            Debug.Log("Imported " + count + " CC0 OBJ source models (material setup remains manual).");
        }

        [MenuItem("Tools/Matto PC/2 Create three editable layout scenes")]
        public static void BuildLayouts()
        {
            Folder(Root + "/Scenes");
            Folder(Root + "/Materials");
            Build("Mansion", new[]{
                R("LOBBY_1F",-24,0,-15,15,10), R("ROOM_1_1F",-7,0,-15), R("ROOM_2_1F",8,0,-15), R("ROOM_3_1F",23,0,-15),
                R("ROOM_4_2F",-24,8,14), R("ROOM_5_2F",-8,8,14), R("ROOM_6_2F",8,8,14), R("STORY_ROOM_2F",24,8,14)
            }, "#5F5045", "#E2D4C8", "#3B2F33", "#7E3343");
            Build("Cruise", new[]{
                R("ATRIUM_DECK_3",-32,0,-14,13,9), R("ROOM_1_DECK_3",-16,0,-14), R("ROOM_2_DECK_3",0,0,-14),
                R("ROOM_3_DECK_3",16,0,-14), R("ROOM_4_DECK_3",32,0,-14), R("ROOM_5_DECK_4",-16,8,14),
                R("ROOM_6_DECK_4",0,8,14), R("STORY_ROOM_DECK_4",16,8,14)
            }, "#B8C6CC", "#F4F4EE", "#24496F", "#C9AB78");
            Build("MusicBroadcast", new[]{
                R("WAITING_HALL",0,0,-13,13,12),
                R("LEFT_A01",-19,0,-18), R("LEFT_A02",-19,0,-4), R("LEFT_A03",-19,0,10),
                R("RIGHT_B01",19,0,-18), R("RIGHT_B02",19,0,-4), R("RIGHT_B03",19,0,10),
                R("VIP_ROOM",0,0,18,13,10)
            }, "#CEBEAB", "#FFF0F2", "#B7965D", "#D994B3");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Three editable layout drafts generated. Unity Play Mode was not started.");
        }

        struct Room
        {
            public string Id; public Vector3 Position; public float Width, Depth;
        }
        static Room R(string id,float x,float y,float z,float w=11,float d=8)
        {
            return new Room {Id=id,Position=new Vector3(x,y,z),Width=w,Depth=d};
        }
        static void Build(string name, Room[] rooms,string floorHex,string wallHex,string trimHex,string accentHex)
        {
            string scenePath=Root+"/Scenes/"+name+"_DRAFT.unity";
            if (File.Exists(scenePath)) { Debug.LogWarning("Existing scene preserved: "+scenePath); return; }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject(name+"_LAYOUT_DRAFT_NOT_FINAL");
            Material floor=Mat(name+"_Floor",floorHex),wall=Mat(name+"_Wall",wallHex),
                     trim=Mat(name+"_Trim",trimHex),accent=Mat(name+"_Accent",accentHex);
            foreach (var room in rooms) MakeRoom(root.transform,room,floor,wall,trim,accent);
            var sun=new GameObject("Preview_DirectionalLight");
            sun.transform.SetParent(root.transform);
            sun.transform.rotation=Quaternion.Euler(48f,-32f,0f);
            Light light=sun.AddComponent<Light>();
            light.type=LightType.Directional; light.intensity=1.0f;light.shadows=LightShadows.Soft;

            var cameraObject=new GameObject("Preview_IsometricCamera");
            cameraObject.transform.SetParent(root.transform);
            cameraObject.tag="MainCamera";
            cameraObject.transform.position=new Vector3(43,65,-55);
            cameraObject.transform.LookAt(new Vector3(0,2,0));
            Camera camera=cameraObject.AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=46;camera.nearClipPlane=.3f;camera.farClipPlane=300;
            EditorSceneManager.SaveScene(scene,scenePath);
        }
        static void MakeRoom(Transform root,Room room,Material floor,Material wall,Material trim,Material accent)
        {
            var parent=new GameObject(room.Id+"_DRAFT");
            parent.transform.SetParent(root);
            parent.transform.position=room.Position;
            float w=room.Width,d=room.Depth;
            Box(parent.transform,"Floor",new Vector3(0,-.2f,0),new Vector3(w,.35f,d),floor);
            Box(parent.transform,"BackWall",new Vector3(0,1.6f,d/2),new Vector3(w,3.2f,.25f),wall);
            Box(parent.transform,"LeftWall",new Vector3(-w/2,1.6f,0),new Vector3(.25f,3.2f,d),wall);
            Box(parent.transform,"RightWall",new Vector3(w/2,1.6f,0),new Vector3(.25f,3.2f,d),wall);
            Box(parent.transform,"Cutaway_Left",new Vector3(-w*.34f,.21f,-d/2),new Vector3(w*.31f,.42f,.24f),trim);
            Box(parent.transform,"Cutaway_Right",new Vector3(w*.34f,.21f,-d/2),new Vector3(w*.31f,.42f,.24f),trim);
            Box(parent.transform,"Back_Trim",new Vector3(0,.16f,d/2-.18f),new Vector3(w-.2f,.3f,.1f),trim);
            Box(parent.transform,"Accent_Strip",new Vector3(0,.01f,-d*.32f),new Vector3(w*.34f,.04f,.06f),accent);
        }
        static void Box(Transform root,string name,Vector3 position,Vector3 size,Material material)
        {
            GameObject b=GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name=name;b.transform.SetParent(root,false);
            b.transform.localPosition=position;b.transform.localScale=size;
            b.GetComponent<Renderer>().sharedMaterial=material;
        }
        static Material Mat(string name,string hex)
        {
            string path=Root+"/Materials/"+name+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing!=null)return existing;
            Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
            var mat=new Material(shader);
            Color c; ColorUtility.TryParseHtmlString(hex,out c);
            if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",c);else mat.color=c;
            AssetDatabase.CreateAsset(mat,path);return mat;
        }
        static void Folder(string path)
        {
            string current="Assets";
            string[] parts=path.Split('/');
            for(int i=1;i<parts.Length;i++)
            {
                string next=current+"/"+parts[i];
                if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);
                current=next;
            }
        }
    }
}
#endif
