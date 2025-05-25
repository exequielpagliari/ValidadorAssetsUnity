using UnityEditor;
using UnityEngine;

// Este script DEBE estar en una carpeta 'Editor'
public class MyEditorToolWindow : EditorWindow
{
    // Esta es tu clase no estática, que ahora es una instancia de tu EditorWindow
    // O podrías tener una referencia a otra clase no estática que tú instancies aquí.
    // Aquí, MyEditorToolWindow *es* la ModelAssetDataEditor "lógica".

    // Una variable de instancia para almacenar datos específicos de esta ventana
    private string _message = "¡Hola desde la instancia de la ventana!";

    [MenuItem("Tools/My Non-Static Editor Tool")]
    public static void ShowWindow()
    {
        // Al mostrar la ventana, obtenemos una instancia de MyEditorToolWindow
        MyEditorToolWindow window = GetWindow<MyEditorToolWindow>("My Editor Tool");
        window.minSize = new Vector2(300, 200);
        window.Show();
    }

    // Se llama cuando la ventana está habilitada (abierta o cargada)
    private void OnEnable()
    {
        Debug.Log("Editor Window Enabled. ¡La instancia de MyEditorToolWindow está viva!");
        // Aquí puedes realizar inicializaciones específicas de esta instancia.
        // Si DrawIconsOnRun es para el editor, aquí lo llamarías.
        DrawIconsOnRun();

        // Puedes suscribirte a eventos del editor para ejecutar lógica cuando ocurran ciertos eventos
        EditorApplication.update += UpdateIconsInEditor; // Se llama cada frame del editor
        SceneView.duringSceneGui += OnSceneGUI; // Se llama durante la renderización de la SceneView
    }

    // Se llama cuando la ventana está deshabilitada (cerrada o Unity recompila)
    private void OnDisable()
    {
        Debug.Log("Editor Window Disabled. ¡La instancia de MyEditorToolWindow está muriendo!");
        EditorApplication.update -= UpdateIconsInEditor;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // Tu método no estático que ahora opera sobre esta instancia
    public void DrawIconsOnRun()
    {
        Debug.Log($"Método DrawIconsOnRun() ejecutado en la instancia de la ventana. Mensaje: {_message}");
        // Aquí puedes realizar la lógica de dibujo o procesamiento
        // Si necesitas dibujar en la vista de escena, lo harías en OnSceneGUI
        
    }

    // Este método se llama cada frame del editor
    private void UpdateIconsInEditor()
    {
        // Debug.Log("Editor Update called."); // ¡Cuidado con los logs en Update, llenan la consola!
        // Si necesitas actualizar el estado para el dibujo, lo harías aquí.
        // Forzar un repaint de la SceneView si los iconos cambian dinámicamente
        SceneView.RepaintAll();
        
    }

    private void SceneView_beforeSceneGui(SceneView obj)
    {
        throw new System.NotImplementedException();
    }

    // Aquí es donde dibujarías los gizmos o handles en la vista de escena,
    // utilizando la instancia de tu EditorWindow.
    // Esto se llama CADA VEZ que la vista de escena se repinta.
    private void OnSceneGUI(SceneView sceneView)
    {
        // Puedes dibujar gizmos o handles aquí usando UnityEditor.Handles
        // Por ejemplo, dibujar una esfera en el centro del mundo
        Handles.color = Color.blue;
        Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, 0.5f, EventType.Repaint);
        Handles.Label(Vector3.zero + Vector3.up, "Central Point from MyEditorToolWindow");

        // Si tu ModelAssetDataEditor tiene datos que quieres visualizar,
        // accederías a ellos desde aquí, ya que OnSceneGUI es un método de instancia
        // y tiene acceso a todas las variables de tu MyEditorToolWindow.
        Handles.Label(new Vector3(1, 1, 1), _message); // Ejemplo de acceso a variable de instancia
    }

    // Este es el método que dibuja la interfaz de usuario de la ventana
    private void OnGUI()
    {
        GUILayout.Label("Editor Window", EditorStyles.boldLabel);
        _message = EditorGUILayout.TextField("Window Message", _message);

        if (GUILayout.Button("Execute DrawIconsOnRun"))
        {
            DrawIconsOnRun(); // Llamando al método no estático de esta instancia
        }
    }
}