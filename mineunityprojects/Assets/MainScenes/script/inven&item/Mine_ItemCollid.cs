using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 아이템 조합을 위한 스크립트입니다.
 * 미완성
 */
public class Mine_ItemCollid : MonoBehaviour
{

    public Mine_Inventory myInven;
    public int resultNum; // 조합결과물 아이템번호

    [System.Serializable]
    public class myItemCollidStruc // 조합조건방식을 나타낸 구조체 , 이것을 리스트로해서 한개의 조합식을 구성한다.
    {
        public int itemInfo; // 해당 조합칸의 해당 아이템 번호
        public int slotNum; // 몇번째 칸인지
    }
    [System.Serializable]
    public class myItemCollidStruc2 // 조합방식을 나타낸 클래스
    {
        // 해당 조건 만족하면 결과 아이템 번호
        public int result;
        //조건 리스트
        public List<myItemCollidStruc> myItemCollidList = new List<myItemCollidStruc>();
    }
    [System.Serializable]
    public class _Smyitemcollidstr3 // 조합방식을 나타낸 클래스
    {
        public List<myItemCollidStruc2> myItemCollid2 = new List<myItemCollidStruc2>();
    }

    // 최대 9개 칸의 조합식을 나타내는 리스트
    public _Smyitemcollidstr3[] myitemcollid = new _Smyitemcollidstr3[9];

    /* slotNum이 slot을 읽는 방식
     *  1   2   3 
     *  4   5   6
     *  7   8   9
     * 
     */

    // 조합식을 세팅하는 함수
    void Set_CollidF()
    {
        //나무가지 조합방식
        List<myItemCollidStruc> tree_Collid1 = new List<myItemCollidStruc>();
        tree_Collid1.Add(new myItemCollidStruc { itemInfo = 1 , slotNum = 8 });
        tree_Collid1.Add(new myItemCollidStruc { itemInfo = 1 , slotNum = 5 });
        myitemcollid[2].myItemCollid2.Add(new myItemCollidStruc2 { myItemCollidList = tree_Collid1 });
    }

    void Item_CollidF()
    {
        resultNum = 0;
        for (int i = myitemcollid.Length; i > 0; --i)
        {
            // 
            if (myitemcollid[i].myItemCollid2.Count == i)
            {
                for (int i1 = 0; i1 < myitemcollid[i].myItemCollid2.Count; ++i1)
                {
                    int count = 0;
                    for (int i2 = 0; i2 < myitemcollid[i].myItemCollid2[i1].myItemCollidList.Count; ++i2)
                    {
                        for (int i3 = 0; i3 < myInven.invenslots.Length; ++i3)
                        {
                            if (myitemcollid[i].myItemCollid2[i1].myItemCollidList[i2].slotNum == myInven.invenslots[i3].slot_num_collid &&
                                myitemcollid[i].myItemCollid2[i1].myItemCollidList[i2].itemInfo == myInven.invenslots[i3].itemvalue
                                ) // 조합식과 아이템 아이템슬롯 일치할경우
                            {
                                count += 1;
                                break;
                            }
                        }
                        if (count == myitemcollid[i].myItemCollid2[i1].myItemCollidList.Count)
                        {
                            resultNum = myitemcollid[i].myItemCollid2[i1].result;
                            break;
                        }
                    }
                }
            }
        }
    }

}
