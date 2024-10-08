using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeBlackHole : MonoBehaviour
{
    void Start()
    {

    }

    void Update()
    {

    }

    int _layer = (1 << (int)Define.Layer.SlimeWater);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if (collision.gameObject.name == "BlackHole" && gameObject.tag == "Player")
        //    // StartCoroutine(Boom()); 이렇게 코드를 작성하면 Stop이 안됨
        //    // 둘다 string으로 넣어줘야 stop이 가능하다.
        //    StartCoroutine("Boom");
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer != _layer)
            return;

        // 블랙홀
        Vector3 dir = collision.gameObject.transform.position - gameObject.transform.position;
        Vector3.Normalize(dir);
        Rigidbody rb = collision.gameObject.GetOrAddComponent<Rigidbody>();
        rb.AddForce(dir * 70, ForceMode.Force);
        return;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //if (collision.gameObject.name == "BlackHole" && gameObject.tag == "Player")
        //    StopCoroutine("Boom");
    }
}
