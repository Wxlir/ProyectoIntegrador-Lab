using UnityEngine;
using UnityEngine.AI;

// Enemigo persecutor para el laberinto.
// - Persigue al jugador mientras lo vea dentro de su cono de vision.
// - Al perder la vista, rastrea la posicion real del jugador 'tiempoRastreoExtra' segundos
//   para saber en que pasillo entro (linger tracking), luego va a ese punto 'tiempoMemoria' seg.
// - La persecucion dura maximo 'tiempoPersecucion' segundos antes de rendirse.
// - Para persecucion infinita: marcar 'perseguirPermanentemente' en el Inspector.
public class IAEnemigo : MonoBehaviour
{
    [Header("Persecucion")]
    [Tooltip("Segundos maximos de persecucion antes de rendirse. Ignorado si 'perseguirPermanentemente' esta activo.")]
    public float tiempoPersecucion = 5f;
    [Tooltip("Si esta activo, el enemigo persigue indefinidamente (ignora el timer).")]
    public bool perseguirPermanentemente = false;
    public float velocidadPersecucion = 1.5f;
    [Tooltip("Segundos que el enemigo sigue la ultima posicion conocida tras perder al jugador de vista (ej: al doblar esquinas).")]
    public float tiempoMemoria = 5f;
    [Tooltip("Segundos extra que el enemigo rastrea la posicion real del jugador tras perderlo de vista. Permite saber en que pasillo entro al doblar. (linger tracking)")]
    public float tiempoRastreoExtra = 1f;
    [Tooltip("Segundos de cooldown tras agotar el timer de persecucion. El enemigo patrulla y no puede ver al jugador hasta que se acabe.")]
    public float tiempoCooldown = 5f;

    [Header("Patrulla (cuando no persigue)")]
    public float velocidadPatrulla = 0.75f;
    public float radioPatrulla = 15f;

    [Header("Vision")]
    public float radioDeteccion = 8f;
    [Tooltip("Semi-angulo del cono de vision en grados. Ej: 45 = 90 grados de FOV total.")]
    public float anguloVision = 45f;
    [Tooltip("A esta distancia el enemigo detecta al jugador aunque este fuera del cono (como oirlo). Util para que no puedas pararte justo atras de el sin consecuencias.")]
    public float radioDeteccionCercana = 1.5f;
    public LayerMask capaObstaculos;

    private enum Estado { Patrullando, Persiguiendo, Rastreando, Recordando, Enfriando }
    private Estado estado;

    private NavMeshAgent agente;
    private Transform jugador;
    private float timerPersecucion;
    private float timerRastreo;
    private float timerMemoria;
    private float timerCooldown;
    private Vector3 ultimaPosicionJugador;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        // Buena calidad de evitacion para que los enemigos no se atraviesen entre si.
        agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Fix convulsion: si hay Rigidbody, el NavMeshAgent debe ser el unico que mueve
        // el objeto. Un Rigidbody no-cinematico pelea contra el agente y causa el temblor.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.freezeRotation = true;
        }

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            jugador = obj.transform;

        estado = Estado.Patrullando;
        agente.speed = velocidadPatrulla;
    }

    void Update()
    {
        if (jugador == null || agente == null) return;

        bool veAlJugador = PuedeVerAlJugador();

        switch (estado)
        {
            case Estado.Patrullando:
                Patrullar();
                if (veAlJugador)
                    IniciarPersecucion();
                break;

            case Estado.Persiguiendo:
                ultimaPosicionJugador = jugador.position;
                agente.SetDestination(ultimaPosicionJugador);
                if (!veAlJugador)
                {
                    // Perdio la vista: rastrear posicion real unos instantes para
                    // saber en que pasillo entro el jugador al doblar la esquina.
                    timerRastreo = tiempoRastreoExtra;
                    estado = Estado.Rastreando;
                }
                DescontarTimerPersecucion();
                break;

            case Estado.Rastreando:
                // Sin cono: sigue actualizando la posicion real del jugador.
                // Asi al terminar el timer el destino esta DENTRO del pasillo correcto.
                ultimaPosicionJugador = jugador.position;
                agente.SetDestination(ultimaPosicionJugador);
                if (veAlJugador)
                {
                    estado = Estado.Persiguiendo;
                }
                else
                {
                    timerRastreo -= Time.deltaTime;
                    if (timerRastreo <= 0f)
                    {
                        timerMemoria = tiempoMemoria;
                        estado = Estado.Recordando;
                    }
                }
                DescontarTimerPersecucion();
                break;

            case Estado.Recordando:
                if (veAlJugador)
                {
                    estado = Estado.Persiguiendo;
                }
                else
                {
                    timerMemoria -= Time.deltaTime;
                    if (timerMemoria <= 0f)
                        DetenerPersecucion();
                }
                DescontarTimerPersecucion();
                break;

            case Estado.Enfriando:
                // Durante el cooldown el enemigo solo patrulla, no puede detectar al jugador.
                Patrullar();
                timerCooldown -= Time.deltaTime;
                if (timerCooldown <= 0f)
                    estado = Estado.Patrullando;
                break;
        }
    }

    // ---- Logica de persecucion ----------------------------------------

    void IniciarPersecucion()
    {
        estado = Estado.Persiguiendo;
        timerPersecucion = tiempoPersecucion;
        ultimaPosicionJugador = jugador.position;
        agente.speed = velocidadPersecucion;
    }

    void DetenerPersecucion()
    {
        estado = Estado.Patrullando;
        agente.speed = velocidadPatrulla;
        agente.ResetPath();
    }

    // Para eliminar el timer: borrar este metodo, sus llamadas en Update(), e IniciarCooldown().
    void DescontarTimerPersecucion()
    {
        if (perseguirPermanentemente) return;
        timerPersecucion -= Time.deltaTime;
        if (timerPersecucion <= 0f)
            IniciarCooldown();
    }

    void IniciarCooldown()
    {
        estado = Estado.Enfriando;
        timerCooldown = tiempoCooldown;
        agente.speed = velocidadPatrulla;
        agente.ResetPath();
    }

    // ---- Patrulla aleatoria -------------------------------------------

    void Patrullar()
    {
        agente.speed = velocidadPatrulla;
        if (!agente.pathPending && agente.remainingDistance < 0.5f)
            BuscarNuevoPuntoPatrulla();
    }

    void BuscarNuevoPuntoPatrulla()
    {
        Vector3 puntoAleatorio = transform.position + Random.insideUnitSphere * radioPatrulla;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, radioPatrulla, NavMesh.AllAreas))
            agente.SetDestination(hit.position);
    }

    // ---- Dano al jugador (deshabilitado, listo para activar) ----------
    // Para activar:
    //   1. Descomentar el bloque de abajo.
    //   2. Asegurarse de que el enemigo tenga un Collider no-Trigger.
    //   3. El jugador debe tener el tag "Player".
    /*
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            CausarDanio(collision.gameObject);
    }

    void CausarDanio(GameObject objetivo)
    {
        ControlPlayer cp = objetivo.GetComponent<ControlPlayer>();
        if (cp != null)
            cp.RecibirDanio();
    }
    */

    // ---- Vision en cono ------------------------------------------------
    bool PuedeVerAlJugador()
    {
        // 1. Chequeo de distancia maxima.
        float dist = Vector3.Distance(transform.position, jugador.position);
        if (dist > radioDeteccion) return false;

        // 2. Deteccion cercana: si el jugador esta muy cerca, lo siente
        //    aunque este fuera del cono (como oirlo o sentir su presencia).
        if (dist <= radioDeteccionCercana) return true;

        // 3. Chequeo de angulo: el jugador debe estar dentro del cono frontal.
        Vector3 dir = (jugador.position - transform.position).normalized;
        float angulo = Vector3.Angle(transform.forward, dir);
        if (angulo > anguloVision) return false;

        // 3. Chequeo de linea de vista: no debe haber un muro en medio.
        Vector3 origen = transform.position + Vector3.up * 0.5f;
        Debug.DrawRay(origen, dir * radioDeteccion, Color.red);

        if (Physics.Raycast(origen, dir, out RaycastHit hit, radioDeteccion, capaObstaculos))
        {
            // El rayo choco con algo: solo ve al jugador si ese algo ES el jugador.
            return hit.transform.CompareTag("Player");
        }

        // Nada en medio: vision libre.
        return true;
    }


    // ---- Gizmos de depuracion -----------------------------------------

    void OnDrawGizmosSelected()
    {
        // Radio de patrulla (naranja)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radioPatrulla);

        // Cono de vision (amarillo): linea central + dos bordes + arco de cierre
        Gizmos.color = Color.yellow;
        Vector3 origen = transform.position;

        // Linea central (direccion de mirada)
        Gizmos.DrawLine(origen, origen + transform.forward * radioDeteccion);

        // Bordes del cono
        Quaternion rotIzq = Quaternion.Euler(0, -anguloVision, 0);
        Quaternion rotDer = Quaternion.Euler(0, anguloVision, 0);
        Vector3 izq = rotIzq * transform.forward * radioDeteccion;
        Vector3 der = rotDer * transform.forward * radioDeteccion;
        Gizmos.DrawLine(origen, origen + izq);
        Gizmos.DrawLine(origen, origen + der);

        // Arco que cierra el cono al final (una linea cada 5 grados)
        Vector3 prevPunto = origen + izq;
        for (int i = -(int)anguloVision + 5; i <= (int)anguloVision; i += 5)
        {
            Vector3 puntoActual = origen + Quaternion.Euler(0, i, 0) * transform.forward * radioDeteccion;
            Gizmos.DrawLine(prevPunto, puntoActual);
            prevPunto = puntoActual;
        }

        // Radio de deteccion cercana (rojo)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radioDeteccionCercana);
    }
}