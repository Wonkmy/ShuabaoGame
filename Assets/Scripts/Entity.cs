using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public float moveSpeed { get; set; }
    public Transform FirePos { get; set; }
    public Vector3 FireDirection { get; set; }
    public int CurrentBulletCount { get; set; }

    public string EntityTag { get; set; }
}
