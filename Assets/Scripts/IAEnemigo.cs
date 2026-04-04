using UnityEngine;
using UnityEngine.AI;

public class IAEnemigo : MonoBehaviour
{
    public float radioDeteccion = 2f;
    public float anguloVision = 45f; // Como un cono de luz
    public LayerMask capaObstaculos; // Para que sepa qué es una pared

    private NavMeshAgent agente;
    private Transform jugador;
    private Vector3 destinoPatrulla;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null) jugador = jugadorObj.transform;

        NuevaDireccionPatrulla();
    }

    void Update()
    {
        if (jugador == null) return;

        if (PuedeVerAlJugador())
        {
            // COMPORTAMIENTO CAZA: Te sigue
            agente.SetDestination(jugador.position);
            agente.speed = 1.2f; // Corre más rápido al verte
        }
        else
        {
            // COMPORTAMIENTO PATRULLA: Camina por ahí
            agente.speed = 0.8f;
            if (!agente.pathPending && agente.remainingDistance < 0.5f)
            {
                NuevaDireccionPatrulla();
            }
        }
    }

    bool PuedeVerAlJugador()
    {
        float distancia = Vector3.Distance(transform.position, jugador.position);

        // 1. Primero chequeamos si el jugador está dentro del círculo (radio)
        if (distancia < radioDeteccion)
        {
            Vector3 direccionAlJugador = (jugador.position - transform.position).normalized;
            Vector3 origenRayo = transform.position + new Vector3(0, 0.5f, 0);

            // Dibujamos el rayo para que lo veas en 360 grados
            Debug.DrawRay(origenRayo, direccionAlJugador * radioDeteccion, Color.red);

            // 2. RAYCAST: Solo chequeamos si hay una PARED en medio
            RaycastHit hit;
            if (Physics.Raycast(origenRayo + (direccionAlJugador * 0.2f), direccionAlJugador, out hit, radioDeteccion, capaObstaculos))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    return true; // Te "siente" porque no hay pared, sin importar hacia dónde mira
                }
            }
        }
        return false;
    }

    void NuevaDireccionPatrulla()
    {
        // Busca un punto aleatorio en el NavMesh
        Vector3 puntoAleatorio = transform.position + Random.insideUnitSphere * 10f;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, 10f, NavMesh.AllAreas))
        {
            agente.SetDestination(hit.position);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<ControlPlayer>().RecibirDanio();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}