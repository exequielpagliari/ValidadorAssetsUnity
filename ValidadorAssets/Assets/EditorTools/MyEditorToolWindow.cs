using UnityEditor;
using UnityEngine;

// Este script DEBE estar en una carpeta 'Editor'
public class MyEditorToolWindow : EditorWindow
{
    // Esta es tu clase no est�tica, que ahora es una instancia de tu EditorWindow
    // O podr�as tener una referencia a otra clase no est�tica que t� instancies aqu�.
    // Aqu�, MyEditorToolWindow *es* la ModelAssetDataEditor "l�gica".

    // Una variable de instancia para almacenar datos espec�ficos de esta ventana
    private string _message = "�Hola desde la instancia de la ventana!";

    [MenuItem("Tools/My Non-Static Editor Tool")]
    public static void ShowWindow()
    {
        // Al mostrar la ventana, obtenemos una instancia de MyEditorToolWindow
        MyEditorToolWindow window = GetWindow<MyEditorToolWindow>("My Editor Tool");
        window.minSize = new Vector2(300, 200);
        window.Show();
    }

    // Se llama cuando la ventana est� habilitada (abierta o cargada)
    private void OnEnable()
    {
        Debug.Log("Editor Window Enabled. �La instancia de MyEditorToolWindow est� viva!");
        // Aqu� puedes realizar inicializaciones espec�ficas de esta instancia.
        // Si DrawIconsOnRun es para el editor, aqu� lo llamar�as.
        DrawIconsOnRun();

        // Puedes suscribirte a eventos del editor para ejecutar l�gica cuando ocurran ciertos eventos
        EditorApplication.update += UpdateIconsInEditor; // Se llama cada frame del editor
        SceneView.duringSceneGui += OnSceneGUI; // Se llama durante la renderizaci�n de la SceneView
    }

    // Se llama cuando la ventana est� deshabilitada (cerrada o Unity recompila)
    private void OnDisable()
    {
        Debug.Log("Editor Window Disabled. �La instancia de MyEditorToolWindow est� muriendo!");
        EditorApplication.update -= UpdateIconsInEditor;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // Tu m�todo no est�tico que ahora opera sobre esta instancia
    public void DrawIconsOnRun()
    {
        Debug.Log($"M�todo DrawIconsOnRun() ejecutado en la instancia de la ventana. Mensaje: {_message}");
        // Aqu� puedes realizar la l�gica de dibujo o procesamiento
        // Si necesitas dibujar en la vista de escena, lo har�as en OnSceneGUI
        
    }

    // Este m�todo se llama cada frame del editor
    private void UpdateIconsInEditor()
    {
        // Debug.Log("Editor Update called."); // �Cuidado con los logs en Update, llenan la consola!
        // Si necesitas actualizar el estado para el dibujo, lo har�as aqu�.
        // Forzar un repaint de la SceneView si los iconos cambian din�micamente
        SceneView.RepaintAll();
        
    }

    private void SceneView_beforeSceneGui(SceneView obj)
    {
        throw new System.NotImplementedException();
    }

    // Aqu� es donde dibujar�as los gizmos o handles en la vista de escena,
    // utilizando la instancia de tu EditorWindow.
    // Esto se llama CADA VEZ que la vista de escena se repinta.
    private void OnSceneGUI(SceneView sceneView)
    {
        // Puedes dibujar gizmos o handles aqu� usando UnityEditor.Handles
        // Por ejemplo, dibujar una esfera en el centro del mundo
        Handles.color = Color.blue;
        Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, 0.5f, EventType.Repaint);
        Handles.Label(Vector3.zero + Vector3.up, "Central Point from MyEditorToolWindow");

        // Si tu ModelAssetDataEditor tiene datos que quieres visualizar,
        // acceder�as a ellos desde aqu�, ya que OnSceneGUI es un m�todo de instancia
        // y tiene acceso a todas las variables de tu MyEditorToolWindow.
        Handles.Label(new Vector3(1, 1, 1), _message); // Ejemplo de acceso a variable de instancia
    }

    // Este es el m�todo que dibuja la interfaz de usuario de la ventana
    private void OnGUI()
    {
        GUILayout.Label("Editor Window", EditorStyles.boldLabel);
        _message = EditorGUILayout.TextField("Window Message", _message);

        if (GUILayout.Button("Execute DrawIconsOnRun"))
        {
            DrawIconsOnRun(); // Llamando al m�todo no est�tico de esta instancia
        }
    }
}