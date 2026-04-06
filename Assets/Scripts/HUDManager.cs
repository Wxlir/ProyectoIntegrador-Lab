using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public const int CorazonesMaximos = 5;

    [Header("Hearts")]
    [SerializeField] private RectTransform contenedorCorazones;
    [SerializeField] private GameObject prefabCorazon;

    private GameObject[] _corazones;

    [Header("Dialogue")]
    public TMP_Text dialogueText;
    [SerializeField] private Color colorMensajeSistema = new Color(1f, 0.55f, 0.2f);
    [SerializeField] private Color colorMensajePersonaje = new Color(0.75f, 0.92f, 1f);
    [SerializeField] [Range(3, 40)]
    [Tooltip("Arriba = más reciente. Al superar el máximo se descartan líneas viejas por abajo.")]
    private int maxMensajesEnHistorial = 12;
    [Tooltip("Opcional: ScrollRect que envuelve el texto del diálogo para desplazarte cuando hay muchas líneas.")]
    [SerializeField] private ScrollRect scrollHistorialDialogo;

    private readonly List<string> _historialMensajes = new List<string>();
    private Coroutine _scrollHistorialCoroutine;

    [Header("Win Text")]
    public GameObject winText;

    [Header("Lose Text")]
    public GameObject loseText;
    [SerializeField] private Button botonReiniciar;

    void Awake()
    {
        _corazones = new GameObject[CorazonesMaximos];
        for (int i = 0; i < CorazonesMaximos; i++)
        {
            GameObject go = Instantiate(prefabCorazon, contenedorCorazones);
            go.name = $"Corazon_{i + 1}";
            go.SetActive(false);
            _corazones[i] = go;
        }
    }

    void Start()
    {
        if (botonReiniciar != null)
            botonReiniciar.onClick.AddListener(Reiniciar);
    }

    void OnDestroy()
    {
        if (botonReiniciar != null)
            botonReiniciar.onClick.RemoveListener(Reiniciar);
    }

    public void Reiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ActualizarVida(int corazonesRestantes)
    {
        for (int i = 0; i < CorazonesMaximos; i++)
            _corazones[i].SetActive(i < corazonesRestantes);
    }

    public void AddMensajeSistema(string message)
    {
        AgregarMensajeAlHistorial(message, colorMensajeSistema);
    }

    public void AddMensajePersonaje(string message)
    {
        AgregarMensajeAlHistorial(message, colorMensajePersonaje);
    }

    void AgregarMensajeAlHistorial(string message, Color color)
    {
        if (string.IsNullOrEmpty(message) || dialogueText == null)
            return;

        var bloque = new List<string>();
        foreach (string linea in message.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(linea))
                continue;
            bloque.Add(EnvolverEnColor(linea.TrimEnd(), color));
        }

        if (bloque.Count == 0)
            return;

        _historialMensajes.InsertRange(0, bloque);
        while (_historialMensajes.Count > maxMensajesEnHistorial)
            _historialMensajes.RemoveAt(_historialMensajes.Count - 1);

        RefrescarTextoDialogo();
    }

    void RefrescarTextoDialogo()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < _historialMensajes.Count; i++)
        {
            if (i > 0)
                sb.Append('\n');
            sb.Append(_historialMensajes[i]);
        }

        dialogueText.text = sb.ToString();

        if (scrollHistorialDialogo != null)
        {
            if (_scrollHistorialCoroutine != null)
                StopCoroutine(_scrollHistorialCoroutine);
            _scrollHistorialCoroutine = StartCoroutine(ScrollHistorialAlInicio());
        }
    }

    IEnumerator ScrollHistorialAlInicio()
    {
        dialogueText.ForceMeshUpdate(true);
        yield return null;
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        RectTransform content = scrollHistorialDialogo.content;
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        scrollHistorialDialogo.velocity = Vector2.zero;
        scrollHistorialDialogo.verticalNormalizedPosition = 1f;

        _scrollHistorialCoroutine = null;
    }

    static string EnvolverEnColor(string message, Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(color);
        return $"<color=#{hex}>{message}</color>";
    }

    public void ShowWin()
    {
        winText.SetActive(true);
        botonReiniciar?.gameObject.SetActive(true);
    }

    public void ShowLose()
    {
        loseText.SetActive(true);
        botonReiniciar?.gameObject.SetActive(true);
    }
}
