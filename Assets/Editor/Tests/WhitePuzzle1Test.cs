using System;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WhitePuzzle1Test {
    public static int targetRangeMin;
    public static int targetRangeMax;
    public static int possibilitySpace;
    
    struct TargetSolutions {
        public int target;
        public List<string> solutions;
        private float avgAnswerLength => solutions.Sum(s => s.Split(",").Length) / (float)solutions.Count;
        // An answer is defined as complex if it's solutions uses about half the possibility space
        private float avgComplexity => (possibilitySpace/2f) - Mathf.Abs(avgAnswerLength - (possibilitySpace / 2f));
        private float closenessToZero => 1 - (float)Mathf.Abs(target) / Mathf.Max(Mathf.Abs(targetRangeMax), Mathf.Abs(targetRangeMin));
        // Sort by avgComplexity first, then closeness to zero (adding up the only 3 negative numbers is not very difficult)
        public float difficulty => 10*avgComplexity + closenessToZero - solutions.Count;

        public TargetSolutions(int target, List<string> solutions) {
            this.target = target;
            this.solutions = solutions;
        }
    }
    
	[Test]
	public void NumSolutionsTest() {
		//List<int> values = new List<int> { 9, 37, 15, 4, 31, -18, -11, -7, -20, -43 };
        List<int> values = new List<int>() { -3, 4, 7, 11, 13, 17 };
        //List<int> values = new List<int>() { -30, -18, -3, 3, 7, 11, 31 };
        //List<int> values = new List<int>() { 1,2,3,4,5,6,7,8, -3,4,5,8,9, -30,-18,7,-3,3,11,31 };
        
        Debug.Log($"Values: {string.Join(",", values)}");

        //int targetRangeMin = 47;
        possibilitySpace = values.Count;
        targetRangeMin = values.Where(v => v < 0).Sum();
        targetRangeMax = values.Where(v => v > 0).Sum();
        List<TargetSolutions> allTargetSolutions = new List<TargetSolutions>();
        for (int target = targetRangeMin; target <= targetRangeMax; target++) {
            List<string> solutions = new List<string>();
            SumUpRecursive(values, target, new List<int>(), ref solutions);

            bool IsTrivialSolution(string s) {
                return !s.Contains(",");
            }
            
            if (solutions.TrueForAll(IsTrivialSolution)) continue; // Omit targets that only have trivial solutions

            if (solutions.Count > 0) {
                allTargetSolutions.Add(new TargetSolutions(target, solutions));
                //Debug.Log($"Target: {target}\nNumber of solutions: {solutions.Count}\nSolutions:\n{string.Join("\n", solutions)}");
            }
        }

        foreach (var ts in allTargetSolutions.OrderByDescending(ts => ts.difficulty)) {
            Debug.Log($"Target: {ts.target}\nNumber of solutions: {ts.solutions.Count}\nDifficulty rating: {ts.difficulty:F3}\nSolutions:\n{string.Join("\n", ts.solutions)}");
        }
        //Assert.That(solutions.Count == 1);
    }
    
    static void SumUpRecursive(List<int> numbers, int target, List<int> partial, ref List<string> solutions) {
        int sum = partial.Sum();

        if (sum == target) {
            if (partial.Count <= 0) return; // Omit trivial solutions
            // Debug.Log("sum(" + string.Join(", ", partial) + ")=" + target);
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
