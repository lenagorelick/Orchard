using System;
using System.Numerics;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using Sequence = DG.Tweening.Sequence;
using Vector3 = UnityEngine.Vector3;


namespace Orchard
{
    
    public class Fruit : MonoBehaviour
    {
        // serialized properties
        [Tooltip("Ref to audio source for playing sound effects.")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Ref to grow sound effect.")]
        [SerializeField] private AudioClip growSound;
        [Tooltip("Ref to detach-from-tree sound effect.")]
        [SerializeField] private AudioClip detachSound;
        [Tooltip("Ref to fall sound effect.")]
        [SerializeField] private AudioClip fallSound;
        [Tooltip("Duration of fruit grow phase.")]
        [SerializeField] private float growDuration = 0.3f;
        [Tooltip("How fast fruit detaches from tree.")]
        [SerializeField] private float detachSpeed = 50;
        [Tooltip("How fast fruit follows the mouse when dragged.")]
        [SerializeField] private float dragSpeed = 20;
        [Tooltip("How fast fruit falls.")]
        [SerializeField] private float fallSpeed = 30;
        [Tooltip("Distance between mouse and fruit that causes detachment.")]
        [SerializeField] private float detachThreshold = 1;
        [Tooltip("Strength of shake when detaching from tree.")]
        [SerializeField] private float shakeRadius = 0.02f;
        [Tooltip("Reference to splash particle system")]
        [SerializeField] private ParticleSystem splashParticleSystem;
        

        // ref to the tree that holds the fruit
        public TreeManager MyTree;
        // ref to the location of the floor
        public Transform floorTransform;
        
        // fruit offset with repsect to mouse click
        private Vector3 dragOffset;
        // ref to camera
        private Camera cam;
        // ref to fruit spawning position
        private Vector3 spawnPosition;


        // states of a fruit
        enum State
        {
            Grow,
            Rest,
            Shake,
            Detach,
            Drag,
            Fall,
            Die
        }
        // fruit state
        private State myState;
        
        
        /// <summary>
        /// Grows the fruit gradually:
        /// Sets the sprite for the fruit
        /// Plays grow sound
        /// Assigns color to splash
        /// 
        /// </summary>
        /// <param name="fruitSprite"></param>
        /// <param name="fruitSplashColor"></param>
        public void Grow(Sprite fruitSprite, Color fruitSplashColor)
        {

            // change state
            myState = State.Grow;
            // assign sprite
            this.GetComponent<SpriteRenderer>().sprite = fruitSprite;
            // gradually change the scale of fruit
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, growDuration).SetEase(Ease.OutBack).OnComplete(delegate { myState = State.Rest; });
            // play grow sound
            PlaySound(growSound);
            // remember spawn position
            spawnPosition = transform.position;
            // assign color to the splash particle system
            var main = splashParticleSystem.main;
            main.startColor = fruitSplashColor;

        }

        /// <summary>
        /// Plays a given sound clip
        /// Randomizes the pitch
        /// </summary>
        /// <param name="soundClip"></param>
        private void PlaySound(AudioClip soundClip)
        {
            
            audioSource.clip = soundClip;
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.Play();

        }
        
        /// <summary>
        /// Awake: keeps camera position
        /// </summary>
        void Awake() 
        {
            
            cam = Camera.main;
            
        }

        /// <summary>
        /// What happens when mouse clicks on the fruit
        /// If the fruit is resting on the tree:
        /// sets the dragOffset and changes the state to shaking
        /// </summary>
        void OnMouseDown()
        {
            
            // if fruit is resting on the tree
            if (myState == State.Rest)
            {
                myState = State.Shake;
                dragOffset = transform.position - GetMousePos();
            }
            
        }

        /// <summary>
        /// What happens when mouse lets go of the fruit
        /// If the state is shaking, then the fruit did not detach and it goes back to rest on the tree
        /// If the state is drag or detach, the fruit will fall
        /// </summary>
        private void OnMouseUp()
        {
            
            // is the state is shaking, go back to resting on the tree
            if (myState == State.Shake)
            {
                Rest();    
            }
            
            // if the state is drag or detach, start falling
            if (myState == State.Drag || myState==State.Detach)
            {
                myState = State.Fall;
            }
            
        }

        /// <summary>
        /// Fruit goes back to resting on the tree at its original spawn location
        /// The parent tree stops reaching after the fruit and goes back to its original position 
        /// </summary>
        void Rest()
        {
            
            // gradually move fruit back to its original location
            transform.DOMove(spawnPosition, 0.2f);
            // change the state to rest
            myState = State.Rest;
            // tell the tree to stop reaching
            MyTree.Unreach(false);
            
        }
        
        /// <summary>
        /// What happens when the fruit is dragged
        /// </summary>
        void OnMouseDrag()
        {
            
            // this is where the fruit needs to be with respect to the mouse
            var fruitTargetPosition = GetMousePos() + dragOffset;
            // if the fruit is shaking
            if (myState == State.Shake)
            {
                // check the distance whether it is time to detach
                if (Vector3.Distance(fruitTargetPosition, spawnPosition) > detachThreshold)
                {
                    Detach();
                }
            }

            // if the fruit is dragging
            if (myState == State.Drag)
            {
                // keep dragging after the mouse gradually with drag speed
                transform.position = Vector3.MoveTowards(transform.position, fruitTargetPosition,
                    dragSpeed * Time.deltaTime);
            }
            
            
            // if the fruit is detaching
            if (myState == State.Detach)
            {
                // play detach sound
                PlaySound(detachSound);
                
                // keep dragging after the mouse gradually with fast spring like detach speed
                transform.position = Vector3.MoveTowards(transform.position, fruitTargetPosition,
                    detachSpeed * Time.deltaTime);
                
                // if close enough to the mouse 
                if (Vector3.Distance(fruitTargetPosition, transform.position) < 0.01f)
                {
                    // change the state to drag (that will affect its speed)
                    myState = State.Drag;
                }
            }
        }

        /// <summary>
        /// Detaches fruit from the tree
        /// Moves fruit into interactive layer, so it is in front of everything else
        /// Tell tree to stop reaching after the fruit
        /// </summary>
        private void Detach()
        {
            
            // change the state to detach
            myState = State.Detach;
            // change sprite layer
            this.GetComponent<SpriteRenderer>().sortingLayerName = "Interactive";
            // each new interaction is put in front of everything else
            this.GetComponent<SpriteRenderer>().sortingOrder = (int)Time.time;
            // stop tree from reaching after the fruit
            MyTree.Unreach(true);
            
        }
        
        /// <summary>
        /// Get mouse position in world coord
        /// </summary>
        /// <returns></returns>
        Vector3 GetMousePos() 
        {
            
            var mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            return mousePos;
            
        }

        /// <summary>
        /// Every frame do:
        /// if shake state - fruit keeps shaking and tree keeps reaching
        /// if fall state - fruit keeps falling
        /// </summary>
        void Update()
        {
            // shake
            if (myState == State.Shake)
            {
                Shake();
                MyTree.Reach(GetMousePos());
                
            }

            // fall
            if (myState == State.Fall)
            {
                Fall();

            }
        
        }

        /// <summary>
        /// fruit simulates shaking
        /// </summary>
        void Shake()
        {
           
            // shake radius is larger as the distance between the spawn position and the mouse grows
            // normalized by detach threshold
            float currShakeRadius = shakeRadius * Vector3.Distance(GetMousePos() + dragOffset, spawnPosition)/ detachThreshold;
            // shake randomly around within shake radius
            var p2 = Random.insideUnitCircle * currShakeRadius;
            // shake closer to the mouse - this is vecotr in the direction of the mouse
            var d = Vector3.Lerp(spawnPosition, GetMousePos(), 0.2f);
            // shake closer to the mouse
            transform.position = new Vector3(p2.x,p2.y,0) + d;
            
        }

        /// <summary>
        /// Fruit falls down until it hits abstract floor location
        /// </summary>
        void Fall()
        {
            
            // calculate the fall target position under the curr fruit position
            var p = transform.position;
            Vector3 fallPosition = new Vector3(p.x, Mathf.NegativeInfinity, p.z);
            // fall with falling speed
            transform.position += fallSpeed * Vector3.down * Time.deltaTime;

            // check if we reached the height of the floor
            if (transform.position.y < floorTransform.position.y)
            {
                // fruit "dies"
                Die();
            }
            
        }

        /// <summary>
        /// Fruit dies after falling:
        /// disconnect the particle effect from the fruit, so the effect stays while fruit bounces
        /// play particle effect
        /// change state to die
        /// tell tree to forget about the fruit
        /// bounce the fruit
        /// play fall sound
        /// move the fruit into "fruits" sorting layer
        /// </summary>
        private void Die()
        {
            // disconnect the particle system from the parent
            splashParticleSystem.transform.parent = null;
            // play splash effect
            splashParticleSystem.Play();
            // change the state of the fruit
            myState = State.Die;
            // remove fruit from the tree
            MyTree.Dispawn(this);
            
            // perform bouncing effect
            float y = transform.position.y;
            Sequence sequence =  DOTween.Sequence();
            // first bounce up
            sequence.Append(transform.DOMoveY(y+0.7f, 0.2f).SetEase(Ease.OutQuad));
            // then bounce down
            sequence.Append(transform.DOMoveY(y, 0.2f).SetEase(Ease.InQuad));
            // bounce sideways randomly
            float direction = Mathf.Sign(Random.Range(-1,1));
            transform.DOMoveX(transform.position.x + 0.7f*direction, 0.4f);
            // play sound
            PlaySound(fallSound);
            // this fruit goes back to "Fruits" sorting level
            this.GetComponent<SpriteRenderer>().sortingLayerName = "Fruits";

        }
    }
}