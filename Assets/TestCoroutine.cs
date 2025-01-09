using System.Collections;
using UnityEngine;

public class TestCoroutine : MonoBehaviour
{
    private void Start()
    {
     //   StartCoroutine(MyCoroutine());
    }

    private IEnumerator MyCoroutine()
    {
        while (true)
        {
            Debug.Log("this happens");
            yield return new WaitForEndOfFrame();
            Debug.Log("Actually fucking waited");
        }
    }
}