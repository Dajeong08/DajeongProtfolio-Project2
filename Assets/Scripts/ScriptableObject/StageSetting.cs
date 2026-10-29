using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public enum WaveType { BasicMonster, BossMonster }

[CreateAssetMenu(fileName = "Stage", menuName = "Scriptable Objects/Stage")]
public class StageSetting : ScriptableObject
{
    public string StageName; // 스테이지 이름
    public List<Wave> WaveList; // 웨이브 리스트
}

[Serializable]
public struct Wave
{
    public int WaveNumber;
    public WaveType monsterType; // 현재 웨이브 타입
    public SpriteLibraryAsset spriteLibraryAsset;
    public int WaveTimeLimit; // 웨이브 제한 시간 => 제한시간 안에 보스 처치 불가시 패배 
}