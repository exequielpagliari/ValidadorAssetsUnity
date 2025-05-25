using UnityEditor;
using UnityEngine;
using System.Linq; // Para usar .Any()
using System.IO;
using System.Collections.Generic;
using static UnityEditor.U2D.ScriptablePacker;
using System;
using Unity.VisualScripting;

public static class BatchModeAuditor
{
    // Este m�todo ser� llamado por Unity desde la l�nea de comandos
    // Ejemplo: Unity.exe -batchmode -nographics -projectPath "C:/MyUnityProject" -executeMethod BatchModeAuditor.RunAutomatedAudit
    public static void RunAutomatedAudit()
    {
        Debug.Log("Starting automated texture audit...");

        // Paso 1: Ejecutar la l�gica de auditor�a
        AuditReport report = TextureAuditor.RunFullTextureAudit();

        // Paso 2: Guardar el reporte en un archivo
        string reportPath = Path.Combine(Application.dataPath, "../", "AuditReport.json"); // Guarda fuera de Assets
        File.WriteAllText(reportPath, report.ToJson());
        Debug.Log($"Audit report saved to: {reportPath}");

        // Paso 3: Salir con un c�digo de error si hay problemas cr�ticos
        if (report.HasErrors)
        {
            Debug.LogError("Automated audit finished with ERRORS!");
            EditorApplication.Exit(1); // Salir con c�digo de error para CI/CD
        }
        else if (report.HasWarnings)
        {
            Debug.LogWarning("Automated audit finished with WARNINGS.");
            EditorApplication.Exit(0); // Salir con �xito, pero con advertencias
        }
        else
        {
            Debug.Log("Automated audit finished successfully. No issues found.");
            EditorApplication.Exit(0); // Salir con �xito
        }
    }
}

// --- L�gica de TextureAuditor (simplificada para el ejemplo) ---
public static class TextureAuditor
{
    public static AuditReport RunFullTextureAudit()
    {
        AuditReport report = new AuditReport();
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null) continue;

            AnalyzeMaterial(material, report);
        }
        return report;
    }

    private static void AnalyzeMaterial(Material material, AuditReport report)
    {
        string materialPath = AssetDatabase.GetAssetPath(material);

        // _Metallic
        if (material.HasProperty("_MetallicGlossMap"))
        {
            Texture texture = material.GetTexture("_MetallicGlossMap");
            if (texture != null)
            {
                // En un escenario real, necesitar�as una l�gica para verificar
                // si la textura es "uniformemente negra" o "uniformemente blanca"
                // Esto es complejo sin cargar la textura.
                // Para la automatizaci�n, una buena aproximaci�n es el tama�o en disco
                // o la configuraci�n de importaci�n.
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (importer != null)
                {
                    // Ejemplo simplificado: Si es 2k y comprimida a un tama�o rid�culamente peque�o (ej. < 10KB)
                    // y su formato indica que es para un mapa met�lico, podr�a ser sospechosa.
                    // La l�gica real ser�a m�s avanzada.
                    long textureSize = GetTextureFileSize(AssetDatabase.GetAssetPath(texture)); // Implementar esta funci�n
                    string extension = GetTextureExtension(AssetDatabase.GetAssetPath(texture)).ToLower();
                    if (extension == "png")
                    {
                            if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 60 * 1024) // 10KB
                            {
                                report.AddIssue(materialPath,
                                                $"Metallic map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                                AuditIssueSeverity.Warning,
                                                "Consider removing metallic map if material is non-metallic, or replacing with a 1x1 black texture.");
                            }
                    }
                    else if (extension == ".jpg" || extension == ".jpeg")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 60 * 1024) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"Metallic map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Consider removing metallic map if material is non-metallic, or replacing with a 1x1 black texture.");
                        }
                    }
                    else if (extension == ".bmp")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 60 * 1024) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"Metallic map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Consider removing metallic map if material is non-metallic, or replacing with a 1x1 black texture.");
                        }
                    }

                }
            }
        }

        // Repetir para _Albedo, _Roughness, _AO, _Mask, _UI con l�gica espec�fica para cada una.
        // Para _Albedo/_Diffuse, el chequeo de "uniformidad" es m�s dif�cil sin cargar la textura.
        // Podr�as buscar si son texturas de "placeholder" comunes o de un solo color.
        // _Albedo
        if (material.HasProperty("_MainTex"))
        {
            Texture texture = material.GetTexture("_MainTex");

            if (texture != null)
            {
                // En un escenario real, necesitar�as una l�gica para verificar
                // si la textura es "uniformemente negra" o "uniformemente blanca"
                // Esto es complejo sin cargar la textura.
                // Para la automatizaci�n, una buena aproximaci�n es el tama�o en disco
                // o la configuraci�n de importaci�n.
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (importer != null)
                {
                    // Ejemplo simplificado: Si es 2k y comprimida a un tama�o rid�culamente peque�o (ej. < 10KB)
                    // y su formato indica que es para un mapa met�lico, podr�a ser sospechosa.
                    // La l�gica real ser�a m�s avanzada.
                    long textureSize = GetTextureFileSize(AssetDatabase.GetAssetPath(texture)); // Implementar esta funci�n
                    string extension = GetTextureExtension(AssetDatabase.GetAssetPath(texture)).ToLower();
                    if (extension == "png")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 20 * 1024) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"Albedo map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Revisar la textura Albedo.");
                        }
                    }
                    else if (extension == ".jpg" || extension == ".jpeg")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 40 * 1024) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"Albedo map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Revisar la textura Albedo.");
                        }
                    }
                    else if (extension == ".bmp")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 16 * 1024000) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"Albeto map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Revisar la textura Albedo.");
                        }
                    }
                }
            }
        }

        // _Roughness
        if ( material.HasProperty("_RoughnessMap"))
        {
            Texture texture = material.GetTexture("_RoughnessMap");
            

            if (texture != null)
            {
                // En un escenario real, necesitar�as una l�gica para verificar
                // si la textura es "uniformemente negra" o "uniformemente blanca"
                // Esto es complejo sin cargar la textura.
                // Para la automatizaci�n, una buena aproximaci�n es el tama�o en disco
                // o la configuraci�n de importaci�n.
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (importer != null)
                {
                    // Ejemplo simplificado: Si es 2k y comprimida a un tama�o rid�culamente peque�o (ej. < 10KB)
                    // y su formato indica que es para un mapa met�lico, podr�a ser sospechosa.
                    // La l�gica real ser�a m�s avanzada.
                    long textureSize = GetTextureFileSize(AssetDatabase.GetAssetPath(texture)); // Implementar esta funci�n
                    string extension = GetTextureExtension(AssetDatabase.GetAssetPath(texture)).ToLower();
                    if (extension == "png")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 20 * 1024) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"Roughness map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Revisar Roughness map");
                        }
                    }
                    else if (extension == ".jpg" || extension == ".jpeg")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 40 * 1024) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"Roughness map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Revisar Roughness map");
                        }
                    }
                    else if (extension == ".bmp")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 16 * 1024000) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"Roughness map '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Revisar Roughness map");
                        }
                    }
                }
            }
        }

        // _OcclusionMap
        if (material.HasProperty("_OcclusionMap"))
        {
            Texture texture = material.GetTexture("_OcclusionMap");
            if (texture != null)
            {
                // En un escenario real, necesitar�as una l�gica para verificar
                // si la textura es "uniformemente negra" o "uniformemente blanca"
                // Esto es complejo sin cargar la textura.
                // Para la automatizaci�n, una buena aproximaci�n es el tama�o en disco
                // o la configuraci�n de importaci�n.
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (importer != null)
                {
                    // Ejemplo simplificado: Si es 2k y comprimida a un tama�o rid�culamente peque�o (ej. < 10KB)
                    // y su formato indica que es para un mapa met�lico, podr�a ser sospechosa.
                    // La l�gica real ser�a m�s avanzada.
                    long textureSize = GetTextureFileSize(AssetDatabase.GetAssetPath(texture)); // Implementar esta funci�n
                    string extension = GetTextureExtension(AssetDatabase.GetAssetPath(texture)).ToLower();
                    if (extension == "png")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 20 * 1024) // 10KB
                        {
                            report.AddIssue(materialPath,
                                            $"OcclusionMap '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                            AuditIssueSeverity.Warning,
                                            "Revisar OcclusionMap");
                        }
                    }
                    else if (extension == ".jpg" || extension == ".jpeg")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 40 * 1024) // 10KB
                        {
                            report.AddIssue(materialPath,
                                $"OcclusionMap '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                AuditIssueSeverity.Warning,
                                "Revisar OcclusionMap");
                        }
                    }
                    else if (extension == ".bmp")
                    {
                        if (texture.width >= 1024 && texture.height >= 1024 && textureSize < 16 * 1024000) // 10KB
                        {
                            report.AddIssue(materialPath,
                                $"OcclusionMap '{texture.name}' ({texture.width}x{texture.height}) is suspiciously small ({textureSize / 1024}KB) for its resolution. Likely uniform/empty.",
                                AuditIssueSeverity.Warning,
                                "Revisar OcclusionMap");
                        }
                    }
                }
            }
        }
    }

    // Simulaci�n de obtener el tama�o de archivo de la textura
    private static long GetTextureFileSize(string texturePath)
    {
        if (File.Exists(texturePath))
        {
            return new FileInfo(texturePath).Length;
        }
        return 0;
    }

    private static string GetTextureExtension(string texturePath)
    {
        if (File.Exists(texturePath))
        {
            return new FileInfo(texturePath).Extension;
        }
        return null;
    }



    [MenuItem("Assets/GameDev/Audit Material")]
    public static void AuditSelectedMaterial()
    {
        Material selectedMaterial = Selection.activeObject as Material;
        if (selectedMaterial != null)
        {
            AuditReport report = new AuditReport();
            TextureAuditor.AnalyzeMaterial(selectedMaterial, report); // Necesitas que AnalyzeMaterial sea p�blico o adaptar
                                                                      // Mostrar los resultados en una ventana temporal o en la ventana principal si ya est� abierta
            Debug.Log($"Audit for {selectedMaterial.name}: {report.Issues.Count} issues found.");
            // Podr�as abrir la ventana principal y cargar este reporte si est� dise�ada para ello.
        }
    }
}

public class AuditReport
{
    public List<AuditIssue> Issues { get; private set; }
    public bool HasErrors => Issues.Any(i => i.Severity == AuditIssueSeverity.Error);
    public bool HasWarnings => Issues.Any(i => i.Severity == AuditIssueSeverity.Warning);

    public AuditReport() { Issues = new List<AuditIssue>(); }

    public void AddIssue(string assetPath, string message, AuditIssueSeverity severity, string suggestedFix)
    {
        Issues.Add(new AuditIssue(assetPath, message, severity, suggestedFix));
    }

    public string ToJson()
    {
        // Implementar serializaci�n a JSON (ej. usando JsonUtility o Newtonsoft.Json)
        // Para el ejemplo, una string simple:
        return "{\"issues\": [" + string.Join(",", Issues.Select(i => i.ToJson())) + "]}";
    }
}

public class AuditIssue
{
    public string AssetPath { get; private set; }
    public string Message { get; private set; }
    public AuditIssueSeverity Severity { get; private set; }
    public string SuggestedFix { get; private set; }

    public AuditIssue(string assetPath, string message, AuditIssueSeverity severity, string suggestedFix)
    {
        AssetPath = assetPath;
        Message = message;
        Severity = severity;
        SuggestedFix = suggestedFix;
    }

    public string ToJson()
    {
        // Implementar serializaci�n a JSON
        return $"{{ \"assetPath\": \"{AssetPath}\", \"message\": \"{Message}\", \"severity\": \"{Severity}\", \"suggestedFix\": \"{SuggestedFix}\" }}";
    }
}

public enum AuditIssueSeverity { Info, Warning, Error }