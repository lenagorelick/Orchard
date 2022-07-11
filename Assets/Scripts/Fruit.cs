using System;
using System.Numerics;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Sequence = DG.Tweening.Sequence;
using Vector3 = UnityEngine.Vector3;


namespace Orchard
{
    
    public class Fruit : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip growSound;
        [SerializeField] private float growDuration = 0.3f;
        
        

        [SerializeField] private float dragSpeed = 20;
        [SerializeField] private float detachSpeed = 50;
        [SerializeField] private float fallSpeed = 30;
        [SerializeField] private float detachThreshold = 1;
        [SerializeField] private float shakeRadius = 0.02f;
        

        private Vector3 dragOffset;
        private Camera cam;
        public TreeManager MyTree;
        public Transform floorTransform;


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

        private State myState;
        private Vector3 spawnPosition;
        
        public void Grow()
        {

            myState = State.Grow;
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, growDuration).SetEase(Ease.OutBack).OnComplete(delegate { myState = State.Rest; });
            PlayGrowSound();
            spawnPosition = transform.position;



        }

        private void PlayGrowSound()
        {
            // sound for spawn
            audioSource.clip = growSound;
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            float fromSeconds = Random.Range(0, growSound.length - growDuration);
            PlaySoundInterval(fromSeconds);

        }
        
        private void PlaySoundInterval(float fromSeconds)
        {
            audioSource.time = fromSeconds;
            audioSource.Play();
            audioSource.SetScheduledEndTime( AudioSettings.dspTime + growDuration);
            
        }
        
        
        void Awake() 
        {
            cam = Camera.main;
        }

        void OnMouseDown()
        {
            myState = State.Shake;
            dragOffset = transform.position - GetMousePos();
        }

        private void OnMouseUp()
        {
            Debug.Log("mouse up");
            if (myState == State.Shake)
            {
                Rest();    
            }
            if (myState == State.Drag || myState==State.Detach)
            {
                myState = State.Fall;
            }
            
        }

        void Rest()
        {
            transform.DOMove(spawnPosition, 0.2f);
            myState = State.Rest;
            MyTree.Unreach();
        }
        void OnMouseDrag()
        {
            var fruitTargetPosition = GetMousePos() + dragOffset;
            if (myState == State.Shake)
            {
                if (Vector3.Distance(fruitTargetPosition, spawnPosition) > detachThreshold)
                {
                    myState = State.Detach;
                    MyTree.Unreach();
                }
            }

            if (myState == State.Drag)
            {
                transform.position = Vector3.MoveTowards(transform.position, fruitTargetPosition,
                    dragSpeed * Time.deltaTime);
            }
            if (myState == State.Detach)
            {
                transform.position = Vector3.MoveTowards(transform.position, fruitTargetPosition,
                    detachSpeed * Time.deltaTime);
                if (Vector3.Distance(fruitTargetPosition, transform.position) < 0.01f)
                {
                    myState = State.Drag;
                }
            }
        }

        Vector3 GetMousePos() 
        {
            var mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            return mousePos;
        }

        void Update()
        {
            if (myState == State.Shake)
            {
                Shake();
                MyTree.Reach(GetMousePos());
                
            }

            if (myState == State.Fall)
            {
                Fall();

            }
            

        
        }

        void Shake()
        {
           
            float currShakeRadius = shakeRadius * Vector3.Distance(GetMousePos() + dragOffset, spawnPosition)/ detachThreshold;
            var p2 = Random.insideUnitCircle * currShakeRadius;
            var d = Vector3.Lerp(spawnPosition, GetMousePos(), 0.2f);
            transform.position = new Vector3(p2.x,p2.y,0) + d;
        }

        void Fall()
        {
            
            var p = transform.position;
            Vector3 fallPosition = new Vector3(p.x, Mathf.NegativeInfinity, p.z);
            transform.position += fallSpeed * Vector3.down * Time.deltaTime;

            if (transform.position.y < floorTransform.position.y)
            {
                Die();
            }
            
        }

        private void Die()
        {
            
            myState = State.Die;
            MyTree.Dispawn(this);
            float y = transform.position.y;
            Sequence sequence =  DOTween.Sequence();
            sequence.Append(transform.DOMoveY(y+0.7f, 0.2f).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOMoveY(y, 0.2f).SetEase(Ease.InQuad));

            float direction = Mathf.Sign(Random.Range(-1,1));
            transform.DOMoveX(transform.position.x + 0.7f*direction, 0.4f);


        }
    }
}