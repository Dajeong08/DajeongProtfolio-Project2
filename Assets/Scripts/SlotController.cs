using JetBrains.Annotations;
using UnityEngine;

public class SlotController : MonoBehaviour
{
    public int SlotIndex; // 인덱스 
    public GameObject Box_1;
    public GameObject Box_2;
    public GameObject Box_3;
    public bool IsFull; //모두 채워졌는지 

    // Box_1의 오브젝트 위치 변경 ~~~
    // Box가 모두 채워졌으면 IsFull 투르 
}
