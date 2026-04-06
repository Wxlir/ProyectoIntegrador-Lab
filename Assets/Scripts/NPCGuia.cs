using UnityEngine;
using UnityEngine.AI;

public class NPCGuia : MonoBehaviour
{
    [SerializeField] private HUDManager hudManager;
    [Tooltip("Segundos que el NPC sigue al jugador al aparecer en escena.")]
    public float tiempoSeguimiento = 5f;

    private NavMeshAgent agente;
    private Transform jugador;
    private float timerSeguimiento;
    private bool jugadorCerca = false;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        if (agente == null)
            Debug.LogError("NPCGuia: NO tiene componente NavMeshAgent.", this);
        else
            Debug.Log("NPCGuia: NavMeshAgent OK. isOnNavMesh=" + agente.isOnNavMesh);

        // Si tiene Rigidbody, ponerlo kinematic para que el NavMeshAgent
        // sea el unico que controla el movimiento (igual que el enemigo).
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.freezeRotation = true;
        }

        // El BoxCollider es solo para detectar al jugador (chat).
        // Forzar Is Trigger para que no colisione fisicamente con el suelo.
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
            box.isTrigger = true;

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj == null)
            Debug.LogError("NPCGuia: NO encontro objeto con tag 'Player'.", this);
        else
            jugador = obj.transform;

        timerSeguimiento = tiempoSeguimiento;
    }

    void Update()
    {
        // Seguir al jugador durante los primeros 'tiempoSeguimiento' segundos.
        if (timerSeguimiento > 0f && jugador != null && agente != null)
        {
            timerSeguimiento -= Time.deltaTime;
            bool ok = agente.SetDestination(jugador.position);
            Debug.Log("NPCGuia seguimiento: SetDestination=" + ok + " isOnNavMesh=" + agente.isOnNavMesh + " pathStatus=" + agente.pathStatus + " velocity=" + agente.velocity);

            if (timerSeguimiento <= 0f)
                agente.ResetPath(); // Deja de seguir al terminar el timer.
        }

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