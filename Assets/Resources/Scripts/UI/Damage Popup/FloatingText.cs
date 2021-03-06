﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingText : MonoBehaviour {

    Color critColor;

    public Animator animator;
    Text damageText;
    public Transform location;
    float clipLen; 
    public float bonusX, bonusY;

    void Awake()
    {
        critColor = new Color(0, 155, 255);
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        clipLen = clipInfo[0].clip.length;
        damageText = animator.GetComponent<Text>();
        Destroy(gameObject, clipLen);
        bonusX = Random.Range(-0.5f, 0.5f);
        bonusY = Random.Range(0f, 0.6f);
    }

    void LateUpdate()
    {
        if (location != null)
        gameObject.transform.position = new Vector2 (location.position.x + bonusX, location.position.y + bonusY);
    }

    public void SetCritColor()
    {
        damageText.color = critColor;
    }

    public void SetText(string text)
    {
        damageText.text = text;
    }

    public void SetTextColor(Color color)
    {
        damageText.color = color;
    }
}
