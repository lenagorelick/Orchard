using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

namespace Orchard
{

    public class TreeManager : MonoBehaviour
    {
        [SerializeField] private int maxNumFruits = 5;
        [SerializeField] private float minTimeBetweenSpawn = 1.0f;
        [SerializeField] private float maxTimeBetweenSpawn = 3.0f;
        [SerializeField] private GameObject fruitPrefab;
        [SerializeField] private BoxCollider2D treeCrownZone;
        
        private float fruitTimer = 0.0f;
        private float nextFruitTime = 0.0f;
        private List<Fruit> fruits = new List<Fruit>();
         

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (fruits.Count < maxNumFruits)
            {
                if (fruitTimer > nextFruitTime)
                {
                    
                    Shake();
                    SpawnFruit();
                    fruitTimer = 0;
                    nextFruitTime = Random.Range(minTimeBetweenSpawn, maxTimeBetweenSpawn);
                }

                fruitTimer += Time.deltaTime;

            }

        }

        
        private void SpawnFruit()
        {
            
            GameObject newFruitGameObject = Instantiate(fruitPrefab, this.transform);
            Fruit newFruit = newFruitGameObject.GetComponent<Fruit>();
            var randomPos = RandomPointInCrown(treeCrownZone.bounds);
            newFruit.transform.position = randomPos;
            fruits.Add(newFruit);
            // grow in size
            newFruit.Grow();

        }

        private void Shake()
        {
            // sounds for shake
        }

        private void Sigh()
        {
            
        }
        
        public static Vector3 RandomPointInCrown(Bounds colliderBounds) 
        {
            return new Vector3(
                Random.Range(colliderBounds.min.x, colliderBounds.max.x),
                Random.Range(colliderBounds.min.y, colliderBounds.max.y),
                Random.Range(colliderBounds.min.z, colliderBounds.max.z)
            );
        }
    }

}

