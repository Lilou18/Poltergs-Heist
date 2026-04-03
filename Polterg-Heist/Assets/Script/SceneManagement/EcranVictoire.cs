using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EcranVictoire : MonoBehaviour
{
   public void Retour()
   {
        SceneManager.LoadScene("LevelSelect");
   }
}
