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
        dialogueText.text = EnvolverEnColor(message, colorMensajeSistema);
    }

    public void AddMensajePersonaje(string message)
    {
        dialogueText.text = EnvolverEnColor(message, colorMensajePersonaje);
    }

    static string EnvolverEnColor(string message, Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(color);
        return $"<color=#{hex}>{message}</color>";
    }

    public void ShowWin()
    {
        winText.SetActive(true);
        botonReiniciar.gameObject.SetActive(true);
    }

    public void ShowLose()
    {
        loseText.SetActive(true);
        botonReiniciar.gameObject.SetActive(true);
    }
}
