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
    const int NUM_TRIGGERS = 4;
    public int triggersActive = 0;

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
                triggers[i].OnMagicTriggerEnter += TriggerForward;
            }
            else {
                backwardTriggers.Add(triggers[i]);
                triggers[i].OnMagicTriggerEnter += TriggerBackward;
            }
        }
	}

    void TriggerForward(Collider player) {
        // If this is not the first trigger to activate, we need to switch from obscure wall to full red wall for a certain direction
        if (triggersActive > 0) {
            int triggerIndex = (triggersActive + (int)triggerBase - 1) % NUM_TRIGGERS;
            if (DEBUG) {
                print("Turning on " + (Triggers)(triggerIndex) + " full red" + "\n" +
                    "Turning off " + (Triggers)(triggerIndex) + " obscure wall because it's now covered by full red");
            }
            fullRed[triggerIndex].SetActive(true);
            obscureWalls[triggerIndex].SetActive(false);

            // Allow the way out of the first room for the player
            if ((Triggers)triggerIndex == Triggers.Front) {
                if (DEBUG) {
                    print("Turning off the front wall collider (allowing player to pass into the hallway)");
                    hallwayBlockingCollider.enabled = false;
                }
            }
        }

        // Increment the trigger base if we have already activated all the triggers
        if (triggersActive == NUM_TRIGGERS) {
            triggerBase = (Triggers)(((int)triggerBase + 1) % NUM_TRIGGERS);
        }
        // Else turn on the appropriate things
        else {
            int triggerIndex = (triggersActive + (int)triggerBase) % NUM_TRIGGERS;
            if (DEBUG) {
                print("Turning on " + (Triggers)(triggerIndex) + " obscure wall");
            }
            obscureWalls[triggerIndex].SetActive(true);
            triggersActive++;
        }


    }
    void TriggerBackward(Collider player) {
        if (triggersActive > 0) {
            int triggerIndex = (triggersActive + (int)triggerBase - 2);
            if (triggerIndex < 0) triggerIndex += NUM_TRIGGERS;
            if (DEBUG) {
                print("Turning off " + (Triggers)(triggerIndex) + " full red" + "\n" +
                    "Turning on " + (Triggers)(triggerIndex) + " obscure wall because the full red isn't covering it anymore");
            }
            fullRed[triggerIndex].SetActive(false);
            obscureWalls[triggerIndex].SetActive(true);
            
            if ((Triggers)triggerIndex == Triggers.Front) {
                if (DEBUG) {
                    print("Turning off the front wall collider (disallowing player to pass into the hallway)");
                }
                hallwayBlockingCollider.enabled = false;
            }
        }

        if (triggersActive == 0) {
            if (triggerBase == Triggers.Front) triggerBase = Triggers.Left;
            else triggerBase = (Triggers)(((int)triggerBase - 1) % NUM_TRIGGERS);
        }
        else {
            int triggerIndex = (triggersActive + (int)triggerBase - 1) % NUM_TRIGGERS;
            if (DEBUG) {
                print("Turning off " + (Triggers)(triggerIndex) + " obscure wall");
            }
            obscureWalls[triggerIndex].SetActive(false);

            triggersActive--;
        }


    }
}
