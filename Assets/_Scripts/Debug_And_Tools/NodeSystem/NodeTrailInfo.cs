using System.Collections.Generic;

/// <summary>
/// Helper class to store distance (from root node) information about each node in a NodeSystem
/// </summary>
public class NodeTrailInfo {
    public Node parent;
    public Node thisNode;
    public float startDistance;
    public float endDistance;
}

public static class NodeSystemExt {
	/// <summary>
	/// Generates the NodeTrailInfo for each node in the NodeSystem, both for the full path and the simple path
	/// (defined as a straight line segment in the place of staircase segments)
	/// </summary>
	/// <param name="ns">NodeSystem to generate the NodeTrailInfo for</param>
	/// <returns>(Full path NodeTrailInfo, Simple path NodeTrailInfo)</returns>
	public static (List<NodeTrailInfo>, List<NodeTrailInfo>) GenerateTrailInfoAndSimplePath(this NodeSystem ns) {
		float maxDistance = 0;
		List<NodeTrailInfo> trailInfo = new List<NodeTrailInfo>();
		List<NodeTrailInfo> simplePath = new List<NodeTrailInfo>();
		
		void PopulateTrailInfoAndSimplePathRecursively(Node curNode, float curDistance) {
			if (!curNode.IsRootNode) {
				Node parentNode = curNode.parent;
				float endDistance = parentNode.zeroDistanceToChildren ? curDistance : curDistance + (curNode.pos - parentNode.pos).magnitude;
				// If there is a parent node, add trail info here
				NodeTrailInfo info = new NodeTrailInfo {
					parent = parentNode,
					thisNode = curNode,
					startDistance = curDistance,
					endDistance = endDistance
				};
				trailInfo.Add(info);

				// Create the simple path as we traverse the nodes
				if (!parentNode.zeroDistanceToChildren) {
					if (parentNode.staircaseSegment) {
						if (curNode.IsLeafNode) {
							// End of staircase segment, find start and add segment
							Node end = curNode;
							Node start = parentNode;
							float startDistance = curDistance;
							while (start.parent?.staircaseSegment ?? false) {
								startDistance -= (start.pos - start.parent.pos).magnitude;
								start = start.parent;
							}

							// Add just the staircase segment
							NodeTrailInfo staircaseSegment = new NodeTrailInfo {
								parent = start,
								thisNode = end,
								startDistance = startDistance,
								endDistance = curDistance
							};
							simplePath.Add(staircaseSegment);
						}
						else if (!curNode.staircaseSegment) {
							// End of staircase segment, find start and add segment
							Node end = parentNode;
							Node start = parentNode;
							float startDistance = curDistance;
							while (start.parent?.staircaseSegment ?? false) {
								startDistance -= (start.pos - start.parent.pos).magnitude;
								start = start.parent;
							}

							// Add the staircase segment first
							NodeTrailInfo staircaseSegment = new NodeTrailInfo {
								parent = start,
								thisNode = end,
								startDistance = startDistance,
								endDistance = curDistance
							};
							simplePath.Add(staircaseSegment);

							// Add this segment as normal as well
							NodeTrailInfo segment = new NodeTrailInfo {
								parent = parentNode,
								thisNode = curNode,
								startDistance = curDistance,
								endDistance = endDistance
							};
							simplePath.Add(segment);
						}
					}
					// parent is not a staircase segment
					else {
						// Add line segment as normal
						NodeTrailInfo segment = new NodeTrailInfo {
							parent = parentNode,
							thisNode = curNode,
							startDistance = curDistance,
							endDistance = endDistance
						};
						simplePath.Add(segment);
					}
				}

				// Update maxDistance as you add new trail infos
				if (info.endDistance > maxDistance) {
					maxDistance = info.endDistance;
				}

				// Recurse for each child
				if (!curNode.IsLeafNode) {
					foreach (Node child in curNode.children) {
						PopulateTrailInfoAndSimplePathRecursively(child, info.endDistance);
					}
				}
			}
			// Base case of root parent node
			else {
				foreach (Node child in curNode.children) {
					PopulateTrailInfoAndSimplePathRecursively(child, curDistance);
				}
			}
		}
		
		PopulateTrailInfoAndSimplePathRecursively(ns.rootNode, 0);
		return (trailInfo, simplePath);
	}
	
	/// <summary>
	/// Generates the NodeTrailInfo for each node in the NodeSystem for the full path
	/// </summary>
	/// <param name="ns">NodeSystem to generate the NodeTrailInfo for</param>
	/// <returns>Full path NodeTrailInfo</returns>
	public static List<NodeTrailInfo> GenerateTrailInfo(this NodeSystem ns) {
		return GenerateTrailInfoAndSimplePath(ns).Item1;
	}
}