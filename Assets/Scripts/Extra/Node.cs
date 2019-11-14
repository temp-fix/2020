using UnityEngine;
using System.Collections;

public class Node:IHeapItem<Node>{
	public bool walkable;
	public Vector3 worldPosition;
	public double gCost, hCost;
	public int gridX,gridY;
	public Node parent;
	public double distanceToCollisionObject=0;
	int heapIndex;
	public Node(bool _walkable, Vector3 _worldPos,int _gridX, int _gridY){
		walkable = _walkable;
		worldPosition = _worldPos;
		gridX = _gridX;
		gridY = _gridY;
	}

	public double fCost{
		get{
			return gCost+hCost;
		}
	}

	public int HeapIndex{
		get{
			return heapIndex;
		}
		set{
			heapIndex=value;
		}
	}

	public int CompareTo(Node other){
		int compare = fCost.CompareTo (other.fCost);
		if (compare == 0) {
			compare=hCost.CompareTo(other.hCost);	
		}
		return -compare;
	}
}
