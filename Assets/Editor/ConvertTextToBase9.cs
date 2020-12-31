using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class ConvertTextToBase9 : ScriptableWizard {
	[MenuItem("My Tools/Convert Text to Base 9")]
	static void ConvertTextToBase9Wizard() {
		DisplayWizard<ConvertTextToBase9>("Convert Text to Base 9", "Convert");
	}

	static Dictionary<int, char> valueToBase9Char = new Dictionary<int, char>() {
		{ 0, '0' },
		{ 1, '1' },
		{ 2, '2' },
		{ 3, '3' },
		{ 4, '4' },
		{ 5, '5' },
		{ 6, '6' },
		{ 7, '7' },
		{ 8, '8' },
		{ 9, '9' },
		{ 10, '!' },
		{ 11, '@' },
		{ 12, '#' },
		{ 13, '$' },
		{ 14, '%' },
		{ 15, '^' },
		{ 16, '&' },
		{ 17, '*' },
		{ 18, '(' },
		{ 19, ')' },
		{ 20, 'a' },
		{ 21, 'b' },
		{ 22, 'c' },
		{ 23, 'd' },
		{ 24, 'e' },
		{ 25, 'f' },
		{ 26, 'g' },
		{ 27, 'h' },
		{ 28, 'i' },
		{ 29, 'j' },
		{ 30, 'k' },
		{ 31, 'l' },
		{ 32, 'm' },
		{ 33, 'n' },
		{ 34, 'o' },
		{ 35, 'p' },
		{ 36, 'q' },
		{ 37, 'r' },
		{ 38, 's' },
		{ 39, 't' },
		{ 40, 'u' },
		{ 41, 'v' },
		{ 42, 'w' },
		{ 43, 'x' },
		{ 44, 'y' },
		{ 45, 'z' },
		{ 46, 'A' },
		{ 47, 'B' },
		{ 48, 'C' },
		{ 49, 'D' },
		{ 50, 'E' },
		{ 51, 'F' },
		{ 52, 'G' },
		{ 53, 'H' },
		{ 54, 'I' },
		{ 55, 'J' },
		{ 56, 'K' },
		{ 57, 'L' },
		{ 58, 'M' },
		{ 59, 'N' },
		{ 60, 'O' },
		{ 61, 'P' },
		{ 62, 'Q' },
		{ 63, 'R' },
		{ 64, 'S' },
		{ 65, 'T' },
		{ 66, 'U' },
		{ 67, 'V' },
		{ 68, 'W' },
		{ 69, 'X' },
		{ 70, 'Y' },
		{ 71, 'Z' },
		{ 72, '[' },
		{ 73, '\\' },
		{ 74, ']' },
		{ 75, '{' },
		{ 76, '|' },
		{ 77, '}' },
		{ 78, ',' },
		{ 79, '.' },
		{ 80, '/' }
	};

	private void OnWizardCreate() {
		foreach (GameObject go in Selection.gameObjects) {
			TextMeshPro tmp = go.GetComponent<TextMeshPro>();
			if (tmp == null) continue;

			try {
				string text = tmp.text;
				string[] splitText = text.Split(' ');

				for (int i = 0; i < splitText.Length; i++) {
					string piece = splitText[i];
					if (int.TryParse(piece, out int value)) {
						splitText[i] = valueToBase9Char[value].ToString();
					}
				}

				tmp.text = string.Join(" ", splitText);
			}
			catch {
				Debug.Log($"Could not parse text {tmp.text}, skipping.");
				continue;
			}
		}
	}
}