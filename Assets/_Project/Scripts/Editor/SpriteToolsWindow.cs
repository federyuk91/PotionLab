using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteToolsWindow : EditorWindow
{

    private Palette oldPalette;
    public Palette newPalette;
    public float ratio = 1f;
    public int realTimePreviewUnder = 512;
    public string saveName = "";

    public Color selection, variation;

    SpriteToolsWindow window;
    Texture2D toRecolor;
    Texture2D recolored;
    Texture2D preview;
    static Texture2D picker;

    float y_button = 80, height_button = 30;
    float y_palette;

    static int saved = 0;

    Rect toRecolor_rect, preview_rect;

    [MenuItem("Tools/Sprite Editor")]
    public static void Open()
    {
        picker = EditorGUIUtility.FindTexture("EyeDropper.Large");//Resources.Load("EyeDrop") as Texture2D;

        Debug.Log(picker.width + " " + picker.height);
        SpriteToolsWindow window = GetWindow<SpriteToolsWindow>("Sprite tools");
        window.position = new Rect(0, 0, 800, 800);
        window.Show();

    }

    public static Palette GetPaletteFrom(Texture2D texture)
    {
        Palette p = new Palette();
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                Color c = texture.GetPixel(x, y);
                if (c.a != 0 && !p.palette.Contains(c))
                {
                    p.palette.Add(c);
                }
            }
        }
        return p;
    }



    void OnGUI()
    {
        //EditorGUI.BeginChangeCheck();
        //SerializedObject obj = new SerializedObject(this);

        GUILayout.BeginHorizontal();
        Texture prevToRecolor = toRecolor;
        toRecolor = TextureField("To Recolor", toRecolor);//(Texture2D)EditorGUI.ObjectField(new Rect(55, 10, 70, 70), toRecolor, typeof(Texture2D), false);//



        //recolored = TextureField("Recolored", recolored);
        if (toRecolor)
        {
            if (toRecolor.isReadable)
            {
                if (!toRecolor.Equals(prevToRecolor))
                {
                    Debug.Log("Sprite change");
                    InitializeView();
                    ApplyPalette();
                }
                /*if (!preview)
                {
                    Debug.Log("Preview not found creating one");
                    InitializeView();
                    
                }
                else
                {*/

                // }

                DrawButtons();

                DrawImages();

                DrawFields();

                y_palette = y_button + height_button * 2 + toRecolor.height * ratio;

                DrawPalette(this, newPalette, 55, y_palette, 40, 30, 8, "Palette variation");
                /*if (EditorGUI.EndChangeCheck())
                {
                    ApplyPalette();
                }*/
            }
            else
            {
                preview = null;
                EditorGUILayout.HelpBox("Please enable read/write flag to edit", MessageType.Info);
                GUILayout.EndHorizontal();
            }

        }
        else
        {
            EditorGUILayout.HelpBox("Select a Texture", MessageType.Info);
        }
    }

    void InitializeView()
    {
        // Add an image to the editor/resources directory & set its type to 'Cursor' first...

        Cursor.SetCursor(picker, new Vector2(0, 19), CursorMode.Auto);
        toRecolor.filterMode = FilterMode.Point;
        toRecolor.Apply();
        saveName = toRecolor.name + " " + saved;

        preview = DuplicateTexture(toRecolor);

        oldPalette = GetPaletteFrom(toRecolor);
        newPalette = GetPaletteFrom(toRecolor);
        if (oldPalette.palette.Count > 0)
        {
            Debug.Log("Selection changed by InitializeView");
            selection = oldPalette.palette[0];
            variation = newPalette.palette[0];
        }
    }

    private void DrawImages()
    {
        toRecolor_rect = new Rect(55, y_button + height_button * 2, toRecolor.width * ratio, toRecolor.height * ratio);
        preview_rect = new Rect(80 + preview.width * ratio, y_button + height_button * 2, preview.width * ratio, preview.height * ratio);

        EditorGUI.LabelField(new Rect(55, y_button + height_button, 100, 30), "Original:");
        EditorGUI.DrawTextureTransparent(toRecolor_rect, toRecolor, ScaleMode.ScaleToFit, 0);

        EditorGUIUtility.AddCursorRect(toRecolor_rect, MouseCursor.CustomCursor);

        if (toRecolor.width > realTimePreviewUnder)
        {
            if (GUI.Button(new Rect(80 + toRecolor.width * ratio, y_button + height_button, 100, 30), "Update Preview"))
            {

                ApplyPalette();
                return;
            }
        }
        else
        {
            EditorGUI.LabelField(new Rect(80 + toRecolor.width * ratio, y_button + height_button, 100, 30), "Recolored:");
            ApplyPalette();
        }
        EditorGUI.DrawTextureTransparent(preview_rect, preview, ScaleMode.ScaleToFit, 0);
    }

    void DrawFields()
    {
        //EditorGUILayout.LabelField( "Save name:");//new Rect(510, y_button - height_button, 100, height_button),
        saveName = EditorGUI.TextField(new Rect(170, 0, 300, 20), "Save Name", saveName);//new Rect(520, y_button, 100, height_button),

        realTimePreviewUnder = EditorGUI.IntField(new Rect(170, 20, 200, 20), "Realtime Preview Under", realTimePreviewUnder);
        EditorGUI.LabelField(new Rect(370, 20, 120, 20), " px");
        //EditorGUI.LabelField(new Rect(55, y_button, 75, 30), "Zoom: ");
        ratio = EditorGUI.Slider(new Rect(170, 40, 300, 20), "Zoom: ", ratio, 0.1f, 10f);


        if (Event.current.type == EventType.MouseDown)
        {
            selection = PickColor();
            if (oldPalette.palette.Contains(selection))
                variation = newPalette.palette[oldPalette.palette.IndexOf(selection)];
            //Update the window to be consistent with selected color
            Repaint();
        }
        EditorGUI.DrawRect(new Rect(520, 60, 50, 50), selection);
        GUIContent eyedropper = EditorGUIUtility.IconContent("EyeDropper.Large");
        EditorGUI.LabelField(new Rect(530, 85, 20, 20), eyedropper);
        //EditorGUI.DrawTextureAlpha(new Rect(500, 0, 50, 50), picker, ScaleMode.ScaleToFit);
        //selection  = EditorGUI.ColorField(new Rect(500, 0, 100, 25), "Picked color", selection);


        variation = EditorGUI.ColorField(new Rect(580, 60, 50, 50), variation);

        if (oldPalette.palette.Contains(selection))
        {
            newPalette.palette[oldPalette.palette.IndexOf(selection)] = variation;
        }

    }


    void DrawButtons()
    {
        if (GUI.Button(new Rect(170, y_button, 100, height_button), "Restore Palette"))
        {
            InitializeView();
        }
        if (GUI.Button(new Rect(280, y_button, 100, height_button), "Save"))
        {
            string path = AssetDatabase.GetAssetPath(toRecolor).Replace(toRecolor.name + ".png", "");

            SaveTexture(preview, saveName, path);
            //SaveTexture(picker, "picker", path);
            saved++;
        }
        if (GUI.Button(new Rect(390, y_button, 100, height_button), "Save As..."))
        {
            string path = path = EditorUtility.SaveFilePanel(
            "Save texture as PNG",
            "",
            saveName + ".png",
            "png");

            SaveTexture(preview, saveName, path);
            //SaveTexture(picker, "picker", path);
            saved++;
        }
    }

    public void ApplyPalette()
    {
        for (int x = 0; x < preview.width; x++)
        {
            for (int y = 0; y < preview.height; y++)
            {
                Color toReplace = toRecolor.GetPixel(x, y);
                if (oldPalette.palette.Contains(toReplace))
                {
                    Color color = newPalette.palette[oldPalette.palette.IndexOf(toReplace)];
                    preview.SetPixel(x, y, color);
                }
            }
        }
        preview.Apply();
    }


    public Color PickColor()
    {
        Color c = selection;
        if (IsMouseOverRect(toRecolor_rect))
        {
            Vector2Int pos = TextureCoordinate(toRecolor_rect, toRecolor, ratio);
            c = toRecolor.GetPixel(pos.x, pos.y);
            Debug.Log("Picking color");
        }
        return c;
    }

    #region SUPPORT_FUNCTION
    private bool SaveTexture(Texture2D t, string fileName, string path)
    {
        var bytes = t.EncodeToPNG();
        string fullPath = path + fileName + ".png";
        var file = File.Open(fullPath, FileMode.Create);
        var binary = new BinaryWriter(file);
        binary.Write(bytes);
        file.Close();


        AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ImportRecursive);
        this.ShowNotification(new GUIContent("Saved " + fileName + " in " + path), 5);
        Debug.Log("Saved " + fileName + " in " + path);

        return true;
    }

    public static void InvertColors(Texture2D texture)
    {
        for (int m = 0; m < texture.mipmapCount; m++)
        {
            Color[] c = texture.GetPixels(m);
            for (int i = 0; i < c.Length; i++)
            {
                c[i].r = 1 - c[i].r;
                c[i].g = 1 - c[i].g;
                c[i].b = 1 - c[i].b;
            }
            texture.SetPixels(c, m);
        }
        texture.Apply();
    }

    private static Texture2D TextureField(string name, Texture2D texture)
    {
        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperCenter;
        style.fixedWidth = 70;
        GUILayout.Label(name, style);
        var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));

        return result;
    }

    public static Vector2 RelativeMousePosIn(Rect rect)
    {
        Vector2 relativePos = new Vector2(-1, -1);
        Vector2 mouse = Event.current.mousePosition;
        if (IsMouseOverRect(rect))
        {
            //Inside the rectangle
            relativePos.x = (int)(mouse.x - rect.x);
            relativePos.y = (int)(mouse.y - rect.y);
        }
        return relativePos;
    }

    public static bool IsMouseOverRect(Rect rect)
    {
        Vector2 mouse = Event.current.mousePosition;
        return (mouse.x > rect.x && mouse.x < rect.x + rect.width && mouse.y > rect.y && mouse.y < rect.y + rect.height);
    }

    public static Vector2Int TextureCoordinate(Rect rect, Texture texture, float ratio)
    {
        Vector2Int coordinate = new Vector2Int(0, 0);
        if (IsMouseOverRect(rect))
        {
            Vector2 v2 = RelativeMousePosIn(rect);
            coordinate = new Vector2Int((int)(v2.x / ratio), (int)(v2.y / ratio));
            //Inside the rectangle
            coordinate.y = ((texture.height - 1) - coordinate.y);
        }
        return coordinate;
    }

    public static Texture2D DuplicateTexture(Texture2D texture)
    {
        Texture2D duplicated = new Texture2D(texture.width, texture.height);

        duplicated.SetPixels(texture.GetPixels());
        duplicated.filterMode = FilterMode.Point;

        duplicated.Apply();

        return duplicated;
    }

    public static void DrawPalette(SpriteToolsWindow window, Palette p, float x_origin = 0, float y_origin = 0, float cell_width = 40, float cell_height = 40, float spacing = 2, string paletteName = "Palette: ")
    {
        EditorGUI.LabelField(new Rect(x_origin, y_origin, 100, cell_height), paletteName);

        int col, row = 0;
        float x = x_origin, y = y_origin;

        for (int i = 0; i < p.palette.Count; i++)
        {
            int cells = Mathf.CeilToInt(window.position.width / (cell_width * 1.5f + spacing)) - 2;
            cells = cells < 1 ? 1 : cells;


            col = i % cells;
            x = x_origin + col * (cell_width * 1.5f + spacing);

            if (col == 0)
            {
                row++;
                y = y_origin + row * (cell_height + spacing);
            }


            Rect rect = new Rect(x, y + 1, cell_width / 2f, cell_height - 1);
            if (IsMouseOverRect(new Rect(x - 1, y - 1, cell_width * 1.5f + 3, cell_height + 3)))
            {
                Debug.Log("Click palette color");
                if (Event.current.type == EventType.MouseDown)
                {
                    window.selection = window.oldPalette.palette[i];
                    window.variation = p.palette[i];
                    window.Repaint();
                }
            }
            if (window.variation.Equals(p.palette[i]))
                EditorGUI.DrawRect(new Rect(x - 2, y - 2, cell_width * 1.5f + 5, cell_height + 3), new Color(.6f, .6f, .6f, 1f));
            else
                EditorGUI.DrawRect(new Rect(x - 1, y - 1, cell_width * 1.5f + 3, cell_height + 3), new Color(.3f, .3f, .3f, 1f));


            Color temp = p.palette[i];
            EditorGUI.DrawRect(rect, window.oldPalette.palette[i]);
            p.palette[i] = EditorGUI.ColorField(new Rect(x + cell_width / 2f, y, cell_width, cell_height), p.palette[i]);

            if (!temp.Equals(p.palette[i]))
            {
                window.selection = window.oldPalette.palette[i];
                window.variation = p.palette[i];
            }
        }
    }
    #endregion
}

[Serializable]
public class Palette
{
    public List<Color> palette = new List<Color>();

    public Palette()
    {
    }
}