using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

// enum은 정해진 종류 중 하나를 선택하는 자료형이다. 일반 = 0, 보스 = 1.
public enum WaveType { BasicMonster, BossMonster }

[CreateAssetMenu(fileName = "Stage", menuName = "Scriptable Objects/Stage")]
public class StageSetting : ScriptableObject
{
    public string StageName;
    public float BasicMonsterHP = 200; // 초기 체력
    public float BasicHPIncrease = 20; // 웨이브 마다 증가하는 체력
    public float BossMonsterHP = 2000; // 초기 체력
    public float BossHPIncrease = 1000; // 등장 마다 증가하는 체력

    public List<Wave> WaveList = new List<Wave>(); // 웨이브마다 등장하는 몬스터 타입과 외형을 설정한다.
}

// 일반/보스 타입과 외형을 함께 확인할 수 있다.
[Serializable]
public struct Wave
{
    public WaveType monsterType;
    public SpriteLibraryAsset MonsterSpriteLibrary;
}
