using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BooleanExpressionTests {

    [Test]
    public void InfixToPostfixTest() {
        List<string> infix = new List<string>() { "(","0","||","1",")","&&","(","2","&&","(","0","||","3",")",")" };
        List<string> postfix = DimensionObject.InfixToPostfix(infix);

        float[] solutions = DimensionObject.SolutionArrayFromPostfixExpression(postfix);
        
        Debug.Log(string.Join(", ", solutions));
    }
}
