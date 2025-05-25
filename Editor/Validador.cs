#if UNITY_EDITOR


using UnityEngine;
using UnityEditor;
using System.IO; // Para Path
using System.Linq;
using System.Data; // Para el .Any() de las extensiones

public class Validador : AssetPostprocessor
{
    // Cargar la instancia del ScriptableObject de reglas de validaci�n.
    // Usamos 'static' para que se cargue una sola vez y est� disponible para todas las instancias de AssetPostprocessor.
    private static ValidationRules_SO _validationRules;

    private static ValidationRules_SO GetValidationRules()
    {
        if (_validationRules == null)
        {
            // Carga la instancia del ScriptableObject desde la carpeta Resources.
            // Aseg�rate de que el nombre del asset coincida (sin la extensi�n .asset).
            _validationRules = Resources.Load<ValidationRules_SO>("ProjectValidationRules"); // <<<<<<<<<<<<<<<< NOMBRE DEL ARCHIVO SO

            if (_validationRules == null)
            {
                Debug.LogError("[Validation] ValidationRules_SO not found! Please create one at Assets/Editor/ProjectValidationRules.asset");
                // Crea una instancia temporal si no se encuentra para evitar NullReferenceException
                _validationRules = ScriptableObject.CreateInstance<ValidationRules_SO>();
            }
        }
        return _validationRules;
    }
    // --- M�todos de Importaci�n ---

    // Se llama antes de que un modelo (FBX, OBJ) sea importado
    void OnPreprocessModel()
    {

        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null) return;

        ValidationRules_SO rules = GetValidationRules(); // Obtiene las reglas

        // --- VALIDAR Y CORREGIR ESCALA GLOBAL (Esta s� es correcta y estable) ---
        if (importer.globalScale != rules.requiredModelScale)
        {
            Debug.LogWarning($"[Validation] Model '{importer.assetPath}' has non-standard global scale ({importer.globalScale}). Setting to 1.0.");
            importer.globalScale = 1.0f; // �Correcci�n autom�tica!
        }

        // --- OPTIMIZACIONES GENERALES DE MALLA (Aqu� 'optimizeMeshVertices' s� es v�lido) ---
        // Esta propiedad le dice a Unity que intente optimizar la malla en general.
        // A menudo, esto incluye la reordenaci�n de v�rtices e �ndices para mejor cach�.
        if (!importer.optimizeMeshVertices)
        {
            Debug.LogWarning($"[Validation] Model '{importer.assetPath}' has 'Optimize Mesh Vertices' disabled. Enabling it for better performance.");
            importer.optimizeMeshVertices = true;
        }

        // --- CONFIGURACI�N DE UVs DE LIGHTMAP (Esta tambi�n es correcta) ---
        // Crucial para la iluminaci�n global de objetos est�ticos.
        if (importer.assetPath.Contains("StaticProp") || importer.assetPath.Contains("LevelGeometry"))
        {
            if (!importer.generateSecondaryUV)
            {
                Debug.LogWarning($"[Validation] Model '{importer.assetPath}' is likely static geometry but 'Generate Lightmap UVs' is disabled. Enabling it.");
                importer.generateSecondaryUV = true;
            }
        }

        // --- VALIDACIONES DE ANIMACI�N ---
        // �Esta l�nea es correcta si 'using UnityEditor;' est� presente!
        if (importer.animationType == ModelImporterAnimationType.Human)
        {
            // Aqu� puedes a�adir validaciones espec�ficas para modelos Humanoid,
            // como si tienen un Avatar configurado, si los nombres de huesos son est�ndar, etc.
            Debug.Log($"[Validation] Model '{importer.assetPath}' is configured as Humanoid. Ensure Avatar configuration is correct.");
        }
        else if (importer.animationType == ModelImporterAnimationType.Generic)
        {
            // Validaciones para Generic Rigs (ej: criaturas, veh�culos)
            Debug.Log($"[Validation] Model '{importer.assetPath}' is configured as Generic. Ensure Generic rig setup is correct.");
        }
        else if (importer.animationType == ModelImporterAnimationType.None)
        {
            // Validaciones para modelos sin animaci�n
            // Si el modelo es un personaje, esto ser�a un ERROR.
            if (importer.assetPath.Contains("Character") && !importer.assetPath.Contains("Prop"))
            {
                Debug.LogError($"[Validation] Model '{importer.assetPath}' seems to be a character but has Animation Type set to 'None'.");
            }
        }

        // --- Otras propiedades que s� son comunes y estables en ModelImporter ---
        // importer.importMaterials = false; // Si no quieres que Unity cree materiales autom�ticamente
        importer.meshCompression = ModelImporterMeshCompression.High; // Para modelos con mucha geometr�a
        // importer.useFileUnits = true; // Si el modelo debe usar las unidades del archivo 3D
        // importer.vertexStreamCompression = true; // Para reducir el tama�o en disco de los v�rtices
    }

    // --- Validaciones Post-Importaci�n (aqu� es donde se inspecciona el resultado) ---
    public void OnPostprocessModel(GameObject root)
    {
        ModelImporter importer = assetImporter as ModelImporter;
        // Validar si se a�adi� un MeshCollider (esto se hace en OnPostprocessModel)
        MeshCollider meshCollider = root.GetComponentInChildren<MeshCollider>();
        if (meshCollider != null)
        {
            Debug.LogWarning($"[Validation] Model '{importer.assetPath}' has a MeshCollider generated during import. This is often inefficient. Consider disabling 'Generate Colliders' in the Model Importer settings and adding simpler colliders manually.");
            // Si quieres eliminarlo autom�ticamente (solo si la escena se puede re-importar con seguridad):
            // Esto es delicado: DestroyImmediate(meshCollider, true);
        }
        ValidationRules_SO rules = GetValidationRules(); // Obtiene las reglas

        // Puedes inspeccionar los MeshFilters y sus mallas si necesitas validar v�rtices/pol�gonos.
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in meshFilters)
        {
            if (filter.sharedMesh != null)
            {
                // Ejemplo: Validar conteo de tri�ngulos
                if (filter.sharedMesh.triangles.Length / 3 > rules.maxTrianglesForProp) // 10,000 tri�ngulos, por ejemplo
                {
                    Debug.LogWarning($"[Validation] Mesh '{filter.sharedMesh.name}' in '{importer.assetPath}' has too many triangles ({filter.sharedMesh.triangles.Length / 3}). Consider optimizing.");
                }
                // Ejemplo: Validar si hay UVs para lightmaps si se generaron
                if (importer.generateSecondaryUV && filter.sharedMesh.uv2.Length == 0)
                {
                    Debug.LogWarning($"[Validation] Model '{importer.assetPath}' was set to generate Lightmap UVs, but mesh '{filter.sharedMesh.name}' has no UV2 channel.");
                }
            }
        }
    }

    // Se llama antes de que una textura sea importada
    void OnPreprocessTexture()
    {
        TextureImporter importer = assetImporter as TextureImporter;
        if (importer == null) return;
        ValidationRules_SO rules = GetValidationRules(); // Obtiene las reglas
        // Validar formato de textura (ej: forzar a PNG o JPG)
        string extension = Path.GetExtension(importer.assetPath).ToLower();
        string assetFileName = Path.GetFileNameWithoutExtension(importer.assetPath); // Nombre del archivo sin extensi�n
        if (!rules.allowedTextureExtensions.Any(ext => ext == extension))
        {
            Debug.LogError($"[Validation] Texture '{importer.assetPath}' is not in PNG or JPG format. Please use these formats.");
            // Puedes incluso detener la importaci�n lanzando una excepci�n si es cr�tico.
            // throw new UnityException($"Invalid texture format for {importer.assetPath}");
        }

        // Validar tama�o de textura (potencias de 2)
        // Aunque Unity las maneja, es buena pr�ctica validar el original.
        // --- VALIDAR TAMA�O DE TEXTURA (POTENCIAS DE DOS Y TAMA�O M�XIMO) ---
        // Para obtener el tama�o de la imagen original antes de que Unity la procese,
        // necesitamos cargarla manualmente. Esto puede ser un poco costoso para MUCHOS assets,
        // pero para validaci�n en el editor es aceptable.
        Texture2D tempTexture = new Texture2D(1, 1); // Crea una textura m�nima
        byte[] fileData = File.ReadAllBytes(importer.assetPath); // Lee los bytes del archivo original

        bool loaded = false;
        try
        {
            // Carga los bytes en la textura temporal. Esto funcionar� para PNG/JPG.
            loaded = tempTexture.LoadImage(fileData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Validation] Failed to load image data for '{importer.assetPath}' to check dimensions: {e.Message}");
            loaded = false;
        }

        if (loaded)
        {
            // Validar Potencias de Dos
            if (rules.enforcePowerOfTwoTextureSizes &&
                (!Mathf.IsPowerOfTwo(tempTexture.width) || !Mathf.IsPowerOfTwo(tempTexture.height)))
            {
                Debug.LogWarning($"[Validation] Texture '{importer.assetPath}' size ({tempTexture.width}x{tempTexture.height}) is not a power of two. This can lead to inefficient compression and mipmap generation. Please resize to 256, 512, 1024, 2048, etc.");
            }

            // Validar Tama�o M�ximo
            if (tempTexture.width > rules.maxTextureSize || tempTexture.height > rules.maxTextureSize)
            {
                Debug.LogWarning($"[Validation] Texture '{importer.assetPath}' size ({tempTexture.width}x{tempTexture.height}) exceeds max allowed size ({rules.maxTextureSize}). Consider optimizing.");
            }
        }
        else
        {
            // Esto podr�a pasar si el archivo no es una imagen v�lida o un formato que LoadImage soporta.
            Debug.LogWarning($"[Validation] Could not read image dimensions for '{importer.assetPath}'. Skipping size validation.");
        }

        // �IMPORTANTE! Limpiar la textura temporal para liberar memoria
        GameObject.DestroyImmediate(tempTexture);
        // --- AUTOMATIZAR TIPO DE TEXTURA Y sRGB BASADO EN CONVENCIONES DE NOMBRES ---
        bool typeRuleApplied = false;
        foreach (var rule in rules.textureTypeRules)
        {
            if (assetFileName.Contains(rule.nameContains))
            {
                // Aplica la regla encontrada
                importer.textureType = rule.textureType;
                importer.sRGBTexture = rule.sRGB;
                Debug.Log($"[Validation] Applied texture type rule for '{importer.assetPath}': Type={rule.textureType}, sRGB={rule.sRGB} (matched '{rule.nameContains}').");
                typeRuleApplied = true;
                break; // Importante: Salir despu�s de aplicar la primera regla que coincida
            }
        }

        if (!typeRuleApplied)
        {
            // Si ninguna regla coincide, puedes establecer un valor por defecto o advertir
            Debug.LogWarning($"[Validation] No specific texture type rule found for '{importer.assetPath}'. Defaulting to 'Default' type and sRGB true.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
        }

        // --- CONFIGURACI�N DE COMPRESI�N (Puedes hacer esto configurable tambi�n) ---
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.crunchedCompression = true;
        importer.compressionQuality = 50; // Podr�as a�adir esto al SO
    }


    // Se llama antes de que un audio sea importado
    void OnPreprocessAudio()
    {
            AudioImporter importer = assetImporter as AudioImporter;
            if (importer == null) return;

            // --- VALIDACIONES DE FORMATO DE ARCHIVO ORIGINAL ---
            string extension = Path.GetExtension(importer.assetPath).ToLower();
            if (extension != ".wav" && extension != ".ogg")
            {
                Debug.LogError($"[Validation] Audio '{importer.assetPath}' is not in WAV or OGG format. Please use these formats.");
                // Considera lanzar una excepci�n si esto es un error cr�tico para detener la importaci�n.
                // throw new UnityException($"Invalid audio format for {importer.assetPath}");
            }

            // --- CONFIGURACI�N DE LAS PROPIEDADES DE IMPORTACI�N DEL AUDIO ---

            // Unity usa 'AudioImporterSampleSettings' para agrupar estas propiedades.
            // Puedes obtener las configuraciones por defecto o un override si ya existe.

            // Opci�n 1: Modificar las configuraciones por defecto (afecta a todas las plataformas)
            // Puedes acceder directamente a 'importer.defaultSampleSettings'
            // y luego llamar a 'importer.SetCustomSamplerSettings(platform, settings)' si quieres overrides
            // o simplemente asignar a defaultSampleSettings y luego re-importar.

            // Opci�n 2: Crear un nuevo AudioImporterSampleSettings y aplicarlo
            // Esta es la forma m�s com�n para configurar plataformas espec�ficas o la configuraci�n por defecto.

            AudioImporterSampleSettings settings = importer.defaultSampleSettings; // O settings = new AudioImporterSampleSettings();

            // Configurar tipo de carga (Streaming para m�sica, DecompressOnLoad para SFX)
            if (importer.assetPath.Contains("Music"))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.preloadAudioData = false; // Generalmente false para streaming
            }
            else // SFX
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
            }

            // Configurar el formato de compresi�n y calidad
            settings.compressionFormat = AudioCompressionFormat.Vorbis; // O PCM, ADPCM
            settings.quality = 0.5f; // Calidad de compresi�n (0.0f a 1.0f)
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; // O OptimizeSampleRate, etc.

            // Aplica los settings.
            // Si no est�s usando overrides por plataforma, simplemente asigna a defaultSampleSettings
            importer.defaultSampleSettings = settings;

            // Si quieres aplicar settings espec�ficos para una plataforma (ej: "Android", "Standalone"):
            // AudioImporterSampleSettings androidSettings = new AudioImporterSampleSettings();
            // androidSettings.compressionFormat = AudioCompressionFormat.ADPCM;
            // importer.SetOverrideSampleSettings("Android", androidSettings);

            // Si el assetPath contiene "_SFX" y quieres una compresi�n m�s ligera para SFX:
            if (importer.assetPath.Contains("_SFX") || importer.assetPath.Contains("SFX_"))
            {
                settings.compressionFormat = AudioCompressionFormat.ADPCM; // Muy bueno para SFX cortos
                settings.quality = 0.8f; // ADPCM es fijo, pero el 'quality' puede influir en otros formatos
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            }
            else if (importer.assetPath.Contains("_Music") || importer.assetPath.Contains("Music_"))
            {
                settings.compressionFormat = AudioCompressionFormat.Vorbis; // Excelente para m�sica
                settings.quality = 0.5f; // Calidad media
                settings.loadType = AudioClipLoadType.Streaming;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            }

            // Vuelve a aplicar la configuraci�n (importante despu�s de cualquier modificaci�n)
            importer.defaultSampleSettings = settings;
        
    }

    // --- M�todos Post-Importaci�n (cuando ya se han cargado los datos) ---

    // Se llama despu�s de que un asset ha sido importado.
    // Aqu� puedes acceder a los datos ya importados del asset.
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string importedAssetPath in importedAssets)
        {
            Debug.Log(importedAssetPath);
            // Solo procesar modelos 3D (ej. .fbx, .obj)
            if (IsModelFile(importedAssetPath))
            {
                Debug.Log(importedAssetPath);
                GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(importedAssetPath);
                if (importedModel == null) continue;
                Debug.Log(importedModel);
                // Definir la ruta donde quieres guardar el ScriptableObject.
                // Podr�as crear una subcarpeta "ModelData" junto al modelo.
                string modelDirectory = Path.GetDirectoryName(importedAssetPath);
                string assetName = Path.GetFileNameWithoutExtension(importedAssetPath);
                string modelDataFolderPath = Path.Combine(modelDirectory, "ModelData");
                Debug.Log(modelDataFolderPath);
                if (!AssetDatabase.IsValidFolder(modelDataFolderPath))
                {
                    AssetDatabase.CreateFolder(modelDirectory, "ModelData");
                }

                string modelDataPath = Path.Combine(modelDataFolderPath, $"{assetName}_ModelData.asset");

                ModelAssetData existingModelData = AssetDatabase.LoadAssetAtPath<ModelAssetData>(modelDataPath);

                if (existingModelData == null)
                {
                    // No existe, creamos uno nuevo
                    Debug.Log($"[ModelAssetPostprocessor] Creating new ModelAssetData for: {importedAssetPath}");
                    ModelAssetData newModelData = ScriptableObject.CreateInstance<ModelAssetData>();
                    newModelData.ModelPrefab = importedModel;
                    newModelData.ModelName = assetName;
                    newModelData.RefreshModifiedDate(); // Asegurarse que la fecha sea actual

                    // Intenta poblar los materiales autom�ticamente al crear
                    PopulateMaterialsForModelData(newModelData, importedModel);
                    FindAndAssignMainAlbedoTextureForModelData(newModelData, importedModel); // �Nuevo! Asigna la textura principal
                    AssetDatabase.CreateAsset(newModelData, modelDataPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                {
                    // Ya existe, podr�as querer actualizarlo
                    // Por ejemplo, si el prefab del modelo ha cambiado
                    // O si quieres re-poblar los materiales si la estructura del modelo cambi�.
                    Debug.Log($"[ModelAssetPostprocessor] Updating existing ModelAssetData for: {importedAssetPath}");
                    existingModelData.ModelPrefab = importedModel; // Asegurar que referencia al prefab correcto
                    existingModelData.RefreshModifiedDate();
                    PopulateMaterialsForModelData(existingModelData, importedModel); // Re-poblar materiales
                    FindAndAssignMainAlbedoTextureForModelData(existingModelData, importedModel); // �Nuevo! Asigna la textura principal
                    EditorUtility.SetDirty(existingModelData); // Marcar como modificado para guardar
                    AssetDatabase.SaveAssets();
                }
            }
        }

        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".unity")) // Si es una escena
            {
                Debug.Log($"[Validation] Scene imported: {assetPath}. Consider running scene-specific validations.");
                // Podr�as cargar la escena temporalmente y validar GameObjects
                // Esto es m�s complejo y generalmente se hace con una ventana de editor separada.
            }

            // Validaciones m�s complejas que requieren que el asset ya est� en memoria
            // Ejemplo: revisar si un prefab tiene ciertos componentes o layers
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (go != null && PrefabUtility.IsPartOfPrefabAsset(go))
            {
                // Validar prefabs:
                // - �Tiene los colliders necesarios?
                // - �Tiene un script base de Character si es un personaje?
                // - �Est� en la capa correcta?
                // if (go.layer != LayerMask.NameToLayer("Characters")) { ... }
                // if (go.GetComponent<Rigidbody>() == null) { ... }
            }
        }

    }

    private static bool IsModelFile(string assetPath)
    {
        string extension = Path.GetExtension(assetPath).ToLower();
        return extension == ".fbx" || extension == ".obj" || extension == ".blend" || extension == ".gltf" || extension == ".glb";
    }

    // Helper para poblar materiales, similar al del Editor
    private static void PopulateMaterialsForModelData(ModelAssetData modelData, GameObject modelPrefab)
    {
        if (modelPrefab == null) return;

        modelData.Materials.Clear();
        MeshRenderer[] meshRenderers = modelPrefab.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in meshRenderers)
        {
            foreach (var mat in mr.sharedMaterials)
            {
                if (mat != null && !modelData.Materials.Contains(mat))
                {
                    modelData.Materials.Add(mat);
                }
            }
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = modelPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var smr in skinnedMeshRenderers)
        {
            foreach (var mat in smr.sharedMaterials)
            {
                if (mat != null && !modelData.Materials.Contains(mat))
                {
                    modelData.Materials.Add(mat);
                }
            }
        }
    }

    /// <summary>
    /// Intenta encontrar la textura principal (Albedo) del modelo y asignarla al ModelAssetData.
    /// Refactorizado de ModelAssetDataEditor para uso en post-procesamiento.
    /// </summary>
    private static void FindAndAssignMainAlbedoTextureForModelData(ModelAssetData modelData, GameObject modelPrefab)
    {
        if (modelData == null || modelPrefab == null)
        {
            modelData.MainAlbedoTexture = null;
            return;
        }

        Texture2D foundTexture = null;

        // Se asume que PopulateMaterialsForModelData ya se ejecut� y modelData.Materials est� actualizado
        if (modelData.Materials != null && modelData.Materials.Count > 0)
        {
            foreach (Material mat in modelData.Materials)
            {
                if (mat == null) continue;

                // Propiedades comunes de textura principal
                if (mat.HasProperty("_MainTex"))
                {
                    foundTexture = mat.mainTexture as Texture2D;
                }
                else if (mat.HasProperty("_BaseMap")) // URP/HDRP Standard
                {
                    foundTexture = mat.GetTexture("_BaseMap") as Texture2D;
                }
                else if (mat.HasProperty("_Albedo")) // Custom shaders
                {
                    foundTexture = mat.GetTexture("_Albedo") as Texture2D;
                }

                if (foundTexture != null)
                {
                    // Preferimos una textura real sobre un color base o textura nula
                    // Si encuentras una textura v�lida en cualquier material, �sala y sal.
                    break;
                }
            }
        }

        modelData.MainAlbedoTexture = foundTexture;
        if (foundTexture != null)
        {
            Debug.Log($"[ModelAssetPostprocessor] Main Albedo Texture assigned for {modelData.name}: {foundTexture.name}");
        }
        else
        {
            Debug.LogWarning($"[ModelAssetPostprocessor] No suitable main albedo texture found for {modelData.name}.");
        }
    }
}
#endif