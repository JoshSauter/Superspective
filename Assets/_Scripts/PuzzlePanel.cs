using System;
using System.Collections;
using System.Collections.Generic;
using Interactables;
using PuzzlePanelHelpers;
using UnityEngine;
#if UNITY_EDITOR

#endif

[Serializable]
public class PuzzlePanelSerializedState {
    public Vector2Int puzzleSize;

    [TextArea]
    public string startingStateStr;

    [TextArea]
    public string currentStateStr;

    [TextArea]
    public string solutionStr;
}

public class PuzzlePanel : MonoBehaviour {
    const float buttonToSpacerRatio = 3f;
    const float borderSpacerSize = 0.5f;
    public PuzzlePanelSerializedState state;

    readonly Vector2 signSize = new Vector2(3.75f, 3.75f);
    bool _solved;
    bool[,] currentState;
    Vector2Int gridSize;
    Button[] puzzleButtons;
    bool[,] solution;
    bool[,] startingState;

    public bool solved {
        get => _solved;
        set {
            bool oldValue = _solved;
            _solved = value;
            if (oldValue != _solved && OnPuzzlePanelStateChanged != null) OnPuzzlePanelStateChanged(_solved);
        }
    }

    // Use this for initialization
    IEnumerator Start() {
        gridSize = state.puzzleSize;
        startingState = state.startingStateStr.Deserialize(gridSize);
        solution = state.solutionStr.Deserialize(gridSize);
        currentState = new bool[gridSize.y, gridSize.x];
        puzzleButtons = CreateButtonsForPanel();

        yield return null;
        SubscribeToButtonEvents();
        SetStartingState();
    }

    void SubscribeToButtonEvents() {
        for (int i = 0; i < puzzleButtons.Length; i++) {
            puzzleButtons[i].OnButtonPressFinish += UpdatePuzzleState;
            puzzleButtons[i].OnButtonUnpressFinish += UpdatePuzzleState;
        }
    }

    void UpdatePuzzleState(Button b) {
        int index = Array.IndexOf(puzzleButtons, b);
        int row = index / gridSize.y;
        int col = index % gridSize.y;

        currentState[row, col] = b.ButtonPressed;
        state.currentStateStr = currentState.Serialize();

        CheckIfSolved();
    }

    void CheckIfSolved() {
        solved = state.currentStateStr == state.solutionStr;

        // This just to suppress warning about unused solution variable
        if (solution[0, 0]) solution[0, 0] = true;
    }

    void SetStartingState() {
        for (int row = 0; row < gridSize.y; row++) {
            for (int col = 0; col < gridSize.x; col++) {
                if (startingState[row, col]) puzzleButtons[row * gridSize.x + col].PressButton();
            }
        }
    }

    Button[] CreateButtonsForPanel() {
        GameObject buttonParentGO = new GameObject("Buttons");
        Transform buttonParent = buttonParentGO.transform;
        Transform sign = transform.Find("Sign");
        buttonParent.SetParent(sign, false);

        // Floating point Vector2 representation of gridSize to be used in the math below
        Vector2 gridSizeF = gridSize;

        Vector2 buttonArea = signSize - new Vector2(borderSpacerSize, borderSpacerSize);
        Vector2 numSpacers = gridSize - Vector2.one;
        Vector2 sizeOfSpacers = buttonArea / (gridSizeF * buttonToSpacerRatio + numSpacers);
        Vector2 sizeOfButtons = sizeOfSpacers * buttonToSpacerRatio;

        Vector2 startPosition = new Vector2(-buttonArea.x, buttonArea.y) / 2f +
                                new Vector2(sizeOfButtons.x, -sizeOfButtons.y) / 2f;

        Vector3 spawnPosition = new Vector3(startPosition.x, startPosition.y, 0);
        List<Button> newButtons = new List<Button>();
        for (int row = 0; row < gridSize.y; row++) {
            for (int col = 0; col < gridSize.x; col++) {
                GameObject newButtonGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                newButtonGO.name = "Button" + (row * gridSize.y + col);
                newButtonGO.transform.SetParent(buttonParent, false);
                newButtonGO.transform.localPosition = spawnPosition;
                newButtonGO.transform.localScale = new Vector3(sizeOfButtons.x, sizeOfButtons.y, 0.25f);
                newButtonGO.AddComponent<SuperspectiveRenderer>()
                    .SetSharedMaterial(Resources.Load<Material>("Materials/Unlit/Unlit"));
                newButtons.Add(SetUpNewButton(newButtonGO));

                spawnPosition.x += sizeOfSpacers.x + sizeOfButtons.x;
            }

            spawnPosition.y -= sizeOfSpacers.y + sizeOfButtons.y;
            spawnPosition.x = startPosition.x;
        }

        return newButtons.ToArray();
    }

    Button SetUpNewButton(GameObject buttonGO) {
        Button b = buttonGO.AddComponent<Button>();
        b.buttonPressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        b.buttonUnpressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        b.timeToPressButton = 0.25f;
        b.timeToUnpressButton = 0.125f;
        b.pressDistance = 0;
        b.unpressAfterPress = false;
        b.timeBetweenPressEndDepressStart = 0;

        ButtonColorChange c = b.gameObject.AddComponent<ButtonColorChange>();
        c.pressColor = new Color(58f / 255f, 58f / 255f, 58f / 255f, 1);
        return b;
    }

#region events
    public delegate void PuzzlePanelStateChangeAction(bool solved);

    public event PuzzlePanelStateChangeAction OnPuzzlePanelStateChanged;
#endregion
}

namespace PuzzlePanelHelpers {
    internal static class PuzzlePanelHelpers {
        public static bool[,] Deserialize(this string s, Vector2Int expectedSize) {
            bool[,] matrix = new bool[expectedSize.y, expectedSize.x];

            string[] rows = s.Split('\n');
            if (rows.Length != expectedSize.y) {
                Debug.LogError(
                    "Error converting string to 2D Array:\nFound " + rows.Length + " rows, expected " + expectedSize.y
                );
                return null;
            }

            for (int x = 0; x < rows.Length; x++) {
                char[] columns = rows[x].ToCharArray();
                if (columns.Length != expectedSize.x) {
                    Debug.LogError(
                        "Error converting string to 2D Array:\nFound " + columns.Length + " columns, expected " +
                        expectedSize.x
                    );
                    return null;
                }

                for (int y = 0; y < columns.Length; y++) {
                    // value is equal to false if s[x,y] == '0', else true
                    bool value = !(columns[y] == '0');
                    matrix[x, y] = value;
                }
            }

            return matrix;
        }

        public static string Serialize(this bool[,] m) {
            string s = "";
            for (int i = 0; i < m.GetLength(0); i++) {
                for (int j = 0; j < m.GetLength(1); j++) {
                    s += m[i, j] ? "1" : "0";
                }

                if (i < m.GetLength(0) - 1)
                    s += "\n";
            }

            return s;
        }
    }
}