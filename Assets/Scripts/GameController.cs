using UnityEngine;
using PolyReduction;

namespace Assets.Scripts
{
    public class GameController : MonoBehaviour
    {
        public void Start()
        {
            Debug.Log("Start");
            PolyReducer polyReducer = new PolyReducer();
            polyReducer.InitModel();
        }
    }
}
