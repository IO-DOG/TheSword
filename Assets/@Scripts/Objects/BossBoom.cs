using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBoom : MonoBehaviour
{
    const int BossBoomCount = 4;
    Transform[] BossBooms = new Transform[BossBoomCount];
    // Start is called before the first frame update
    void Awake()
    {
        for(int i = 0; i <  BossBoomCount; i++)
            BossBooms[i] = transform.GetChild(i);
    }


    Coroutine CoBossBoom;
    public void StartCoBossBoom(GameObject boss)
    {
        CoBossBoom = StartCoroutine(BossBoomEffect(boss));
    }

    IEnumerator BossBoomEffect(GameObject boss)
    {
        SpriteRenderer sr = boss.GetComponent<SpriteRenderer>();
        Bounds bounds = sr.bounds;

        for(int i = 0; i < BossBoomCount; i++)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            Vector3 randomPos = new Vector3(randomX, randomY, bounds.center.z - 0.01f);
            BossBooms[i].position = randomPos;
        }

        yield return new WaitForSeconds(1);

        Managers.Resource.Destroy(gameObject);
    }
}
