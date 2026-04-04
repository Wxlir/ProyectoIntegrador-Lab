using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlPlayer : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidad = 4f;
    public float sensibilidadMouse = 10.5f;

    [Header("Sistema de Vida")]
    public int vida = 2;

    private CharacterController controller;
    private float rotacionX = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // MOVIMIENTO DE CÁMARA (360 grados)
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
    }

    void LanzarBomba()
    {
        // 1. Creamos la esfera
        GameObject bomba = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // 2. Posición en los pies (restamos 0.5 en Y) y un poco adelante
        Vector3 posicionSuelo = transform.position + new Vector3(0, -0.5f, 0) + (transform.forward * 0.5f);
        bomba.transform.position = posicionSuelo;
        bomba.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        // 3. Configuramos la física para que NO se mueva
        Rigidbody rb = bomba.AddComponent<Rigidbody>();
        rb.isKinematic = true; // <--- Esto evita que ruede o salga volando

        // 4. Hacerla Trigger para que no te empuje a ti ni a los muros al aparecer
        SphereCollider sc = bomba.GetComponent<SphereCollider>();
        if (sc != null) sc.isTrigger = true;

        // 5. Destrucción y explosión
        Destroy(bomba, 2f);

        // OJO: Pasamos la posición de la BOMBA a la explosión, no la del jugador
        StartCoroutine(EsperarYExplotar(posicionSuelo, 2f));
    }

    // Usamos una Corrutina para que la explosión ocurra donde se puso la bomba
    System.Collections.IEnumerator EsperarYExplotar(Vector3 posicionBomba, float tiempo)
    {
        yield return new WaitForSeconds(tiempo);

        // Buscamos basura alrededor de la bomba, no del jugador
        Collider[] objetosCercanos = Physics.OverlapSphere(posicionBomba, 3f);
        foreach (Collider col in objetosCercanos)
        {
            if (col.CompareTag("Destructible"))
            {
                Destroy(col.gameObject);
            }
        }
    }   

    void Explotar()
    {
        // Busca basura con el Tag "Destructible"
        Collider[] objetosCercanos = Physics.OverlapSphere(transform.position + transform.forward * 2, 3f);
        foreach (Collider col in objetosCercanos)
        {
            if (col.CompareTag("Destructible"))
            {
                Destroy(col.gameObject);
            }
        }
    }

    public void RecibirDanio()
    {
        vida--;
        Debug.Log("Vida restante: " + vida);
        if (vida <= 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}