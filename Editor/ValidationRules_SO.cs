using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[CreateAssetMenu(fileName = "ValidationRules", menuName = "Validation/Validation Rules")]
public class ValidationRules_SO : ScriptableObject
{
    [Header("General Rules")]
    public List<string> allowedTextureExtensions = new List<string> { ".png", ".jpg", ".jpeg" };
    public List<string> allowedAudioExtensions = new List<string> { ".wav", ".ogg" };

    [Header("Texture Type Rules")] // Nueva sección
    public List<TextureTypeRule> textureTypeRules;
    public bool enforcePowerOfTwoTextureSizes = true;
    public int maxTextureSize = 2048; // Global max

    [System.Serializable] // Importante para que Unity lo muestre en el Inspector
    public class TextureTypeRule
    {
        public string nameContains; // Ej: "_Normal", "_Albedo", "_Mask"
        public TextureImporterType textureType; // El tipo de textura a asignar
        public bool sRGB = true; // Si es un mapa de color (sRGB) o no (linear)
    }


    [Header("Model Rules")]
    public float requiredModelScale = 1.0f;
    public bool allowGenerateColliders = false; // Por defecto no
    public int maxTrianglesForProp = 5000;
    public int maxVerticesForCharacter = 10000;
    public string requiredCharacterLayer = "Characters";
    // ... más reglas, quizás por prefijo de asset o carpeta

    [Header("Naming Conventions")]
    public List<NamingConventionRule> namingRules; // Lista de reglas de naming

    // Estructura para reglas de nomenclatura
    [System.Serializable]
    public class NamingConventionRule
    {
        public string folderPath; // Ej: "Assets/Art/Characters"
        public string requiredPrefix; // Ej: "CH_"
        public string requiredSuffix; // Ej: "_Mesh"
        public bool appliesToFolders = false; // ¿Aplica a carpetas o archivos?
    }


}