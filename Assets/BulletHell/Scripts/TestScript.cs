using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestClass
{
    public int testNum = 0;
}

public class TestScript : MonoBehaviour
{
    public TestClass[] testFunc(TestClass[] tcArr)
    {
        int i = 0;
        foreach(var tc in tcArr)
        {
            tc.testNum = ++i;
        }
        tcArr = null;
        return tcArr;
    }
    
    void Start()
    {
        TestClass[] tcArr = new TestClass[10];
        for(int i = 0; i < tcArr.Length; i++)
        {
            tcArr[i] = new TestClass();
        }
        TestClass[] newTcArr = (TestClass[])tcArr.Clone();
        print(object.ReferenceEquals(newTcArr, tcArr));
        int [] intArr = new int[10];
        int[] newIntArr = intArr;
        print(object.ReferenceEquals(intArr, newIntArr));
    }

}
