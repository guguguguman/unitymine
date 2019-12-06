using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*물 움직임에 관한 스크립트*/
public class Mine_Water : MonoBehaviour
{
    // 딕셔너리로 된 세이브 데이터 접근을 위해 vector3형식을 구조체로 선언합니다.

    public GameObject myPlayer;
    public Mine_BlockCreate myBlockCreateScript;
    public Mine_Thread mythr;

    public MinePostBox mySingleton_Instance;

    // 물 연산 범위
    public int myWaterCalculateWidth;

    // Start is called before the first frame update
    public void Water_Start()
    {
        InvokeRepeating("Water_ChunkCheck" , 0 , 2.5f);
    }

    //플레이어 기준으로 일정 범위 이내의 청크들만 물연산 실행
    public void Water_ChunkCheck()
    {

        // 거리가 범위 이내인 청크일시
        int num = 0;
        for (int  i2 = 0; i2 < myBlockCreateScript.m_block_node_obj_array.Length; ++i2) {
            if (Vector3.Distance(myPlayer.transform.position  , myBlockCreateScript.m_block_node_obj_array[i2].transform.position) < myWaterCalculateWidth )
            {
                    lock (myBlockCreateScript.lockObject[num % MinePostBox.mytreadnum])
                    {
                        mySingleton_Instance.PushData2Water(new MinePostBox.myInputData { chunknum = i2, chunkvector = myBlockCreateScript.m_block_node_obj_array[i2].transform.position }, num % MinePostBox.mytreadnum);
                        num += 1;
                    }
                
            }
        }

        
    }

}
