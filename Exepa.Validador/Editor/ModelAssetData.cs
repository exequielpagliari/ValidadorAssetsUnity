using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "NewModelData", menuName = "Game Assets/Model Asset Data")]
public class ModelAssetData : ScriptableObject
{
    [Header("Core Model Information")]
    [Tooltip("Referencia al GameObject o Prefab del modelo 3D.")]
    public GameObject ModelPrefab;

    [Tooltip("Lista de materiales asociados a este modelo.")]
    public List<Material> Materials = new List<Material>();

    [Header("Icon Settings")]
    [Tooltip("Sprite opcional para usar como �cono del ScriptableObject. Si se asigna, tendr� prioridad sobre la vista previa del modelo 3D.")]
    public Sprite CustomIconSprite;

    [Header("Material Visual Reference")]
    [Tooltip("Textura principal que representa visualmente este modelo (ej. Albedo).")]
    public Texture2D MainAlbedoTexture; // �Nueva propiedad para la textura principal!

    [Header("Metadata for Audit and Management")]
    [Tooltip("ID �nico para este modelo (ej. UUID). Generado autom�ticamente si es nulo.")]
    public string ModelID = Guid.NewGuid().ToString();

    [Tooltip("Nombre legible del modelo.")]
    public string ModelName = "New Model";

    [TextArea(3, 5)]
    [Tooltip("Descripci�n detallada del modelo y sus usos.")]
    public string Description = "Una breve descripci�n del modelo y sus caracter�sticas.";

    [Tooltip("Estado de auditor�a del modelo. 'True' si ha sido revisado y aprobado.")]
    public bool IsAudited = false;

    [Tooltip("Fecha de la �ltima modificaci�n de este ScriptableObject.")]
    public string LastModifiedDate;

    [Tooltip("Nombre del artista o equipo responsable de este modelo.")]
    public string Author = "Unknown";

    [Tooltip("Etiquetas para facilitar la b�squeda y categorizaci�n.")]
    public List<string> Tags = new List<string>();

    [Tooltip("Notas internas o comentarios sobre el modelo.")]
    [TextArea(2, 4)]
    public string InternalNotes;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(ModelID) || ModelID == Guid.Empty.ToString())
        {
            ModelID = Guid.NewGuid().ToString();
        }
        LastModifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void RefreshModifiedDate()
    {
        LastModifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}