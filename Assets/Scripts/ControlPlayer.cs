using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ControlPlayer : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidad = 4f;
    public float sensibilidadMouse = 10.5f;
    public GameObject explosionPrefab;
    public AudioClip sonidoExplosion;

    [Header("Sistema de Vida")]
    [Range(0, 5)] public int vida = 2;

    [SerializeField] private HUDManager hudManager;

    [Header("Clima Dinamico")]
    [SerializeField] private GameObject lluviaRoot;
    [SerializeField] private bool usarNieblaDinamica = true;
    [SerializeField] private float densidadNieblaPeligro = 0.06f;
    [SerializeField] private ParticleSystem lluviaCaidaPS;
    [SerializeField] private ParticleSystem lluviaSalpicaduraPS;
    [SerializeField] private ParticleSystem lluviaOndasPS;
    [SerializeField] [Range(0.05f, 1f)] private float intensidadSuaveCaida = 0.35f;
    [SerializeField] [Range(0.05f, 1f)] private float intensidadSuaveSalpicadura = 0.3f;
    [SerializeField] [Range(0.05f, 1f)] private float intensidadSuaveOndas = 0.4f;

    [Header("Paisaje Sonoro")]
    [SerializeField] private AudioClip lluviaSuaveClip;
    [SerializeField] private AudioClip lluviaFuerteClip;
    [SerializeField] private AudioClip particulasPolvoClip;
    [SerializeField] private AudioClip vientoNieblaClip;
    [SerializeField] private AudioClip vidrioClip;
    [SerializeField][Range(0f, 1f)] private float volumenLluviaSuave = 0.2f;
    [SerializeField][Range(0f, 1f)] private float volumenLluviaFuerte = 0.35f;
    [SerializeField][Range(0f, 1f)] private float volumenParticulasPolvo = 0.18f;
    [SerializeField][Range(0f, 1f)] private float volumenVientoNiebla = 0.16f;
    [SerializeField][Range(0f, 1f)] private float volumenVidrio = 0.85f;

    [Header("Escombros de Cristal")]
    [SerializeField] private int cantidadChunksPorMuro = 7;
    [SerializeField] private float fuerzaExplosionChunks = 4.5f;
    [SerializeField] private float radioExplosionChunks = 2.5f;
    [FormerlySerializedAs("vidaChunks")]
    [SerializeField][Min(0.5f)] private float tiempoVidaChunks = 4f;

    private CharacterController controller;
    private float rotacionX = 0f;
    private bool juegoTerminado;
    private float densidadNieblaNormal;
    private bool climaPeligroActivo;
    private AudioSource lluviaSuaveSource;
    private AudioSource lluviaFuerteSource;
    private AudioSource particulasPolvoSource;
    private AudioSource vientoNieblaSource;
    private float lluviaCaidaRateBase;
    private float lluviaSalpicaduraRateBase;
    private float lluviaOndasRateBase;
    private int lluviaCaidaMaxBase;
    private int lluviaSalpicaduraMaxBase;
    private int lluviaOndasMaxBase;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        densidadNieblaNormal = RenderSettings.fogDensity;
        if (lluviaRoot == null)
            lluviaRoot = GameObject.Find("Lluvia");

        ConfigurarParticulasLluvia();
        CargarClipsPaisajeSonoro();
        ConfigurarPaisajeSonoro();
        hudManager?.ActualizarVida(vida);
        ActualizarClimaPorVida(true);
    }

    void Update()
    {
        if (juegoTerminado)
            return;

        // MOVIMIENTO DE C�MARA (360 grados)
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadMouse;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadMouse;

        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);

        Camera.main.transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // MOVIMIENTO WASD
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 mover = transform.right * x + transform.forward * z;
        controller.SimpleMove(mover * velocidad);

        // LANZAR BOMBA (Tecla F)
        if (Input.GetKeyDown(KeyCode.F))
        {
            LanzarBomba();
        }

        ActualizarVolumenesPaisajeSonoro();
    }

    void LanzarBomba()
    {
        // 1. Creamos la esfera
        GameObject bomba = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // 2. PosiciÃƒÂ³n en los pies (restamos 0.5 en Y) y un poco adelante
        Vector3 posicionSuelo = transform.position + new Vector3(0, -0.5f, 0) + (transform.forward * 0.5f);
        bomba.transform.position = posicionSuelo;
        bomba.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        // 3. Configuramos la fÃƒÂ­sica para que NO se mueva
        Rigidbody rb = bomba.AddComponent<Rigidbody>();
        rb.isKinematic = true; // <--- Esto evita que ruede o salga volando

        // 4. Hacerla Trigger para que no te empuje a ti ni a los muros al aparecer
        SphereCollider sc = bomba.GetComponent<SphereCollider>();
        if (sc != null) sc.isTrigger = true;

        // 5. DestrucciÃƒÂ³n y explosiÃƒÂ³n
        Destroy(bomba, 2f);

        // OJO: Pasamos la posiciÃƒÂ³n de la BOMBA a la explosiÃƒÂ³n, no la del jugador
        StartCoroutine(EsperarYExplotar(posicionSuelo, 2f));
    }

    // Usamos una Corrutina para que la explosiÃƒÂ³n ocurra donde se puso la bomba
    System.Collections.IEnumerator EsperarYExplotar(Vector3 posicionBomba, float tiempo)
    {
        yield return new WaitForSeconds(tiempo);

        if (sonidoExplosion != null)
        {
            AudioSource.PlayClipAtPoint(sonidoExplosion, posicionBomba);
        }

        // 💥 EXPLOSIÓN VISUAL (PARTÍCULAS)

        GameObject explosion = Instantiate(explosionPrefab, posicionBomba, Quaternion.identity);
        Destroy(explosion, 2f);

        DestruirDestructiblesEnRadio(posicionBomba, 3f);
    }

    void Explotar()
    {
        DestruirDestructiblesEnRadio(transform.position + transform.forward * 2f, 3f);
    }

    void DestruirDestructiblesEnRadio(Vector3 centroExplosion, float radio)
    {
        Collider[] objetosCercanos = Physics.OverlapSphere(centroExplosion, radio);
        var destruidos = new HashSet<GameObject>();

        foreach (Collider col in objetosCercanos)
        {
            if (!col.CompareTag("Destructible"))
                continue;

            GameObject objetivo = col.gameObject;
            if (!destruidos.Add(objetivo))
                continue;

            ReproducirVidrio(objetivo.transform.position);
            CrearEscombrosCristal(objetivo, centroExplosion);
            Destroy(objetivo);
        }
    }

    void CargarClipsPaisajeSonoro()
    {
        if (lluviaSuaveClip == null)
            lluviaSuaveClip = Resources.Load<AudioClip>("Sounds/Ambience/Lluvia suave");
        if (lluviaFuerteClip == null)
            lluviaFuerteClip = Resources.Load<AudioClip>("Sounds/Ambience/Lluvia fuerte");
        if (particulasPolvoClip == null)
            particulasPolvoClip = Resources.Load<AudioClip>("Sounds/Ambience/ParticulasPolvo");
        if (vientoNieblaClip == null)
            vientoNieblaClip = Resources.Load<AudioClip>("Sounds/Ambience/Viento - niebla");
        if (vidrioClip == null)
            vidrioClip = Resources.Load<AudioClip>("Sounds/Ambience/Vidrio");
    }

    void ConfigurarParticulasLluvia()
    {
        if (lluviaRoot == null)
            return;

        if (lluviaCaidaPS == null || lluviaSalpicaduraPS == null || lluviaOndasPS == null)
        {
            ParticleSystem[] sistemas = lluviaRoot.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem sistema in sistemas)
            {
                if (lluviaCaidaPS == null && sistema.gameObject.name == "Caida_Lluviaa")
                    lluviaCaidaPS = sistema;
                else if (lluviaSalpicaduraPS == null && sistema.gameObject.name == "Salpicadura")
                    lluviaSalpicaduraPS = sistema;
                else if (lluviaOndasPS == null && sistema.gameObject.name == "Ondas")
                    lluviaOndasPS = sistema;
            }
        }

        lluviaRoot.SetActive(true);
        CachearBaseLluvia(lluviaCaidaPS, ref lluviaCaidaRateBase, ref lluviaCaidaMaxBase);
        CachearBaseLluvia(lluviaSalpicaduraPS, ref lluviaSalpicaduraRateBase, ref lluviaSalpicaduraMaxBase);
        CachearBaseLluvia(lluviaOndasPS, ref lluviaOndasRateBase, ref lluviaOndasMaxBase);
    }

    void CachearBaseLluvia(ParticleSystem sistema, ref float rateBase, ref int maxBase)
    {
        if (sistema == null)
            return;

        var emission = sistema.emission;
        var main = sistema.main;
        rateBase = emission.rateOverTimeMultiplier;
        maxBase = main.maxParticles;

        if (!sistema.isPlaying)
            sistema.Play();
    }

    void AplicarIntensidadLluvia(bool lluviaPeligro)
    {
        AplicarIntensidadParticulaLluvia(lluviaCaidaPS, lluviaPeligro ? 1f : intensidadSuaveCaida, lluviaCaidaRateBase, lluviaCaidaMaxBase);
        AplicarIntensidadParticulaLluvia(lluviaSalpicaduraPS, lluviaPeligro ? 1f : intensidadSuaveSalpicadura, lluviaSalpicaduraRateBase, lluviaSalpicaduraMaxBase);
        AplicarIntensidadParticulaLluvia(lluviaOndasPS, lluviaPeligro ? 1f : intensidadSuaveOndas, lluviaOndasRateBase, lluviaOndasMaxBase);
    }

    void AplicarIntensidadParticulaLluvia(ParticleSystem sistema, float intensidad, float rateBase, int maxBase)
    {
        if (sistema == null)
            return;

        intensidad = Mathf.Max(0.01f, intensidad);
        var emission = sistema.emission;
        emission.rateOverTimeMultiplier = rateBase * intensidad;

        var main = sistema.main;
        main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(maxBase * intensidad));

        if (!sistema.isPlaying)
            sistema.Play();
    }

    void ConfigurarPaisajeSonoro()
    {
        lluviaSuaveSource = CrearLoop2D("RainSoftLoop", lluviaSuaveClip, volumenLluviaSuave);
        lluviaFuerteSource = CrearLoop2D("RainStrongLoop", lluviaFuerteClip, volumenLluviaFuerte);
        particulasPolvoSource = CrearLoop2D("DustLoop", particulasPolvoClip, volumenParticulasPolvo);
        vientoNieblaSource = CrearLoop2D("WindFogLoop", vientoNieblaClip, volumenVientoNiebla);

        IniciarLoopSiTieneClip(lluviaSuaveSource);
        IniciarLoopSiTieneClip(particulasPolvoSource);
        IniciarLoopSiTieneClip(vientoNieblaSource);
    }

    AudioSource CrearLoop2D(string nombre, AudioClip clip, float volumen)
    {
        if (clip == null)
            return null;

        GameObject audioGO = new GameObject(nombre);
        audioGO.transform.SetParent(transform, false);

        AudioSource source = audioGO.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = clip;
        source.volume = volumen;
        return source;
    }

    void IniciarLoopSiTieneClip(AudioSource source)
    {
        if (source != null && source.clip != null && !source.isPlaying)
            source.Play();
    }

    void ActualizarVolumenesPaisajeSonoro()
    {
        if (lluviaSuaveSource != null)
            lluviaSuaveSource.volume = volumenLluviaSuave;
        if (lluviaFuerteSource != null)
            lluviaFuerteSource.volume = volumenLluviaFuerte;
        if (particulasPolvoSource != null)
            particulasPolvoSource.volume = volumenParticulasPolvo;
        if (vientoNieblaSource != null)
            vientoNieblaSource.volume = volumenVientoNiebla;
    }

    void ReproducirVidrio(Vector3 posicion)
    {
        if (vidrioClip == null)
            return;

        AudioSource.PlayClipAtPoint(vidrioClip, posicion, volumenVidrio);
    }

    void CrearEscombrosCristal(GameObject objetivo, Vector3 centroExplosion)
    {
        Renderer rendererObjetivo = objetivo.GetComponent<Renderer>();
        Collider colliderObjetivo = objetivo.GetComponent<Collider>();

        Bounds bounds = rendererObjetivo != null
            ? rendererObjetivo.bounds
            : colliderObjetivo != null
                ? colliderObjetivo.bounds
                : new Bounds(objetivo.transform.position, Vector3.one);

        Material materialCristal = rendererObjetivo != null ? rendererObjetivo.sharedMaterial : null;
        int cantidad = Mathf.Max(4, cantidadChunksPorMuro);
        float tamanoBase = Mathf.Max(0.12f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.35f);

        for (int i = 0; i < cantidad; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.name = "CrystalChunk";
            chunk.layer = objetivo.layer;

            Vector3 offset = new Vector3(
                Random.Range(-bounds.extents.x * 0.55f, bounds.extents.x * 0.55f),
                Random.Range(-bounds.extents.y * 0.25f, bounds.extents.y * 0.75f),
                Random.Range(-bounds.extents.z * 0.55f, bounds.extents.z * 0.55f));

            chunk.transform.position = bounds.center + offset;

            Vector3 direccionSalida = (chunk.transform.position - centroExplosion).normalized;
            if (direccionSalida.sqrMagnitude < 0.001f)
                direccionSalida = Random.onUnitSphere;
            direccionSalida.y = Mathf.Abs(direccionSalida.y) + 0.35f;
            direccionSalida.Normalize();

            chunk.transform.rotation = Quaternion.LookRotation(direccionSalida) *
                                       Quaternion.Euler(Random.Range(-22f, 22f), Random.Range(0f, 360f), Random.Range(-22f, 22f));
            chunk.transform.localScale = new Vector3(
                Random.Range(tamanoBase * 0.18f, tamanoBase * 0.35f),
                Random.Range(tamanoBase * 1.2f, tamanoBase * 2.8f),
                Random.Range(tamanoBase * 0.18f, tamanoBase * 0.42f));

            Renderer chunkRenderer = chunk.GetComponent<Renderer>();
            if (chunkRenderer != null && materialCristal != null)
                chunkRenderer.sharedMaterial = materialCristal;

            Rigidbody rb = chunk.AddComponent<Rigidbody>();
            rb.mass = 0.12f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearDamping = 0.08f;
            rb.angularDamping = 0.05f;
            rb.AddExplosionForce(fuerzaExplosionChunks, centroExplosion, radioExplosionChunks, 0.35f, ForceMode.Impulse);
            rb.AddForce(direccionSalida * (fuerzaExplosionChunks * 0.35f), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 7f, ForceMode.Impulse);

            Destroy(chunk, tiempoVidaChunks);
        }
    }

    void ActualizarClimaPorVida(bool forzar = false)
    {
        bool activarClimaPeligro = vida == 1 && !juegoTerminado;
        if (!forzar && climaPeligroActivo == activarClimaPeligro)
            return;

        climaPeligroActivo = activarClimaPeligro;

        AplicarIntensidadLluvia(activarClimaPeligro);

        if (lluviaFuerteSource != null)
        {
            if (activarClimaPeligro)
            {
                if (!lluviaFuerteSource.isPlaying)
                    lluviaFuerteSource.Play();
            }
            else if (lluviaFuerteSource.isPlaying)
            {
                lluviaFuerteSource.Stop();
            }
        }

        if (usarNieblaDinamica)
        {
            RenderSettings.fog = true;
            RenderSettings.fogDensity = activarClimaPeligro ? densidadNieblaPeligro : densidadNieblaNormal;
        }

        if (activarClimaPeligro)
            hudManager?.AddMensajeSistema("La tormenta empeora. Solo te queda una vida.");
    }

    public void RecibirDanio()
    {
        if (juegoTerminado)
            return;

        vida--;
        hudManager?.ActualizarVida(vida);
        hudManager?.AddMensajeSistema("Te han golpeado!");
        ActualizarClimaPorVida();
        if (vida <= 0)
        {
            juegoTerminado = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            hudManager?.ShowLose();
        }
    }
}
