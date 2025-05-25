#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Threading.Tasks; // Necesario para Task si usas async/await
using System.Collections.Generic; // Para List

[CustomEditor(typeof(ModelAssetData))]
public class ModelAssetDataEditor : Editor
{
    private ModelAssetData _targetModelData;
    private Texture2D _currentIconPreview; // Para mostrar el icono actual en el inspector
    private void OnEnable()
    {

        _targetModelData = (ModelAssetData)target;
        if (_targetModelData.CustomIconSprite != null)
        {

            SetIconFromCustomSprite();

        }
        else
        {
            if (_targetModelData.ModelPrefab != null)
            {

                GenerateAndSetIconFromModelPrefab();

            }
            else
            {
                EditorGUILayout.HelpBox("Asigna un Prefab de modelo o un Sprite personalizado para generar el icono.", MessageType.Info);
            }
        }
        LoadCurrentIconPreview();
    }

    private void Icons()
    {
        _targetModelData = (ModelAssetData)target;
        if (_targetModelData.CustomIconSprite != null)
        {

            SetIconFromCustomSprite();

        }
        else
        {
            if (_targetModelData.ModelPrefab != null)
            {

                GenerateAndSetIconFromModelPrefab();

            }
            else
            {
                EditorGUILayout.HelpBox("Asigna un Prefab de modelo o un Sprite personalizado para generar el icono.", MessageType.Info);
            }
        }
        LoadCurrentIconPreview();
    }

    private void OnDisable()
    {
        _currentIconPreview = null;
    }


    public void DrawIconsOnRun()
    {
        _targetModelData = (ModelAssetData)target;
        if (_targetModelData.CustomIconSprite != null)
        {

            SetIconFromCustomSprite();

        }
        else
        {
            if (_targetModelData.ModelPrefab != null)
            {

                GenerateAndSetIconFromModelPrefab();

            }
            else
            {
                EditorGUILayout.HelpBox("Asigna un Prefab de modelo o un Sprite personalizado para generar el icono.", MessageType.Info);
            }
        }
        LoadCurrentIconPreview();
    }

    public override void OnInspectorGUI()
    {
        // Dibuja el Inspector por defecto
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Model Utilities", EditorStyles.boldLabel);

        // --- Botón para poblar materiales ---
        if (_targetModelData.ModelPrefab != null)
        {
            if (GUILayout.Button("Populate Materials from Model Prefab"))
            {
                PopulateMaterialsFromModelPrefab();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Asigna un Prefab de modelo para poder poblar los materiales automáticamente.", MessageType.Info);
        }

        EditorGUILayout.Space();

        // --- Gestión de la Textura Principal ---
        EditorGUILayout.LabelField("Main Texture Reference", EditorStyles.boldLabel);
        if (_targetModelData.ModelPrefab != null)
        {
            if (GUILayout.Button("Find and Assign Main Albedo Texture"))
            {
                FindAndAssignMainAlbedoTexture();
            }
            EditorGUILayout.HelpBox("Intenta encontrar la textura principal (Albedo) del modelo y asignarla aquí.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Asigna un Prefab de modelo para encontrar su textura principal.", MessageType.Info);
        }
        EditorGUILayout.Space();

        // --- Extracción de Materiales y Texturas (mismo código anterior) ---
        EditorGUILayout.LabelField("Material & Texture Extraction", EditorStyles.boldLabel);
        if (_targetModelData.ModelPrefab != null)
        {
            if (GUILayout.Button("Extract Materials and Textures from Prefab"))
            {
                ExtractMaterialsAndTexturesAsync().Forget();
            }
            EditorGUILayout.HelpBox("Extrae los materiales y texturas incrustadas del Prefab a archivos .mat y .png/.jpg en el disco. Esto puede ser necesario para editarlos directamente.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Asigna un Prefab de modelo para extraer materiales y texturas.", MessageType.Info);
        }
        EditorGUILayout.Space();

        // --- Gestión del icono (mismo código anterior) ---
        EditorGUILayout.LabelField("Asset Icon Management", EditorStyles.boldLabel);
        if (_currentIconPreview != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(_currentIconPreview, GUILayout.Width(64), GUILayout.Height(64));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        if (_targetModelData.CustomIconSprite != null)
        {
            if (GUILayout.Button("Set Custom Sprite as Icon"))
            {
                SetIconFromCustomSprite();
            }
        }
        else
        {
            if (_targetModelData.ModelPrefab != null)
            {
                if (GUILayout.Button("Generate and Set Icon from Model Prefab"))
                {
                    GenerateAndSetIconFromModelPrefab();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Asigna un Prefab de modelo o un Sprite personalizado para generar el icono.", MessageType.Info);
            }
        }

        if (GUILayout.Button("Clear Custom Icon"))
        {
            ClearCustomIcon();
        }

        EditorGUILayout.Space();

        // --- Botones de utilidad (mismo código anterior) ---
        if (GUILayout.Button("Generate New Model ID"))
        {
            _targetModelData.ModelID = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(_targetModelData);
        }

        if (GUILayout.Button("Refresh Last Modified Date"))
        {
            _targetModelData.RefreshModifiedDate();
            EditorUtility.SetDirty(_targetModelData);
        }
    }

    // --- Métodos existentes: PopulateMaterialsFromModelPrefab, LoadCurrentIconPreview, SetIconFromCustomSprite, GenerateAndSetIconFromModelPrefab, ClearCustomIcon ---
    // (Asegúrate de que estos métodos están copiados de la versión anterior)

    private void PopulateMaterialsFromModelPrefab()
    {
        if (_targetModelData.ModelPrefab == null)
        {
            Debug.LogWarning("No Model Prefab assigned to populate materials from.");
            return;
        }

        _targetModelData.Materials.Clear();
        MeshRenderer[] meshRenderers = _targetModelData.ModelPrefab.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in meshRenderers)
        {
            foreach (var mat in mr.sharedMaterials)
            {
                if (mat != null && !_targetModelData.Materials.Contains(mat))
                {
                    _targetModelData.Materials.Add(mat);
                }
            }
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = _targetModelData.ModelPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var smr in skinnedMeshRenderers)
        {
            foreach (var mat in smr.sharedMaterials)
            {
                if (mat != null && !_targetModelData.Materials.Contains(mat))
                {
                    _targetModelData.Materials.Add(mat);
                }
            }
        }

        if (_targetModelData.Materials.Count > 0)
        {
            Debug.Log($"Successfully populated {_targetModelData.Materials.Count} materials from {_targetModelData.ModelPrefab.name}.");
        }
        else
        {
            Debug.LogWarning($"No materials found on MeshRenderers or SkinnedMeshRenderers in {_targetModelData.ModelPrefab.name}.");
        }
        EditorUtility.SetDirty(_targetModelData);
    }

    private void LoadCurrentIconPreview()
    {
        _currentIconPreview = AssetPreview.GetAssetPreview(_targetModelData);
        if (_currentIconPreview == null && _targetModelData.ModelPrefab != null)
        {
            _currentIconPreview = AssetPreview.GetAssetPreview(_targetModelData.ModelPrefab);
        }
        if (_targetModelData.CustomIconSprite != null)
        {
            _currentIconPreview = _targetModelData.CustomIconSprite.texture;
        }
    }

    private void SetIconFromCustomSprite()
    {
        if (_targetModelData.CustomIconSprite == null)
        {
            Debug.LogWarning("Cannot set icon: No Custom Icon Sprite assigned.");
            return;
        }
        Texture2D iconTexture = _targetModelData.CustomIconSprite.texture;
        if (iconTexture != null)
        {
            EditorGUIUtility.SetIconForObject(_targetModelData, iconTexture);
            _currentIconPreview = iconTexture;
            EditorUtility.SetDirty(_targetModelData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Custom Sprite '{_targetModelData.CustomIconSprite.name}' set as icon for {_targetModelData.name}.");
        }
        else
        {
            Debug.LogError($"Failed to get texture from Custom Icon Sprite: {_targetModelData.CustomIconSprite.name}");
        }
    }

    private void GenerateAndSetIconFromModelPrefab()
    {
        if (_targetModelData.ModelPrefab == null)
        {
            Debug.LogWarning("Cannot generate icon: Model Prefab is not assigned.");
            return;
        }
        Texture2D previewTexture = AssetPreview.GetAssetPreview(_targetModelData.ModelPrefab);
        if (previewTexture == null)
        {
            previewTexture = AssetPreview.GetMiniThumbnail(_targetModelData.ModelPrefab);
        }
        if (previewTexture != null)
        {
            EditorGUIUtility.SetIconForObject(_targetModelData, previewTexture);
            _currentIconPreview = previewTexture;
            EditorUtility.SetDirty(_targetModelData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Icon successfully set from Model Prefab for {_targetModelData.name}.");
        }
        else
        {
            Debug.LogWarning("Failed to generate asset preview from Model Prefab. Ensure it's valid.");
        }
    }

    private void ClearCustomIcon()
    {
        EditorGUIUtility.SetIconForObject(_targetModelData, null);
        _currentIconPreview = null;
        EditorUtility.SetDirty(_targetModelData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Custom icon cleared for {_targetModelData.name}. Default icon restored.");
    }

    private async Task ExtractMaterialsAndTexturesAsync()
    {
        if (_targetModelData.ModelPrefab == null)
        {
            Debug.LogWarning("No Model Prefab assigned to extract materials and textures from.");
            return;
        }

        string modelPath = AssetDatabase.GetAssetPath(_targetModelData.ModelPrefab);
        if (string.IsNullOrEmpty(modelPath))
        {
            Debug.LogError($"Could not find asset path for Model Prefab: {_targetModelData.ModelPrefab.name}");
            return;
        }

        string modelDirectory = Path.GetDirectoryName(modelPath);
        string destinationFolder = Path.Combine(modelDirectory, "ExtractedAssets");

        if (!AssetDatabase.IsValidFolder(destinationFolder))
        {
            AssetDatabase.CreateFolder(modelDirectory, "ExtractedAssets");
        }

        Object[] allSubAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        List<Object> assetsToExtract = new List<Object>();
        foreach (Object subAsset in allSubAssets)
        {
            if ((subAsset is Material || subAsset is Texture) && AssetDatabase.IsSubAsset(subAsset))
            {
                assetsToExtract.Add(subAsset);
            }
        }

        if (assetsToExtract.Count == 0)
        {
            Debug.Log($"No embedded materials or textures found in {Path.GetFileName(modelPath)} to extract.");
            return;
        }

        Debug.Log($"Attempting to extract {assetsToExtract.Count} embedded assets from {Path.GetFileName(modelPath)} to {destinationFolder}...");

        await Task.Run(() =>
        {
            foreach (var asset in assetsToExtract)
            {
                string newPath = Path.Combine(destinationFolder, $"{asset.name}{Path.GetExtension(AssetDatabase.GetAssetPath(asset))}");
                if (asset is Material)
                {
                    newPath = Path.Combine(destinationFolder, $"{asset.name}.mat");
                }
                else if (asset is Texture)
                {
                    string originalExtension = Path.GetExtension(AssetDatabase.GetAssetPath(asset));
                    if (string.IsNullOrEmpty(originalExtension)) originalExtension = ".png";
                    newPath = Path.Combine(destinationFolder, $"{asset.name}{originalExtension}");
                }

                string moveResult = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(asset), newPath);
                if (string.IsNullOrEmpty(moveResult))
                {
                    Debug.Log($"Extracted and moved: {asset.name} to {newPath}");
                }
                else
                {
                    Debug.LogError($"Failed to extract and move {asset.name}: {moveResult}");
                }
            }
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        await Task.Delay(100);
        PopulateMaterialsFromModelPrefab();

        Debug.Log($"Extraction process completed for {_targetModelData.name}. Materials and textures should now be external.");
    }

    /// <summary>
    /// Intenta encontrar la textura principal (Albedo) del modelo y asignarla a MainAlbedoTexture.
    /// </summary>
    private void FindAndAssignMainAlbedoTexture()
    {
        if (_targetModelData.ModelPrefab == null)
        {
            Debug.LogWarning("Cannot find main albedo texture: Model Prefab is not assigned.");
            _targetModelData.MainAlbedoTexture = null;
            EditorUtility.SetDirty(_targetModelData);
            return;
        }

        Texture2D foundTexture = null;

        // Estrategia 1: Buscar la propiedad _MainTex en el primer material disponible
        if (_targetModelData.Materials != null && _targetModelData.Materials.Count > 0)
        {
            foreach (Material mat in _targetModelData.Materials)
            {
                if (mat != null && mat.HasProperty("_MainTex"))
                {
                    foundTexture = mat.mainTexture as Texture2D;
                    if (foundTexture != null)
                    {
                        Debug.Log($"Found main texture '_MainTex' in material '{mat.name}': {foundTexture.name}");
                        break; // Salir del bucle una vez que encontramos la primera textura
                    }
                }
            }
        }

        // Estrategia 2: Si no se encuentra con _MainTex, buscar propiedades comunes de Albedo/BaseMap
        if (foundTexture == null)
        {
            if (_targetModelData.Materials != null && _targetModelData.Materials.Count > 0)
            {
                foreach (Material mat in _targetModelData.Materials)
                {
                    if (mat == null) continue;

                    // Intentar nombres de propiedades comunes para Albedo/BaseColor
                    if (mat.HasProperty("_BaseMap")) // HDRP/URP
                    {
                        foundTexture = mat.GetTexture("_BaseMap") as Texture2D;
                    }
                    else if (mat.HasProperty("_Albedo")) // Common custom shaders
                    {
                        foundTexture = mat.GetTexture("_Albedo") as Texture2D;
                    }
                    // Puedes añadir más nombres de propiedades aquí si tienes shaders específicos

                    if (foundTexture != null)
                    {
                        Debug.Log($"Found main texture by name (e.g., _BaseMap, _Albedo) in material '{mat.name}': {foundTexture.name}");
                        break;
                    }
                }
            }
        }

        // Asignar la textura encontrada
        _targetModelData.MainAlbedoTexture = foundTexture;
        if (foundTexture != null)
        {
            Debug.Log($"Main Albedo Texture assigned: {_targetModelData.MainAlbedoTexture.name}");
        }
        else
        {
            Debug.LogWarning("No suitable main albedo texture found for this model's materials.");
        }

        EditorUtility.SetDirty(_targetModelData); // Marca el SO como modificado para guardar la referencia
        AssetDatabase.SaveAssets(); // Guarda el asset para que persista
    }
}

// Clase de extensión para permitir .Forget() en Task
public static class TaskExtensions
{
    public static void Forget(this Task task)
    {
        // No hacer nada, solo para suprimir la advertencia del compilador de que la tarea no se espera.
        // Solo para escenarios donde el resultado de la tarea no necesita ser observado y las excepciones son manejadas o no son críticas.
    }
}


#endif