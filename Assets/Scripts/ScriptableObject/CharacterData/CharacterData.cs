using UnityEngine;

// Inspector에서 캐릭터마다 선택할 등급이다.
public enum CharacterGrade
{
    [InspectorName("일반")] Normal = 0,
    [InspectorName("희귀")] Rare = 1,
    [InspectorName("에픽")] Epic = 2,
    [InspectorName("전설")] Legendary = 3,
    [InspectorName("신화")] Mythic = 4
}

// 프로젝트 창의 Create 메뉴에서 생성하는 캐릭터 종류별 공통 데이터다.
[CreateAssetMenu(fileName = "Character", menuName = "Scriptable Objects/Character")]
public class CharacterData : ScriptableObject
{
    [Header("Character")]
    public string CharacterName;
    public CharacterGrade Grade;
    public GameObject Prefab; // SlotManager가 이 원본을 Instantiate로 생성한다.

    [Header("Base Stats")]
    public float AttackDamage = 10f; // 기본 공격력
    // 초당 공격 횟수. 1이면 1초마다, 2이면 0.5초마다 공격한다.
    public float AttackSpeed = 1f; // 기본 공격 속도
}
