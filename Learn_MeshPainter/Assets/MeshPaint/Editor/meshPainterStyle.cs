using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(meshPainter))]
[CanEditMultipleObjects]
public class meshPainterStyle : Editor {

    string controlTexName = "";

    public override void OnInspectorGUI()
    {
        if (Check())
        {
        }
    }

    bool Check()
    {
        bool check = false;
        Transform select = Selection.activeTransform;
        Material mat = select.gameObject.GetComponent<MeshRenderer>().sharedMaterial;
        Texture controlTex = mat.GetTexture("_Control");
        if (mat.shader == Shader.Find("meshPainter/TerrainTextureBlend") || mat.shader == Shader.Find("meshPainter/TerrainTextureBlendNormal"))
        {
            if (controlTex == null)
            {
                EditorGUILayout.HelpBox("当前模型材质球中未找到Control贴图，绘制功能不可用！", MessageType.Error);
                if (GUILayout.Button("创建Control贴图"))
                    createControlTex();
            }
            else
                check = true;
        }
        else
        {
            EditorGUILayout.HelpBox("当前模型shader错误！请更换！",MessageType.Error);
        }
        return check;
    }

    //创建Control贴图
    void createControlTex()
    {
        //创建一个新的Control贴图
        string controlTexFolder = "Assets/MeshPaint/Controller/";
        Texture2D newMaskTex = new Texture2D(512, 512, TextureFormat.ARGB32, true);
        Color[] colorBase = new Color[512 * 512];

        for (int i = 0; i < colorBase.Length; i++)
            colorBase[i] = new Color(1, 0, 0, 0);

        newMaskTex.SetPixels(colorBase);

        //判断是否重名
        bool exportNameSuccess = true;
        for (int num = 1; exportNameSuccess; num++)
        {
            string next = Selection.activeTransform.name + "_" + num;
            if (!File.Exists(controlTexFolder + Selection.activeTransform.name + ".png"))
            {
                controlTexName = Selection.activeTransform.name;
                exportNameSuccess = false;
            }
            else if (!File.Exists(controlTexFolder + next + ".png"))
            {
                controlTexName = next;
                exportNameSuccess = false;
            }
        }

        string path = controlTexFolder + controlTexName + ".png";
        byte[] bytes = newMaskTex.EncodeToPNG();
        File.WriteAllBytes(path,bytes);//保存

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);//导入资源
        //Control贴图的导入设置
        TextureImporter textureIm = AssetImporter.GetAtPath(path) as TextureImporter;
        textureIm.textureFormat  = TextureImporterFormat.ARGB32;

    }
}
