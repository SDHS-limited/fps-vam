using UnityEngine;
using System.Collections;

public class TimeItemSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject timeItemPrefab; // 생성할 시간 아이템 프리팹
    public Transform[] spawnPoints;   // 스폰 위치들 (배열)
    public float spawnInterval = 10f; // 아이템 스폰 주기 (초)
    public int maxItemsOnMap = 3;     // 맵에 동시에 존재할 수 있는 최대 아이템 개수

    void Start()
    {
        // 스폰 포인트가 하나도 지정되지 않았다면 경고 출력
        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("스폰 포인트가 설정되지 않았습니다! 인스펙터를 확인하세요.");
            return;
        }

        // 게임 시작과 동시에 스폰 코루틴 실행
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // 무한 루프를 돌며 주기적으로 아이템 생성 시도
        while (true)
        {
            // 1. 현재 맵에 있는 TimeItem 스크립트(아이템)의 개수를 셉니다.
            TimeItem[] existingItems = FindObjectsOfType<TimeItem>();

            // 2. 맵에 있는 아이템이 최대치보다 적을 때만 새로 생성합니다.
            if (existingItems.Length < maxItemsOnMap)
            {
                SpawnItem();
            }

            // 3. 설정한 시간(초)만큼 대기한 후 다시 루프를 돕니다.
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnItem()
    {
        // 지정된 스폰 포인트 배열 중 랜덤하게 하나를 선택합니다.
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedPoint = spawnPoints[randomIndex];

        // 선택된 스폰 포인트의 위치와 회전값에 맞춰 아이템 프리팹을 생성합니다.
        Instantiate(timeItemPrefab, selectedPoint.position, selectedPoint.rotation);
    }
}