using UnityEngine;
using UnityEditor;
using System.IO; // Para Path
using System.Linq;
using System.Data; // Para el .Any() de las extensiones

public class Validador : AssetPostprocessor
{
    // Cargar la instancia del ScriptableObject de reglas de validación.
    // Usamos 'static' para que se cargue una sola vez y esté disponible para todas las instancias de AssetPostprocessor.
    private static ValidationRules_SO _validationRules;

    private static ValidationRules_SO GetValidationRules()
    {
        if (_validationRules == null)
        {
            // Carga la instancia del ScriptableObject desde la carpeta Resources.
            // Asegúrate de que el nombre del asset coincida (sin la extensión .asset).
            _validationRules = Resources.Load<ValidationRules_SO>("ProjectValidationRules"); // <<<<<<<<<<<<<<<< NOMBRE DEL ARCHIVO SO

            if (_validationRules == null)
            {
                Debug.LogError("[Validation] ValidationRules_SO not found! Please create one at Assets/EditorTools/AssetValidation/Resources/ProjectValidationRules.asset");
                // Crea una instancia temporal si no se encuentra para evitar NullReferenceException
                _validationRules = ScriptableObject.CreateInstance<ValidationRules_SO>();
            }
        }
        return _validationRules;
    }
    // --- Métodos de Importación ---

    // Se llama antes de que un modelo (FBX, OBJ) sea importado
    void OnPreprocessModel()
    {

        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null) return;

        ValidationRules_SO rules = GetValidationRules(); // Obtiene las reglas

        // --- VALIDAR Y CORREGIR ESCALA GLOBAL (Esta sí es correcta y estable) ---
        if (importer.globalScale != rules.requiredModelScale)
        {
            Debug.LogWarning($"[Validation] Model '{importer.assetPath}' has non-standard global scale ({importer.globalScale}). Setting to 1.0.");
            importer.globalScale = 1.0f; // ¡Corrección automática!
        }

        // --- OPTIMIZACIONES GENERALES DE MALLA (Aquí 'optimizeMeshVertices' sí es válido) ---
        // Esta propiedad le dice a Unity que intente optimizar la malla en general.
        // A menudo, esto incluye la reordenación de vértices e índices para mejor caché.
        if (!importer.optimizeMeshVertices)
        {
            Debug.LogWarning($"[Validation] Model '{importer.assetPath}' has 'Optimize Mesh Vertices' disabled. Enabling it for better performance.");
            importer.optimizeMeshVertices = true;
        }

        // --- CONFIGURACIÓN DE UVs DE LIGHTMAP (Esta también es correcta) ---
        // Crucial para la iluminación global de objetos estáticos.
        if (importer.assetPath.Contains("StaticProp") || importer.assetPath.Contains("LevelGeometry"))
        {
            if (!importer.generateSecondaryUV)
            {
                Debug.LogWarning($"[Validation] Model '{importer.assetPath}' is likely static geometry but 'Generate Lightmap UVs' is disabled. Enabling it.");
                importer.generateSecondaryUV = true;
            }
        }

        // --- VALIDACIONES DE ANIMACIÓN ---
        // ¡Esta línea es correcta si 'using UnityEditor;' está presente!
        if (importer.animationType == ModelImporterAnimationType.Human)
        {
            // Aquí puedes añadir validaciones específicas para modelos Humanoid,
            // como si tienen un Avatar configurado, si los nombres de huesos son estándar, etc.
            Debug.Log($"[Validation] Model '{importer.assetPath}' is configured as Humanoid. Ensure Avatar configuration is correct.");
        }
        else if (importer.animationType == ModelImporterAnimationType.Generic)
        {
            // Validaciones para Generic Rigs (ej: criaturas, vehículos)
            Debug.Log($"[Validation] Model '{importer.assetPath}' is configured as Generic. Ensure Generic rig setup is correct.");
        }
        else if (importer.animationType == ModelImporterAnimationType.None)
        {
            // Validaciones para modelos sin animación
            // Si el modelo es un personaje, esto sería un ERROR.
            if (importer.assetPath.Contains("Character") && !importer.assetPath.Contains("Prop"))
            {
                Debug.LogError($"[Validation] Model '{importer.assetPath}' seems to be a character but has Animation Type set to 'None'.");
            }
        }

        // --- Otras propiedades que sí son comunes y estables en ModelImporter ---
        // importer.importMaterials = false; // Si no quieres que Unity cree materiales automáticamente
        importer.meshCompression = ModelImporterMeshCompression.High; // Para modelos con mucha geometría
        // importer.useFileUnits = true; // Si el modelo debe usar las unidades del archivo 3D
        // importer.vertexStreamCompression = true; // Para reducir el tamaño en disco de los vértices
    }

    // --- Validaciones Post-Importación (aquí es donde se inspecciona el resultado) ---
    public void OnPostprocessModel(GameObject root)
    {
        ModelImporter importer = assetImporter as ModelImporter;
        // Validar si se añadió un MeshCollider (esto se hace en OnPostprocessModel)
        MeshCollider meshCollider = root.GetComponentInChildren<MeshCollider>();
        if (meshCollider != null)
        {
            Debug.LogWarning($"[Validation] Model '{importer.assetPath}' has a MeshCollider generated during import. This is often inefficient. Consider disabling 'Generate Colliders' in the Model Importer settings and adding simpler colliders manually.");
            // Si quieres eliminarlo automáticamente (solo si la escena se puede re-importar con seguridad):
            // Esto es delicado: DestroyImmediate(meshCollider, true);
        }

        // Puedes inspeccionar los MeshFilters y sus mallas si necesitas validar vértices/polígonos.
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in meshFilters)
        {
            if (filter.sharedMesh != null)
            {
                // Ejemplo: Validar conteo de triángulos
                if (filter.sharedMesh.triangles.Length / 3 > 100) // 10,000 triángulos, por ejemplo
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
        string assetFileName = Path.GetFileNameWithoutExtension(importer.assetPath); // Nombre del archivo sin extensión
        if (!rules.allowedTextureExtensions.Any(ext => ext == extension))
        {
            Debug.LogError($"[Validation] Texture '{importer.assetPath}' is not in PNG or JPG format. Please use these formats.");
            // Puedes incluso detener la importación lanzando una excepción si es crítico.
            // throw new UnityException($"Invalid texture format for {importer.assetPath}");
        }

        // Validar tamaño de textura (potencias de 2)
        // Aunque Unity las maneja, es buena práctica validar el original.
        // --- VALIDAR TAMAÑO DE TEXTURA (POTENCIAS DE DOS Y TAMAÑO MÁXIMO) ---
        // Para obtener el tamaño de la imagen original antes de que Unity la procese,
        // necesitamos cargarla manualmente. Esto puede ser un poco costoso para MUCHOS assets,
        // pero para validación en el editor es aceptable.
        Texture2D tempTexture = new Texture2D(1, 1); // Crea una textura mínima
        byte[] fileData = File.ReadAllBytes(importer.assetPath); // Lee los bytes del archivo original

        bool loaded = false;
        try
        {
            // Carga los bytes en la textura temporal. Esto funcionará para PNG/JPG.
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

            // Validar Tamaño Máximo
            if (tempTexture.width > rules.maxTextureSize || tempTexture.height > rules.maxTextureSize)
            {
                Debug.LogWarning($"[Validation] Texture '{importer.assetPath}' size ({tempTexture.width}x{tempTexture.height}) exceeds max allowed size ({rules.maxTextureSize}). Consider optimizing.");
            }
        }
        else
        {
            // Esto podría pasar si el archivo no es una imagen válida o un formato que LoadImage soporta.
            Debug.LogWarning($"[Validation] Could not read image dimensions for '{importer.assetPath}'. Skipping size validation.");
        }

        // ¡IMPORTANTE! Limpiar la textura temporal para liberar memoria
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
                break; // Importante: Salir después de aplicar la primera regla que coincida
            }
        }

        if (!typeRuleApplied)
        {
            // Si ninguna regla coincide, puedes establecer un valor por defecto o advertir
            Debug.LogWarning($"[Validation] No specific texture type rule found for '{importer.assetPath}'. Defaulting to 'Default' type and sRGB true.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
        }

        // --- CONFIGURACIÓN DE COMPRESIÓN (Puedes hacer esto configurable también) ---
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.crunchedCompression = true;
        importer.compressionQuality = 50; // Podrías añadir esto al SO
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
                // Considera lanzar una excepción si esto es un error crítico para detener la importación.
                // throw new UnityException($"Invalid audio format for {importer.assetPath}");
            }

            // --- CONFIGURACIÓN DE LAS PROPIEDADES DE IMPORTACIÓN DEL AUDIO ---

            // Unity usa 'AudioImporterSampleSettings' para agrupar estas propiedades.
            // Puedes obtener las configuraciones por defecto o un override si ya existe.

            // Opción 1: Modificar las configuraciones por defecto (afecta a todas las plataformas)
            // Puedes acceder directamente a 'importer.defaultSampleSettings'
            // y luego llamar a 'importer.SetCustomSamplerSettings(platform, settings)' si quieres overrides
            // o simplemente asignar a defaultSampleSettings y luego re-importar.

            // Opción 2: Crear un nuevo AudioImporterSampleSettings y aplicarlo
            // Esta es la forma más común para configurar plataformas específicas o la configuración por defecto.

            AudioImporterSampleSettings settings = importer.defaultSampleSettings; // O settings = new AudioImporterSampleSettings();

            // Configurar tipo de carga (Streaming para música, DecompressOnLoad para SFX)
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

            // Configurar el formato de compresión y calidad
            settings.compressionFormat = AudioCompressionFormat.Vorbis; // O PCM, ADPCM
            settings.quality = 0.5f; // Calidad de compresión (0.0f a 1.0f)
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; // O OptimizeSampleRate, etc.

            // Aplica los settings.
            // Si no estás usando overrides por plataforma, simplemente asigna a defaultSampleSettings
            importer.defaultSampleSettings = settings;

            // Si quieres aplicar settings específicos para una plataforma (ej: "Android", "Standalone"):
            // AudioImporterSampleSettings androidSettings = new AudioImporterSampleSettings();
            // androidSettings.compressionFormat = AudioCompressionFormat.ADPCM;
            // importer.SetOverrideSampleSettings("Android", androidSettings);

            // Si el assetPath contiene "_SFX" y quieres una compresión más ligera para SFX:
            if (importer.assetPath.Contains("_SFX") || importer.assetPath.Contains("SFX_"))
            {
                settings.compressionFormat = AudioCompressionFormat.ADPCM; // Muy bueno para SFX cortos
                settings.quality = 0.8f; // ADPCM es fijo, pero el 'quality' puede influir en otros formatos
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            }
            else if (importer.assetPath.Contains("_Music") || importer.assetPath.Contains("Music_"))
            {
                settings.compressionFormat = AudioCompressionFormat.Vorbis; // Excelente para música
                settings.quality = 0.5f; // Calidad media
                settings.loadType = AudioClipLoadType.Streaming;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            }

            // Vuelve a aplicar la configuración (importante después de cualquier modificación)
            importer.defaultSampleSettings = settings;
        
    }

    // --- Métodos Post-Importación (cuando ya se han cargado los datos) ---

    // Se llama después de que un asset ha sido importado.
    // Aquí puedes acceder a los datos ya importados del asset.
    public void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".unity")) // Si es una escena
            {
                Debug.Log($"[Validation] Scene imported: {assetPath}. Consider running scene-specific validations.");
                // Podrías cargar la escena temporalmente y validar GameObjects
                // Esto es más complejo y generalmente se hace con una ventana de editor separada.
            }

            // Validaciones más complejas que requieren que el asset ya esté en memoria
            // Ejemplo: revisar si un prefab tiene ciertos componentes o layers
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (go != null && PrefabUtility.IsPartOfPrefabAsset(go))
            {
                // Validar prefabs:
                // - ¿Tiene los colliders necesarios?
                // - ¿Tiene un script base de Character si es un personaje?
                // - ¿Está en la capa correcta?
                // if (go.layer != LayerMask.NameToLayer("Characters")) { ... }
                // if (go.GetComponent<Rigidbody>() == null) { ... }
            }
        }
    }
}
