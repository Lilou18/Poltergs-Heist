using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class HeightAndSpriteResizeSystem : MonoBehaviour
{
    Collider2D col2D;
    MovementController controller;
    Transform pivot;
    Transform height;
    Transform sprite;
    Transform parentObject;

    [SerializeField] public Vector3 positionAdjuster;
    [HideInInspector] public Vector3 pivotPos;
    Vector2 spriteSize;
    Quaternion iniRotation;
    bool isColSmaller;
    bool isPossessable;

    public Transform Parent { get { return parentObject; } }
    public Transform Sprite { get { return sprite; } }
    public Transform Pivot { get { return Pivot; } }

    enum Side
    {
        None,
        Bottom, //Landing
        Top,    //Jumping
        Left,   //BonkL
        Right,  //BonkR
    }

    Side sideDown;
    Side curEvent;

    void OnEnable()
    {
        if (this.transform.TryGetComponent<Animator>(out Animator anim))
        {
            height = this.transform;
            col2D = height.parent.GetComponent<Collider2D>();
            sprite = height.GetChild(0).transform;
            parentObject = height.parent;
            iniRotation = parentObject.rotation;
            controller = parentObject.GetComponent<MovementController>();
            isPossessable = parentObject.TryGetComponent<PossessionController>(out PossessionController possession);
        }
        else
        {
            pivot = this.transform;
            col2D = this.transform.parent.GetComponent<Collider2D>();
            height = pivot.GetComponentInChildren<Animator>().transform;
            sprite = pivot.GetComponentInChildren<SpriteRenderer>().transform;
            parentObject = col2D.transform;
            iniRotation = parentObject.rotation;
            controller = parentObject.GetComponent<MovementController>();
            isPossessable = parentObject.TryGetComponent<PossessionController>(out PossessionController possession);
            spriteSize = sprite.GetComponent<Renderer>().bounds.size;
            if ( positionAdjuster != Vector3.zero ) { isColSmaller = true; }
            else { isColSmaller = false; }
        }

        if (Application.isPlaying)
        {
            StartCoroutine(OnEnableRelated());
        }
    }

    void OnDisable()
    {
        if (Application.isPlaying)
        {
            controller.onJump -= EventParameter;
            controller.onLand -= EventParameter;
            controller.onBonkL -= EventParameter;
            controller.onBonkR -= EventParameter;
            controller.onShakeL -= EventParameter;
            controller.onShakeR -= EventParameter;
        }
    }

    IEnumerator OnEnableRelated()
    {
        yield return new WaitForEndOfFrame();
        controller.onJump += EventParameter;
        controller.onLand += EventParameter;
        controller.onBonkL += EventParameter;
        controller.onBonkR += EventParameter;
        controller.onShakeL += EventParameter;
        controller.onShakeR += EventParameter;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!this.transform.TryGetComponent<Animator>(out Animator anim))
        {
            pivot.rotation = new Quaternion(0, 0, 0, 1);
        }
        float rotationZ = parentObject.rotation.eulerAngles.z;

        if (rotationZ <= 361 && rotationZ >= 359 || rotationZ <= 1 && rotationZ >= -1)
        {
            pivotPos = parentObject.position + new Vector3(col2D.offset.x * parentObject.localScale.x, -col2D.bounds.extents.y + (col2D.offset.y * parentObject.localScale.y), 0);
            sideDown = Side.Bottom;
        }
        else if (rotationZ <= 181 && rotationZ >= 179)
        {
            pivotPos = parentObject.position - new Vector3(col2D.offset.x * parentObject.localScale.x, col2D.bounds.extents.y + (col2D.offset.y * parentObject.localScale.y), 0);
            sideDown = Side.Top;
        }
        else if (rotationZ <= 91 && rotationZ >= 89)
        {
            pivotPos = parentObject.position + new Vector3(-col2D.offset.y * parentObject.localScale.y, -col2D.bounds.extents.y + (col2D.offset.x * parentObject.localScale.x), 0);
            sideDown = Side.Left;
        }
        else if (rotationZ <= 271 && rotationZ >= 269)
        {
            pivotPos = parentObject.position - new Vector3(-col2D.offset.y * parentObject.localScale.y, col2D.bounds.extents.y + (col2D.offset.x * parentObject.localScale.x), 0);
            sideDown = Side.Right;
        }
        else
        {
            pivotPos = col2D.ClosestPoint(parentObject.position + (Vector3.down * 100));
            sideDown = Side.None;
        }

        if (!this.transform.TryGetComponent<Animator>(out Animator anima))
        {
            pivot.position = pivotPos;
        }

        if (Application.isEditor && !Application.isPlaying)
        {
            height.position = pivotPos;
            sprite.position = col2D.bounds.center + positionAdjuster;
        }

        if (Application.isPlaying)
        {
            if (!this.transform.TryGetComponent<Animator>(out Animator animat))
            {

                if (isPossessable && !parentObject.GetComponent<Rigidbody2D>().freezeRotation)
                {
                    sprite.rotation = parentObject.rotation;
                }

                SpriteCenterer(sideDown);
            }
            else
            {
                height.rotation = new Quaternion(0, 0, 0, 1);

                height.position = pivotPos;

                if (isPossessable)
                {
                    sprite.rotation = parentObject.rotation;
                }
            }
        }
        
    }

    void SpriteCenterer(Side side)
    {
        Vector2 colSize = col2D.bounds.size;
        Vector2 heightScale = height.localScale;
        Vector3 heightPos = height.localPosition;
        Vector3 colCenter;

        if (side == Side.Bottom || side == Side.Top) { colCenter = pivotPos + new Vector3(0, col2D.bounds.extents.y, 0); }
        else if (side == Side.Left || side == Side.Right) { colCenter = pivotPos + new Vector3(0, col2D.bounds.extents.y, 0); }
        else { colCenter = col2D.bounds.center; }

        Vector3 spritePos = sprite.position;

        if (isColSmaller)
        {
            switch (curEvent)
            {
                case Side.Bottom:   //Works
                    spritePos.x = colCenter.x;
                    spritePos.y = colCenter.y - (spriteSize.y - spriteSize.y * heightScale.y) / 2;
                    break;
                case Side.Top:      //Works
                    spritePos.x = colCenter.x;
                    spritePos.y = colCenter.y - (spriteSize.y - spriteSize.y * heightScale.y) / 2;
                    break;
                case Side.Left:     //Works
                    spritePos.x = colCenter.x - (spriteSize.x - spriteSize.x * heightScale.x) / 2;
                    spritePos.y = colCenter.y - (spriteSize.y - spriteSize.y * heightScale.y) / 2;
                    break;
                case Side.Right:    //Works
                    spritePos.x = colCenter.x + (spriteSize.x - spriteSize.x * heightScale.x) / 2;
                    spritePos.y = colCenter.y - (spriteSize.y - spriteSize.y * heightScale.y) / 2;
                    break;
                default:
                    spritePos.x = colCenter.x;
                    spritePos.y = colCenter.y;
                    break;
            }
        }
        else
        {
            switch (curEvent)
            {
                    case Side.Bottom:   //Works
                        spritePos.x = colCenter.x;
                        spritePos.y = colCenter.y - (colSize.y - colSize.y * heightScale.y) / 2;
                        break;
                    case Side.Top:      //Works
                        spritePos.x = colCenter.x;
                        spritePos.y = colCenter.y - (colSize.y - colSize.y * heightScale.y) / 2;
                        break;
                    case Side.Left:     //Works
                        spritePos.x = colCenter.x - (colSize.x - colSize.x * heightScale.x) / 2;
                        spritePos.y = colCenter.y - (colSize.y - colSize.y * heightScale.y) / 2;
                        break;
                    case Side.Right:    //Works
                        spritePos.x = colCenter.x + (colSize.x - colSize.x * heightScale.x) / 2;
                        spritePos.y = colCenter.y - (colSize.y - colSize.y * heightScale.y) / 2;
                        break;
                    default:
                        spritePos.x = colCenter.x;
                        spritePos.y = colCenter.y;
                        break;
            }
        }
        sprite.position = spritePos + positionAdjuster;
    }

    void EventParameter(string param)
    {
        if (param == controller.landParam)
        {
            curEvent = Side.Bottom;
        }
        else if (param == controller.jumpParam)
        {
            curEvent = Side.Top;
        }
        else if (param == controller.bonkLeftParam)
        {
            curEvent = Side.Left;
        }
        else if (param == controller.bonkRightParam)
        {
            curEvent = Side.Right;
        }
        else if (param == controller.cantWalkLeftParam)
        {
            curEvent = Side.Right;
        }
        else if (param == controller.cantWalkRightParam)
        {
            curEvent = Side.Left;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(col2D.bounds.center, col2D.bounds.size);
        Gizmos.DrawLine(col2D.bounds.center + new Vector3(0, col2D.bounds.extents.y, 0), col2D.bounds.center - new Vector3(0, col2D.bounds.extents.y, 0));
        Gizmos.DrawLine(col2D.bounds.center + new Vector3(col2D.bounds.extents.x, 0, 0), col2D.bounds.center - new Vector3(col2D.bounds.extents.x, 0, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(pivotPos + Vector3.up * .2f, pivotPos - Vector3.up * .2f);
        Gizmos.DrawLine(pivotPos + Vector3.right * .2f, pivotPos - Vector3.right * .2f);
    }
}
