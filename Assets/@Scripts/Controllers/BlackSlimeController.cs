using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackSlimeController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Dead()
    {
        Vector3 pos = transform.localPosition;
        float size = Define.TILE_SIZE;

        int[] dx = { -1, 0, 1, 1, 1, 0, -1, -1 };
        int[] dy = { 1, 1, 1, 0, -1, -1, -1, 0 };
        bool[] ch = { false, false, false, false, false, false, false, false };
        List<int> s = new List<int>();
        int cnt = 0;

        while (cnt < 3)
        {
            int randValue = UnityEngine.Random.Range(0, 8);
            if (ch[randValue] == false)
            {
                cnt++;
                ch[randValue] = true;
                s.Add(randValue);
            }
        }

        for (int i = 0; i < s.Count; i++)
        {
            int idx = s[i];
            Vector3 vector = new Vector3(pos.x + dx[idx] * size, pos.y, pos.z + dy[idx] * size);
            Debug.Log($"vector : {vector.x}, {vector.y}, {vector.z}");
            // todo
            // vector 위치에 분열된 슬라임 생성

            // test
            //GameObject monsters = GameObject.Find("Monsters");
            GameObject monster = Managers.Resource.Instantiate("Monster", transform.parent);
            monster.GetOrAddComponent<MonsterController>().id = 1;
            monster.transform.localPosition = vector;
            monster.transform.localScale = new Vector3(2, 2, 2);
            monster.name = $"black slime split monster";
        }
    }
}
