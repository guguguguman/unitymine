using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 월드 밖으로 생성된 아이템에 연관된 스크립트입니다.  블록의 uv 를 설정해주고 플레이어와 접촉하면 인벤토리에 아이템을 애드시킵니다.*/


public class Mine_Item : MonoBehaviour
   
{
    public int myblocknum; // 해당 아이템의 번호
    public int addnum = 1; // 해당 아이템의 갯수
    public GameObject player;
    public Mine_Inventory mineinv;

    int xnum = 1;
    int ynum = 16;
    int xnum2 = 1;
    int ynum2 = 16;
    int xnum3 = 1;
    int ynum3 = 16;
    float speed = 2.5f;
    bool movechk;
    public void Start()
    {
        StartCoroutine(Connect_Player_CheckF());

    }
    public void Update()
    {
        // 플레이어가 가까이 왔을시 플레이어쪽으로 계속 이동시켜주는 기능입니다.
        if (movechk == true)
        {
            gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, player.transform.position, speed * Time.deltaTime);
        }
    }
    Vector2[] uvs = new Vector2[24];
    Vector2 _00 = new Vector2();
    Vector2 _10 = new Vector2();
    Vector2 _01 = new Vector2();
    Vector2 _11 = new Vector2();
    Vector2 _002 = new Vector2();
    Vector2 _102 = new Vector2();
    Vector2 _012 = new Vector2();
    Vector2 _112 = new Vector2();
    Vector2 _003 = new Vector2();
    Vector2 _103 = new Vector2();
    Vector2 _013 = new Vector2();
    Vector2 _113 = new Vector2();


    /*월드 밖으로 나온 아이템 오브젝트의 uv를 설정하는 메서드 */

    public void Block_Uv_SetF()
    {
        int drawonemat = 1; // 한개 텍스쳐로 6개면을 다 그리는경우 2일시 두개로 적용
        if (myblocknum == 1) // 흙
        {
            xnum = 3; ynum = 1;
            drawonemat = 1;
        }
        else if (myblocknum == 2) // 풀
        {
            xnum = 4; ynum = 1;
            xnum2 = 13; ynum2 = 13;
            xnum3 = 3; ynum3 = 1;
            drawonemat = 2;
        }
        else if (myblocknum == 3) // 밝은 회색 돌
        {
            xnum = 1; ynum = 1;
            drawonemat = 1;
        }
        else if (myblocknum == 4) // 깊은 회색 돌
        {
            xnum = 2; ynum = 1;
            drawonemat = 1;
        }
        else if (myblocknum == 31 || myblocknum == 32 || myblocknum == 33) // 나무
        {
            xnum = 5; ynum = 2;
            xnum2 = 6; ynum2 = 2;
            xnum3 = 6; ynum3 = 2;
            drawonemat = 2;
        }
        else if (myblocknum == 34) // 나무풀
        {
            xnum = 5; ynum = 4;
            drawonemat = 1;
        }
        else if (myblocknum == 35) // 나무풀
        {
            xnum = 6; ynum = 4;
            drawonemat = 1;
        }
        else if (myblocknum == 11) // 석탄
        {
            xnum = 3; ynum = 3;
            drawonemat = 1;
        }
        else if (myblocknum == 12) // 철
        {
            xnum = 2; ynum = 3;
            drawonemat = 1;
        }
        else if (myblocknum == 13) // 코발트
        {
            xnum = 4; ynum = 4;
            drawonemat = 1;
        }
        else if (myblocknum == 14) // 코발트 블루
        {
            xnum = 1; ynum = 11;
            drawonemat = 1;
        }
        else if (myblocknum == 15) // 금
        {
            xnum = 1; ynum = 3;
            drawonemat = 1;
        }
        else if (myblocknum == 16) // 다이아
        {
            xnum = 3; ynum = 4;
            drawonemat = 1;
        }
        if (drawonemat == 1)
        {
            _00.x = 0 + 0.0625f * (xnum - 1); _00.y = 1 - 0.0625f * ynum;
            _10.x = 0 + 0.0625f * xnum; _10.y = 1 - 0.0625f * ynum;
            _01.x = 0 + 0.0625f * (xnum - 1); _01.y = 1 - 0.0625f * (ynum - 1);
            _11.x = 0 + 0.0625f * xnum; _11.y = 1 - 0.0625f * (ynum - 1);
            uvs[0] = _11; uvs[1] = _01; uvs[2] = _00; uvs[3] = _10; // 아랫면
            uvs[4] = _11; uvs[5] = _01; uvs[6] = _00; uvs[7] = _10; // 왼쪽
            uvs[8] = _11; uvs[9] = _01; uvs[10] = _00; uvs[11] = _10; // 정면
            uvs[12] = _11; uvs[13] = _01; uvs[14] = _00; uvs[15] = _10; // 뒷면
            uvs[16] = _11; uvs[17] = _01; uvs[18] = _00; uvs[19] = _10; // 오른쪽
            uvs[20] = _11; uvs[21] = _01; uvs[22] = _00; uvs[23] = _10; // 윗면
        }

        else if (drawonemat == 2) // 윗면 아랫면이 다른 면을 그려야할경우
        {
            _00.x = 0 + 0.0625f * (xnum - 1); _00.y = 1 - 0.0625f * ynum; // 오른쪽 왼쪽 뒷쪽 정면
            _10.x = 0 + 0.0625f * xnum; _10.y = 1 - 0.0625f * ynum;
            _01.x = 0 + 0.0625f * (xnum - 1); _01.y = 1 - 0.0625f * (ynum - 1);
            _11.x = 0 + 0.0625f * xnum; _11.y = 1 - 0.0625f * (ynum - 1);

            _002.x = 0 + 0.0625f * (xnum2 - 1); _002.y = 1 - 0.0625f * ynum2; // 윗면
            _102.x = 0 + 0.0625f * xnum2; _102.y = 1 - 0.0625f * ynum2;
            _012.x = 0 + 0.0625f * (xnum2 - 1); _012.y = 1 - 0.0625f * (ynum2 - 1);
            _112.x = 0 + 0.0625f * xnum2; _112.y = 1 - 0.0625f * (ynum2 - 1);

            _003.x = 0 + 0.0625f * (xnum3 - 1); _003.y = 1 - 0.0625f * ynum3; //아랫면
            _103.x = 0 + 0.0625f * xnum3; _103.y = 1 - 0.0625f * ynum3;
            _013.x = 0 + 0.0625f * (xnum3 - 1); _013.y = 1 - 0.0625f * (ynum3 - 1);
            _113.x = 0 + 0.0625f * xnum3; _113.y = 1 - 0.0625f * (ynum3 - 1);
            uvs[0] = _113; uvs[1] = _013; uvs[2] = _003; uvs[3] = _103; // 아랫면
            uvs[4] = _11; uvs[5] = _01; uvs[6] = _00; uvs[7] = _10; // 왼쪽
            uvs[8] = _11; uvs[9] = _01; uvs[10] = _00; uvs[11] = _10; // 정면
            uvs[12] = _11; uvs[13] = _01; uvs[14] = _00; uvs[15] = _10; // 뒷면
            uvs[16] = _11; uvs[17] = _01; uvs[18] = _00; uvs[19] = _10; // 오른쪽
            uvs[20] = _112; uvs[21] = _012; uvs[22] = _002; uvs[23] = _102; // 윗면
        }

        gameObject.GetComponent<MeshFilter>().mesh.uv = uvs;
    }

    // 플레이어와 인접했는지 체크하는 코루틴입니다.
    IEnumerator Connect_Player_CheckF()
    {
        while (true)
        {
            if (Vector3.Distance(gameObject.transform.position, player.transform.position) < 0.5f)
            {
                mineinv.Item_AddF(myblocknum , addnum);
                Destroy(gameObject);
            }
                if (Vector3.Distance(gameObject.transform.position , player.transform.position)  < 2.5f )
            {
                movechk = true;
            }

            yield return new WaitForSeconds(0.3f);
        }


    }


}
