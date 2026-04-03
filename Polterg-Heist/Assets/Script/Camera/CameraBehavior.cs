using UnityEngine;

/// But: Suivre un GameObject (le Player) tout en limitant le mouvement de la caméra
/// Requiert: Camera
/// État: Adéquat(temp)
public class CameraBehavior : MonoBehaviour
{
    // Singleton
    public static CameraBehavior Instance { get; private set; }



    //Gameobject autres
    [Header("Other GameObjects")]
    [SerializeField] private Transform limiteHG;              //Pour limiter la caméra à un point haut-gauche
    [SerializeField] private Transform limiteBD;              //Pour limiter la caméra à un point bas-droit
    [SerializeField] private Transform focus;                 //Pour centrer sur le joueur

    //Camera Limits
    float minX;
    float maxX;
    float halfSizeX;
    float minY;
    float maxY;
    float halfSizeY;

    //Variables
    [Header("Variables")]
    [SerializeField] public float camSpeed;                 //Vitesse de la caméra vers le Focus
    Vector3 posSelf;                                        //Pour modifier la position de la caméra

    //Shortcut
    Camera cam;

    [SerializeField] LayerMask onClickLayers;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        cam = gameObject.GetComponent<Camera>();
        minX = limiteHG.position.x;
        maxX = limiteBD.position.x;
        minY = limiteBD.position.y;
        maxY = limiteHG.position.y;
        posSelf = transform.position;
        cam.eventMask = onClickLayers;
    }

    //Pour le mouvement de la caméra
    void FixedUpdate()
    {
        Vector3 currentPos = transform.position;
        Vector3 focusPos = focus.position;
        float yMinAdjust = minY + halfSizeY;
        float yMaxAdjust = maxY - halfSizeY;
        float xMinAdjust = minX + halfSizeX;
        float xMaxAdjust = maxX - halfSizeX;

        //Gestion des limites et du mouvement de la caméra sur y
        halfSizeY = cam.orthographicSize;
        posSelf.y = Mathf.Lerp(currentPos.y, focusPos.y, camSpeed * Time.deltaTime);
        posSelf.y = Mathf.Clamp(posSelf.y, yMinAdjust, yMaxAdjust);

        if (halfSizeY * 2 > maxY - minY && (focusPos.y > yMinAdjust || focusPos.y < yMaxAdjust))
        {
            posSelf.y = yMinAdjust;
        }

        //Gestion des limites et du mouvement de la caméra sur x
        halfSizeX = halfSizeY * cam.aspect;
        posSelf.x = Mathf.Lerp(currentPos.x, focusPos.x, camSpeed * Time.deltaTime);
        posSelf.x = Mathf.Clamp(posSelf.x, minX + halfSizeX, maxX - halfSizeX);

        if (halfSizeX * 2 > maxX - minX && (focusPos.x > xMinAdjust || focusPos.y < xMaxAdjust))
        {
            posSelf.x = xMinAdjust;
        }

        transform.position = posSelf;
    }

    // Update Camera bounds to fit next room
    public void UpdateCameraLimits(Transform limitTopLeft, Transform limitBottomRight)
    {
        if(limitBottomRight != null && limitTopLeft != null)
        {
            limiteHG = limitTopLeft;
            limiteBD = limitBottomRight;

            minX = limiteHG.position.x;
            maxX = limiteBD.position.x;
            minY = limiteBD.position.y;
            maxY = limiteHG.position.y;
        }
    }
}
