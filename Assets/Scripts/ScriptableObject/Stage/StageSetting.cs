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
    public List<Wave> WaveList;
}

// 웨이브 하나의 정보를 묶는다. Serializable로 Inspector에서 내용을 편집한다.
[Serializable]
public struct Wave
{
    public int WaveNumber;
    public WaveType monsterType;
    public SpriteLibraryAsset MonsterSpriteLibrary;
    public int WaveTimeLimit;

    // 현재 진행 순서는 목록 순서이며, 실제 제한 시간은 BattleManager의 30초 고정값을 쓴다.
}
