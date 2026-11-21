using UnityEngine;

// 캐릭터 등급
public enum CharacterGrade
{
    [InspectorName("일반")] Normal = 0,
    [InspectorName("희귀")] Rare = 1,
    [InspectorName("에픽")] Epic = 2,
    [InspectorName("전설")] Legendary = 3,
    [InspectorName("신화")] Mythic = 4
}

[CreateAssetMenu(fileName = "Character", 
    menuName = "Scriptable Objects/Character")]
// 캐릭터 종류별 공통 데이터
public class CharacterData : ScriptableObject
{
    [Header("Character")]
    public string CharacterName; // 캐릭터 이름
    public CharacterGrade Grade; // 등급
    public GameObject Prefab; // 소환할 프리팹

    [Header("Base Stats")]
    public float AttackDamage = 10f; // 기본 공격력
    public float AttackSpeed = 1f; // 초당 공격 횟수
}
