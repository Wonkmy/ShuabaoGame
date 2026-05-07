using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Player : MonoBehaviour
{
    public float moveSpeed;
    public Transform firePos;
    private Transform fire;

    float fireTime = 0.0f;
    float fireInterval = 0.33f;
    float fireAngle = 120.0f;// 扇形角度范围

    Vector3 fireDirection;// 朝向

    int currentUsedBulletIndex = 0;// 当前使用的子弹类型索引

    [SerializeField]private int currentBulletCount = 0;// 当前子弹数量

    private void Start()
    {
        fire = transform.Find("Fire");
        currentBulletCount = 1;
    }
    void Update()
    {
        Move();
        Rotate();
        Attack();
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            currentUsedBulletIndex = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentUsedBulletIndex = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentUsedBulletIndex = 2;
        }

        if(Input.GetAxis("Mouse ScrollWheel") > 0f) // 向上滚动
        {
            currentBulletCount = currentBulletCount + 1;
        }
        else if(Input.GetAxis("Mouse ScrollWheel") < 0f) // 向下滚动
        {
            currentBulletCount = currentBulletCount - 1;
        }
    }

    void Rotate()
    {
        Vector3 mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mpos.z = 0;
        fireDirection = mpos - transform.position;
        fireDirection = fireDirection.normalized;
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        fire.localEulerAngles = new Vector3(0, 0, angle - 90);
    }

    void Attack()
    {
        fireTime += Time.deltaTime;
        if (fireTime > fireInterval)
        {
            fireTime = 0;
            BulletData bulletData = DataManager.bulletsDataDict[currentUsedBulletIndex];
            if (currentBulletCount <= 1)
            {
                bulletData.type = BulletType.Liner;
            }
            else if (currentBulletCount == 2)
            {
                bulletData.type = BulletType.Liner;
            }
            else if (currentBulletCount < 8)
            {
                bulletData.type = BulletType.Sector;
            }
            else
            {
                bulletData.type = BulletType.Cicle;
            }

            switch (bulletData.type)
            {
                case BulletType.Liner:
                    for (int i = 0; i < currentBulletCount; i++)
                    {
                        SpwanBullet(bulletData, fireDirection, currentBulletCount == 2 ? true : false);
                    }
                    break;
                case BulletType.Sector:
                    var allDires = DataManager.GetFanDirections2D(fireDirection, fireAngle, fireAngle / (currentBulletCount - 1));
                    for (int i = 0; i < allDires.Length; i++)
                    {
                        SpwanBullet(bulletData, allDires[i]);
                    }
                    break;
                case BulletType.Cicle:
                    for (int i = 0; i < currentBulletCount; i++)
                    {
                        float angle = (360.0f / currentBulletCount) * i;
                        Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                        SpwanBullet(bulletData, dir);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void SpwanBullet(BulletData bulletData, Vector3 dir, bool onlyDouble = false)
    {
        
        if (onlyDouble)
        {
            Vector3 offset = Vector3.Cross(dir, Vector3.forward).normalized * 0.5f; // 计算垂直于攻击方向的偏移量
            // 生成第一发子弹，位置偏移到攻击方向的右侧
            SpwanBulletSingle(bulletData, dir, firePos.position + offset);
            // 生成第二发子弹，位置偏移到攻击方向的左侧
            SpwanBulletSingle(bulletData, dir, firePos.position - offset);
        }
        else
        {
            SpwanBulletSingle(bulletData, dir, firePos.position);
        }
    }

    void SpwanBulletSingle(BulletData bulletData, Vector3 dir, Vector3 pos)
    {
        GameObject newBullet_Liner = Instantiate(Resources.Load<GameObject>("bullet"));
        newBullet_Liner.transform.position = pos;
        newBullet_Liner.GetComponent<Bullet>().SetBullet(bulletData, dir);
        newBullet_Liner.GetComponent<Bullet>().CanMove = true;
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(x, y, 0);
        transform.position = Vector3.Lerp(transform.position, transform.position + dir, moveSpeed * Time.deltaTime);
    }
}
