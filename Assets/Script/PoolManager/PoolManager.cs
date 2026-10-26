using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System.Transactions;

public class PoolManager : MonoBehaviour
{
    [Header("Prfabs Data")]
    public Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();

    [HideInInspector] public static PoolManager instance;

    public void Awake()
    {
        instance = this;
    }


    public GameObject Get(GameObject prefab)
    {
        if(!pools.TryGetValue(prefab,out List<GameObject> pool))
        {
            pool = new List<GameObject>();
            pools.Add(prefab, pool);
        }

        foreach(GameObject item in pool)
        {
            if(item!=null && item.activeSelf!=true && item.transform.parent==transform)
            {
                item.SetActive(true);
                return item;
            }
        }

        GameObject newItem=Instantiate(prefab);
        pool.Add(newItem);
        return newItem;
    }

    public void Recycle(GameObject item)
    {
        item.SetActive(false);
        item.transform.SetParent(transform);
    }

}
