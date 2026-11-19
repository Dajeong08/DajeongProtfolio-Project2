using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

// 웨이브 총 25개로 고정
// 일반, 보스 웨이브 구분
public enum WaveType { BasicMonster, BossMonster }

[CreateAssetMenu(fileName = "Stage", menuName = "Scriptable Objects/Stage")]
// 스테이지 체력과 외형 설정
public class StageSetting : ScriptableObject
{
    public string StageName; // 스테이지 이름
    public float BasicMonsterHP = 200; // 일반 몬스터 시작 체력
    public float BasicHPIncrease = 20; // 웨이브당 일반 몬스터 체력 증가량
    public List<float> BossHPList = new List<float> { 6000, 22000, 32000, 48000, 85000 }; // 5, 10, 15, 20, 25웨이브 보스 체력

    public List<Wave> WaveList = new List<Wave>(); // 일반, 보스 외형을 5세트로 등록
}

[Serializable]
// 웨이브에 사용할 몬스터 외형
public struct Wave
{
    public SpriteLibraryAsset MonsterSpriteLibrary; // 몬스터 애니메이션 외형
}
