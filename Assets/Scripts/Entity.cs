using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public float moveSpeed { get; set; }
    public bool CanMove { get; set; }
    public bool Dead { get; set; }
    public Transform FirePos { get; set; }
    public Vector3 FireDirection { get; set; }
    public int CurrentBulletCount { get; set; }


    public AttackType attackType { get; set; }
    public string EntityTag { get; set; }

    public Weapon weapon;// 武器类

    public virtual Entity GetNearestTarget() {  return null; }
    public virtual void TakeDamage(int damage) { }

    public Weapon GetCurrentWeapon()
    {
        return weapon;
    }
}
