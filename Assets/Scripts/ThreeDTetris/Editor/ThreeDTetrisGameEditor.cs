using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ThreeDTetrisSceneAutoBuilder
{
    static ThreeDTetrisSceneAutoBuilder()
    {
        EditorApplication.delayCall += BuildUiForOpenScenes;
    }

    private static void BuildUiForOpenScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        ThreeDTetrisGame[] games = Object.FindObjectsOfType<ThreeDTetrisGame>();
        for (int i = 0; i < games.Length; i++)
        {
            games[i].BuildEditableSceneObjects();
            EditorUtility.SetDirty(games[i]);
            EditorSceneManager.MarkSceneDirty(games[i].gameObject.scene);
        }
    }
}

[CustomEditor(typeof(ThreeDTetrisGame))]
public sealed class ThreeDTetrisGameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(12f);
        EditorGUILayout.LabelField("Editable Scene Tools", EditorStyles.boldLabel);

        ThreeDTetrisGame game = (ThreeDTetrisGame)target;
        if (GUILayout.Button("Build / Link Editable Scene Objects"))
        {
            game.BuildEditableSceneObjects();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }

        if (GUILayout.Button("Rebuild Container Visuals"))
        {
            game.RebuildContainerVisuals();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
    }
}
