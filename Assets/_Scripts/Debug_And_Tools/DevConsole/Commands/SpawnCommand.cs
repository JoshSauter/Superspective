using System;
using System.Collections.Generic;
using GrowShrink;
using Saving;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeveloperConsole {
    public class SpawnCommand : ConsoleCommand {
        private const float SPAWN_DISTANCE = 3;
        
        public enum DynamicObjectType {
            Cube,
            MultiDimensionCube,
            CubeBlue,
            CubeRed,
            CubeGreen
        }

        private Dictionary<DynamicObjectType, DynamicObject> spawnableObjects;

        public SpawnCommand(string commandWord, Dictionary<DynamicObjectType, DynamicObject> spawnableObjects) : base(commandWord) {
            this.spawnableObjects = spawnableObjects;
        }
        
        public override CommandResponse Execute(string[] args) {
            // Parse the input and run the command here
            if (args.Length < 1) {
                return new FailureResponse("No DynamicObject index or name provided.");
            }

            string spawnObjArg = args[0];
            if (int.TryParse(spawnObjArg, out int dynamicObjIndex)) {
                if (!Enum.IsDefined(typeof(DynamicObjectType), dynamicObjIndex)) {
                    return new FailureResponse($"Argument {dynamicObjIndex} is not a valid {nameof(DynamicObjectType)} index.");
                }

                Transform camTransform = Player.instance.playerCam.transform;
                return SpawnObject((DynamicObjectType)dynamicObjIndex, camTransform.position + camTransform.forward * SPAWN_DISTANCE * Player.instance.scale);
            }
            else if (Enum.TryParse(spawnObjArg, true, out DynamicObjectType type)) {
                Transform camTransform = Player.instance.playerCam.transform;
                return SpawnObject(type, camTransform.position + camTransform.forward * SPAWN_DISTANCE * Player.instance.scale);
            }
            else {
                return new FailureResponse($"Argument {spawnObjArg} is not a valid DyanmicObjectType index or name.");
            }
        }

        CommandResponse SpawnObject(DynamicObjectType type, Vector3 location) {
            if (!spawnableObjects.ContainsKey(type)) {
                return new FailureResponse($"Could not find prefab for type {type}");
            }

            DynamicObject spawnedObj = Object.Instantiate(spawnableObjects[type], location, Quaternion.identity);
            if (spawnedObj.TryGetComponent(out GrowShrinkObject growShrink)) {
                growShrink.startingScale = Player.instance.scale;
            }

            if (spawnedObj.TryGetComponent(out GravityObject gravity)) {
                gravity.gravityDirection = -Player.instance.transform.up;
            }
            return new SuccessResponse($"Spawned {type.ToString()}");
        }
    }
}
