using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldButton : Button
{
    public UnityEvent onPointerDown;
    public UnityEvent onPointerUp;
    public UnityEvent whilePointerPressed;

    public Button button;

    new void Awake()
    {
        button = GetComponent<Button>();
    }

    IEnumerator<Coroutine> WhilePressed()
    {
        // Loops forever until the mouse is released
        while (true)
        {
            whilePointerPressed?.Invoke();
            onClick?.Invoke();
            yield return null;
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        StopAllCoroutines();
        StartCoroutine(WhilePressed());

        base.OnPointerDown(eventData);
        onPointerDown?.Invoke();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        StopAllCoroutines();
        base.OnPointerUp(eventData);
        onPointerUp?.Invoke();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        base.OnPointerExit(eventData);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
    }
}