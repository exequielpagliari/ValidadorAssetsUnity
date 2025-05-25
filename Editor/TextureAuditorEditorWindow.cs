using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Para usar LINQ

public class TextureAuditorEditorWindow : EditorWindow
{
    private AuditReport currentReport = new AuditReport();
    private Vector2 scrollPosition;
    private bool showHelp = true;

    // Menú para abrir la ventana
    [MenuItem("Tools/GameDev/Texture & Material Auditor")]
    public static void ShowWindow()
    {
        GetWindow<TextureAuditorEditorWindow>("Texture & Material Auditor").Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Texture & Material Audit", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Detecta texturas que pueden ser innecesarias o no óptimas (ej. mapas metálicos negros, AO blancos).", MessageType.Info);
        showHelp = EditorGUILayout.Foldout(showHelp, "Ayuda y Recomendaciones");
        if (showHelp)
        {
            EditorGUILayout.LabelField("Cómo interpretar:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Errores: Problemas críticos que deberían ser corregidos.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("- Advertencias: Problemas que impactan la optimización y deberían ser revisados.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("- Información: Detalles útiles sobre assets.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Acciones sugeridas:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- 'Clean Map': Eliminar la textura del material (si es uniforme).", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("- 'Go to Material': Seleccionar el material en el Project Window.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("- 'Select Texture': Seleccionar la textura en el Project Window.", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Botón para ejecutar la auditoría
        if (GUILayout.Button("Run Full Project Audit", GUILayout.Height(30)))
        {
            RunAudit();
        }

        EditorGUILayout.Space();

        // Mostrar el resumen del reporte
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Audit Summary: {currentReport.Issues.Count} issues found.", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Errors: {currentReport.Issues.Count(i => i.Severity == AuditIssueSeverity.Error)}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Warnings: {currentReport.Issues.Count(i => i.Severity == AuditIssueSeverity.Warning)}", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Mostrar los resultados detallados en una scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true));
        DisplayAuditIssues();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Botón para limpiar todos los problemas "limpiables" (opcional y con precaución)
        if (GUILayout.Button("Attempt Auto-Clean All Issues", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirm Auto-Clean", "Are you sure you want to attempt to auto-clean all detectable issues? This action cannot be undone easily.", "Yes, Clean", "Cancel"))
            {
                AutoCleanIssues();
            }
        }
    }

    private void RunAudit()
    {
        Debug.Log("Starting editor texture audit...");
        // La lógica del auditor es la misma que para el batch mode, pero puede ser más verbosa en los logs.
        currentReport = TextureAuditor.RunFullTextureAudit();
        Repaint(); // Forzar el repintado de la ventana
        Debug.Log("Editor texture audit complete.");
    }

    private void DisplayAuditIssues()
    {
        if (currentReport == null || currentReport.Issues.Count == 0)
        {
            EditorGUILayout.LabelField("No issues found in the last audit or audit not run yet.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        foreach (var issue in currentReport.Issues.OrderBy(i => (int)i.Severity).ThenBy(i => i.AssetPath))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Severity: {issue.Severity}", GetSeverityStyle(issue.Severity));
            EditorGUILayout.LabelField($"Asset: {issue.AssetPath}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Issue: {issue.Message}");
            EditorGUILayout.LabelField($"Suggested Fix: {issue.SuggestedFix}");

            EditorGUILayout.BeginHorizontal();
            // Botón para seleccionar el material en el Project Window
            if (GUILayout.Button("Go to Material", GUILayout.Width(120)))
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(issue.AssetPath);
                if (mat != null) Selection.activeObject = mat;
                EditorGUIUtility.PingObject(mat);
            }

            // Si el problema está relacionado con una textura, añadir botón para seleccionarla
            Texture tex = GetTextureFromIssuePath(issue.AssetPath, issue.Message); // Necesitaríamos una forma de extraer la textura del mensaje o del material
            if (tex != null)
            {
                if (GUILayout.Button("Select Texture", GUILayout.Width(120)))
                {
                    Selection.activeObject = tex;
                    EditorGUIUtility.PingObject(tex);
                }
            }

            // Botón para intentar limpiar el problema (ej. remover Metallic Map)
            if (issue.SuggestedFix.Contains("remove") || issue.SuggestedFix.Contains("adjust value"))
            {
                if (GUILayout.Button("Clean Map", GUILayout.Width(100)))
                {
                    AttemptFixIssue(issue);
                    RunAudit(); // Re-ejecutar para ver el cambio
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }

    private GUIStyle GetSeverityStyle(AuditIssueSeverity severity)
    {
        switch (severity)
        {
            case AuditIssueSeverity.Error: return new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.red } };
            case AuditIssueSeverity.Warning: return new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(1.0f, 0.64f, 0.0f) } }; // Orange
            case AuditIssueSeverity.Info: return new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.blue } };
            default: return EditorStyles.label;
        }
    }

    // Método para intentar arreglar el problema específico
    private void AttemptFixIssue(AuditIssue issue)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(issue.AssetPath);
        if (material == null) return;

        // Lógica de fix basada en el mensaje o tipo de problema
        if (issue.Message.Contains("Metallic map") && issue.SuggestedFix.Contains("remove"))
        {
            Undo.RecordObject(material, "Clean Metallic Map"); // Para poder deshacer
            material.SetTexture("_MetallicGlossMap", null);
            material.SetFloat("_Metallic", 0f); // Asegurar el valor en 0
            EditorUtility.SetDirty(material); // Marcar el material como modificado
            AssetDatabase.SaveAssets();
            Debug.Log($"Cleaned metallic map for {material.name}");
        }
        // Añadir más lógica para otros tipos de fix: AO, Roughness, etc.
        // Por ejemplo, para AO:
        else if (issue.Message.Contains("AO map") && issue.SuggestedFix.Contains("remove"))
        {
            Undo.RecordObject(material, "Clean AO Map");
            material.SetTexture("_OcclusionMap", null);
            material.SetFloat("_OcclusionStrength", 1f); // Fuerza de AO a 1 (ninguna)
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log($"Cleaned AO map for {material.name}");
        }
        // ¡Importante! Aquí deberías expandir la lógica para cada tipo de textura y sugerencia.
    }

    // Intento de autocorrección para todos los problemas 'limpiables'
    private void AutoCleanIssues()
    {
        foreach (var issue in currentReport.Issues.ToList()) // Usar ToList() para evitar modificar la colección mientras se itera
        {
            // Solo intentar arreglar si la sugerencia es una acción "limpiable" directa
            if (issue.SuggestedFix.Contains("remove") || issue.SuggestedFix.Contains("adjust value"))
            {
                AttemptFixIssue(issue);
            }
        }
        RunAudit(); // Re-ejecutar para confirmar los cambios
    }

    // Helper para obtener la textura del mensaje de la issue (simplificado, necesitaría un parsing más robusto)
    private Texture GetTextureFromIssuePath(string materialPath, string message)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null) return null;

        // Aquí se necesitaría una lógica más inteligente. Podrías extraer el nombre de la textura del mensaje,
        // o buscar las texturas más comunes en el material por sus propiedades.
        if (message.Contains("Metallic map")) return mat.GetTexture("_MetallicGlossMap");
        if (message.Contains("AO map")) return mat.GetTexture("_OcclusionMap");
        // ... añadir más casos
        return null;
    }


}

// Las clases AuditReport, AuditIssue, AuditIssueSeverity, TextureAuditor (con sus métodos de análisis)
// serían las mismas que las definidas para el Batch Mode, pero con la lógica de IsTextureUniform
// más optimizada para el editor (evitando PixelData si es posible, usando metadatos).
// Si necesitas una detección de píxeles, considera usar GetPixels de una textura pequeña (ej. 16x16)
// para una estimación rápida y menos costosa.