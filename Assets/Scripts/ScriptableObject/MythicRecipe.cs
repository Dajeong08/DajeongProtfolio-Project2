using System;
using System.Collections.Generic;
using UnityEngine;

// 재료의 종류와 수량을 함께 저장한다.
[Serializable]
public class MythicMaterial
{
    public CharacterData Character;
    [Min(1)] public int Count = 1;
}

// 조합 규칙만 저장한다. 실제 재료 소비와 소환은 별도 매니저에서 처리한다.
[CreateAssetMenu(fileName = "MythicRecipe", menuName = "Scriptable Objects/Mythic Recipe")]
public class MythicRecipe : ScriptableObject
{
    public CharacterData Result;
    public Sprite Icon;
    public List<MythicMaterial> Materials = new List<MythicMaterial>();
}
