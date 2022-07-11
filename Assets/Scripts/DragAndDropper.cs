using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DragAndDropper : MonoBehaviour {

    private Vector3 _dragOffset;
    private Camera _cam;

    [SerializeField] private float _speed = 50;

    void Awake() {
        _cam = Camera.main;
    }

    void OnMouseDown() {
        _dragOffset = transform.position - GetMousePos();
    }

    void OnMouseDrag() {
        transform.position = Vector3.MoveTowards(transform.position, GetMousePos() + _dragOffset, _speed * Time.deltaTime) ;
    }

    Vector3 GetMousePos() {
        var mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        return mousePos;
    }

    private void OnMouseUp()
    {
        Vector3 bottomPos = new Vector3();
        bottomPos.x = transform.position.x;
        bottomPos.y = 0;
        Debug.Log("dropping down");
        transform.DOMove(bottomPos, 3.0f);
        
    }
}
