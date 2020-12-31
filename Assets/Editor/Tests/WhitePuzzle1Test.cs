using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WhitePuzzle1Test {
	[Test]
	public void NumSolutionsTest() {
		List<int> values = new List<int> { 9, 37, 15, 4, 31, -18, -11, -7, -20, -43 };
		int target = 47;

        List<string> solutions = new List<string>();
        SumUpRecursive(values, target, new List<int>(), ref solutions);

        Debug.Log($"Values: {string.Join(",", values)}\nNumber of solutions: {solutions.Count}:\n{string.Join("\n", solutions)}");
        //Assert.That(solutions.Count == 1);
	}

    static void SumUpRecursive(List<int> numbers, int target, List<int> partial, ref List<string> solutions) {
        int sum = partial.Sum();

        if (sum == target) {
            Debug.Log("sum(" + string.Join(", ", partial) + ")=" + target);
            solutions.Add(string.Join(", ", partial));
        }

        for (int i = 0; i < numbers.Count; i++) {
            List<int> remaining = new List<int>();
            int n = numbers[i];
            for (int j = i + 1; j < numbers.Count; j++) remaining.Add(numbers[j]);
            List<int> partial_rec = new List<int>(partial);
            partial_rec.Add(n);
            SumUpRecursive(remaining, target, partial_rec, ref solutions);
        }
    }
}
