using UnityEngine;

public class NPCGuia : MonoBehaviour
{
    [SerializeField] private HUDManager hudManager;

    private bool jugadorCerca = false;

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            hudManager?.AddMensajePersonaje("Hey..! Corre.\n*Usa F para las bombas*");
        }
    }

    // Se activa cuando entras al área del Box Collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            hudManager?.AddMensajeSistema("Presiona E para hablar con el guía");
        }
    }

    // Se activa cuando sales del área
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
        }
    }
}