using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CreateRuinsWizard : ScriptableWizard
{
    public string nickname = "Unnamed";
    public int rounds = 100;
    public int elites = 10;

    private static string prefabs_folder = "Assets/Prefabs";
    private static string ruins_folder = prefabs_folder + "/Ruins";


    [MenuItem ("Henry's Tools/Generate Ruins")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<CreateRuinsWizard>("Generate Ruins", "Create");
    }

    private void ShowGenerationProgressBar(int generations_completed)
    {
        EditorUtility.DisplayProgressBar(
            "Ruin generation progress", 
            string.Format("Rounds run {0} / {1}", generations_completed, this.rounds), 
            ((float)generations_completed) / this.rounds);
    }

    private void OnWizardCreate()
    {
        if (!AssetDatabase.IsValidFolder(prefabs_folder)) {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(ruins_folder)) {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Ruins");
        }
        string ruinPath = "Assets/Prefabs/Ruins/" + nickname + ".prefab";
        
        // Warning dialog if file exists.
        Debug.Log(AssetDatabase.FindAssets(ruinPath));
        if (AssetDatabase.LoadAssetAtPath(ruinPath, typeof(GameObject)) &&
            !EditorUtility.DisplayDialog("Replace Existing File?", "Should " + ruinPath + " be replaced?", "replace", "cancel"))
        {
            return;
        }
        RuinGenerator gen = new RuinGenerator
        {
            num_rounds = rounds,
            num_elite = elites
        };
        GameObject obj = gen.GenerateRuin(ShowGenerationProgressBar).Instantiate();
        PrefabUtility.CreatePrefab(ruinPath, obj);
        DestroyImmediate(obj);
        EditorUtility.ClearProgressBar();
    }
}
