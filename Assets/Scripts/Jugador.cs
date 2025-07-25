using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Jugador : MonoBehaviour
{
    #region CORE

    private void Awake()
    {
        Awake_ObtenerComponentes();
        Awake_Electricidad();
    }

    private void Start()
    {
        Start_Photon();
    }

    private void Update()
    {
        Update_Imput();
        Update_Ataque();
        Update_Triggers();
    }

    private void FixedUpdate()
    {
        FixedUpdate_Movimiento();
    }

    #endregion CORE

    #region COMPONENTES

    Rigidbody2D rb;
    BoxCollider2D boxCollider;
    SpriteRenderer sprite;
    public SpriteRenderer Sprite => sprite;
    Animator animator;
    TextMeshProUGUI nickName;
    private Light2D luzInterna; // using UnityEngine.Rendering.Universal;

    //Photon
    PhotonView photonView;
    PhotonTransformViewClassic photonTransform;
    PhotonTransformViewClassic PhotonTransform => photonTransform;

    private void Awake_ObtenerComponentes()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        sprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
        animator = transform.GetChild(0).GetComponent<Animator>();
        nickName = transform.GetComponentInChildren<TextMeshProUGUI>();
        luzInterna = transform.GetChild(2).GetComponentInChildren<Light2D>();

        photonView = GetComponent<PhotonView>();
        photonTransform = GetComponent<PhotonTransformViewClassic>();
    }

    #endregion COMPONENTES

    #region PHOTON
    public Player Player => photonView.Owner;

    private void Start_Photon()
    {
        // Mostramos el Nickname del propietario
        nickName.text = Player.NickName;

        //Si contiene la propiedad de Asesino
        if (Player.CustomProperties.ContainsKey("Asesino"))
        {
            _asesino = true;

            //Si somos el Asesino mostramos el texto
            if (photonView.IsMine) GameManager.MostrarTxtCentral("Tu Eres el Asesino");

        }
    }
    #endregion PHOTON

    #region PROPIEDADES
    private bool _asesino = false;
    public bool Asesino => _asesino;

    internal bool Fantasma = false;

    public bool Tangible
    {
        get => boxCollider.enabled;
        set => boxCollider.enabled = value;
    }

    public float Opacidad
    {
        get => sprite.color.a;
        set
        {
            sprite.color = new Color(r: 1, g: 1, b: 1, a: value);
            nickName.alpha = value;
        }
    }

    #endregion PROPIEDADES

    #region MOVIMIENTO

    private Vector2 axis;
    private Vector2 axisGuardado;
    private Vector2 movimiento;
    private float velocidad = 5f;
    private bool bloquearMovimiento;

    private void Update_Imput()
    {
        //Return si n o es nuestro personaje
        if (!photonView.IsMine) return;

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        axis = new Vector2(x, y);
    }

    private void FixedUpdate_Movimiento()
    {
        //return si no es nuestro personaje
        if (!photonView.IsMine) return;

        //Return si esta bloquedo el movimiento
        if (bloquearMovimiento) return;

        //Return si se bloque el movimiento desde el GM
        if (GameManager.bloquearMovimiento) return;

        //Hay movimiento
        if (axis != Vector2.zero)
        {
            //Guardamos el axis cuando n o es 0
            axisGuardado = axis;

            //Animator
            animator.SetBool("caminando", true);

            //Movimiento Lateral
            if (axis.x != 0)
            {
                //Animator Blend Tree
                animator.SetFloat("x", 1);
                animator.SetFloat("y", 0);

                //Flip
                float rotacionY = axis.x > 0 ? 0 : 180;
                transform.rotation = Quaternion.Euler(new Vector3(0, rotacionY, 0));
                if (nickName != null) nickName.transform.rotation = Quaternion.identity;
            }

            //Movimiento Vertical
            else
            {
                //Movimiento Blend Tree
                animator.SetFloat("x", 0);
                animator.SetFloat("y", axis.y);
            }

            movimiento = axis.normalized * velocidad;
            transform.position += (Vector3)movimiento * Time.deltaTime;
        }

        //Sin movimiento
        else
        {
            //Animator
            animator.SetBool("caminando", false);
        }
    }

    #endregion MOVIMIENTO

    #region ATAQUE

    private bool bloquearAtaque = false;

    private void Update_Ataque()
    {
        //Si no es asesino retorna
        if (!Asesino) return;

        //Si se presina la tecla K
        if (Input.GetKeyDown(KeyCode.K)) Atacar();
    }

    private void Atacar()
    {
        // RETURN: Si esta bloqueado el ataque
        if (bloquearAtaque) return;

        // Ejecuta la animacion
        animator.SetTrigger(name: "ataque");

        // Obtenemos el offset segun donde este mirando el personaje
        Vector2 offset = Vector2.zero;

        // La direccion es donde esta viendo el personaje
        Vector2 direccion = axisGuardado;

        // Mirando arriba o abajo
        if (axisGuardado.x == 0)
        {
            if (axisGuardado.y > 0) // Arriba
                offset = new Vector2(x: 0, y: boxCollider.size.y + 0.001f);
            else // Abajo
                offset = new Vector2(x: 0, y: -0.001f);
        }
        // Mirando lateralmente
        else
        {
            offset = new Vector2(x: transform.right.x * (boxCollider.size.x / 2 + 0.001f), boxCollider.offset.y);
            direccion = (Vector2)transform.right;
        }

        // Origen de donde se lanza el rayo
        Vector2 origen = (Vector2)transform.position + offset;

        // Distancia del rayo
        float distancia = 0.5f;

        // La capa en la que se filtrara el rayo
        LayerMask capa = LayerMask.GetMask("Jugador");

        // Lanzamos el rayo
        RaycastHit2D hit = Physics2D.Raycast(origin: origen, direction: direccion, distancia, (int)capa);

        // Si colisiono con otro jugador ...
        if (hit)
        {
            // Obtenemos el componente jugador, desde la colision del rayo
            PhotonView view = hit.transform.GetComponent<PhotonView>();

            // Obtenemos las propiedades de quien matamos
            // using ExitGames.Client.Photon;
            Hashtable propiedades = view.Owner.CustomProperties;

            // Le decimos que va a ser un fantasma
            propiedades["Fantasma"] = true;

            // Aplicamos los cambios
            view.Owner.SetCustomProperties(propiedades);

            // Bloqueamos el ataque por 10 segundos
            bloquearAtaque = true;
            Invoke(methodName: "DesbloquearAtaque", time: 10);
        }
    }

    private void DesbloquearAtaque()
    {
        bloquearAtaque = false;
    }

    #endregion ATAQUE

    #region ALCANTARILLAS

    private Transform alcantarilla;

    private void Usar_Alcantarilla()
    {
        // Obtenemos al padre de la alcantarilla
        Transform padre = alcantarilla.parent;

        // Obtenemos el índice de la alcantarilla contraria
        int indice = alcantarilla.GetSiblingIndex() == 0 ? 1 : 0;

        // Nos "teleportamos" a la otra alcantarilla
        transform.position = padre.GetChild(indice).position;
    }
    #endregion ALCANTARILLAS

    #region ELECTRICIDAD

    private string electricidadTrigger = string.Empty;
    private string electricidadActivada = string.Empty;


    private void Awake_Electricidad()
    {
        luzInterna.gameObject.SetActive(false);
        GameManager.OnElectricidadCambiada += ElectricidadCambiada;
    }

    private void ElectricidadCambiada(bool valor)
    {
        luzInterna.gameObject.SetActive(!valor);
    }


    private void Usar_Electricidad()
    {
        //RETURN: Si esta encendida la luz y no somos el asesino
        if (GameManager.Electricidad && !Asesino) return;

        //RETURN: Si la luz esta apagada y somos el asesino
        if (!GameManager.Electricidad && Asesino) return;

        //Primer Activacion
        if (electricidadActivada == string.Empty)
        {
            //Susuda el primer interruptor
            electricidadActivada = electricidadTrigger;
        }
        //Segunda Activacion
        else
        {
            //Verifiecemos que no sea el mismo interruptor
            if (electricidadTrigger != electricidadActivada)
            {
                //Esta seria la segunda activacion para apagar las luces
                GameManager.Electricidad = !Asesino;

                //Barre el interruptor que ya había activado
                electricidadActivada = string.Empty;
            }
        }
    }

    #endregion ELECTRICIDAD

    #region TRIGGERS

    private void Update_Triggers()
    {
        //RETURN: Si no se esta presionando la tecla E
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (alcantarilla)
            Usar_Alcantarilla();
        else if (electricidadTrigger != string.Empty)
            Usar_Electricidad();

    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        switch (other.gameObject.layer)
        {
            case 8: if (Asesino) alcantarilla = other.transform; break;
            case 9: electricidadTrigger = other.gameObject.name; break;
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        switch (other.gameObject.layer)
        {
            case 8: if (Asesino) alcantarilla = null; break;
            case 9: electricidadTrigger = string.Empty; break;
        }
    }

    #endregion TRIGGERS
}


