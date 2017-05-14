﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageCalc : MonoBehaviour
{
    public static DamageCalc damageCalc;
    static Skill playerUseSkillCopy;
    public static bool enemyCanAttack = true;
    public static bool enemyDead = false;

    void Start()
    {
        damageCalc = gameObject.GetComponent<DamageCalc>();
    }

    public static int BasicDamageReduction(Stats user, Stats victim, Skill.SkillType skilltype, int damage)
    {
        int dmg;
        if (skilltype == Skill.SkillType.True)
        {
            dmg = (int)((damage * (user.dmgOutput.totalAmount / 100f)) * (victim.dmgTaken.totalAmount / 100f));
        }
        else if (skilltype == Skill.SkillType.Physical)
        {
            dmg = (int)((damage * (user.dmgOutput.totalAmount / 100f) - victim.armor.totalAmount) * (victim.dmgTaken.totalAmount / 100f));
        }
        else // magical
        {
            dmg = (int)((damage * (user.dmgOutput.totalAmount / 100f) - victim.resist.totalAmount) * (victim.dmgTaken.totalAmount / 100f));
        }
        if (dmg <= 0)
        {
            dmg = 0;
        }
        return dmg;
    }

    public static int DmgModifier(Stats user, Stats victim, Skill skill)
    {
        int dmg;
        dmg = BasicDamageReduction(user, victim, skill.skillType, skill.skillDamage);
        Transform who;
        if (user == PlayerStats.stats)
        {
            who = BattleUI.playerStatus;
        }
        else
        {
            who = BattleUI.enemyStatus;
        }
        foreach (Transform status in who)
        {
            if (status.GetComponent<StatusHolder>().skill.skillID != -1)
            {
                Skill statusSkill = status.GetComponent<StatusHolder>().skill;
                switch (statusSkill.skillID)
                {
                    case 7:
                        {
                            if (skill.skillType == Skill.SkillType.Magical)
                            {
                                dmg = (int)(dmg * (statusSkill.skillDamage / 100f));
                                BattleUI.TextAdd(user, 18, string.Format("{0} is charged from {1}", skill.skillName, statusSkill.skillName));
                                BattleUI.RemoveActive(who, statusSkill.skillID);
                            }
                            break;
                        }
                }
            }
        }
        return dmg;
    }

    public static int HitChanceModifier(Stats user, Stats victim, Skill skill)
    {
        int hit = user.hitChance.totalAmount + skill.skillHitChance - victim.dodgeChance.totalAmount;
        return hit;
    }

    public static int CritChanceModifier(Stats user, Stats victim, Skill skill)
    {
        int crit = user.critChance.totalAmount + skill.skillCritChance;
        return crit;
    }

    public static void CheckSkillStatusEff(Stats victim, Skill skill)
    {
        foreach (Status status in skill.skillStatusEff.statusList)
        {
            if (status.statusChance > Random.Range(0, 101))
            {
                BattleUI.AddStatus(victim, status);
                switch (status.statusID)
                {
                    case 0:
                        {
                            BattleUI.TextAdd(victim, 26, "red", string.Format("has been Burned!"));
                            break;
                        }
                    case 1:
                        {
                            BattleUI.TextAdd(victim, 26, "yellow", string.Format("became Paralyzed!"));
                            break;
                        }
                    case 6:
                        {
                            BattleUI.TextAdd(victim, 26, "orange", string.Format("became Confused!"));
                            break;
                        }
                }
            }

        }
        //BattleUI.UpdateAllStatusHolder(victim);
    }

    public static IEnumerator ApplyStatusEff(Stats user, Stats victim, Status status, bool last)
    {
        if (status.statusID != -1)
        {
            switch (status.statusID)
            {
                case 0:
                    {
                        SoundDatabase.PlaySound(35);
                        int damage = (victim.maxHealth.totalAmount / 10 - Random.Range(0, victim.maxHealth.totalAmount / 100))
                            + Random.Range(0, 10 + victim.maxHealth.totalAmount / 80);
                        victim.health -= damage;
                        BattleUI.TextAdd(victim, 26, "red", string.Format("took {0} burn damage", damage));
                        BattleUI.UpdateEnemySliders();
                        StatusBar.UpdateSliders();
                        yield return new WaitForSeconds(1.1f);
                        break;
                    }
                case 1:
                    {
                        SoundDatabase.PlaySound(37);
                        BattleUI.TextAdd(victim, 26, "yellow", string.Format("is Paralyzed"));
                        yield return new WaitForSeconds(1.1f);
                        if (Random.Range(0, 101) >= 50)
                        {
                            SoundDatabase.PlaySound(37);
                            BattleUI.TextAdd(victim, 26, "yellow", string.Format("couldn't move!"));
                            if (!last)
                                yield return new WaitForSeconds(1.1f);
                            enemyCanAttack = false;
                        }
                        break;
                    }
                case 6:
                    {
                        SoundDatabase.PlaySound(46);
                        BattleUI.TextAdd(victim, 26, "orange", string.Format("is Confused"));
                        yield return new WaitForSeconds(1.1f);
                        if (Random.Range(0, 101) >= 50)
                        {
                            int damage = victim.physAtk.totalAmount / 2;
                            victim.health -= damage;
                            BattleUI.TextAdd(victim, 26, "red", string.Format("hurt itself for {0} True Damage", damage));
                            BattleUI.UpdateEnemySliders();
                            StatusBar.UpdateSliders();
                            SoundDatabase.PlaySound(-1);
                            if (!last)
                                yield return new WaitForSeconds(1.1f);
                            enemyCanAttack = false;
                        }
                        break;
                    }
            }
        }
    }

    public static bool IsSkill(Transform holder)
    {
        bool isSkill = false;
        if (holder.GetComponent<StatusHolder>().skill.skillID != -1)
        {
            isSkill = true;
        }
        else
        {
            isSkill = false;
        }
        return isSkill;
    }

    public static void SkillModifier(Stats user, Stats victim, Skill skill)
    {
        // this is should be used for attacking skills, if an attacking skill would have a cooldown, would need to find it in the player skill list and give it as it would only give the copied skill the cooldown.
        Transform who;
        Transform victimWho;
        if (user == PlayerStats.stats)
        {
            who = BattleUI.playerStatus;
            victimWho = BattleUI.enemyStatus;
        }
        else
        {
            victimWho = BattleUI.playerStatus;
            who = BattleUI.enemyStatus;
        }
        foreach (Transform status in who)
        {

            if (IsSkill(status))
            {
                Skill statusSkill = status.GetComponent<StatusHolder>().skill;
                switch (statusSkill.skillID)
                {
                    case 18:
                        {
                            skill.skillDamage += statusSkill.skillDamage;
                            skill.skillStatusEff.AddStatusChance(1, statusSkill.skillHitChance);
                            skill.skillCritChance += statusSkill.skillCritChance;
                            skill.skillManaCost = (int)(skill.skillManaCost * (statusSkill.skillCritMulti / 100f));
                            break;
                        }
                    case 32:
                        {
                            skill.skillStatusEff.AddPercentStatusChance(0, statusSkill.skillManaCost);
                            break;
                        }
                    case 33:
                        {
                            if (skill.skillStatusEff.GetStatusChance(6) != 0)
                            {
                                skill.skillDamage += (int)(skill.skillDamage * (skill.skillStatusEff.GetStatusChance(6) / 100f * 8));
                                skill.skillStatusEff.SetStatusChance(6, 0);
                            }
                            break;
                        }
                }
            }
            //else
            //{
            //    Status status = status.GetComponent<StatusHolder>().status;
            //}

        }
        foreach (Transform status in victimWho) // if opponent has debuff
        {
            if (IsSkill(status))
            {

            }
            else
            {
                Status statusSkill = status.GetComponent<StatusHolder>().status;
                switch (statusSkill.statusID)
                {
                    case 8:
                        {
                            if (skill.skillID == 4)
                            {
                                BattleUI.RemoveStatus(victimWho, 8);
                                skill.skillDamage = (int)(skill.skillDamage * (1 + PlayerSkills.FindSkill(22).skillHitChance / 100f));
                                BattleUI.TextAdd(user, 17, "yellow", string.Format("dealt more damage from {0}", PlayerSkills.FindSkill(22).skillName));
                            }
                            break;
                        }
                }
            }
        }
    }


    public static void AttackingStatusEffectActivations(Stats user, Stats victim, int dmg)
    {
        Transform who;
        if (user == PlayerStats.stats)
        {
            who = BattleUI.playerStatus;
        }
        else
        {
            who = BattleUI.enemyStatus;
        }
        foreach (Transform status in who)
        {
            Skill statusSkill = status.GetComponent<StatusHolder>().skill;
            switch (statusSkill.skillID)
            {
                case 17:
                    {
                        int damageWillTake = (int)(dmg * (statusSkill.skillCritChance / 100f));
                        int reduced = BasicDamageReduction(user, user, Skill.SkillType.True, damageWillTake); // this case where the damage output and taken is the user
                        user.health -= reduced;
                        BattleUI.TextAdd(user, 17, "green", string.Format("took {0} True damage from {1}", reduced, statusSkill.skillName));
                        break;
                    }
            }
        }       
    }

    public static void AttackingPassiveEffectActivations(Stats user, Stats victim, Skill skill)
    {
        for (int i = 0; i < PlayerSkills.learnedSkills.Count; i += 1)
        {
            foreach (Skill passiveSkill in PlayerSkills.learnedSkills[i])
            {
                if (passiveSkill.skillType == Skill.SkillType.Passive)
                {
                    switch (passiveSkill.skillID)
                    {
                        case 8:
                            {
                                if (Random.Range(0,100) < passiveSkill.skillManaCost)
                                {
                                    int manaHeal = (int)(skill.skillManaCost * (passiveSkill.skillCritChance / 100f));
                                    user.HealMP(manaHeal);
                                    BattleUI.TextAdd(user, 17, "black", string.Format("gained {0} Mana from {1}", manaHeal, passiveSkill.skillName));
                                }
                                break;
                            }
                        case 22:
                            {
                                if (skill.skillID == 2 && Random.Range(0, 100) < passiveSkill.skillDamage)
                                {
                                    BattleUI.AddStatus(victim, new Status(StatusDatabase.GetStatus(8)));
                                    BattleUI.TextAdd(user, 17, "blue", string.Format("applied Soaked debuff to enemy"));
                                }
                                break;
                            }
                    }
                }
            }
        }
    }

    public static void FinalCalculationModifier(Stats user, Stats victim, Skill.SkillType skilltype, int dmg)
    {
        Transform who;
        bool doNormalCalculation = true;
        // defending status effects
        if (victim == PlayerStats.stats)
        {
            who = BattleUI.playerStatus;
        }
        else
        {
            who = BattleUI.enemyStatus;
        }
        // defensive damage calculation priorities
        List<Transform> userStatusCount = new List<Transform>();
        foreach (Transform status in who)
        {
            userStatusCount.Add(status);
        }
        // priority 1
        // shield
        if (StatusBar.hasShield)
        {
            if (victim.shield > 0)
            {
                // shield breaks
                if (victim.shield - dmg < 0)
                {
                    int willBeTaking = dmg - victim.shield;
                    victim.health -= willBeTaking;
                    BattleUI.TextAdd(user, 17, "black", string.Format("broke the shield for {0} {1} damage", victim.shield, skilltype));
                    if (userStatusCount.Exists(status => status.GetComponent<StatusHolder>().skill.skillID == 5))
                    {
                        ManaGaurdCalc(user, victim, willBeTaking, skilltype);
                    }
                    else
                    {
                        BattleUI.TextAdd(user, 17, "black", string.Format("deals {0} {1} damage", willBeTaking, skilltype));
                    }
                    StatusBar.LoseShield();
                }
                else
                {
                    victim.shield -= dmg;
                    BattleUI.TextAdd(user, 20, "black", string.Format("dealt {0} damage to shield", dmg));
                    if (victim.shield == 0)
                    {
                        StatusBar.LoseShield();
                    }
                }
                StatusBar.UpdateShield();
                doNormalCalculation = false;
            }
        }
        else if (userStatusCount.Exists(status => status.GetComponent<StatusHolder>().skill.skillID == 5))
        {
            ManaGaurdCalc(user, victim, dmg, skilltype);
            doNormalCalculation = false;
        }
        //foreach (Transform status in who)
        //{
        //    if (status.GetComponent<StatusHolder>().status.statusID != -1)
        //    {
        //        //switch (status.GetComponent<StatusHolder>().status.statusID)
        //        //{
        //        //    case 0:
        //        //        {
        //        //            break;
        //        //        }
        //        //}
        //    }
        //}
        if (doNormalCalculation)
        {
            victim.health -= dmg;
            if (user == PlayerStats.stats)
            {
                BattleUI.TextAdd(user, 25, "black", string.Format("deal {0} {1} damage", dmg, skilltype));
            }
            else
            {
                BattleUI.TextAdd(user, 25, "black", string.Format("deals {0} {1} damage", dmg, skilltype));
            }
        }
    }

    public static IEnumerator TurnEndChecker(Stats user, Stats victim)
    {
        Transform who;
        // check the duration end on battle status
        if (user == PlayerStats.stats)
        {
            who = BattleUI.playerStatus;
        }
        else
        {
            who = BattleUI.enemyStatus;
        }
        foreach (Transform status in who)
        {
            // duration is over
            if (status.GetComponent<StatusHolder>().turnEnd == Battle.turnCount)
            {
                // lose bonus effects
                yield return new WaitForSeconds(1.1f);
                SoundDatabase.PlaySound(41);
                LoseStatusBonus(user, victim, status.GetComponent<StatusHolder>());
                BattleUI.TextAdd(user, 25, string.Format("<color=red>{0} wore off</color>", status.GetComponent<StatusHolder>().skill.skillName));
                Destroy(status.gameObject);
            }
        }
        // check each skill in learned skill if cooldown is ended
        int i = 0;
        foreach (List<Skill> page in PlayerSkills.learnedSkills)
        {
            foreach (Skill aSkill in PlayerSkills.learnedSkills[i])
            {
                if (aSkill.skillID != -1)
                {
                    if (Battle.turnCount == aSkill.skillCooldownEnd)
                    {
                        aSkill.skillOnCooldown = false; // removing cooldown in skills
                    }
                }
            }
            i += 1;
        }
    }

    public static void LoseStatusBonus(Stats user, Stats victim, StatusHolder statusholder)
    {
        // status
        // skills id
        Skill skill = statusholder.skill;
        switch (skill.skillID)
        {
            case 13:
                {
                    StatusBar.LoseShield();
                    break;
                }
            case 15:
                {
                    user.armor.buffedAmount -= skill.skillCritChance;
                    user.resist.buffedAmount -= skill.skillCritChance;
                    break;
                }
            case 16:
                {
                    user.armor.buffedAmount -= skill.skillCritChance * skill.skillHitChance;
                    user.resist.buffedAmount -= skill.skillCritChance * skill.skillHitChance * 2;
                    break;
                }
            case 17:
                {
                    user.manaComs.buffedAmount -= skill.skillDamage;
                    user.dmgOutput.buffedAmount -= skill.skillHitChance;
                    user.dmgTaken.buffedAmount -= skill.skillHitChance;
                    break;
                }
            case 31:
                {
                    user.armor.buffedAmount -= skill.skillCritChance;
                    user.resist.buffedAmount -= skill.skillCritMulti;
                    break;
                }
            case 34:
                {
                    user.dodgeChance.buffedAmount -= skill.skillDamage;
                    user.critChance.buffedAmount -= skill.skillHitChance;
                    break;
                }
        }
        user.SimpleStatUpdate();
    }

    public static void LoseAllPlayerStatusEffects()
    {
        foreach (Transform status in BattleUI.playerStatus)
        {
            LoseStatusBonus(PlayerStats.stats, EnemyHolder.enemy.stats, status.GetComponent<StatusHolder>());
            Destroy(status.gameObject);
        }
    }

    public static void SkillActive(Stats user, Stats victim, Skill skill)
    {
        Transform who;
        Transform whoVictim;
        if (user == PlayerStats.stats)
        {
            who = BattleUI.playerStatus;
            whoVictim = BattleUI.enemyStatus;
        }
        else
        {
            whoVictim = BattleUI.playerStatus;
            who = BattleUI.enemyStatus;
        }
        BattleUI.TextAdd(user, 30, "#000000ff", "used " + skill.skillName);
        user.mana -= skill.skillManaCost;
        // actives that dont add status pic
        List<int> dontAddToStatus = new List<int>(new int[] {6, 16, 17});
        if (!dontAddToStatus.Exists(id => id == skill.skillID))
        {
            BattleUI.AddStatus(user, skill);
        }
        // activate effect at activation of skill
        switch (skill.skillID)
        {
            case 6: // resotre
                {
                    user.HealHP(skill.skillDamage);
                    BattleUI.TextAdd(user, 30, "red", string.Format("healed for {0} HP", skill.skillDamage));
                    BattleUI.AddCooldown(skill);
                    break;
                }
            case 13:
                {
                    user.shield += skill.skillDamage;
                    StatusBar.SetNewShield();
                    BattleUI.TextAdd(user, 30, "orange", string.Format("gained a {0} HP Shield", skill.skillDamage));
                    break;
                }
            case 15:
                {
                    user.armor.buffedAmount += skill.skillCritChance;
                    user.resist.buffedAmount += skill.skillCritChance;
                    user.SimpleStatUpdate();
                    break;
                }
            case 16:
                {
                    skill.skillCritChance = 0;
                    int healAmount = 0;
                    foreach(Transform status in who)
                    {
                        skill.skillCritChance += 1;
                    }
                    LoseAllPlayerStatusEffects();
                    healAmount = skill.skillCritChance * skill.skillDamage;
                    user.HealHP(healAmount);
                    user.armor.buffedAmount += skill.skillCritChance * skill.skillHitChance;
                    user.resist.buffedAmount += skill.skillCritChance * skill.skillHitChance * 2;
                    BattleUI.TextAdd(user, 30, "red", string.Format("healed for {0} HP", healAmount));
                    user.SimpleStatUpdate();
                    BattleUI.AddStatus(user, skill); // special case where we need to add the status after effect
                    break;
                }
            case 17:
                {
                    BattleUI.AddStatus(victim, skill);
                    victim.manaComs.buffedAmount += skill.skillDamage;
                    victim.dmgOutput.buffedAmount += skill.skillHitChance;
                    victim.dmgTaken.buffedAmount += skill.skillHitChance;
                    victim.SimpleStatUpdate();
                    break;
                }
            case 31:
                {
                    int healAmount = 0;
                    // Stored values are here
                    skill.skillCritChance = (int)(victim.physAtk.totalAmount * (skill.skillDamage / 100f));
                    skill.skillCritMulti = (int)(victim.magicAtk.totalAmount * (skill.skillDamage / 100f));
                    user.armor.buffedAmount += skill.skillCritChance;
                    user.resist.buffedAmount += skill.skillCritMulti;
                    healAmount = (int)(victim.health * (skill.skillHitChance / 100f));
                    user.HealHP(healAmount);
                    BattleUI.TextAdd(user, 30, "red", string.Format("healed for {0} HP", healAmount));
                    user.SimpleStatUpdate();
                    break;
                }
            case 34:
                {
                    user.dodgeChance.buffedAmount += skill.skillDamage;
                    user.critChance.buffedAmount += skill.skillHitChance;
                    break;
                }

        }
        SoundDatabase.PlaySound(skill.skillSoundID);
        BattleUI.UpdateEnemySliders();
        StatusBar.UpdateSliders();
    }

    public static void SkillAttack(Stats user, Stats victim, Skill skill)
    {
        SkillModifier(user, victim, skill);
        // Mana Cost
        BattleUI.TextAdd(user, 30, "#000000ff", "used " + skill.skillName);
        user.mana -= skill.skillManaCost;
        // Hit Chance
        int hitChance = HitChanceModifier(user, victim, skill);
        bool ifHit = hitChance >= Random.Range(0, 101);
        if (ifHit)
        {
            ////// Damage
            int damage = DmgModifier(user, victim, skill);
            // Apply Status effects ex. Burn
            CheckSkillStatusEff(victim, skill);
            // Crit Chance
            int critChance = CritChanceModifier(user, victim, skill);
            //// Check if Crit
            bool didCrit = critChance >= Random.Range(0, 101);
            if (didCrit)
            {
                SoundDatabase.PlaySound(11);
                BattleUI.TextAdd(user, 30, "cyan", "critically striked!");
                //// Crit Multiplier
                int bonus = (user.critMulti.totalAmount + skill.skillCritMulti);
                float calcBonus = bonus / 100f;
                float putBonus = damage * calcBonus;
                damage = (int)(putBonus);
            }
            ////// Damage Calculation
            FinalCalculationModifier(user, victim, skill.skillType, damage);
            AttackingStatusEffectActivations(user, victim, damage);
            AttackingPassiveEffectActivations(user, victim, skill);
            // End
            SoundDatabase.PlaySound(skill.skillSoundID);
            //user.SimpleStatUpdate();
        }
        else // missed
        {
            BattleUI.TextAdd(user, 30, "blue", "missed!");
            SoundDatabase.PlaySound(0);
        }
        BattleUI.UpdateEnemySliders();
        StatusBar.UpdateSliders();
        // Crit Chance
        //bool ifHit = StatUtilities.
    }


    public static IEnumerator StartBattle(Stats player, Stats enemy, Skill playeruseskill) // once we know what skill the player wants to use, we can start a battle.
    {        
        BattleUI.battling.gameObject.SetActive(true);
        BattleUI.NextTurn();
        BattleUI.ResetScrollsPosition();
        // disable all images as we need the skill page to be active
        SkillPageImagesOn(false);
        // disable all buttons (basically cant do anything
        GameManager.InvisibleWallOn(true);
        // reset text
        BattleUI.TextReset();
        //////////////////
        // battle
        // speed calculation goes here
        playerUseSkillCopy = new Skill(playeruseskill);
        Stats firstAttacker = player;
        Stats secondAttacker = enemy;
        if (playerUseSkillCopy.skillType == Skill.SkillType.Active)
        {
            SkillActive(firstAttacker, secondAttacker, playeruseskill);
        }
        else
        {
            SkillAttack(firstAttacker, secondAttacker, playerUseSkillCopy);
        }
        yield return new WaitForSeconds(1.1f);
        // checking if dead after first move
        if (enemy.IsDead())
        {
            yield return EnemyDeadAfter(player, enemy);
        }
        else
        {
            // apply status effects after first attack
            yield return AfterAttackStatusEffectApply(firstAttacker, player, enemy, playerUseSkillCopy);
            // enemy attack (second attack, will be changing)
            if (enemy.IsDead())
            {
                yield return EnemyDeadAfter(player, enemy);
            }
            else
            {
                if (enemyCanAttack)
                {
                    enemy.SimpleStatUpdate();
                    SkillAttack(secondAttacker, firstAttacker, EnemyHolder.enemy.skills[Random.Range(0, EnemyHolder.enemy.skills.Count)]);
                    // end turn effects
                    yield return EndTurnStatusEffects(BattleUI.playerStatus, player, enemy);
                }
                if (enemy.IsDead())
                {
                    yield return EnemyDeadAfter(player, enemy);
                }
                else
                {
                    yield return EndTurnStatusEffects(BattleUI.enemyStatus, player, enemy);
                }
            }
            yield return TurnEndChecker(player, enemy);
            yield return TurnEndChecker(enemy, player);
        }
        // return to normals
        SkillPageImagesOn(true);
        GameManager.InvisibleWallOn(false);
        if (SkillPage.skillPage.gameObject.activeInHierarchy)
        {
            GameManager.OpenClosePage("Skill Page");
        }
        enemyCanAttack = true;
        enemyDead = false;
        BattleUI.battling.gameObject.SetActive(false);
        // update after effects
        player.SimpleStatUpdate();
        PlayerSkills.SkillUpdate();
        EnemyHolder.enemy.UpdateSkills();
        
    }

    static IEnumerator AfterAttackStatusEffectApply(Stats whojustattacked, Stats player, Stats enemy, Skill playeruseskill)
    {
        Transform whoStatus;
        if (whojustattacked == PlayerStats.stats)
        {
            whoStatus = BattleUI.enemyStatus;
        }
        else
        {
            whoStatus = BattleUI.playerStatus;
        }
        foreach (Transform status in whoStatus)
        {
            bool lastStatus = false;
            if (status == whoStatus.GetChild(whoStatus.childCount - 1))
            {
                lastStatus = true;
            }
            yield return ApplyStatusEff(player, enemy, status.GetComponent<StatusHolder>().status, lastStatus);
            if (enemy.IsDead())
            {
                break;
            }
        }
    }

    static IEnumerator EndTurnStatusEffects(Transform who, Stats player, Stats enemy)
    {
        List<int> endTurnSkillIDs = new List<int>(new int[] {15});
        foreach (Transform status in who)
        {
            Skill statusSkill = status.GetComponent<StatusHolder>().skill;
            if (endTurnSkillIDs.Exists(id => id == statusSkill.skillID))
            {
                if (statusSkill.skillID != -1)
                {
                    switch (statusSkill.skillID)
                    {
                        case 15:
                            {
                                if (Random.Range(0, 101) <= statusSkill.skillHitChance)
                                {
                                    yield return new WaitForSeconds(1.1f);
                                    SoundDatabase.PlaySound(38);
                                    enemy.health -= statusSkill.skillDamage;
                                    BattleUI.TextAdd(enemy, 17, "red", string.Format("took {0} {1} damage from exploded Volcano Shield", statusSkill.skillDamage, statusSkill.skillType));
                                    CheckSkillStatusEff(enemy, statusSkill);
                                }
                                break;
                            }
                    }
                }
                BattleUI.UpdateEnemySliders();
            }
        }
    }

    static IEnumerator EnemyDeadAfter(Stats player, Stats enemy)
    {
        if (!GameManager.inTutorial)
        {
            BattleUI.TextAdd(enemy, 25, "black", string.Format("has been defeated!"));
            yield return new WaitForSeconds(1.1f);
            LoseAllPlayerStatusEffects();
            VictoryScreen.AddDetails(string.Format("You defeated {0}!", enemy.mingZi));
            VictoryScreen.AddDetails(string.Format("Cash: +{0}", enemy.cash));
            VictoryScreen.AddDetails(string.Format("EXP: +{0}", enemy.experience));
            ActivatePassiveEffectsEnemyDead(player);
            player.cash += enemy.cash;
            InvEq.UpdateCashText();
            player.experience += enemy.experience;
            StatusBar.UpdateSliders();
            if (player.experience >= player.maxExperience)
            {
                GameManager.OpenClosePage("Invisible Wall");
                GameManager.OpenClosePage("Level Up Screen");
                StatPage.OpenCloseCelebration(true);
                SoundDatabase.bgmSource.Pause();
                SoundDatabase.PlaySound(36);
                yield return new WaitForSeconds(1.75f);
                StatPage.OpenCloseCelebration(false);
                PlayerStats.LevelUp();
                StatPage.SetCurrentStats();
                StatPage.UpdateText();
            }
            GameManager.InvisibleWallOn(false);
            VictoryScreen.OpenCloseVictoryScreen();
        }
        else
        {
            BattleUI.TextAdd(enemy, 25, "black", string.Format("has been defeated!"));
            BattleUI.battling.gameObject.SetActive(false);
            yield return new WaitForSeconds(1.1f);
            SkillPageImagesOn(true);
            GameManager.InvisibleWallOn(false);
            Battle.EndBattle();
            Tutorial.battleDesc.gameObject.transform.parent.gameObject.SetActive(false);
            Tutorial.gotIt.gameObject.transform.parent.gameObject.SetActive(true);
            SoundDatabase.PlayMusic(0);
        }
    }


    static void ActivatePassiveEffectsEnemyDead(Stats player)
    {
        for (int i = 0; i < PlayerSkills.learnedSkills.Count; i += 1)
        {
            foreach (Skill skill in PlayerSkills.learnedSkills[i])
            {
                if (skill.skillType == Skill.SkillType.Passive)
                {
                    switch (skill.skillID)
                    {
                        case 8:
                            {
                                if (Random.Range(0,100) <= skill.skillDamage)
                                {
                                    int manaHeal = (int)(player.maxMana.totalAmount * (skill.skillHitChance / 100f));
                                    player.HealMP(manaHeal);
                                    VictoryScreen.AddDetails(string.Format("Gain {0} Mana from {1}", manaHeal, skill.skillName));
                                }
                                break;
                            }
                    }
                }
            }
        }
    }

    static void SkillPageImagesOn(bool yes)
    {
        for (int i = 0; i < SkillPage.skillPage.FindChild("Skills").transform.childCount; i += 1)
        {
            SkillPage.skillPage.FindChild("Skills").transform.GetChild(i).GetComponent<Image>().enabled = yes;
        }
        SkillPage.skillPage.GetComponent<Image>().enabled = yes;
        SkillPage.learnedSkillsButton.gameObject.SetActive(yes);
        //SkillPage.skillPoints.gameObject.SetActive(yes);
        SkillPage.closeButton.gameObject.SetActive(yes);
        SkillPage.leftButton.gameObject.SetActive(yes);
        SkillPage.rightButton.gameObject.SetActive(yes);
        SkillPage.pageNum.gameObject.SetActive(yes);
    }

    static void ManaGaurdCalc(Stats user, Stats victim, int dmg, Skill.SkillType skilltype)
    {
        if (victim.mana - dmg < 0)
        {
            BattleUI.TextAdd(user, 17, "black", string.Format("deals {0} {1} damage to your Mana", victim.mana, skilltype));
            victim.health -= dmg - victim.mana;
            victim.mana = 0;
            BattleUI.TextAdd(user, 20, "black", string.Format("deals {0} {1} damage", dmg - victim.mana, skilltype));
        }
        else
        {
            victim.mana -= dmg;
            BattleUI.TextAdd(user, 17, "black", string.Format("deals {0} {1} damage to your Mana", dmg, skilltype));
        }
    }

}