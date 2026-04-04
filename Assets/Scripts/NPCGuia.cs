using UnityEngine;

public class NPCGuia : MonoBehaviour
{
    private bool jugadorCerca = false;

    void Update()
    {
        // Si el jugador está dentro del cuadro verde y presiona E
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("------------------------------------------------");
            Debug.Log("GUÍA: Hey..! Corre. *Usa F para las bombas*");
            Debug.Log("------------------------------------------------");
        }
    }

    // Se activa cuando entras al área del Box Collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            Debug.Log("Presiona E para hablar con el guía");
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