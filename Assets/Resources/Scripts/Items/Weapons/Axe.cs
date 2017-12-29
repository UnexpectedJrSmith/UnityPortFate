﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Axe : Weapon {

    WeaponHitbox axeBlade;
    WeaponHitbox axeHandle;

    [SerializeField]
    float bladeDamage;
    [SerializeField]
    float handleDamage;

    public override WeaponTypes Type { get { return WeaponTypes.Axe; } }

    protected override void Awake()
    {
        base.Awake();
        axeBlade = transform.GetChild(0).GetComponent<WeaponHitbox>();
        axeHandle = transform.GetChild(1).GetComponent<WeaponHitbox>();
        axeBlade.DamageMultiplier = bladeDamage;
        axeHandle.DamageMultiplier = handleDamage;
    }


}
