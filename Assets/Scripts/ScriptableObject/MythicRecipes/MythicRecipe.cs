using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// 조합 재료 한 종류와 수량
public class MythicMaterial
{
    public CharacterData Character; // 필요한 재료 캐릭터
    [Min(1)] public int Count = 1; // 필요한 수량
}

[CreateAssetMenu(fileName = "MythicRecipe", menuName = "Scriptable Objects/Mythic Recipe")]
// 신화 조합 레시피
public class MythicRecipe : ScriptableObject
{
    public CharacterData Result; // 조합으로 생성할 신화
    public List<MythicMaterial> Materials = new List<MythicMaterial>(); // 재료 캐릭터와 필요 수량
}
