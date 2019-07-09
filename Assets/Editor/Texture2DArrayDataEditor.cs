using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Texture2DArrayData), true)]
public class Texture2DArrayDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate"))
        {
            Texture2DArrayData component = (Texture2DArrayData)target;
            Texture2D[] textures = component.textures;
            Texture2DArray texture2DArray = new Texture2DArray(
                textures[0].width, textures[0].height, textures.Length, TextureFormat.RGBA32, true);
            
            for (int i = 0; i < textures.Length; i++)
            {
                texture2DArray.SetPixels32(textures[i].GetPixels32(), i);
            }
            texture2DArray.Apply();

            AssetDatabase.CreateAsset(texture2DArray, "Assets/Textures/" + component.name + ".asset");

            GameObject exampleMesh = GameObject.Find("ExampleTerrainMesh");
            Shader texturedDynamicTerrainShader = exampleMesh.GetComponent<Shader>();

            component.NotifyOfUpdatedValues();
            EditorUtility.SetDirty(target);
        }
    }
}
