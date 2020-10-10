using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalObject : MonoBehaviour {
    // GlobalObjects are moved between scenes as they hit objects from various scenes
    private void OnCollisionEnter(Collision collision) {
        Scene sceneOfContact = collision.collider.gameObject.scene;
        SceneManager.MoveGameObjectToScene(gameObject, sceneOfContact);
    }
}
