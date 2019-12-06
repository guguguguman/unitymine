using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mine_ItemManager : MonoBehaviour
    // 
{

    public GameObject player;
    public GameObject[] Itmes;
    // Start is called before the first frame update
    
    public void call_instant_item(int num , Vector3 pos)
    {
        if (num == 1) { }
        GameObject a = Instantiate(Itmes[0] , pos , Quaternion.identity);
        a.GetComponent<Mine_Item>().myblocknum = num;
        a.GetComponent<Mine_Item>().Block_Uv_SetF();
    }
}
