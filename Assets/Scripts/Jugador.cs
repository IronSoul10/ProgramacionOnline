using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class Jugador : MonoBehaviour
{
    #region CORE

    private void Awake()
    {
        Awake_ObtenerComponentes();
    }

    private void Start()
    {
        Start_Photon();
    }

    private void Update()
    {
        Update_Imput();
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
    SpriteRenderer Sprite => sprite;
    Animator animator;
    TextMeshProUGUI nickName;

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
        if(Player.CustomProperties.ContainsKey("Asesino"))
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
        if(axis != Vector2.zero)
        {
            //Guardamos el axis cuando n o es 0
            axisGuardado = axis;

            //Animator
            animator.SetBool("caminando", true);

            //Movimiento Lateral
            if(axis.x != 0)
            {
                //Animator Blend Tree
                animator.SetFloat("x",1);
                animator.SetFloat("y", 0);

                //Flip
                float rotacionY = axis.x > 0 ? 0 : 180;
                transform.rotation = Quaternion.Euler(new Vector3( 0, rotacionY, 0));
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
}

