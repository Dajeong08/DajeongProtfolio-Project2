using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

// enum은 정해진 종류 중 하나를 선택하는 자료형이다. 일반 = 0, 보스 = 1.
public enum WaveType { BasicMonster, BossMonster }

// CreateAssetMenu는 프로젝트 창에 데이터 에셋 생성 메뉴를 만든다.
[CreateAssetMenu(fileName = "Stage", menuName = "Scriptable Objects/Stage")]
public class StageSetting : ScriptableObject
{
    public string StageName;
    public int StageNumber;
    public float BasicMonsterHP = 200;
    public float BasicHPIncrease = 20;
    public float BossMonsterHP = 2000;
    public float BossHPIncrease = 1000;

    // 0: 1~4 일반, 1: 5 보스, 2: 6~9 일반, 3: 10 보스...
    // 25웨이브까지 일반/보스 한 쌍씩 총 10개를 등록한다.
    public List<Wave> WaveList = new List<Wave>();
}

// 일반/보스 타입과 외형을 함께 확인할 수 있다.
[Serializable]
public struct Wave
{
    public WaveType monsterType;
    public SpriteLibraryAsset MonsterSpriteLibrary;
}
