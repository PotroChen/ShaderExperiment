using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(meshPainter))]
[CanEditMultipleObjects]
public class meshPainterStyle : Editor
{

    string controlTexName = "";

    bool isPaint;

    float brushSize = 16f;
    float brushStrength = 0.5f;

    Texture[] brushTex;//文件夹里已经储存的笔刷
    Texture[] texLayer;//材质中的贴图

    void OnSceneGUI()
    {
        if (isPaint)
        {
            Painter();
        }

    }

    int selBrush = 0;
    int selTex = 0;

    int brushSizeInPourcent;
    Texture2D maskTex;

    public override void OnInspectorGUI()
    {
        if (Check())
        {
            GUIStyle boolBtnOn = new GUIStyle(GUI.skin.GetStyle("Button"));//得到Button样式

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            isPaint = GUILayout.Toggle(isPaint, EditorGUIUtility.IconContent("EditCollider"), boolBtnOn, GUILayout.Width(35), GUILayout.Height(25));//编辑模式开关
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            brushSize = (int)EditorGUILayout.Slider("Brush Size", brushSize, 1, 36);//笔刷大小
            brushStrength = EditorGUILayout.Slider("Brush Stronger", brushStrength, 0, 1f);//笔刷强度

            InitBrush();//获取笔刷
            layerTex();//获取贴图

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal("box", GUILayout.Width(340));
            selTex = GUILayout.SelectionGrid(selTex, texLayer, 4, "gridlist", GUILayout.Width(340), GUILayout.Height(86));//通道选择
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal("box", GUILayout.Width(340));
            selBrush = GUILayout.SelectionGrid(selBrush, brushTex, 9, "gridlist", GUILayout.Width(340), GUILayout.Height(70));//笔刷选择
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }

    //获取材质球中的贴图
    void layerTex()
    {
        Transform select = Selection.activeTransform;
        Material sharedMat = select.gameObject.GetComponent<MeshRenderer>().sharedMaterial;

        texLayer = new Texture[4];
        texLayer[0] = AssetPreview.GetAssetPreview(sharedMat.GetTexture("_Splat0")) as Texture;
        texLayer[1] = AssetPreview.GetAssetPreview(sharedMat.GetTexture("_Splat1")) as Texture;
        texLayer[2] = AssetPreview.GetAssetPreview(sharedMat.GetTexture("_Splat2")) as Texture;
        texLayer[3] = AssetPreview.GetAssetPreview(sharedMat.GetTexture("_Splat3")) as Texture;
    }

    //获取笔刷
    void InitBrush()
    {
        string t4MEditorFolder = "Assets/MeshPaint/Editor/";
        ArrayList brushList = new ArrayList();
        Texture brushesTL;
        int BrushNum = 0;
        //从Brush0.png这个文件名开始对Assets/MeshPaint/Editor/Brushes文件夹进行搜索,把搜到的图片加入到ArrayList里
        do
        {
            brushesTL = (Texture)AssetDatabase.LoadAssetAtPath(t4MEditorFolder + "Brushes/Brush" + BrushNum + ".png", typeof(Texture));

            if (brushesTL)
            {
                brushList.Add(brushesTL);
            }
            BrushNum++;
        } while (brushesTL);
        brushTex = brushList.ToArray(typeof(Texture)) as Texture[];//把ArrayList转为Texture[]
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
            EditorGUILayout.HelpBox("当前模型shader错误！请更换！", MessageType.Error);
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
        File.WriteAllBytes(path, bytes);//保存

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);//导入资源
        //Control贴图的导入设置
        TextureImporter textureIm = AssetImporter.GetAtPath(path) as TextureImporter;
        textureIm.textureFormat = TextureImporterFormat.ARGB32;
        textureIm.isReadable = true;
        textureIm.anisoLevel = 9;
        textureIm.mipmapEnabled = false;
        textureIm.wrapMode = TextureWrapMode.Clamp;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);//刷新资源

        setControlTex(path);//设置Controlt贴图
    }

    //设置Control贴图
    void setControlTex(string path)
    {
        Texture2D controlTex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        //controlTex. = TextureFormat.ARGB32;
        Selection.activeTransform.gameObject.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_Control", controlTex);
    }

    void Painter()
    {
        Transform currentSelect = Selection.activeTransform;
        MeshFilter temp = currentSelect.GetComponent<MeshFilter>();//获取当前模型的MeshFilter
        float orthographicSize = (brushSize * currentSelect.localScale.x) * (temp.sharedMesh.bounds.size.x / 200);//笔刷在模型上的正交大小
        maskTex = (Texture2D)currentSelect.gameObject.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_Control");//从材质球中获取Control贴图

        brushSizeInPourcent = (int)Mathf.Round((brushSize * maskTex.width) / 100);//笔刷在模型上的大小
        bool toggleF = false;

        Event e = Event.current;//检测输入
        HandleUtility.AddDefaultControl(0);
        RaycastHit raycastHit = new RaycastHit();
        Ray terrain = HandleUtility.GUIPointToWorldRay(e.mousePosition);//从鼠标位置发射一条射线
        if (Physics.Raycast(terrain, out raycastHit, Mathf.Infinity, 1 << LayerMask.NameToLayer("ground")))//射线检测名为"ground"的层
        {
            Handles.color = new Color(1f, 1f, 0f, 1f);//颜色
            Handles.DrawWireDisc(raycastHit.point, raycastHit.normal, orthographicSize);//根据笔刷大小在鼠标位置显示一个圆

            //鼠标点击或按下并拖动进行绘制
            if ((e.type == EventType.MouseDrag && e.alt == false && e.control == false && e.shift == false && e.button == 0) || (e.type == EventType.MouseDown && e.shift == false && e.alt == false && e.control == false && e.button == 0 && toggleF == false))
            {
                //选择绘制的通道
                Color targetColor = new Color(1f, 0f, 0f, 0f);
                switch (selTex)
                {
                    case 0:
                        targetColor = new Color(1f, 0f, 0f, 0f);
                        break;
                    case 1:
                        targetColor = new Color(0f, 1f, 0f, 0f);
                        break;
                    case 2:
                        targetColor = new Color(0f, 0f, 1f, 0f);
                        break;
                    case 3:
                        targetColor = new Color(0f, 0f, 0f, 1f);
                        break;

                }


                Vector2 pixelUV = raycastHit.textureCoord;

                //计算笔刷所覆盖的区域
                int puX = Mathf.FloorToInt(pixelUV.x * maskTex.width);//uv坐标单位转换
                int puY = Mathf.FloorToInt(pixelUV.y * maskTex.height);

                int x = Mathf.Clamp(puX - brushSizeInPourcent / 2, 0, maskTex.width - 1);//uv原点转换到左下角
                int y = Mathf.Clamp(puY - brushSizeInPourcent / 2, 0, maskTex.height - 1);

                int width = Mathf.Clamp((puX + brushSizeInPourcent / 2), 0, maskTex.width) - x;
                int height = Mathf.Clamp((puY + brushSizeInPourcent / 2), 0, maskTex.height) - y;

                Color[] terrainBay = maskTex.GetPixels(x, y, width, height, 0);//获取Control贴图被笔刷所覆盖的区域的颜色

                Texture2D tBrush = brushTex[selBrush] as Texture2D;//获取笔刷性状贴图
                float[] brushAlpha = new float[brushSizeInPourcent * brushSizeInPourcent];//笔刷透明度

                //根据笔刷贴图计算笔刷的透明度
                for (int i = 0; i < brushSizeInPourcent; i++)
                {
                    for (int j = 0; j < brushSizeInPourcent; j++)
                    {
                        brushAlpha[j * brushSizeInPourcent + i] = tBrush.GetPixelBilinear(((float)i) / brushSizeInPourcent, ((float)j) / brushSizeInPourcent).a;
                    }
                }

                //计算绘制后的颜色
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        int index = (i * width) + j;
                        //TODO 下面这个公式可以简化一下
                        float strength = brushAlpha[Mathf.Clamp((y + i) - (puY - brushSizeInPourcent / 2), 0, brushSizeInPourcent - 1) * brushSizeInPourcent + Mathf.Clamp((x + j) - (puX - brushSizeInPourcent / 2), 0, brushSizeInPourcent - 1)] * brushStrength;

                        terrainBay[index] = Color.Lerp(terrainBay[index], targetColor, strength);
                    }
                }
                Undo.RegisterCompleteObjectUndo(maskTex, "meshPaint");//保存历史记录以便撤销
                maskTex.SetPixels(x, y, width, height, terrainBay, 0);//把绘制后的Control贴图保存起来
                maskTex.Apply();
                toggleF = true;
            }
            else if (e.type == EventType.MouseUp && e.alt == false && e.button == 0 && toggleF == true)
            {

                SaveTexture();//绘制结束保存Control贴图
                toggleF = false;
            }
        }
    }

    public void SaveTexture()
    {
        var path = AssetDatabase.GetAssetPath(maskTex);
        var bytes = maskTex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);//刷新
    }
}
