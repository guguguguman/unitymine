using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/*인벤토리 전체기능과 아이템 데이터 등을 모아놓은 스크립트*/

public class Mine_Inventory : MonoBehaviour
{
    // 인벤토리 안에 존재하는 아이템 데이터에 관한 구조체
    [System.Serializable]
    public class myItem
    {
        public int iteminfonum; // 인포번호
        public int itemnum; // 갯수
        public int slotnum; // 슬롯번호
        
    }
    // db에 매칭하는 아이템 정보
    [System.Serializable]
    public struct myItemInfo
    {
        public string name; // 아이템 이름
        public int infonum; // 대응되는 번호
        public int maxnum; // 한 슬롯에 들어갈 맥스갯수
        public int objdrawnum; // 그려주는 오브젝트넘버
    }
    // 현재 플레이어의 슬롯 정보
    [System.Serializable]
    public class mySlot
    {
        public int addnum; // 아이템 존재여부
        public int itemnum;
        public int itemvalue;

        //조합식의 x y 를 검사하여 비교하기 위함
        public int slot_num_collid;

        public GameObject slot_obj;
        public Image itemui_img; // ui 이미지
        public Text item_numtex;// ui 텍스트
    }

    public GameObject invenmainobj;
    // 카메라 기준 마우스 포지션을 받기 위해 카메라 받음
    public Camera mycam;

    public myItem mydragnumitem;
    //드래그할때 마우스 따라 이동하는 오브젝트
    public GameObject mydragobj;

    public myItemInfo[] iteminfos;
    //유저의 인벤 속 아이템 데이터 리스트
    public List<myItem> myitems = new List<myItem>();
    //유저의 인벤 슬롯 구조체
    public mySlot[] invenslots = new mySlot[30];
    // ui에 보여줄 아이템 스프라이트
    public Sprite[] draw_objs;
    // 아이템 이미지
    public Image[] item_imgs;

    //아이템 드래그시 z축으로 얼마만큼 움직일지에 대한 변수
    public float myz;
    public float myz2;

    //인벤토리 열었는지 체크용 불
    public bool invenopen = false;
    //좌클릭시 바꿔서 아이템 드래그 컨트롤 할 수 있게 만들어주는 변수
    public int mydragnum;
    //좌클릭시 해당 아이템 넘버를 받아온 변수
    public int mydragnum2;
    //우클릭시 바꿔서 아이템 드래그 컨트롤 할 수 있게 만들어주는 변수
    public int mydragnum_split;
    //우클릭시 스플릿 갯수 기본 1 우클릭 상태에서 좌클릭하면 아이템 수 / 2
    public int mydragnum_splitnum;

    private void Start()
    {
        //최초 시작시 마우스 락 걸어준다.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Item_DrawF();
    }
    private void Update()
    {
        // i 누를시 마우스 락 풀고 인벤 ui 보이게 만듬
        if (Input.GetKeyDown(KeyCode.I) && invenopen == false)
        {
            invenopen = true;
            invenmainobj.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;


        }
        // 다시 i 누를시 마우스 락 하고 인벤 ui 안 보이게 만듬
        else if (Input.GetKeyDown(KeyCode.I) && invenopen == true)
        {
            invenopen = false;
            invenmainobj.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

        }
        // ESC 누를시 마우스 락 하고 인벤 ui 안 보이게 만듬
        else if (Input.GetKeyDown(KeyCode.Escape) && invenopen == true)
        {
            invenopen = false;
            invenmainobj.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

        }

        /* 
         마우스 왼쪽 클릭시 아이템 드래그 해주는 기능
         */

        if (mydragnum == 1 && Input.GetMouseButtonDown(0) && mydragnum_split == 0)
        {
            Vector3 mpos;
            mpos = mycam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z + myz));
            mydragobj.transform.position
                 = new Vector3(mpos.x, mpos.y, mpos.z + myz2);
            mydragnum = 2;

            // 아이템 돌려서 가장 가까운 슬롯 알아냄
            for (int i = 0; i < invenslots.Length; ++i)
            {
                // 슬롯과 마우스 거리가 0.5 이하일때
                if (Vector2.Distance(new Vector2(invenslots[i].slot_obj.transform.position.x, invenslots[i].slot_obj.transform.position.y)
                , new Vector2(mydragobj.transform.position.x, mydragobj.transform.position.y)) < 2.4f)
                {
                    Item_DragF(i); // 스플릿 함수
                    break;
                }
            }
        }
        /* 
       마우스 왼쪽 클릭한 것 땟을시 이동시켜주는 기능
       */
        if (mydragnum == 2 && mydragnum_split == 0)
        {
            Vector3 mpos;
            mpos = mycam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z + myz));

            mydragobj.transform.position
                = new Vector3(mpos.x, mpos.y, mpos.z + myz2);


            // 마우스 클릭때고 스플릿이 실행중일시 종료
            if (Input.GetMouseButtonUp(0) && mydragnum == 2)
            {
                mydragnum = 1;
                mydragnum_split = 0;
                mydragobj.GetComponent<Image>().color = new Color(255, 255, 255, 0);
                mydragobj.transform.GetChild(0).GetComponent<Text>().color = new Color(255, 255, 255, 0);

                // 아이템 돌려서 가장 가까운 슬롯 알아냄
                for (int i = 0; i < invenslots.Length; ++i) 
                {
                    // 슬롯과 마우스 거리가 0.5 이하일때
                    if (Vector2.Distance(new Vector2(invenslots[i].slot_obj.transform.position.x, invenslots[i].slot_obj.transform.position.y)
                        , new Vector2(mydragobj.transform.position.x, mydragobj.transform.position.y)) < 2.4f
                        && myitems.Count > 0)
                    {
                        Item_Drag_ChangeF(mydragnumitem.slotnum, i);
                        break;
                    }
                }
            }
        }


        /* 
       마우스 오른쪽 클릭시 아이템 스플릿 해주는 기능
       */
        if (Input.GetMouseButtonDown(1))
        {
            if (mydragnum_split == 0 && mydragnum == 1)
            {
                mydragnum = 1;
                mydragnum_split = 1;
                Vector3 mpos;
                mpos = mycam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z + myz));
                mydragobj.transform.position
                = new Vector3(mpos.x, mpos.y, mpos.z + myz2);

                // 아이템 돌려서 가장 가까운 슬롯 알아냄
                for (int i = 0; i < invenslots.Length; ++i)
                {
                    // 슬롯과 마우스 거리가 0.5 이하일때
                    if (Vector2.Distance(new Vector2(invenslots[i].slot_obj.transform.position.x, invenslots[i].slot_obj.transform.position.y)
                    , new Vector2(mydragobj.transform.position.x, mydragobj.transform.position.y)) < 2.4f)
                    {
                        Item_SplitF(i); // 스플릿 함수
                        break;
                    }
                }
            }
        }

        /* 
       마우스 오른쪽 스플릿 기능 발동상태에서 다시 왼쪽 클릭시 아이템/2로 스플릿 갯수를 바꾼다.
       */
        if (Input.GetMouseButtonDown(0) && mydragnum_split == 1 && mydragnum == 1)
        {
            mydragnum_split = 3;
            mydragnum_splitnum = mydragnumitem.itemnum / 2;
            mydragobj.transform.GetChild(0).GetComponent<Text>().text = mydragnum_splitnum.ToString();
        }

        if (mydragnum_split > 0 && mydragnum == 1)
        {
            Vector3 mpos;
            mpos = mycam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z + myz));

            mydragobj.transform.position
                = new Vector3(mpos.x, mpos.y, mpos.z + myz2);

            if (Input.GetMouseButtonUp(1))
            {
                mydragobj.GetComponent<Image>().color = new Color(255, 255, 255, 0); // 알파 바꿔줌
                mydragobj.transform.GetChild(0).GetComponent<Text>().color = new Color(255, 255, 255, 0);
                if (mydragnum_splitnum > 0)
                {
                    for (int i = 0; i < invenslots.Length; ++i) // 아이템 돌려서 가장 가까운 슬롯 알아냄
                    {
                        if (Vector2.Distance(new Vector2(invenslots[i].slot_obj.transform.position.x, invenslots[i].slot_obj.transform.position.y)
                            , new Vector2(mydragobj.transform.position.x, mydragobj.transform.position.y)) < 2.4f) // 슬롯과 마우스 거리가 0.5 이하일때
                        {
                            Item_Drag_Change2F(mydragnumitem.slotnum, i); // 이전 슬롯 , 바꿀 슬롯
                            break;
                        }
                    }
                }
                mydragnum_splitnum = 0;
                mydragnum_split = 0;
                mydragnum = 1;
            }
        }
    }
    /* 
     마우스 왼쪽 클릭시 호출되는 드래그용 메서드
     */
    public void Item_DragF(int slotnum)
    {
            for (int i = 0; i < myitems.Count; ++i)
            {
                if (myitems[i].slotnum == slotnum)
                {
                    mydragnumitem = myitems[i];
                    mydragnum2 = i;
                    mydragobj.GetComponent<Image>().sprite =
                     draw_objs[iteminfos[myitems[i].iteminfonum].objdrawnum];
                    mydragobj.GetComponent<Image>().color = new Color(255, 255, 255, 1.0f);
                mydragobj.transform.GetChild(0).GetComponent<Text>().color = new Color(255, 255, 255, 1.0f);
                mydragobj.transform.GetChild(0).GetComponent<Text>().text = myitems[i].itemnum.ToString();
            }
            }
      

    }

    /* 
     마우스 오른쪽 클릭시 호출되는 드래그용 메서드
     */
    void Item_SplitF(int slotnum) 
    {

            for (int i = 0; i < myitems.Count; ++i)
            {
                if (myitems[i].slotnum == slotnum)
                {
                    mydragnumitem = myitems[i];
                    mydragnum2 = i;
                    mydragobj.GetComponent<Image>().sprite =
                     draw_objs[iteminfos[myitems[i].iteminfonum].objdrawnum];
                    mydragobj.GetComponent<Image>().color = new Color(255, 255, 255, 1.0f);
                }
            }

        // 갯수 0이상일시만 적용
        if (mydragnumitem.itemnum > 0)
            {

                    mydragobj.transform.GetChild(0).GetComponent<Text>().color = new Color(255, 255, 255, 1.0f);
                    mydragobj.transform.GetChild(0).GetComponent<Text>().text = mydragnum_split.ToString();
                    mydragnum_splitnum = 1;
            }
            else
            {
                mydragnum = 1;
                mydragobj.GetComponent<Image>().color = new Color(255, 255, 255, 0); // 알파 바꿔줌
            }
        
    }

    /* 
     마우스 왼쪽 클릭 떗을시 호출되는 메서드 (이전 슬롯번호 , 바뀌는 슬롯 번호)
     */
    public void Item_Drag_ChangeF( int change_slot_num1 , int change_slot_num2)
    {
        bool chk = false;
        for (int i=0; i < myitems.Count; ++i)
        {
            // 마우스 뗀 슬롯에 아이템있는지 체크. 마우스를 누른곳과 동일 슬롯 아닐시에만 적용 
            if (myitems[i].slotnum == change_slot_num2 && change_slot_num1 != change_slot_num2)
            {
                // 같은 종류의 아이템이고 맥스 수보다 작을시 합체
                if (myitems[i].iteminfonum == mydragnumitem.iteminfonum && mydragnumitem.itemnum + myitems[i].itemnum <= iteminfos[mydragnumitem.iteminfonum].maxnum) 
                {
                    myitems[i].itemnum += mydragnumitem.itemnum;
                    myitems[mydragnum2].itemnum = 0;
                    invenslots[change_slot_num1].itemnum = 0;
                }
                // 다른 종류일시 위치만 바꿔준다.
                else if (myitems[i].iteminfonum != mydragnumitem.iteminfonum )
                {
                    myitems[i].slotnum = change_slot_num1;
                    myitems[mydragnum2].slotnum = change_slot_num2;

                    invenslots[change_slot_num1].itemnum = myitems[i].itemnum;
                    invenslots[change_slot_num2].itemnum = mydragnumitem.itemnum;
                }
                else if (myitems[i].iteminfonum == mydragnumitem.iteminfonum && mydragnumitem.itemnum + myitems[i].itemnum > iteminfos[mydragnumitem.iteminfonum].maxnum)
                {
                    myitems[i].slotnum = change_slot_num1;
                    myitems[mydragnum2].slotnum = change_slot_num2;
                }
                chk = true;
            }

        }

        // 그자리에 아이템이 없으면 그자리로 이동
        if (chk == false && change_slot_num1 != change_slot_num2)
        {
            // 슬롯 정보에 있는 아이템 갯수 바꿔준다.
            invenslots[change_slot_num1].itemnum = 0;
            invenslots[change_slot_num2].itemnum = mydragnumitem.itemnum;

            myitems[mydragnum2].slotnum = change_slot_num2;
        }
        Item_DrawF();
    }

    /* 
     마우스 오른쪽 클릭 떗을시 호출되는 메서드 (이전 슬롯번호 , 바뀌는 슬롯 번호)
     */
    public void Item_Drag_Change2F(int change_slot_num1, int change_slot_num2) // 스플릿용 아이템번호 인자를 넘겨받는다. 바뀔 슬롯번호 받는다.
    {
        bool chk = false;

        // 해당 아이템을 가져옴
        for (int i = 0; i < myitems.Count; ++i)
        {
            // 마우스 뗀 슬롯에 아이템있는지 동일 슬롯 아닐시 
            if (myitems[i].slotnum == change_slot_num2 && change_slot_num1 != change_slot_num2)
            {
                // 다른 슬롯이고 같은 종류의 아이템일시 맥스 수보다 작을시 올려주고 이전 아이템은 해당 만큼 감소
                if (myitems[i].iteminfonum == mydragnumitem.iteminfonum && myitems[i].itemnum + mydragnum_splitnum <= iteminfos[mydragnumitem.iteminfonum].maxnum)
                {
                    myitems[i].itemnum += mydragnum_splitnum;
                    myitems[mydragnum2].itemnum -= mydragnum_splitnum;

                    invenslots[change_slot_num1].itemnum += mydragnum_splitnum;
                    invenslots[change_slot_num2].itemnum -= mydragnum_splitnum;
                }
                else // 다른 종류일시 그냥 종료
                {

                }
                chk = true;
            }
        }
        // 그자리에 아이템이 없으면 그자리에 생성
        if (chk == false && change_slot_num1 != change_slot_num2)
        {
            // 슬롯 정보에 있는 아이템 갯수 바꿔준다.
            invenslots[change_slot_num1].itemnum -= mydragnum_splitnum;
            invenslots[change_slot_num2].itemnum = mydragnum_splitnum;
            myitems[mydragnum2].itemnum -= mydragnum_splitnum;


            myitems.Add(new myItem { iteminfonum = mydragnumitem.iteminfonum, itemnum = mydragnum_splitnum, slotnum = change_slot_num2 }); // 새롭게 추가
        }

        Item_DrawF();
    }

    /* 
    내 아이템 리스트를 가지고 아이템을 다시 그려주는 메서드
     */
    void Item_DrawF()
    {
        for (int i = 0; i < invenslots.Length; ++i)
        {
            invenslots[i].itemui_img.color = new Color(255, 255, 255, 0);
            invenslots[i].item_numtex.color = new Color(255, 255, 255, 0);
            for (int i2 = 0; i2 < myitems.Count; ++i2)
            {
                //인벤슬롯에 맞는 아이템을 찾는다
                if (myitems[i2].slotnum == i)
                {
                    invenslots[i].itemui_img.sprite = draw_objs[iteminfos[myitems[i2].iteminfonum].objdrawnum];
                    invenslots[i].item_numtex.text = iteminfos[myitems[i2].iteminfonum].maxnum + "/" + myitems[i2].itemnum;
                    invenslots[i].itemui_img.color = new Color(255, 255, 255, 1.0f);
                    invenslots[i].item_numtex.color = new Color(255, 255, 255, 1.0f);
                }
                if (myitems[i2].itemnum <= 0)
                {
                    myitems.Remove(myitems[i2]);
                }
            }
        }
    }

    /* 
    필드에있는 오브젝트 아이템을 먹었을시 인벤토리에 아이템 추가하는 메서드
     */
    public void Item_AddF(int value, int addnum)
    {
        bool check1 = false;
        for (int i = 0; i < myitems.Count; ++i)
        {
            //먼저 내 아이템있는것들 다 참조후 아이템속성번호와 일치하는지 아닌지 검사
            if (myitems[i].iteminfonum == value)
            {
                // 맥스 보다 작을시 갯수만증가
                if (iteminfos[value].maxnum >= myitems[i].itemnum + addnum)
                {
                    myitems[i].itemnum += addnum;
                    check1 = true;
                    break;
                }
            }
        }
        // 만약 내 인벤에 해당 속성과 일치하는 아이템이 없을시
        if (check1 == false)
        {
            int slotnum = -1;
            for (int i2 = 0; i2 < invenslots.Length; ++i2)
            {
                if (invenslots[i2].itemnum == 0) { slotnum = i2; break; }

            }
            if (slotnum != -1)
            {
                invenslots[slotnum].itemnum = addnum;
                myitems.Add(new myItem { iteminfonum = value, itemnum = addnum, slotnum = slotnum }); // 새롭게 추가
            }
        }
        Item_DrawF();
    }
}
