using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Para usar LINQ

public class TextureAuditorEditorWindow : EditorWindow
{
    private AuditReport currentReport = new AuditReport();
    private Vector2 scrollPosition;
    private bool showHelp = true;

    // Men� para abrir la ventana
    [MenuItem("Tools/GameDev/Texture & Material Auditor")]
    public static void ShowWindow()
    {
        GetWindow<TextureAuditorEditorWindow>("Texture & Material Auditor").Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Texture & Material Audit", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Detecta texturas que pueden ser innecesarias o no �ptimas (ej. mapas met�licos negros, AO blancos).", MessageType.Info);
        showHelp = EditorGUILayout.Foldout(showHelp, "Ayuda y Recomendaciones");
        if (showHelp)
        {
            EditorGUILayout.LabelField("C�mo interpretar:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Errores: Problemas cr�ticos que deber�an ser corregidos.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("- Advertencias: Problemas que impactan la optimizaci�n y deber�an ser revisados.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("- Informaci�n: Detalles �tiles sobre assets.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Acciones sugeridas:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- 'Clean Map': Eliminar la textura del material (si es uniforme).", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("- 'Go to Material': Seleccionar el material en el Project Window.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("- 'Select Texture': Seleccionar la textura en el Project Window.", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Bot�n para ejecutar la auditor�a
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

        // Bot�n para limpiar todos los problemas "limpiables" (opcional y con precauci�n)
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
        // La l�gica del auditor es la misma que para el batch mode, pero puede ser m�s verbosa en los logs.
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
            // Bot�n para seleccionar el material en el Project Window
            if (GUILayout.Button("Go to Material", GUILayout.Width(120)))
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(issue.AssetPath);
                if (mat != null) Selection.activeObject = mat;
                EditorGUIUtility.PingObject(mat);
            }

            // Si el problema est� relacionado con una textura, a�adir bot�n para seleccionarla
            Texture tex = GetTextureFromIssuePath(issue.AssetPath, issue.Message); // Necesitar�amos una forma de extraer la textura del mensaje o del material
            if (tex != null)
            {
                if (GUILayout.Button("Select Texture", GUILayout.Width(120)))
                {
                    Selection.activeObject = tex;
                    EditorGUIUtility.PingObject(tex);
                }
            }

            // Bot�n para intentar limpiar el problema (ej. remover Metallic Map)
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

    // M�todo para intentar arreglar el problema espec�fico
    private void AttemptFixIssue(AuditIssue issue)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(issue.AssetPath);
        if (material == null) return;

        // L�gica de fix basada en el mensaje o tipo de problema
        if (issue.Message.Contains("Metallic map") && issue.SuggestedFix.Contains("remove"))
        {
            Undo.RecordObject(material, "Clean Metallic Map"); // Para poder deshacer
            material.SetTexture("_MetallicGlossMap", null);
            material.SetFloat("_Metallic", 0f); // Asegurar el valor en 0
            EditorUtility.SetDirty(material); // Marcar el material como modificado
            AssetDatabase.SaveAssets();
            Debug.Log($"Cleaned metallic map for {material.name}");
        }
        // A�adir m�s l�gica para otros tipos de fix: AO, Roughness, etc.
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
        // �Importante! Aqu� deber�as expandir la l�gica para cada tipo de textura y sugerencia.
    }

    // Intento de autocorrecci�n para todos los problemas 'limpiables'
    private void AutoCleanIssues()
    {
        foreach (var issue in currentReport.Issues.ToList()) // Usar ToList() para evitar modificar la colecci�n mientras se itera
        {
            // Solo intentar arreglar si la sugerencia es una acci�n "limpiable" directa
            if (issue.SuggestedFix.Contains("remove") || issue.SuggestedFix.Contains("adjust value"))
            {
                AttemptFixIssue(issue);
            }
        }
        RunAudit(); // Re-ejecutar para confirmar los cambios
    }

    // Helper para obtener la textura del mensaje de la issue (simplificado, necesitar�a un parsing m�s robusto)
    private Texture GetTextureFromIssuePath(string materialPath, string message)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null) return null;

        // Aqu� se necesitar�a una l�gica m�s inteligente. Podr�as extraer el nombre de la textura del mensaje,
        // o buscar las texturas m�s comunes en el material por sus propiedades.
        if (message.Contains("Metallic map")) return mat.GetTexture("_MetallicGlossMap");
        if (message.Contains("AO map")) return mat.GetTexture("_OcclusionMap");
        // ... a�adir m�s casos
        return null;
    }


}

// Las clases AuditReport, AuditIssue, AuditIssueSeverity, TextureAuditor (con sus m�todos de an�lisis)
// ser�an las mismas que las definidas para el Batch Mode, pero con la l�gica de IsTextureUniform
// m�s optimizada para el editor (evitando PixelData si es posible, usando metadatos).
// Si necesitas una detecci�n de p�xeles, considera usar GetPixels de una textura peque�a (ej. 16x16)
// para una estimaci�n r�pida y menos costosa.