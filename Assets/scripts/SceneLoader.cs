using UnityEngine;
using UnityEngine.SceneManagement; // Importujemy przestrzeñ nazw do zarz¹dzania scenami

public class SceneLoader : MonoBehaviour
{
    // Ta funkcja bêdzie wywo³ywana przez przycisk. Publiczna i void, jak wymaga tego Unity [citation:10].
    public void LoadSceneByName(string sceneName)
    {
        Debug.Log("£adujê scenê: " + sceneName); // To pomo¿e nam sprawdziæ, czy wszystko dzia³a
        SceneManager.LoadScene(sceneName); // Funkcja ³aduj¹ca scenê [citation:10]
    }
}