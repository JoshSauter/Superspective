using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Audio;
using DissolveObjects;
using Interactables;
using NaughtyAttributes;
using PortalMechanics;
using PowerTrailMechanics;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

// TODO: Loading while MazeFailed back to a save that's outside the maze doesn't seem to reset the nodes properly, they're all on. EDIT: Recheck this, it might be working now
public class MiniatureMaze : SingletonSuperspectiveObject<MiniatureMaze, MiniatureMaze.MiniatureMazeSave> {
    public override string ID => "GrowShrinkIntro_MiniatureMaze";

    private const float NOISE_OVERLAY_FADE_IN_TIME = 4f;
    private const float NOISE_OVERLAY_FADE_OUT_DELAY = 1f;
    private const float NOISE_OVERLAY_FADE_OUT_TIME = 2.5f;

    public Portal portalToBetweenWorlds;
    public DissolveObject mazeExitDoorwayBlocker;
    public Transform powerTrailsRoot;
    public Transform mazeNodesRoot;
    public Button solutionCheckButton;
    public SpriteRenderer[] solutionRenderers;

    private FlashColors flashColors;

    private List<string> solutionStrings = new List<string>();
    private List<HashSet<MiniatureMazeNode>> solutions;
    
    [ReadOnly]
    public int currentSolutionIndex = -1;
    private HashSet<MiniatureMazeNode> Solution => currentSolutionIndex >= 0 ? solutions[currentSolutionIndex] : null;
    
    private Dictionary<string, MiniatureMazeNode> mazeNodes;
    private Dictionary<string, Edge> mazeEdges;

    private Sprite[] solutionImages;
    public Sprite SolutionImage => currentSolutionIndex > 0 ? solutionImages[currentSolutionIndex] : null;
    
    class Edge {
        public PowerTrail powerTrailEdge;
        public MiniatureMazeNode node1;
        public MiniatureMazeNode node2;
        
        public bool Powered => node1.pwr.PowerIsOn && node2.pwr.PowerIsOn;
        public MiniatureMazeNode nodeLastPowered;
    }
    
    public enum State : byte {
        ShowingSolution,
        PlayerInMaze,
        MazeSolved,
        MazeFailed,
        ResettingMaze
    }
    public StateMachine<State> state;

    protected override void Awake() {
        base.Awake();

        flashColors = this.GetOrAddComponent<FlashColors>();
        flashColors.renderers.AddRange(solutionRenderers);
        flashColors.renderers.Add(solutionCheckButton.GetComponent<Renderer>());
        state = this.StateMachine(State.ShowingSolution);
        solutionImages = Resources.LoadAll<Sprite>("Images/MiniMazePuzzleSolutions/").OrderBy(s => int.Parse(s.name)).ToArray();
    }

    protected override void Start() {
        base.Start();

        mazeNodes = mazeNodesRoot
            .GetComponentsInChildren<MiniatureMazeNode>()
            .ToDictionary(n => n.transform.parent.name);
        mazeEdges = new Dictionary<string, Edge>();
        List<PowerTrail> powerTrails = powerTrailsRoot.GetComponentsInChildren<PowerTrail>().ToList();
        foreach (var powerTrail in powerTrails) {
            string[] nodeNames = powerTrail.name.Split('-');
            MiniatureMazeNode node1 = mazeNodes[nodeNames[0]];
            MiniatureMazeNode node2 = mazeNodes[nodeNames[1]];
            
            flashColors.renderers.AddRange(powerTrail.renderers);

            Edge edge = new Edge {
                powerTrailEdge = powerTrail,
                node1 = node1,
                node2 = node2
            };
            node1.pwr.OnPowerFinish += () => OnNodePowered(edge, node1);
            node2.pwr.OnPowerFinish += () => OnNodePowered(edge, node2);
            node1.pwr.OnDepowerBegin += () => OnNodeDepowered(edge, node1);
            node2.pwr.OnDepowerBegin += () => OnNodeDepowered(edge, node2);
            mazeEdges.Add(powerTrail.name, edge);
        }

        GenerateAllSolutions();
        solutions = solutionStrings
            .Select(s => s.Split(',')
                .Select(n => mazeNodes[n])
                .ToHashSet())
            .ToList();
        
        solutionCheckButton.pwr.OnPowerFinish += () => {
            if (state == State.PlayerInMaze) {
                CheckSolutionResult result = CheckSolution();
                if (result == CheckSolutionResult.Correct) {
                    state.Set(State.MazeSolved);
                }
                else if (result == CheckSolutionResult.Incomplete) {
                    solutionCheckButton.PressButton();
                    AudioManager.instance.Play(AudioName.IncorrectAnswer);
                    flashColors.Flash(3, 0.8f);
                }
            }
        };

        InitializeStateMachine();
    }

    private void InitializeStateMachine() {
        // Reset the puzzle nodes when the player enters the maze
        state.AddTrigger(State.PlayerInMaze, () => {
            if (state.PrevState == State.ShowingSolution) {
                ResetPuzzleNodes();
                solutionCheckButton.interactableObject.SetAsInteractable();
            }
        });
        
        // If the player leaves the maze, select and show a new solution
        state.AddTrigger(State.ShowingSolution, () => {
            if (state.PrevState is State.PlayerInMaze or State.MazeFailed) {
                SelectNewSolution();
            }
        });
        
        // Turn off all the puzzle nodes when the player fails the maze
        state.AddTrigger(State.MazeFailed, () => {
            solutionCheckButton.interactableObject.SetAsDisabled("Wrong answer");
            AudioManager.instance.Play(AudioName.IncorrectAnswer);
            ResetPuzzleNodes();
            mazeEdges.Values.ToList().ForEach(e => e.powerTrailEdge.PauseRendering = true);
            flashColors.Flash(-1, 0.8f);
        });

        // Dissolve the exit doorway blocker when the player solves the maze
        state.AddTrigger(State.MazeSolved, () => {
            portalToBetweenWorlds.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
            AudioManager.instance.Play(AudioName.CorrectAnswer);
            mazeExitDoorwayBlocker.Dematerialize();
            solutionCheckButton.interactableObject.SetAsHidden();
        });
        
        // Materialize the exit doorway blocker when the player enters the maze
        state.AddTrigger((e) => e != State.MazeSolved, () => mazeExitDoorwayBlocker.Materialize());
        
        // Fade in the noise scramble overlay when the player fails the maze, and reset them to outside the maze if they don't leave
        state.WithUpdate(State.MazeFailed, time => {
            if (state == State.MazeFailed) {
                float t = time / NOISE_OVERLAY_FADE_IN_TIME;
            
                // Fade in the noise scramble overlay
                NoiseScrambleOverlay.instance.SetNoiseScrambleOverlayValue(t);

                if (t >= 1) {
                    state.Set(State.ResettingMaze);
                }
            }
        });
        
        // Reset player position and maze state when the player leaves the maze
        state.AddTrigger(State.ResettingMaze, () => {
            // Transport the player outside of the maze
            Player.instance.transform.position = new Vector3(888.5f, 58.34f, -51f);
            Player.instance.transform.rotation = Quaternion.Euler(0, -90, 0f);
            Player.instance.growShrink.SetScaleDirectly(.125f);
            Player.instance.cameraFollow.RecalculateWorldPositionLastFrame();
            
            PlayerMovement.instance.thisRigidbody.isKinematic = true;
            PlayerMovement.instance.StopMovement();
            PlayerLook.instance.RotationY = 0f;
            PlayerLook.instance.Frozen = true;
            PlayerMovement.instance.movementEnabledState = PlayerMovement.MovementEnabledState.Disabled;
            
            SelectNewSolution();
            flashColors.CancelFlash();
            solutionCheckButton.TurnButtonOff();
            mazeEdges.Values.ToList().ForEach(e => e.powerTrailEdge.PauseRendering = false);
        });
        state.AddTrigger(State.ShowingSolution, () => {
            PlayerLook.instance.Frozen = false;
            PlayerMovement.instance.thisRigidbody.isKinematic = false;
            PlayerMovement.instance.movementEnabledState = PlayerMovement.MovementEnabledState.Enabled;
        });
        state.AddStateTransition(State.ResettingMaze, State.ShowingSolution, NOISE_OVERLAY_FADE_OUT_DELAY + NOISE_OVERLAY_FADE_OUT_TIME);
        state.WithUpdate(State.ResettingMaze, time => {
            if (time < NOISE_OVERLAY_FADE_OUT_DELAY) {
                NoiseScrambleOverlay.instance.SetNoiseScrambleOverlayValue(1);
            }
            else {
                float t = (time - NOISE_OVERLAY_FADE_OUT_DELAY) / NOISE_OVERLAY_FADE_OUT_TIME;
                NoiseScrambleOverlay.instance.SetNoiseScrambleOverlayValue(1 - t);
            }
        });
    }

    private enum CheckSolutionResult : byte {
        Correct,
        Incomplete,
        Invalid
    }

    private CheckSolutionResult CheckSolution() {
        if (currentSolutionIndex < 0) return CheckSolutionResult.Incomplete;
        
        // If the player has turned on nodes that are not in the solution, the solution is invalid
        bool allNodesOnAreInSolution = mazeNodes.Values.Where(n => n.state == MiniatureMazeNode.State.On).All(Solution.Contains);
        if (!allNodesOnAreInSolution) return CheckSolutionResult.Invalid;
        
        // If the player has not turned on all the nodes in the solution, the solution is incomplete
        bool allNodesInSolutionAreOn = Solution.All(n => n.state.State == MiniatureMazeNode.State.On);
        if (!allNodesInSolutionAreOn) return CheckSolutionResult.Incomplete;
        
        return CheckSolutionResult.Correct;
    }

    private void OnNodePowered(Edge edge, MiniatureMazeNode node) {
        if (edge.Powered && !edge.powerTrailEdge.pwr.PowerIsOn) {
            edge.powerTrailEdge.reverseDirection = edge.nodeLastPowered == edge.node2;
            edge.powerTrailEdge.pwr.PowerIsOn = true;
        }
        else {
            edge.nodeLastPowered = node;
        }

        if (state == State.PlayerInMaze) {
            CheckSolutionResult result = CheckSolution();
            if (result == CheckSolutionResult.Invalid) {
                state.Set(State.MazeFailed);
            }

            // The TimeSinceLastLoad condition stops the sound of all of them turning on at once when a player loads a save in the maze
            if (result != CheckSolutionResult.Invalid && SaveManager.TimeSinceLastLoad > 5f) {
                AudioManager.instance.PlayAtLocation(AudioName.LowPulse, node.pwr.ID, node.transform.position, false, (job) => job.baseVolume = 0.4f);
            }
        }
    }

    private void OnNodeDepowered(Edge edge, MiniatureMazeNode node) {
        edge.powerTrailEdge.pwr.PowerIsOn = false;
        edge.nodeLastPowered = null;
    }

    private void SelectNewSolution() {
        SelectSolution(solutions.DifferentRandomIndexFrom());
    }

    private void SelectSolution(int solutionIndex) {
        ResetPuzzleNodes();
        currentSolutionIndex = solutionIndex;
        solutionRenderers.ToList().ForEach(r => r.sprite = solutionImages[currentSolutionIndex]);
        Solution.ToList().ForEach(n => n.state.Set(MiniatureMazeNode.State.On));
        flashColors.CancelFlash();
        mazeEdges.Values.ToList().ForEach(e => e.powerTrailEdge.PauseRendering = false);
    }
    
    public void PlayerEnteredMaze() {
        if (state != State.MazeSolved) {
            state.Set(State.PlayerInMaze);
        }
    }
    
    public void PlayerExitedMaze() {
        if (state != State.MazeSolved) {
            state.Set(State.ShowingSolution);
        }
    }

    private void ResetPuzzleNodes() {
        mazeNodes.Values.ToList().ForEach(n => n.state.Set(MiniatureMazeNode.State.Off));
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;

        if (DebugInput.GetKeyDown("r")) {
            currentSolutionIndex = -1;
        }
        if (currentSolutionIndex < 0) {
            SelectNewSolution();
        }

        if (this.InstaSolvePuzzle()) {
            state.Set(State.MazeSolved);
        }
    }

    private void GenerateAllSolutions() {
        Dictionary<string, HashSet<string>> symmetry = new Dictionary<string, HashSet<string>>() {
            {"00", new HashSet<string>(){"03", "40", "43"}},
            {"01", new HashSet<string>(){"02", "41", "42"}},
            {"10", new HashSet<string>(){"30"}},
            {"20", new HashSet<string>(){"23"}},
            {"21", new HashSet<string>(){"22"}}
        };

        // Only the nodes on one half of the maze are needed to define a solution, since we can mirror the solution to the other half
        List<string> solutionGenerationNodes = new List<string>() {
            "00", "01", "10", "20", "21"
        };

        HashSet<string> atLeastOneOfEntrance = new HashSet<string>() {
            "01", "10", "02"
        };
        HashSet<string> atLeastOneOfExit = new HashSet<string>() {
            "30", "41", "42"
        };

        Dictionary<string, HashSet<string>> neighborGraph = new Dictionary<string, HashSet<string>>() {
            { "00", new HashSet<string>() { "01", "20", "21" } },
            { "01", new HashSet<string>() { "00", "02", "10" } },
            { "02", new HashSet<string>() { "01", "03", "10" } },
            { "03", new HashSet<string>() { "02", "22", "23" } },
            { "10", new HashSet<string>() { "01", "02", "21", "22" } },
            { "20", new HashSet<string>() { "00", "21", "40" } },
            { "21", new HashSet<string>() { "00", "10", "20", "22", "30", "40" } },
            { "22", new HashSet<string>() { "03", "10", "21", "23", "30", "43" } },
            { "23", new HashSet<string>() { "03", "22", "43" } },
            { "30", new HashSet<string>() { "21", "22", "41", "42" } },
            { "40", new HashSet<string>() { "20", "21", "41" } },
            { "41", new HashSet<string>() { "30", "40", "42" } },
            { "42", new HashSet<string>() { "30", "41", "43" } },
            { "43", new HashSet<string>() { "22", "23", "42" } }
        };
        
        solutionStrings = new List<string>();

        bool ContainsPathFromEntranceToExit(HashSet<string> clonedSolutionSoFar) {
            HashSet<string> entranceNodesInSolution = atLeastOneOfEntrance.Where(clonedSolutionSoFar.Contains).ToHashSet();
            HashSet<string> exitNodesInSolution = atLeastOneOfExit.Where(clonedSolutionSoFar.Contains).ToHashSet();
            
            if (entranceNodesInSolution.Count == 0 || exitNodesInSolution.Count == 0) return false;
            
            bool PathExistsFromPointAToPointB(string nodeA, string nodeB) {
                HashSet<string> visited = new HashSet<string>();
                Queue<string> queue = new Queue<string>();
                queue.Enqueue(nodeA);
                while (queue.Count > 0) {
                    string node = queue.Dequeue();
                    if (node == nodeB) return true;
                    visited.Add(node);
                    foreach (string neighbor in neighborGraph[node]) {
                        if (!visited.Contains(neighbor) && clonedSolutionSoFar.Contains(neighbor)) {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                return false;
            }
            
            return entranceNodesInSolution.Any(entrance => exitNodesInSolution.Any(exit => PathExistsFromPointAToPointB(entrance, exit)));
        }

        void GenerateSolution(HashSet<string> solutionSoFar, int solutionGenerationNodeIndex, bool include) {
            string node = solutionGenerationNodes[solutionGenerationNodeIndex];
        
            // Clone the solutionSoFar HashSet before making further modifications
            HashSet<string> clonedSolutionSoFar = new HashSet<string>(solutionSoFar);

            if (include) {
                clonedSolutionSoFar.Add(node);
                if (symmetry.ContainsKey(node)) {
                    clonedSolutionSoFar.UnionWith(symmetry[node]);
                }
            }

            if (solutionGenerationNodeIndex == solutionGenerationNodes.Count - 1) {
                // If the solutions has too many nodes, or not enough, exclude it
                if (clonedSolutionSoFar.Count > mazeNodes.Count - 3 || clonedSolutionSoFar.Count <= 4) return;
                // If the solution doesn't contain at least one of the required nodes, exclude it
                if (!(clonedSolutionSoFar.Any(atLeastOneOfEntrance.Contains) && clonedSolutionSoFar.Any(atLeastOneOfExit.Contains))) return;
                // If the solution contains a node that is not connected to the rest of the solution, exclude it
                if (clonedSolutionSoFar.Any(n => !neighborGraph[n].Any(clonedSolutionSoFar.Contains))) return;
                // If we cannot make a path from one of the entrance nodes to one of the the exit nodes, exclude it
                if (!ContainsPathFromEntranceToExit(clonedSolutionSoFar)) return;
                
                solutionStrings.Add(string.Join(",", clonedSolutionSoFar));
            }
            else {
                GenerateSolution(clonedSolutionSoFar, solutionGenerationNodeIndex + 1, true);
                GenerateSolution(clonedSolutionSoFar, solutionGenerationNodeIndex + 1, false);
            }
        }
    
        GenerateSolution(new HashSet<string>(), 0, true);
        GenerateSolution(new HashSet<string>(), 0, false);
    }

#region Saving

    public override void LoadSave(MiniatureMazeSave save) {
        SelectSolution(save.currentSolutionIndex);
        state.LoadFromSave(save.stateSave);
    }

    [Serializable]
	public class MiniatureMazeSave : SaveObject<MiniatureMaze> {
        public StateMachine<State>.StateMachineSave stateSave;
        public int currentSolutionIndex;
        
		public MiniatureMazeSave(MiniatureMaze script) : base(script) {
            this.stateSave = script.state.ToSave();
            this.currentSolutionIndex = script.currentSolutionIndex;
        }
	}
#endregion
}
