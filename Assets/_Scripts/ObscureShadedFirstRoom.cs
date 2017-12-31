using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObscureShadedFirstRoom : MonoBehaviour {
    public bool DEBUG = false;

    public enum Triggers {
        Front,
        Right,
        Back,
        Left
    }
    public Triggers triggerBase = Triggers.Right;
	public bool triggersActive = false;
    const int NUM_TRIGGERS = 4;
    const int MAX_TRIGGERS_ACTIVE = NUM_TRIGGERS + 1;
    public int numTriggersActive = 0;

	public MagicTrigger centralPillarSpawnTrigger;

    List<MagicTrigger> forwardTriggers = new List<MagicTrigger>();
    List<MagicTrigger> backwardTriggers = new List<MagicTrigger>();

    public Collider hallwayBlockingCollider;
    public GameObject[] obscureWalls;
    public GameObject[] fullRed;

	// Use this for initialization
	void Awake () {
        MagicTrigger[] triggers = transform.GetComponentsInChildren<MagicTrigger>();
		for (int i = 0; i < triggers.Length; i++) {
            if (i % 2 == 0) {
                forwardTriggers.Add(triggers[i]);
                triggers[i].OnMagicTriggerStayOneTime += TriggerForward;
            }
            else {
                backwardTriggers.Add(triggers[i]);
                triggers[i].OnMagicTriggerStayOneTime += TriggerBackward;
            }
        }

		centralPillarSpawnTrigger.OnMagicTriggerStay += TurnTriggersOn;
	}

    void TriggerForward(Collider player) {
        // Not all triggers are activated yet
        if (triggersActive && numTriggersActive < MAX_TRIGGERS_ACTIVE) {
            if (numTriggersActive < NUM_TRIGGERS) {
                int obscureWallOnIndex = (numTriggersActive + (int)triggerBase - 1);
                obscureWallOnIndex = (obscureWallOnIndex < 0) ? (obscureWallOnIndex + NUM_TRIGGERS) % NUM_TRIGGERS : obscureWallOnIndex % NUM_TRIGGERS;

                obscureWalls[obscureWallOnIndex].SetActive(true);
            }
            
            // If this is not the first trigger to activate, we need to switch from obscure wall to full red wall for a certain direction
            if (numTriggersActive > 0) {
                int fullRedIndex = (numTriggersActive + (int)triggerBase - 2);
                fullRedIndex = (fullRedIndex < 0) ? (fullRedIndex + NUM_TRIGGERS) % NUM_TRIGGERS : fullRedIndex % NUM_TRIGGERS;
                obscureWalls[fullRedIndex].SetActive(false);
                fullRed[fullRedIndex].SetActive(true);

                // Special case for handling the front wall hallway collider
                if ((Triggers)fullRedIndex == Triggers.Front) {
                    hallwayBlockingCollider.enabled = false;
                }
            }

            // Increment the number of triggers activated
            numTriggersActive++;
        }
        // All triggers are activated, just change the base
        else {
            triggerBase = (Triggers)(((int)triggerBase + 1) % NUM_TRIGGERS);
        }
    }
    void TriggerBackward(Collider player) {
		// Not all triggers are deactivated yet
		if (triggersActive && numTriggersActive > 0) {
			if (numTriggersActive > 1) {
				int fullRedIndex = (numTriggersActive + (int)triggerBase + 1) % NUM_TRIGGERS;
				obscureWalls[fullRedIndex].SetActive(true);
				fullRed[fullRedIndex].SetActive(false);

				// Special case for handling the front wall hallway collider
				if ((Triggers)fullRedIndex == Triggers.Front) {
					hallwayBlockingCollider.enabled = true;
				}
			}
			if (numTriggersActive < MAX_TRIGGERS_ACTIVE) {
				int obscureWallIndex = (numTriggersActive + (int)triggerBase + 2) % NUM_TRIGGERS;
				obscureWalls[obscureWallIndex].SetActive(false);
			}

			numTriggersActive--;
		}
		else {
			if (triggerBase == Triggers.Front) triggerBase = Triggers.Left;
			else triggerBase = (Triggers)(((int)triggerBase - 1) % NUM_TRIGGERS);
		}
    }

	void TurnTriggersOn(Collider c) {
		triggersActive = true;
	}
}
