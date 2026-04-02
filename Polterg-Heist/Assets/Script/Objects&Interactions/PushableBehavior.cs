using UnityEngine;

/// But: Pousser un objet
/// Requiert Externe: tag "Player"
/// Input: E = pousser
/// État: À travailler
public class PushableBehavior : MonoBehaviour
{
    //Variables
    [Header("Variables")]
    [SerializeField] public float distanceFromPlayer;       //La distance fixe entre Player et l'objet poussable
    float stockMaxSpeed;                                    //Garde en mémoire la vitesse initiale du PlayerController
    Vector3 positionPlayer;                                 //Regarde la position du Player

    //Conditions
    bool isPushed;                                          //Condition pour enclencher le déplacement de l'objet poussable

    //Bouge la position de l'objet poussable
    void Update()
    {
        if (isPushed)
        {
            Vector3 _position = transform.position;
            _position.x = positionPlayer.x + distanceFromPlayer;
            transform.position = _position;
        }
    }

    //Pour déterminer la position de l'objet poussable par rapport au Player
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            distanceFromPlayer = Mathf.Abs(distanceFromPlayer);
            if (transform.position.x < collision.transform.position.x)
            {
                distanceFromPlayer = -distanceFromPlayer;
            }
            stockMaxSpeed = collision.GetComponent<PlayerController>().maxSpeed;
        }
    }

    //Pour donner la permission de pousser
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            positionPlayer = collision.transform.position;
            if (Input.GetKey(KeyCode.E) && !collision.GetComponent<PlayerController>().isPossessing)
            {
                collision.GetComponent<PlayerController>().maxSpeed = stockMaxSpeed / 2f;
                isPushed = true;
            }
            else
            {
                collision.GetComponent<PlayerController>().maxSpeed = stockMaxSpeed;
                isPushed = false;
            }
        }
    }
}
