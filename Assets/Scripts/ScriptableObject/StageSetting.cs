using System;
using System.Collections.Generic;
using UnityEngine;

public enum MonsterType { BasicMonster, BossMonster }

[CreateAssetMenu(fileName = "Stage", menuName = "Scriptable Objects/Stage")]
public class StageSetting : ScriptableObject
{
    public string StageName; // 스테이지 이름
    public int StageNumber; // 스테이지 넘버
    public int WaveInterval; // 웨이브 간격
    public List<Wave> StageList; // 웨이브 리스트
}

[Serializable]
public struct Wave
{
    public MonsterType monsterType; // 현재 웨이브 타입
    public GameObject BasicMonster; // 일반 몬스터 프리펩
    public GameObject BossMonster; // 보스 몬스터 프리펩
    public int MonsterSpawonInterval; // 일반 몬스터 스폰 간격
    public int MonsterLimit; // 일반 몬스터 최대 마릿수
    public int WaveTimeLimit; // 웨이브 제한 시간 => 제한시간 안에 보스 처치 불가시 패배 
}